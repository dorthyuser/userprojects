using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SalesforceAccountFunctions.UnitTests.Helpers
{
    // A simple test HttpResponseData implementation that stores response body in a MemoryStream
    public class TestHttpResponseData : HttpResponseData
    {
        private readonly MemoryStream _body = new();

        public TestHttpResponseData(FunctionContext context, HttpStatusCode statusCode) : base(context)
        {
            StatusCode = statusCode;
            Headers = new HttpHeadersCollection();
            Cookies = new HttpCookiesCollection();
        }

        public override Stream Body => _body;
        public override HttpHeadersCollection Headers { get; }
        public override HttpStatusCode StatusCode { get; set; }
        public override HttpCookiesCollection Cookies { get; }

        public async Task<string> ReadAsStringAsync()
        {
            _body.Seek(0, SeekOrigin.Begin);
            using var sr = new StreamReader(_body, Encoding.UTF8, leaveOpen: true);
            var content = await sr.ReadToEndAsync().ConfigureAwait(false);
            return content;
        }
    }

    // Not strictly required to derive HttpRequestData in tests; we use Moq to mock HttpRequestData and return TestHttpResponseData
}
