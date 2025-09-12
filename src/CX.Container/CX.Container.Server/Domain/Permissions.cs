namespace CX.Container.Server.Domain;

using System.Reflection;

public static class Permissions
{
    // Permissions marker - do not delete this comment    
    public const string CanManageNodes = nameof(CanManageNodes);
    public const string CanManagePreferences = nameof(CanManagePreferences);
    public const string CanManageMessages = nameof(CanManageMessages);
    public const string CanManageThreads = nameof(CanManageThreads);
    public const string CanManageSourceDocuments = nameof(CanManageSourceDocuments);
    public const string CanManageSources = nameof(CanManageSources);
    public const string CanManageCountries = nameof(CanManageCountries);
    public const string CanManageAgriculturalActivityTypes = nameof(CanManageAgriculturalActivityTypes);
    public const string CanManageAgriculturalActivities = nameof(CanManageAgriculturalActivities);
    public const string CanManageProfiles = nameof(CanManageProfiles);
    public const string CanManageAccounts = nameof(CanManageAccounts);
    public const string CanDeleteUsers = nameof(CanDeleteUsers);
    public const string CanUpdateUsers = nameof(CanUpdateUsers);
    public const string CanAddUsers = nameof(CanAddUsers);
    public const string CanReadUsers = nameof(CanReadUsers);
    public const string CanDeleteRolePermissions = nameof(CanDeleteRolePermissions);
    public const string CanUpdateRolePermissions = nameof(CanUpdateRolePermissions);
    public const string CanAddRolePermissions = nameof(CanAddRolePermissions);
    public const string CanReadRolePermissions = nameof(CanReadRolePermissions);
    public const string HangfireAccess = nameof(HangfireAccess);
    public const string CanRemoveUserRoles = nameof(CanRemoveUserRoles);
    public const string CanAddUserRoles = nameof(CanAddUserRoles);
    public const string CanGetRoles = nameof(CanGetRoles);
    public const string CanGetPermissions = nameof(CanGetPermissions);
    public const string CanManageProjects = nameof(CanManageProjects);
    public const string ClearPermissionCache = nameof(ClearPermissionCache);
    public const string CanGetNamespaces = nameof(CanGetNamespaces);
    public const string CanEditNamespaces = nameof(CanEditNamespaces);
    public const string CanDeleteNamespaces = nameof(CanDeleteNamespaces);
    public const string CanSetChannelDisplayNames = nameof(CanSetChannelDisplayNames);
    public const string CanUseTextToSchema  = nameof(CanUseTextToSchema);
    public const string CanViewTextToSchemaConfig = nameof(CanViewTextToSchemaConfig);
    public const string CanManageTextToSchemaConfig = nameof(CanManageTextToSchemaConfig);
    public const string CanUseChannels = nameof(CanUseChannels);
    public const string CanViewChannelConfig = nameof(CanViewChannelConfig);
    public const string CanManageChannelsConfig = nameof(CanManageChannelsConfig);
    public const string CanViewAnyConfig = nameof(CanViewAnyConfig);
    public const string CanManageAnyConfig = nameof(CanManageAnyConfig);
    public const string CanViewChannelsConfig = nameof(CanViewChannelsConfig);
    public const string CanViewFlatqueryassistantsConfig = nameof(CanViewFlatqueryassistantsConfig);
    public const string CanManageFlatqueryassistantsConfig = nameof(CanManageFlatqueryassistantsConfig);
    public const string CanViewJsonschemasConfig = nameof(CanViewJsonschemasConfig);
    public const string CanManageJsonschemasConfig = nameof(CanManageJsonschemasConfig);
    public const string CanViewLuacoreConfig = nameof(CanViewLuacoreConfig);
    public const string CanManageLuacoreConfig = nameof(CanManageLuacoreConfig);
    public const string CanViewPineconeNamespacesConfig = nameof(CanViewPineconeNamespacesConfig);
    public const string CanManagePineconeNamespacesConfig = nameof(CanManagePineconeNamespacesConfig);
    public const string CanViewPineconesConfig = nameof(CanViewPineconesConfig);
    public const string CanManagePineconesConfig = nameof(CanManagePineconesConfig);
    public const string CanViewPostgresqlclientsConfig = nameof(CanViewPostgresqlclientsConfig);
    public const string CanManagePostgresqlclientsConfig = nameof(CanManagePostgresqlclientsConfig);
    public const string CanViewScheduledquestionagentsConfig = nameof(CanViewScheduledquestionagentsConfig);
    public const string CanManageScheduledquestionagentsConfig = nameof(CanManageScheduledquestionagentsConfig);
    public const string CanViewTextToSchemaAssistantsConfig = nameof(CanViewTextToSchemaAssistantsConfig);
    public const string CanManageTextToSchemaAssistantsConfig = nameof(CanManageTextToSchemaAssistantsConfig);
    public const string CanViewVectormindliveassistantsConfig = nameof(CanViewVectormindliveassistantsConfig);
    public const string CanManageVectormindliveassistantsConfig = nameof(CanManageVectormindliveassistantsConfig);
    public const string CanViewWalter1assistantsConfig = nameof(CanViewWalter1assistantsConfig);
    public const string CanManageWalter1assistantsConfig = nameof(CanManageWalter1assistantsConfig);
    public const string CanManageLuaSessions = nameof(CanManageLuaSessions);
    public const string CanExecuteLuaCommands = nameof(CanExecuteLuaCommands);
    public const string CanViewLuaSessions = nameof(CanViewLuaSessions);
    public const string CanImportDocuments = nameof(CanImportDocuments);
    public const string CanDeleteDocuments = nameof(CanDeleteDocuments);
    public const string CanManagePineconeArchive = nameof(CanManagePineconeArchive);
    public const string CanModifyTableData = nameof(CanModifyTableData);
    public const string CanReadTableData = nameof(CanReadTableData);
    public const string CanExecuteSqlCommand = nameof(CanExecuteSqlCommand);
    public const string CanExecuteSqlQuery = nameof(CanExecuteSqlQuery);
    public const string CanConvertDocuments = nameof(CanConvertDocuments);
    public const string CanExtractText = nameof(CanExtractText);
    public const string CanUseWalter1Assistant = nameof(CanUseWalter1Assistant);
    public const string CanViewDuoportaBrands = nameof(CanViewDuoportaBrands);
    public const string CanViewDuoportaRanges = nameof(CanViewDuoportaRanges);
    public const string CanViewDuoportaModels = nameof(CanViewDuoportaModels);
    public const string CanViewDuoportaSpecs = nameof(CanViewDuoportaSpecs);
    public const string CanViewDuoportaFeatures = nameof(CanViewDuoportaFeatures);
    public const string CanViewDuoportaImages = nameof(CanViewDuoportaImages);
    public const string CanViewDuoportaMMCode = nameof(CanViewDuoportaMMCode);
    public const string CanViewDuoportaData = nameof(CanViewDuoportaData);
    public const string CanManageQAEvaluations = nameof(CanManageQAEvaluations);
    

    private static IReadOnlyList<string> _allPermissions;
    
    public static IReadOnlyList<string> List()
    {
        return _allPermissions ??= typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue())
            .ToList().AsReadOnly();
    }
}
