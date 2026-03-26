using System;
using Npgsql;
using Npgsql.NameTranslation;

namespace travelcardservice.Helpers
{
    public class ExactNameTranslator : INpgsqlNameTranslator
    {
        public string TranslateMemberName(string name) => name;
        public string TranslateName(string name) => name;
        public string TranslateTypeName(string name) => name;
    }
}
