namespace Npgsql.NameTranslation
{
    // Defined to ensure compatibility with Npgsql versions where this interface type is expected at compile time.
    public interface INpgsqlNameTranslator
    {
        string TranslateMemberName(string memberName);
        string TranslateTypeName(string typeName);
    }
}
