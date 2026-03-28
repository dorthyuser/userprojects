namespace RandomFunctionApp.Models
{
    public class ErrorResponse
    {
        public ErrorDetail Error { get; set; }

        public ErrorResponse()
        {
            Error = new ErrorDetail();
        }
    }

    public class ErrorDetail
    {
        public string Message { get; set; }
        public string Details { get; set; }

        public ErrorDetail()
        {
            Message = string.Empty;
            Details = string.Empty;
        }
    }
}
