using CX.Engine.Common.JsonSchemas;
using JetBrains.Annotations;

namespace CX.Clients.Afriforum.Domain;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Semantic("Filters to use on the query to run against the PostgreSQL database")]
public class SakenetwerkFilter
{
    [Semantic("Filter by filter, explain what conditions you are applying and why.")]
    public string ExplainFilter { get; set; }

    public static class AdminCommands
    {
        public const string None = "None";
        public const string ListAllCities = "List all cities";
        public const string CleanCities = "Clean cities";
        public const string CleanProvinces = "Clean provinces";
        public const string ExpandCategories = "Expand categories";
        public const string ExpandTags = "Expand tags";
        
        //NB: Also add in the attribute below.
        public static readonly string[] All = [None, ListAllCities, CleanCities, CleanProvinces, ExpandCategories, ExpandTags];
    }

    [Semantic("The ILIKE condition to use for the name of the business.  Only use this if a business is requested by name.")]
    public string NameLike { get; set; }

    [Semantic("The PostgreSQL ILIKE condition to use for name of the city in South Africa that the business is in.  City names prefer English form.")]
    public string[] CityLike { get; set; }

    [Semantic("The province to look for the business in", 
        choices: [ "", "Vrystaat", "Noordwes", "Noord-Kaap", "Wes-Kaap", "Mpumalanga", "Gauteng", "Limpopo", "Oos-Kaap", "KwaZulu-Natal" ])]
    public string[] Provinces { get; set; }

    [Semantic("Categories to look for the business in")]
    public string[] Categories { get; set; }

    [Semantic("Tags to look for the business in")]
    public string[] Tags { get; set; }

    [Semantic("The PostgreSQL ILIKE condition to filter the business' email by")]
    public string EmailLike { get; set; }

    [Semantic("A full or partial phone number to search for.")]
    public string PhoneNumber { get; set; }

    [Semantic("The PostgreSQL ILIKE condition to filter the business' URL by. ")]
    public string UrlLike { get; set; }

    [Semantic(anyOf: [typeof(ListCities), typeof(CleanCities), typeof(CleanProvinces), typeof(ExpandCategories), typeof(ExpandTags), typeof(NoTool)])]
    public SakenetwerkToolCall ToolCall { get; set; }
}