using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.FileServices;
using CX.Engine.Assistants;
using CX.Engine.Assistants.Channels;
using CX.Engine.Assistants.Walter1;
using CX.Engine.ChatAgents.OpenAI;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Apis.Sheets.v4;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using BottomBorder = DocumentFormat.OpenXml.Wordprocessing.BottomBorder;
using Break = DocumentFormat.OpenXml.Wordprocessing.Break;
using Color = DocumentFormat.OpenXml.Wordprocessing.Color;
using FontSize = DocumentFormat.OpenXml.Wordprocessing.FontSize;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace CX.Engine.QAndA;

public class QASession : IDisposable
{
    private XLWorkbook _workbook;

    public readonly List<QAEntry> Entries = [];
    public QAScore LastScore;

    public List<BaseCriteria> ParseCriteria(string answerCriteria, string question, HashSet<string> questionTags)
    {
        var res = new List<BaseCriteria>();

        answerCriteria = answerCriteria?.Replace("\r\n", "\n");

        if (string.IsNullOrWhiteSpace(answerCriteria))
            return res;

        //Split into lines
        var lines = answerCriteria.Split('\n');
        //Trim each line
        lines = lines.Select(s => s.Trim()).ToArray();

        string current = null;

        void CurrentFinished()
        {
            if (current != null)
                res.Add(BaseCriteria.FromString(current, question, questionTags));

            current = null;
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            void AppendLineToCurrent()
            {
                if (current == null)
                    current = line;
                else
                    current += '\n' + line;
            }

            if (line.StartsWith("-"))
            {
                CurrentFinished();
                line = line.Substring(1).Trim();
            }

            AppendLineToCurrent();
        }

        CurrentFinished();

        return res;
    }

    private void LoadFromExcelWorkbook()
    {
        if (_workbook == null)
            throw new InvalidOperationException("No workbook loaded.");

        // Select the first worksheet
        var worksheet = _workbook.Worksheet(1);

        // Iterate over the rows in the worksheet
        foreach (var row in worksheet.RangeUsed().RowsUsed())
        {
            // Skip header row
            if (row.RowNumber() == 1)
                continue;

            // Read the values from each column
            var question = row.Cell(1).GetValue<string>();
            var criteria = row.Cell(2).GetValue<string>();
            var answer = row.Cell(4).GetValue<string>();
            var channel = row.Cell(5).GetValue<string>();
            var outcome = row.Cell(6).GetValue<double?>();
            var notes = row.Cell(8).GetValue<string>();

            var entry = new QAEntry
            {
                Question = question,
                Answer = new() { Answer = answer },
                Outcome = outcome,
                Notes = notes,
                ChannelName = channel
            };

            var tags = MiscHelpers.ExtractHashtags(row.Cell(3).GetValue<string>());
            entry.Tags.AddRange(tags);

            entry.Criteria.AddRange(ParseCriteria(criteria, entry.Question, entry.Tags));

            if (!string.IsNullOrWhiteSpace(question))
                Entries.Add(entry);
        }
    }

    public QASession LoadFromExcel(Stream stream)
    {
        Entries.Clear();
        _workbook ??= new(stream);
        LoadFromExcelWorkbook();
        return this;
    }

    public QASession LoadFromExcel(string filePath)
    {
        Entries.Clear();
        _workbook ??= new(filePath);
        LoadFromExcelWorkbook();
        return this;
    }

    public async Task LoadFromGoogleSheetsAsync(string spreadsheetId, string apiKey)
    {
        Entries.Clear();
        // Create Google Sheets API service.

        var service = new SheetsService(new()
        {
            ApiKey = apiKey
        });

        // Define request parameters.
        var range = $"Questions!A:F";
        var request =
            service.Spreadsheets.Values.Get(spreadsheetId, range);
        request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;

        var response = await request.ExecuteAsync();
        IList<IList<object>> values = response.Values;

        var first = true;
        foreach (var row in values)
        {
            if (first)
            {
                first = false;
                continue;
            }

            var question = row.ElementAtOrDefault(0)?.ToString();

            if (string.IsNullOrWhiteSpace(question))
                continue;

            var criteria = row.ElementAtOrDefault(1)?.ToString();
            var tags = row.ElementAtOrDefault(2)?.ToString();
            var notes = row.ElementAtOrDefault(5)?.ToString();

            var entry = new QAEntry
            {
                Question = question,
                Notes = notes
            };

            if (!string.IsNullOrWhiteSpace(MiscHelpers.StripHashtags(tags)))
                throw new InvalidOperationException(
                    $"tags field contains non-tags in row {values.IndexOf(row)}: {tags}");

            if (!string.IsNullOrWhiteSpace(tags))
                entry.Tags.AddRange(MiscHelpers.ExtractHashtags(tags));

            entry.Criteria.AddRange(ParseCriteria(criteria, entry.Question!, entry.Tags));

            Entries.Add(entry);
        }
    }

    private void SaveToExcelQuestions()
    {
        // Select the first worksheet
        var worksheet = _workbook!.AddWorksheet();

        worksheet.Name = "Questions";
        worksheet.Row(1).Cell(1).Value = "Question";
        worksheet.Row(1).Cell(2).Value = "Criteria";
        worksheet.Row(1).Cell(3).Value = "Tags";
        worksheet.Row(1).Cell(4).Value = "Answer";
        worksheet.Row(1).Cell(5).Value = "ChannelName";
        worksheet.Row(1).Cell(6).Value = "Outcome";
        worksheet.Row(1).Cell(8).Value = "Notes";
        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Row(1).Height = 30;
        worksheet.Column(1).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Column(2).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Column(3).Style.Fill.BackgroundColor = XLColor.LightGray;

        worksheet.Column(6).Style.NumberFormat.Format = "0.00%";

        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            var row = worksheet.Row(i + 2);
            row.Cell(1).Value = entry.Question;
            row.Cell(2).Value = string.Join("\n",
                from a
                    in entry.Criteria
                select a.GetCellContent(entry.Tags)).Preview(16_000);
            row.Cell(3).Value = string.Join("\n", entry.Tags).Preview(16_000);
            row.Cell(4).Value = entry.Answer?.GetCellContent().Preview(16_000);
            row.Cell(5).Value = entry.ChannelName;
            row.Cell(6).Value = entry.Outcome;
            row.Cell(8).Value = entry.Notes;
        }

        worksheet.Columns().AdjustToContents();
        worksheet.Column(4).Width = 60;

        worksheet.Column(1).Style.Alignment.WrapText = true;
        worksheet.Column(4).Style.Alignment.WrapText = true;

        worksheet.SheetView.FreezeRows(1);
        worksheet.SheetView.FreezeColumns(1);
        worksheet.RangeUsed().SetAutoFilter();
    }

    private void SaveToExcelOutcomes()
    {
        // Select the first worksheet
        var worksheet = _workbook!.AddWorksheet();

        worksheet.Name = "Outcomes";
        worksheet.Row(1).Cell(1).Value = "Question";
        worksheet.Row(1).Cell(2).Value = "Answer";
        worksheet.Row(1).Cell(3).Value = "Outcome";
        worksheet.Row(1).Cell(4).Value = "Outcome\nDetail";
        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Row(1).Height = 30;

        var rowNo = 1;

        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];

            for (var j = 0; j < entry.OutcomeDetail.Count; j++)
            {
                var detail = entry.OutcomeDetail[j];
                rowNo++;
                var row = worksheet.Row(rowNo);
                row.Cell(1).Value = entry.Question;
                row.Cell(2).Value = entry.Answer?.GetCellContent().Preview(16_000);
                row.Cell(3).Value = detail.pass ? "Pass" : "Fail";
                row.Cell(4).Value = detail.detail.Preview(16_000);
            }
        }

        worksheet.Column(1).Width = 60;
        worksheet.Column(2).Width = 60;
        worksheet.Column(3).Width = 60;

        worksheet.Column(1).Style.Alignment.WrapText = true;
        worksheet.Column(4).Style.Alignment.WrapText = true;
        worksheet.Column(8).Style.Alignment.WrapText = true;

        worksheet.SheetView.FreezeRows(1);
        worksheet.SheetView.FreezeColumns(1);
        worksheet.RangeUsed().SetAutoFilter();
    }

    private void SaveToExcelResults()
    {
        var worksheetChunks = _workbook!.AddWorksheet();
        worksheetChunks.Name = "Results";

        worksheetChunks.Row(1).Cell(1).Value = "Overall Percentage:";
        worksheetChunks.Row(1).Cell(1).Style.Font.Bold = true;
        worksheetChunks.Row(1).Cell(2).Value = LastScore!.OverallPercentage;
        worksheetChunks.Row(1).Cell(2).Style.NumberFormat.Format = "0.00%";
        worksheetChunks.Row(1).Cell(3).Value = LastScore.Overall;
        worksheetChunks.Row(1).Cell(4).Value = LastScore.OverallMax;
        worksheetChunks.Row(2).Cell(1).Value = "Total Criteria:";
        worksheetChunks.Row(2).Cell(1).Style.Font.Bold = true;
        worksheetChunks.Row(2).Cell(2).Value = LastScore!.TotalCriteria;
        worksheetChunks.Row(3).Cell(1).Value = "Total Regex:";
        worksheetChunks.Row(3).Cell(1).Style.Font.Bold = true;
        worksheetChunks.Row(3).Cell(2).Value = LastScore!.TotalRegex;
        worksheetChunks.Row(4).Cell(1).Value = "Natural Language:";
        worksheetChunks.Row(4).Cell(1).Style.Font.Bold = true;
        worksheetChunks.Row(4).Cell(2).Value = LastScore!.TotalCriteria - LastScore!.TotalRegex;
        worksheetChunks.Row(5).Cell(1).Value = "Ignored:";
        worksheetChunks.Row(5).Cell(1).Style.Font.Bold = true;
        worksheetChunks.Row(5).Cell(2).Value = LastScore!.TotalIgnored;

        worksheetChunks.Row(7).Cell(1).Value = "Tag";
        worksheetChunks.Row(7).Cell(2).Value = "%";
        worksheetChunks.Row(7).Cell(3).Value = "Score";
        worksheetChunks.Row(7).Cell(4).Value = "Max";
        worksheetChunks.Row(7).Style.Font.Bold = true;

        var rowNumber = 8;
        foreach (var key in LastScore.ByTagScore.Keys.ToList())
        {
            worksheetChunks.Row(rowNumber).Cell(1).Value = key;
            worksheetChunks.Row(rowNumber).Cell(1).Style.Font.Bold = true;
            worksheetChunks.Row(rowNumber).Cell(2).Value = LastScore.GetTagPercent(key);
            worksheetChunks.Row(rowNumber).Cell(2).Style.NumberFormat.Format = "0.00%";
            worksheetChunks.Row(rowNumber).Cell(3).Value = LastScore.ByTagScore[key];
            worksheetChunks.Row(rowNumber).Cell(4).Value = LastScore.ByTagMax[key];
            rowNumber++;
        }

        worksheetChunks.Columns().AdjustToContents();
    }

    public void SaveToExcelWorkbook()
    {
        if (_workbook == null)
            _workbook = new();

        SaveToExcelQuestions();
        SaveToExcelOutcomes();
        if (LastScore != null)
            SaveToExcelResults();
    }

    private void AddText(Run run, string text, Action<Run> addProperties)
    {
        foreach (var line in text.Split('\n'))
        {
            if (line.Length > 0)
                run.Append(new Text(line));
            run.Append(new Break());
            addProperties(run);
        }
    }

    public MemoryStream SaveToWord()
    {
        var stream = new MemoryStream();
        using var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new();
        var body = new Body();

        #region Document Header

        {
            var headingParagraph = new Paragraph();
            var headingRun = new Run();
            var headingRunProperties =
                new RunProperties(new Bold(), new FontSize { Val = "32" }, new RunFonts { Ascii = "Arial" });
            headingRun.Append(headingRunProperties);
            headingRun.Append(new Text($"Q & A Run - {DateTime.UtcNow:F}"));
            headingParagraph.Append(headingRun);
            body.Append(headingParagraph);

            body.Append(EmptyParagraph());
            body.Append(EmptyParagraph());
        }

        #endregion

        #region Overall Results

        {
            var resultsParagraph = new Paragraph();
            var run = new Run();
            run.Append(new RunProperties(new Bold(), new FontSize { Val = "32" }, new RunFonts { Ascii = "Arial" }));
            run.Append(new Text(
                $"Overall: {LastScore!.OverallPercentage:0.00%} ({LastScore.Overall:#,##0} / {LastScore.OverallMax:#,##0}) "));
            resultsParagraph.Append(run);

            body.Append(resultsParagraph);
            body.Append(EmptyParagraph());
            body.Append(EmptyParagraph());
        }

        #endregion

        var lineNo = 0;

        RunProperties ArialRunProps() => new(new RunFonts { Ascii = "Arial" });

        Paragraph EmptyParagraph()
        {
            var res = new Paragraph();
            res.Append(ArialRunProps());
            res.Append(new Run());
            res.Append(new Break());
            return res;
        }

        foreach (var entry in Entries)
        {
            lineNo++;

            //Question
            var question = entry.Question ?? "No question";
            {
                body.Append(EmptyParagraph());
                var subHeadingParagraph = new Paragraph();
                var subHeadingRun = new Run();
                var subHeadingRunProperties = new RunProperties(new Bold(), new FontSize { Val = "28" },
                    new RunFonts { Ascii = "Arial" });
                subHeadingRun.Append(subHeadingRunProperties);
                subHeadingRun.Append(new Text(lineNo + ".  " + question));
                subHeadingParagraph.Append(subHeadingRun);
                body.Append(subHeadingParagraph);
                body.Append(EmptyParagraph());
            }

            //Outcome
            var outcome = (entry.Outcome?.ToString("##0%") ?? "N/A") +
                          $" ({entry.OutcomeDetail.Count(e => e.pass):#,##0} / {entry.OutcomeDetail.Count:#,##0})";
            {
                var paragraph = new Paragraph();
                var run = new Run();
                run.Append(new RunProperties(new Bold(), new RunFonts { Ascii = "Arial" }));
                run.Append(new Text("Outcome: ") { Space = SpaceProcessingModeValues.Preserve });
                run.Append(new RunProperties(new Bold { Val = false }));
                run.Append(new Text(outcome));
                paragraph.Append(run);
                body.Append(paragraph);
                body.Append(EmptyParagraph());
            }

            //Answer
            var answer = entry.Answer?.Answer ?? "No answer";
            {
                var paragraph = new Paragraph();
                var run = new Run();
                run.Append(ArialRunProps());
                AddText(run, answer, r => r.Append(ArialRunProps()));
                paragraph.Append(run);
                body.Append(paragraph);
                body.Append(EmptyParagraph());
            }

            //Outcome Detail
            foreach (var detail in entry.OutcomeDetail)
            {
                var horizontalRule = new Paragraph();
                var paraProperties = new ParagraphProperties();
                var paraBorders = new ParagraphBorders();
                var bottom = new BottomBorder
                    { Val = BorderValues.Single, Color = "auto", Size = (UInt32Value)12U, Space = (UInt32Value)1U };
                paraBorders.Append(bottom);
                paraProperties.Append(paraBorders);
                horizontalRule.Append(paraProperties);
                horizontalRule.Append(ArialRunProps());
                horizontalRule.Append(new Run());
                body.Append(horizontalRule);
                body.Append(EmptyParagraph());

                var paragraph = new Paragraph();
                var run = new Run();
                run.Append(new RunProperties(new Indentation { Start = "100" },
                    new Bold { Val = false },
                    new FontSize { Val = "24" },
                    new Color { Val = detail.pass ? "006600" : "660000" },
                    new RunFonts { Ascii = "Arial" }));
                AddText(run, detail.detail, r => r.Append(new RunProperties(new Indentation { Start = "100" },
                    new Bold { Val = false },
                    new FontSize { Val = "24" },
                    new Color { Val = detail.pass ? "006600" : "660000" },
                    new RunFonts { Ascii = "Arial" })));
                paragraph.Append(run);
                body.Append(paragraph);
            }
        }

        mainPart.Document.Append(body);
        mainPart.Document.Save();
        stream.Position = 0;
        return stream;
    }

    public async Task SaveToWordAsync(string filePath)
    {
        await using var ms = SaveToWord();
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await ms.CopyToAsync(fs);
        await fs.FlushAsync();
    }

    public void SaveToExcel(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new InvalidOperationException("No file path specified");

        _workbook = new();

        SaveToExcelWorkbook();

        _workbook!.SaveAs(filePath);
    }

    public MemoryStream SaveToExcelStream()
    {
        _workbook = new();

        SaveToExcelWorkbook();

        var ms = new MemoryStream();
        _workbook!.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task<(bool? result, string detail)> EvalBaseCriteriaAsync(AssistantAnswer answer,
        BaseCriteria criteria, ChatCache chatCache, OpenAIChatAgent agent,
        FileService fileService,
        QAEntry qaEntry,
        bool useChatCache = true)
    {
        if (criteria is NLCriteria nl)
            return await EvalQuestionWithNLAsync(answer.Answer ?? "", nl.Criteria, chatCache, agent, qaEntry, useChatCache);

        if (criteria is ChunkRegexCriteria cr)
            return EvalChunkRegexCriteria(answer, cr);

        if (criteria is AttachCriteria ac)
            return await EvalAttachCriteriaAsync(answer, ac, fileService, qaEntry);

        if (criteria is IgnoreCriteria)
            return (null, "");

        throw new NotSupportedException(criteria.GetType().FullName);
    }

    public (bool result, string detail) EvalChunkRegexCriteria(AssistantAnswer answer, ChunkRegexCriteria criteria)
    {
        var pass = criteria.InstantiatedRegex.IsMatch(answer.Answer);

        return (pass, $"[Regex] {criteria.Name}:\n{(pass ? "Final Pass" : "Final Fail")}");
    }

    private static bool ExtractMin(string input, out int min)
    {
        // Define the regex pattern
        var pattern = @"^Min (\d+)$";

        // Create a regex object
        var regex = new Regex(pattern);

        // Match the input string against the regex pattern
        var match = regex.Match(input);

        // Check if the match is successful
        if (match.Success)
        {
            // Extract the number part and parse it as an integer
            if (int.TryParse(match.Groups[1].Value, out int number))
            {
                min = number;
                return true;
            }
        }

        min = default;
        // Return null if there's no match or parsing fails
        return false;
    }

    public static void ExtractGroup(ref string input, out string group)
    {
        // Define the regex pattern to match the first pair of square brackets and capture the content inside
        var pattern = @"\[(.*?)\]";

        // Create a regex object
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        // Match the input string against the regex pattern
        var match = regex.Match(input);

        // Check if the match is successful
        if (match.Success)
        {
            // Extract the content inside the brackets
            group = match.Groups[1].Value;
            // Remove the matched brackets and their content from the original string
            input = regex.Replace(input, "", 1).Trim();
            return;
        }

        // Return null if no match is found
        group = null!;
    }

    public async Task<(bool? result, string detail)> EvalAttachCriteriaAsync(AssistantAnswer answer,
        AttachCriteria criteria,
        FileService fileService,
        QAEntry qaEntry)
    {
        if (qaEntry.AttachmentsInEval == null)
        {
            qaEntry.AttachmentsInEval ??= new();

            if (answer.Attachments != null)
                qaEntry.AttachmentsInEval.AddRange(answer.Attachments);
        }

        var pass = false;
        var optional = false;
        if (answer.Attachments != null)
        {
            if (string.Equals(criteria.Name, "None", StringComparison.InvariantCultureIgnoreCase))
                pass = answer.Attachments.Count == 0;
            else if (string.Equals(criteria.Name, "Page Images", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var att in answer.Attachments.Where(att =>
                             att.FileUrl?.Contains("/api/page-images/") ?? false))
                {
                    pass = true;
                    qaEntry.AttachmentsInEval.Remove(att);
                    qaEntry.AttachmentsMatched.Add(att);
                }
            }
            else if (string.Equals(criteria.Name, "No other", StringComparison.InvariantCultureIgnoreCase))
            {
                pass = qaEntry.AttachmentsInEval.Count == 0;
                return (pass,
                    $"[Attach] {criteria.Name}.  Remaining attachments:\n  {string.Join("\n  ", qaEntry.AttachmentsInEval.Select(ev => ev.FileName))}\n{(pass ? "Final Pass" : "Final Fail")}");
            }
            else if (ExtractMin(criteria.Name, out var min))
                pass = qaEntry.AttachmentsMatched.Count >= min;
            else
            {
                var regex = criteria.Name;

                ExtractGroup(ref regex, out var group);
                if (group != null)
                {
                    optional = true;
                    qaEntry.CriteriaGroups.TryAdd(group, false);
                }

                if (criteria.Name.Trim().StartsWith("(optional)", StringComparison.InvariantCultureIgnoreCase))
                {
                    optional = true;
                    regex = criteria.Name[10..].Trim();
                }

                foreach (var att in qaEntry.AttachmentsInEval)
                {
                    //Not a content check
                    if (criteria.Sha256 == null)
                    {
                        try
                        {
                            if (att.FileName != null && Regex.IsMatch(att.FileName, regex))
                            {
                                qaEntry.AttachmentsInEval.Remove(att);
                                qaEntry.AttachmentsMatched.Add(att);
                                if (group != null)
                                    qaEntry.CriteriaGroups[group] = true;
                                pass = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            return (false,
                                $"[Attach] {criteria.Name}:\nRegex error: {ex.Message}\n\nFinal Fail");
                        }

                        continue;
                    }

                    var stream = await fileService.GetContentStreamAsync(att);
                    if (stream == null)
                        return (false,
                            $"[Attach] {criteria.Name}:\nNo means to get attachment's content stream.\n\nFinal Fail");

                    if (string.Equals(await stream.GetSHA256Async(), criteria.Sha256,
                            StringComparison.InvariantCultureIgnoreCase))
                    {
                        qaEntry.AttachmentsInEval.Remove(att);
                        qaEntry.AttachmentsMatched.Add(att);
                        if (group != null)
                            qaEntry.CriteriaGroups[group] = true;
                        pass = true;
                    }
                }
            }
        }

        return (optional ? null : pass, $"[Attach] {criteria.Name}:\n{(pass ? "Final Pass" : "Final Fail")}");
    }

    public async Task<(bool? result, string detail)> EvalQuestionWithNLAsync(string answer, string criteria,
        ChatCache chatCache,
        OpenAIChatAgent agent,
        QAEntry qaEntry,
        bool useChatCache = true)
    {
        var optional = false;
        ExtractGroup(ref criteria, out var group);
        if (group != null)
        {
            optional = true;
            qaEntry.CriteriaGroups.TryAdd(group, false);
        }

        var req = agent.GetRequest($"Does the statement: '''\n{answer}\n''' meet the criteria:\n'{criteria}'?",
            systemPrompt:
            "You are evaluating statements to determine if they meet provided assessment criteria.  Criteria can be met if they are implied, or the answer is close enough.  Consider the content of the statements, but do not consider precise wording.  Explain your reasoning.  End your response with a new line and the two words 'Final Pass' or 'Final Fail'."); 

        var evalResponse = await chatCache.ChatAsync(req, useChatCache);

        if (evalResponse.Answer == null)
            return (false, $"Evaluation failed: no answer provided by evaluator\nCriteria: {criteria}");

        var pass = evalResponse.Answer.Contains("Final Pass", StringComparison.InvariantCultureIgnoreCase) &&
                   !evalResponse.Answer.Contains("Final Fail", StringComparison.InvariantCultureIgnoreCase);

        if (pass && group != null)
            qaEntry.CriteriaGroups[group] = true;

        return (optional ? null : pass, "Criteria:\n\n- " + criteria.Trim() + "\n\nEvaluation:\n\n" + evalResponse.Answer);
    }

    /// <summary>
    /// Splits the question field up into history and a question.
    /// </summary>
    /// <param name="question">The raw unparsed question.</param>
    /// <param name="ctx">The assistant context to load history into.</param>
    /// <returns>The actual question.</returns>
    private string ParseQuestion(string question, AgentRequest ctx)
    {
        ctx.History.Clear();
        //Start by splitting into lines
        var lines = question.Trim().Replace("\r\n", "\n").Split('\n');
        //and then trimming each line
        lines = lines.Select(l => l.Trim()).ToArray();

        string entry = null;
        var user = true;

        void ProcessEntry()
        {
            if (entry != null)
                ctx.History.Add(new OpenAIChatMessage(user ? "user" : "assistant", entry!));
            entry = null;
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            void AppendLineToEntry()
            {
                if (entry != null)
                    entry += "\n" + line;
                else
                    entry = line;
            }

            //This entry is a message from the user.
            if (line.StartsWith(">"))
            {
                if (!user)
                    ProcessEntry();

                line = line.Substring(1).Trim();
                user = true;
            }
            //This entry is a message from the assistant.
            else if (line.StartsWith("<"))
            {
                if (user)
                    ProcessEntry();

                line = line.Substring(1).Trim();
                user = false;
            }
            //Other this line is a continuation of the previous entry, which requires no action.

            //Append the line to the (new or continued) entry.
            AppendLineToEntry();
        }

        //The last entry is always the question.
        return entry!;
    }

    public async Task EvaluateEntryAsync(QAEntry entry, IAssistant assistant, ChatCache chatCache, OpenAIChatAgent agent, QAScore score,
        AgentRequest agentRequest,
        FileService fileService,
        IServiceProvider sp)
    {
        using var ctx = agentRequest.GetScoped();
        ctx.SessionId = "qass-" + Guid.NewGuid();
        
        assistant = sp.GetNamedService<Channel>(entry.ChannelName)?.Assistant ?? assistant;

        if (entry.Question == null)
            throw new InvalidOperationException("Entry has no question");

        var question = ParseQuestion(entry.Question, ctx);

        if (question == null)
            throw new InvalidOperationException("Parsed entry has no question");

        var res = await assistant.AskAsync(question, ctx);
        entry.Answer = res;
        entry.Chunks = res.Chunks;

        entry.OutcomeDetail.Clear();

        var criteria = 0.0;
        var passes = 0;
        var ignored = 0;

        foreach (var crit in entry.Criteria)
        {
            var (pass, evalResponse) =
                await EvalBaseCriteriaAsync(res, crit, chatCache, agent, fileService, entry, ctx.UseCache);

            if (!pass.HasValue)
            {
                ignored++;
                continue;
            }

            criteria++;
            entry.OutcomeDetail.Add((pass.Value, evalResponse));
            var percent = pass.Value ? 1 : 0;

            foreach (var tag in crit.Tags)
                score.IncTag(tag, percent);

            if (pass.Value)
                passes++;
        }

        foreach (var grp in entry.CriteriaGroups)
        {
            criteria++;
            entry.OutcomeDetail.Add((grp.Value,
                $"Criteria group {grp.Key}: {(grp.Value ? "Final Pass" : "Final Fail")}"));
            var percent = grp.Value ? 1 : 0;

            foreach (var tag in entry.Tags)
                score.IncTag(tag, percent);

            if (grp.Value)
                passes++;
        }

        entry.Outcome = criteria == 0 ? 1 : (passes / criteria);
        if (criteria > 0 || ignored > 0)
            lock (score)
            {
                score.TotalCriteria += entry.OutcomeDetail.Count;
                score.TotalRegex += entry.Criteria.Count(c => c is ChunkRegexCriteria);
                score.TotalAttach += entry.Criteria.Count(c => c is AttachCriteria);
                score.TotalIgnored += ignored;

                score.Overall += passes;
                score.OverallMax += criteria;
            }
    }

    public double CompletedEntries => Entries.Count(e => e.Outcome >= 0);

    public async Task<QAScore> EvaluateAsync(IAssistant assistant, ChatCache chatCache, OpenAIChatAgent agent, AgentRequest ctx,
        FileService fileService, IServiceProvider sp)
    {
        var score = new QAScore();

        foreach (var e in Entries)
            e.Outcome = -1;

        await Entries
            .Select(entry => EvaluateEntryAsync(entry, assistant, chatCache, agent, score, ctx, fileService, sp));

        LastScore = score;
        return score;
    }

    public static async Task<QAScore> EvaluateExcelFileAsync(string filePath, Walter1Assistant walter1Assistant,
        ChatCache chatCache, OpenAIChatAgent agent,
        AgentRequest ctx, FileService fileService, IServiceProvider sp)
    {
        using var qaDoc = new QASession();
        qaDoc.LoadFromExcel(filePath);
        var res = await qaDoc.EvaluateAsync(walter1Assistant, chatCache, agent, ctx, fileService, sp);
        qaDoc.SaveToExcel(filePath);
        return res;
    }

    public static async Task<QAScore> EvaluateExcelFileAsync(Stream stream, Walter1Assistant walter1Assistant,
        ChatCache chatCache, OpenAIChatAgent agent,
        AgentRequest ctx, FileService fileService, IServiceProvider sp)
    {
        using var qaDoc = new QASession();
        qaDoc.LoadFromExcel(stream);
        var res = await qaDoc.EvaluateAsync(walter1Assistant, chatCache, agent, ctx, fileService, sp);
        return res;
    }

    public void Dispose()
    {
    }
}