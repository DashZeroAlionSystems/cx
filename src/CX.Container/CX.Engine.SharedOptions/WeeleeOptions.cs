namespace CX.Engine.SharedOptions
{
    public static class WeeleeDI
    {
        public const string ConfigurationSection = "WeeleeOptions";
    }

    public class WeeleeOptions
    {
        public required string ClientId { get; init; }
        public required string ClientSecret { get; init; }
        public required string Username { get; init; }
        public required string Password { get; init; }
        public required string RequestUrl { get; init; }
    }
}