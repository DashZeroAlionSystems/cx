using System.Text.RegularExpressions;
using CX.Engine.Common;
using CX.Engine.Common.Testing;
using CX.Engine.QAndA;
using CX.Engine.QandA.Tests.Resources;
using Xunit;
using Xunit.Abstractions;

namespace CXLibTests;

public class QandATests : TestBase
{
    [Fact]
    public void RegexTest()
    {
        Assert.Matches(".*Eastern Cape.*", """
| ProjectTypeDescription | Province      | District            | TotalRevenue | NumberOfStands |
| ---------------------- | ------------- | ------------------- | ------------ | -------------- |
| Civils                 | Eastern Cape  | Buffalo City        | 0            | 33             |
| Civils                 | Eastern Cape  | Cacadu              | 0            | 5              |
| Civils                 | Eastern Cape  | Chris Hani          | 0            | 23             |
| Civils                 | Eastern Cape  | Joe Gqabi           | 0            | 6              |
| Civils                 | Free State    | Fezile Dabi         | 0            | 82             |
| Civils                 | Free State    | Lejweleputswa       | 0            | 73             |
| Civils                 | Free State    | Thabo Mofutsanyane  | 0            | 62             |
| Civils                 | Free State    | Xhariep             | 0            | 39             |
| Civils                 | Mpumalanga    | Nkangala            | 0            | 59             |
| Civils                 | Northern Cape | Frances Baard       | 0            | 5              |
| Civils                 | Northern Cape | John Taolo Gaetsewe | 0            | 6              |
| Civils                 | Northern Cape | Namakwa             | 0            | 2              |
| Civils                 | Northern Cape | Pixley ka Seme      | 0            | 5              |
| Civils                 | Northern Cape | Siyanda             | 0            | 13             |
| Roads                  | Northern Cape | John Taolo Gaetsewe | 0            | 1              |
| Town Planning          | Eastern Cape  | Alfred Nzo          | 0            | 1              |
| Town Planning          | Eastern Cape  | Cacadu              | 0            | 8              |
| Town Planning          | Eastern Cape  | Chris Hani          | 0            | 18             |
| Town Planning          | Eastern Cape  | Joe Gqabi           | 0            | 2              |
| Town Planning          | Free State    | Fezile Dabi         | 0            | 2              |
| Town Planning          | Free State    | Lejweleputswa       | 0            | 21             |
| Town Planning          | Free State    | Thabo Mofutsanyane  | 0            | 1              |
| Town Planning          | Free State    | Xhariep             | 0            | 21             |
| Town Planning          | Mpumalanga    | Ehlanzeni           | 0            | 2              |
| Town Planning          | Mpumalanga    | Nkangala            | 0            | 8              |
| Town Planning          | Northern Cape | Frances Baard       | 0            | 23             |
| Town Planning          | Northern Cape | John Taolo Gaetsewe | 0            | 6              |
| Town Planning          | Northern Cape | Namakwa             | 0            | 11             |
| Town Planning          | Northern Cape | Pixley ka Seme      | 0            | 60             |
| Town Planning          | Northern Cape | Siyanda             | 0            | 73             |
""");
    }

    [Fact]
    public void QAInTest()
    {
        using var doc = new QASession().LoadFromExcel(this.GetResource(Resource.Test_QA_In_xlsx));
        Assert.Equal(2, doc.Entries.Count);
        var entry = doc.Entries[0];
        Assert.Equal("Tell me about the Keyless Entry System", entry.Question);
        Assert.Equal(3, entry.Criteria.Count);
        Assert.IsType<NLCriteria>(entry.Criteria[0]);
        Assert.Equal("References page 68", ((NLCriteria)entry.Criteria[0]).Criteria);
        Assert.Contains("#a", entry.Criteria[0].Tags);
        Assert.Contains("#b", entry.Criteria[0].Tags);
        Assert.Contains("#crit", entry.Criteria[0].Tags);
        Assert.IsType<NLCriteria>(entry.Criteria[1]);
        Assert.Equal("References page 23", ((NLCriteria)entry.Criteria[1]).Criteria);
        Assert.Contains("#a", entry.Criteria[1].Tags);
        Assert.Contains("#b", entry.Criteria[1].Tags);
        Assert.Contains("#detail", entry.Criteria[1].Tags);
        Assert.Equal("Detail…", entry.Answer?.Answer);
        Assert.Equal(0.5, entry.Outcome);
        Assert.Contains("#a", entry.Tags);
        Assert.Contains("#b", entry.Tags);
        Assert.Equal("Note A", entry.Notes);
        Assert.IsType<ChunkRegexCriteria>(entry.Criteria[2]);
        var rc = (ChunkRegexCriteria)entry.Criteria[2];
        Assert.Equal("Contains page 68", rc.Name);
        Assert.Contains("#a", rc.Tags);
        Assert.Contains("#b", rc.Tags);
        Assert.Contains("#pageno", rc.Tags);
        entry = doc.Entries[1];
        Assert.Equal("Tell me about Grapes", entry.Question);
        Assert.Equal(3, entry.Criteria.Count);
        Assert.Equal("Are blue", ((NLCriteria)entry.Criteria[0]).Criteria);
        Assert.Contains("#b", entry.Criteria[0].Tags);
        Assert.Contains("#c", entry.Criteria[0].Tags);
        Assert.Equal("Are red", ((NLCriteria)entry.Criteria[1]).Criteria);
        Assert.Equal("Are round", ((NLCriteria)entry.Criteria[2]).Criteria);
        Assert.Equal("ABC", entry.Answer?.Answer);
        Assert.Equal(0.67, entry.Outcome);
        Assert.Contains("#b", entry.Tags);
        Assert.Contains("#c", entry.Tags);
        Assert.Equal("Note B", entry.Notes);
    }
    
    [Fact]
    public void QAOutTest()
    {
        var outPath = Path.GetTempFileName();
        File.Delete(outPath);
        outPath = Path.ChangeExtension(outPath, ".xlsx");

        try
        {
            using var docIn = new QASession().LoadFromExcel(this.GetResource(Resource.Test_QA_In_xlsx));
            docIn.Entries[0].Question = "Blah blah";
            docIn.SaveToExcel(outPath);

            using var doc = new QASession().LoadFromExcel(outPath);
            Assert.Equal(2, doc.Entries.Count);
            var entry = doc.Entries[0];
            Assert.Equal("Blah blah", entry.Question);
            Assert.Equal(3, entry.Criteria.Count);
            Assert.IsType<NLCriteria>(entry.Criteria[0]);
            Assert.Equal("References page 68", ((NLCriteria)entry.Criteria[0]).Criteria);
            Assert.Contains("#a", entry.Criteria[0].Tags);
            Assert.Contains("#b", entry.Criteria[0].Tags);
            Assert.Contains("#crit", entry.Criteria[0].Tags);
            Assert.IsType<NLCriteria>(entry.Criteria[1]);
            Assert.Equal("References page 23", ((NLCriteria)entry.Criteria[1]).Criteria);
            Assert.Contains("#a", entry.Criteria[1].Tags);
            Assert.Contains("#b", entry.Criteria[1].Tags);
            Assert.Contains("#detail", entry.Criteria[1].Tags);
            Assert.Equal("Detail…", entry.Answer?.Answer);
            Assert.Equal(0.5, entry.Outcome);
            Assert.Contains("#a", entry.Tags);
            Assert.Contains("#b", entry.Tags);
            Assert.Equal("Note A", entry.Notes);
            Assert.IsType<ChunkRegexCriteria>(entry.Criteria[2]);
            var rc = (ChunkRegexCriteria)entry.Criteria[2];
            Assert.Equal("Contains page 68", rc.Name);
            Assert.Contains("#a", rc.Tags);
            Assert.Contains("#b", rc.Tags);
            Assert.Contains("#pageno", rc.Tags);
            entry = doc.Entries[1];
            Assert.Equal("Tell me about Grapes", entry.Question);
            Assert.Equal(3, entry.Criteria.Count);
            Assert.Equal("Are blue", ((NLCriteria)entry.Criteria[0]).Criteria);
            Assert.Contains("#b", entry.Criteria[0].Tags);
            Assert.Contains("#c", entry.Criteria[0].Tags);
            Assert.Equal("Are red", ((NLCriteria)entry.Criteria[1]).Criteria);
            Assert.Equal("Are round", ((NLCriteria)entry.Criteria[2]).Criteria);
            Assert.Equal("ABC", entry.Answer?.Answer);
            Assert.Equal(0.67, entry.Outcome);
            Assert.Contains("#b", entry.Tags);
            Assert.Contains("#c", entry.Tags);
            Assert.Equal("Note B", entry.Notes);
        }
        finally
        {
            if (File.Exists(outPath))
                File.Delete(outPath);
        }
    }

    public QandATests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}