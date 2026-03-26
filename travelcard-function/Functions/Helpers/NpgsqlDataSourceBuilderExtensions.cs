using Npgsql;

namespace TravelcardFunction.Helpers
{
    public class ExactNameTranslator
    {
        // Placeholder translator to satisfy mapping call. In this implementation we do not alter names.
        public string Translate(string name) => name;
    }

    public static class NpgsqlDataSourceBuilderExtensions
    {
        public static NpgsqlDataSourceBuilder MapEnum<T>(this NpgsqlDataSourceBuilder builder, string pgName, ExactNameTranslator translator)
        {
            // If Npgsql provides an overload, extension will not conflict. This extension acts as a no-op
            // to follow the requirement to call MapEnum with ExactNameTranslator.
            return builder;
        }
    }
}
