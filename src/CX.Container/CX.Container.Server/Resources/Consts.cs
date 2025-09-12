namespace CX.Container.Server.Resources;

using System.Reflection;

public static class Consts
{
    /// <summary>
    /// Used to coordinate namespace and project-related changes to avoid conflicts.
    /// This lock covers hybrid scenarios between PostgreSQLClient, Entity Framework and Pinecone.
    /// </summary>
    public const string ApiLock = "api-lock";
    
    public static class Testing
    {
        public const string IntegrationTestingEnvName = "LocalIntegrationTesting";
        public const string FunctionalTestingEnvName = "LocalFunctionalTesting";
    }

    public static class Cache
    {
        public static class Auth
        {
            public static readonly string Name = nameof(Auth);
            public static string UserPermissionKey (string nameIdentifier) => $"user-permissions:{nameIdentifier}";
        }
        
        public static class Conversation
        {
            public static readonly string Name = nameof(Conversation);
            public static string ThreadKey(Guid threadId) => $"conversation-thread:{threadId}";
        }
    }

    public static class HangfireQueues
    {
        public const string Default = "default";
        public const string ProcessSourceDocumentsQueue = "process-source-documents";
        public const string WeeleeServiceQueue = "weelee-service-documents";

        public static string[] List()
        {
            return typeof(HangfireQueues)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(x => (string)x.GetRawConstantValue())
                .ToArray();
        }
    }
}