using System.Text.RegularExpressions;
using CX.Engine.Common;

namespace CX.Engine.TextProcessors.Splitters;

public partial class LineSplitterSegmenter
{
    public readonly List<LineSplitterSegment> Segments = [];
    public List<int> PageNoLimit;
    public int TokenLimit = 10_000;

    private static readonly Regex PageBreakRegex = GetPageBreakRegex();

    private readonly string _document;

    private int _curIndex;
    private string _curContent;
    private int _curEoLStrength;
    private int? _curPageNo;
    private readonly LineSplitterRequest _req;

    public LineSplitterSegmenter(LineSplitterRequest req)
    {
        _req = req ?? throw new ArgumentNullException(nameof(req));
        
        //Normalize line endings
        _document = req.Document
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        //Remove line continuations
        _document = _document.CombineHyphenatedLines();
    }

    private bool ReadSegment()
    {
        _curEoLStrength = 0;
        var segmentEndsAt = _document.Length;
        var nextStartsAt = _document.Length;

        for (var i = _curIndex; i < _document.Length; i++)
        {
            //Advance through all line endings
            if (_document[i] == '\n')
            {
                segmentEndsAt = i;

                while (_document[i] == '\n')
                {
                    _curEoLStrength++;
                    i++;
                    nextStartsAt = i;

                    if (i >= _document.Length)
                        break;
                }

                break;
            }
        }

        var gotContent = _curIndex >= 0 && _curIndex < _document.Length;
        _curContent = segmentEndsAt > _curIndex ? _document.Substring(_curIndex, segmentEndsAt - _curIndex) : null;
        _curIndex = nextStartsAt;

        return gotContent;
    }

    public static int? ExtractPageNumber(string line)
    {
        if (!line.StartsWith("--- PAGE "))
            return null;

        // Define the regex pattern to match "--- PAGE X ---" where X is one or more digits

        // Match the input string against the regex pattern
        var match = PageBreakRegex.Match(line);

        // If the input matches the pattern, extract and return the page number
        if (!match.Success)
            return null;

        // Attempt to parse the matched string (group 1) to an integer
        if (int.TryParse(match.Groups[1].Value, out var pageNumber))
        {
            return pageNumber;
        }

        // Return null if there is no match or if parsing fails
        return null;
    }

    private TextSegment GetLastTextSegment()
    {
        for (var i = Segments.Count - 1; i >= 0; i--)
        {
            if (Segments[i] is TextSegment textSegment)
                return textSegment;
        }

        return null;
    }

    public void Parse()
    {
        _curIndex = 0;
        _curContent = null;
        _curPageNo = null;

        while (ReadSegment())
        {
            if (string.IsNullOrEmpty(_curContent))
            {
                //Lines with whitespace only increase the EoL strength of the last text segment by their own
                var ls = GetLastTextSegment();
                if (ls != null)
                    ls.EoLStrength += _curEoLStrength;
                continue;
            }

            {
                var pageNo = ExtractPageNumber(_curContent);
                if (pageNo.HasValue)
                    _curPageNo = pageNo.Value;
            }

            if (PageNoLimit != null && _curPageNo != null && !PageNoLimit.Contains(_curPageNo.Value))
                continue;
            
            var seg = new TextSegment(_curContent);
            
            seg.EoLStrength = _curEoLStrength;
            seg.Metadata.MergeMeta(_req.DocumentMeta);
            if (_curPageNo.HasValue)
                seg.Metadata.GetPageNos(true)!.Add(_curPageNo.Value);

            Segments.Add(seg);
        }

        MergeAndSplit();
    }

    private void MergeAndSplit()
    {
        for (var i = 0; i < Segments.Count; i++)
            if (Segments[i] is TextSegment txt && txt.EstTokens > TokenLimit)
            {
                var idx2 = txt.Content.Length / 2;
                var leftHalf = txt.Content[..idx2];
                var rightHalf = txt.Content[idx2..];
                var leftSeg = new TextSegment(leftHalf).InheritsFrom(txt);
                var rightSeg = new TextSegment(rightHalf).InheritsFrom(txt);
                Segments[i] = leftSeg;
                Segments.Insert(i + 1, rightSeg);
                i--;
            }

        for (var i = 0; i < Segments.Count; i++)
        {
            if (i < Segments.Count - 1 && Segments[i].CanMergeWith(Segments[i + 1], TokenLimit, 1))
            {
                Segments[i] = Segments[i].Merge(Segments[i + 1]);
                Segments.RemoveAt(i + 1);
                i--;
            }
        }
        
        for (var i = 0; i < Segments.Count; i++)
        {
            if (i < Segments.Count - 1 && Segments[i].CanMergeWith(Segments[i + 1], TokenLimit, 2))
            {
                Segments[i] = Segments[i].Merge(Segments[i + 1]);
                Segments.RemoveAt(i + 1);
                i--;
            }
        }
    }

    [GeneratedRegex(@"^--- PAGE (\d+) ---$", RegexOptions.Compiled)]
    private static partial Regex GetPageBreakRegex();
}