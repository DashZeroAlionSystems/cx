using System.Text;
using JetBrains.Annotations;
using SmartFormat.Utilities;

namespace CX.Engine.CognitiveServices.Transcriptions;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Transcription
{
    public List<TranscriptionPhrase> Phrases { get; set; } = [];
    public Dictionary<int, Speaker> Speakers { get; set; } = new();

    public void PopulateSpeakerNames()
    {
        foreach (var phrase in Phrases)
            if (!Speakers.ContainsKey(phrase.Speaker))
                Speakers.Add(phrase.Speaker, new ($"Speaker {phrase.Speaker}"));
    }

    public Speaker GetSpeaker(int id)
    {
        if (Speakers.TryGetValue(id, out var speaker))
            return speaker;
        return null;
    }

    public string GetSpeakerName(int id)
    {
        if (Speakers.TryGetValue(id, out var speaker))
            return speaker.Name;
        return $"Speaker {id}";
    }

    public string GetFullText()
    {
        var sb = new StringBuilder();
        foreach (var line in Phrases)
            sb.AppendLine(GetSpeakerName(line.Speaker) + ": " + line.Phrase);
        return sb.ToString();
    }
}