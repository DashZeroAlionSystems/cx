using System.Text.RegularExpressions;

namespace CX.Engine.Common;

public static partial class StringValidators
{
    // Source-generated regex method
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex AlphaNumericUnderscoreStartingWithLetterPattern();

    // Method to validate the pattern
    public static bool IsValidAlphaNumericUnderscoreStartingWithLetter(string input)
    {
        return AlphaNumericUnderscoreStartingWithLetterPattern().IsMatch(input);
    }
}