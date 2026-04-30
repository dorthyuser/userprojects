// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Functions.Tests
{
    public class OrdersFunctionTests
    {
        private static OrdersFunction CreateSut(Mock<DbHelper> dbHelperMock)
        {
            var loggerMock = new Mock<ILogger<OrdersFunction>>();
            return new OrdersFunction(dbHelperMock.Object, loggerMock.Object);
        }

        private static Mock<DbHelper> CreateDbHelperMock()
        {
            var mock = new Mock<DbHelper>(MockBehavior.Default, Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());
            mock.CallBase = false;
            return mock;
        }

        private static Mock<HttpRequestData> CreateRequestMock(MockFunctionContext functionContextMock)
        {
            var requestMock = new Mock<HttpRequestData>(functionContextMock.Object);
            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
            requestMock.Setup(r => r.Body).Returns(bodyStream);
            requestMock.Setup(r => r.Headers).Returns(new HttpHeadersCollection());
            requestMock.Setup(r => r.Cookies).Returns(new HttpCookies());
            requestMock.Setup(r => r.Url).Returns(new Uri("https://localhost/api/orders"));
            requestMock.Setup(r => r.Method).Returns("GET");
            requestMock.Setup(r => r.FunctionContext).Returns(functionContextMock.Object);
            requestMock.Setup(r => r.CreateResponse(It.IsAny<HttpStatusCode>()))
                .Returns((HttpStatusCode statusCode) => new MockHttpResponseData(functionContextMock.Object, statusCode));
            return requestMock;
        }

        private static MockFunctionContext CreateFunctionContextMock()
        {
            return new MockFunctionContext();
        }

        [Fact]
        public async Task GetOrders_ReturnsOkWithData_WhenOrdersAreRetrieved()
        {
            var functionContextMock = CreateFunctionContextMock();
            var requestMock = CreateRequestMock(functionContextMock);
            var dbHelperMock = CreateDbHelperMock();
            var orders = new List<Models.OrderResponse>
            {
                new Models.OrderResponse { id = 1 }
            };

            dbHelperMock.Setup(d => d.GetOrdersAsync()).ReturnsAsync((IReadOnlyList<Models.OrderResponse>)orders);

            var sut = CreateSut(dbHelperMock);

            var result = await sut.GetOrders(requestMock.Object);

            var response = Assert.IsType<MockHttpResponseData>(result);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = response.BodyText;
            Assert.Contains("data", body);
            Assert.Contains(""id":1", body);
            dbHelperMock.Verify(d => d.GetOrdersAsync(), Times.Once());
        }

        [Fact]
        public async Task GetOrders_ReturnsOkWithEmptyData_WhenNoOrdersExist()
        {
            var functionContextMock = CreateFunctionContextMock();
            var requestMock = CreateRequestMock(functionContextMock);
            var dbHelperMock = CreateDbHelperMock();
            dbHelperMock.Setup(d => d.GetOrdersAsync()).ReturnsAsync((IReadOnlyList<Models.OrderResponse>)new List<Models.OrderResponse>());

            var sut = CreateSut(dbHelperMock);

            var result = await sut.GetOrders(requestMock.Object);

            var response = Assert.IsType<MockHttpResponseData>(result);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = response.BodyText;
            Assert.Contains("data", body);
            dbHelperMock.Verify(d => d.GetOrdersAsync(), Times.Once());
        }

        [Fact]
        public async Task GetOrders_Throws_WhenDbHelperThrows()
        {
            var functionContextMock = CreateFunctionContextMock();
            var requestMock = CreateRequestMock(functionContextMock);
            var dbHelperMock = CreateDbHelperMock();
            dbHelperMock.Setup(d => d.GetOrdersAsync()).ThrowsAsync(new InvalidOperationException("db failure"));

            var sut = CreateSut(dbHelperMock);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetOrders(requestMock.Object));

            Assert.Equal("db failure", ex.Message);
            dbHelperMock.Verify(d => d.GetOrdersAsync(), Times.Once());
        }

        private sealed class MockFunctionContext : Mock<FunctionContext>
        {
            public MockFunctionContext()
            {
                SetupGet(c => c.InstanceServices).Returns(new ServiceCollection().BuildServiceProvider());
            }
        }

        private sealed class MockHttpResponseData : HttpResponseData
        {
            private readonly MemoryStream _body = new();

            public MockHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode) : base(functionContext)
            {
                StatusCode = statusCode;
            }

            public string BodyText
            {
                get
                {
                    _body.Position = 0;
                    using var reader = new StreamReader(_body, Encoding.UTF8, false, 1024, true);
                    return reader.ReadToEnd();
                }
            }

            public override HttpStatusCode StatusCode { get; set; }
            public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();
            public override Stream Body
            {
                get => _body;
                set
                {
                    if (value is MemoryStream ms)
                    {
                        _body.SetLength(0);
                        ms.Position = 0;
                        ms.CopyTo(_body);
                        _body.Position = 0;
                    }
                }
            }
            public override HttpCookies Cookies { get; } = new HttpCookies();
        }
    }
}
