namespace SalesforceAccountFunctions.Helpers.Salesforce
{
    public class SalesforceCreateResult
    {
        public bool Success { get; set; }
        public string? Id { get; set; }
        public string? ErrorDetails { get; set; }
    }

    public class SalesforceDeleteResult
    {
        public bool Success { get; set; }
        public string? ErrorDetails { get; set; }
    }
}
