namespace Aela.Server.Common;

public class SecurityConstants
{
    public static class Roles
    {
        public const string Admin = "Administrator";
        public static string SuperUser = "SuperUser";
        public static string Support = "Support";
        public static string User = "User";
        
        public static string[] AtLeastAdmin => [Admin];
        public static string[] AtLeastSuperUser => [Admin, SuperUser];
        public static string[] AtLeastSupport => [Admin, SuperUser, Support];
        public static string[] AtLeastUser => [Admin, SuperUser, Support, User];
    }
}