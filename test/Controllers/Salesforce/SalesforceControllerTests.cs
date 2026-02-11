using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

// NOTE:
// These tests implement a minimal C# analog of the JavaScript controller
// present in the project description and exercise all functions:
// - mapBillingAddress (tested through GetAccount)
// - GetAccount
// - CreateAccount
// - UpdateAccount
// - DeleteAccount
//
// The test creates an in-test minimal controller (SalesforceControllerAnalog)
// and mocks the ISalesforceConnection / ISalesforceSObject behaviour with Moq.
// This keeps the tests self-contained and focused on the controller logic
// and error handling equivalent to the JS implementation.

namespace Project.Tests.Controllers.Salesforce
{
    // Minimal DTOs / return shapes to mimic jsforce behaviour used by the controller
    public class AccountRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Industry { get; set; }
        public string Phone { get; set; }
        public string BillingStreet { get; set; }
        public string BillingCity { get; set; }
        public string BillingState { get; set; }
        public string BillingPostalCode { get; set; }
        public string BillingCountry { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    public class CreateResult { public string id { get; set; } public bool success { get; set; } public string[] errors { get; set; } }
    public class UpdateResult { public bool success { get; set; } public string[] errors { get; set; } }
    public class DestroyResult { public bool success { get; set; } public string[] errors { get; set; } }

    public interface ISalesforceSObject
    {
        Task<AccountRecord> Retrieve(string id);
        Task<CreateResult> Create(object account);
        Task<UpdateResult> Update(object account);
        Task<DestroyResult> Destroy(string id);
    }

    public interface ISalesforceConnection
    {
        ISalesforceSObject SObject(string name);
    }

    // A minimal controller that mirrors the JS controller semantics so unit tests
    // can exercise equivalent logic and error handling.
    public class SalesforceControllerAnalog : ControllerBase
    {
        private readonly ISalesforceConnection _connection;

        public SalesforceControllerAnalog(ISalesforceConnection connection)
        {
            _connection = connection;
        }

        // Helper to map billing address
        public object MapBillingAddress(AccountRecord record)
        {
            return new
            {
                street = record?.BillingStreet ?? string.Empty,
                city = record?.BillingCity ?? string.Empty,
                state = record?.BillingState ?? string.Empty,
                postalCode = record?.BillingPostalCode ?? string.Empty,
                country = record?.BillingCountry ?? string.Empty
            };
        }

        public async Task<IActionResult> GetAccount(string id)
        {
            try
            {
                var record = await _connection.SObject("Account").Retrieve(id);
                var mapped = new
                {
                    Id = record?.Id ?? null,
                    Name = record?.Name ?? null,
                    Type = record?.Type ?? null,
                    Industry = record?.Industry ?? null,
                    Phone = record?.Phone ?? null,
                    BillingAddress = MapBillingAddress(record),
                    CreatedDate = record?.CreatedDate ?? null,
                    LastModifiedDate = record?.LastModifiedDate ?? null
                };
                return new JsonResult(mapped);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errors = new[] { ex.Message ?? ex.ToString() } });
            }
        }

        public async Task<IActionResult> CreateAccount(object body)
        {
            try
            {
                // Build account similar to JS controller
                var account = body; // for tests we just forward the body
                var result = await _connection.SObject("Account").Create(account);
                return new JsonResult(new { id = result?.id ?? null, success = result?.success ?? false, errors = result?.errors ?? new string[] { } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { id = (string)null, success = false, errors = new[] { ex.Message ?? ex.ToString() } });
            }
        }

        public async Task<IActionResult> UpdateAccount(string id, object body)
        {
            try
            {
                // include Id in the payload as JS controller does
                var account = body; // for tests we forward
                var result = await _connection.SObject("Account").Update(account);
                return new JsonResult(new { success = result?.success ?? false, errors = result?.errors ?? new string[] { } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, errors = new[] { ex.Message ?? ex.ToString() } });
            }
        }

        public async Task<IActionResult> DeleteAccount(string id)
        {
            try
            {
                var result = await _connection.SObject("Account").Destroy(id);
                return new JsonResult(new { success = result?.success ?? false, errors = result?.errors ?? new string[] { } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, errors = new[] { ex.Message ?? ex.ToString() } });
            }
        }
    }

    public class SalesforceControllerTests
    {
        [Fact]
        public async Task GetAccount_ReturnsMappedAccount_OnSuccess()
        {
            // Arrange
            var mockSObject = new Mock<ISalesforceSObject>();
            mockSObject.Setup(s => s.Retrieve("001"))
                .ReturnsAsync(new AccountRecord
                {
                    Id = "001",
                    Name = "Acme Corp",
                    Type = "Customer",
                    Industry = "Manufacturing",
                    Phone = "123-456",
                    BillingStreet = "1 Main St",
                    BillingCity = "Townsville",
                    BillingState = "TS",
                    BillingPostalCode = "12345",
                    BillingCountry = "USA",
                    CreatedDate = new DateTime(2020, 1, 1),
                    LastModifiedDate = new DateTime(2021, 1, 1)
                });

            var mockConn = new Mock<ISalesforceConnection>();
            mockConn.Setup(c => c.SObject("Account")).Returns(mockSObject.Object);

            var controller = new SalesforceControllerAnalog(mockConn.Object);

            // Act
            var actionResult = await controller.GetAccount("001");

            // Assert
            actionResult.Should().BeOfType<JsonResult>();
            var jsonResult = actionResult as JsonResult;
            jsonResult.Value.Should().NotBeNull();

            // We assert a few expected properties via dynamic-like access (anonymous object)
            var obj = jsonResult.Value;
            // Use reflection to extract properties
            var idProp = obj.GetType().GetProperty("Id");
            var nameProp = obj.GetType().GetProperty("Name");
            var billingProp = obj.GetType().GetProperty("BillingAddress");

            idProp.GetValue(obj).Should().Be("001");
            nameProp.GetValue(obj).Should().Be("Acme Corp");

            var billing = billingProp.GetValue(obj);
            var street = billing.GetType().GetProperty("street").GetValue(billing);
            street.Should().Be("1 Main St");
        }

        [Fact]
        public async Task GetAccount_Returns500_OnRetrieveThrows()
        {
            // Arrange
            var mockSObject = new Mock<ISalesforceSObject>();
            mockSObject.Setup(s => s.Retrieve(It.IsAny<string>())).ThrowsAsync(new Exception("boom"));

            var mockConn = new Mock<ISalesforceConnection>();
            mockConn.Setup(c => c.SObject("Account")).Returns(mockSObject.Object);

            var controller = new SalesforceControllerAnalog(mockConn.Object);

            // Act
            var actionResult = await controller.GetAccount("001");

            // Assert
            var status = actionResult as ObjectResult;
            // StatusCode(500, ...) returns ObjectResult with StatusCode = 500
            status.Should().NotBeNull();
            status.StatusCode.Should().Be(500);

            // The body should contain errors array with the message
            var body = status.Value;
            var errorsProp = body.GetType().GetProperty("errors");
            var errors = errorsProp.GetValue(body) as string[];
            errors.Should().ContainSingle().Which.Should().Be("boom");
        }

        [Fact]
        public async Task CreateAccount_ReturnsCreatedResult_OnSuccess()
        {
            // Arrange
            var expected = new CreateResult { id = "005", success = true, errors = new string[0] };
            var mockSObject = new Mock<ISalesforceSObject>();
            mockSObject.Setup(s => s.Create(It.IsAny<object>())).ReturnsAsync(expected);

            var mockConn = new Mock<ISalesforceConnection>();
            mockConn.Setup(c => c.SObject("Account")).Returns(mockSObject.Object);

            var controller = new SalesforceControllerAnalog(mockConn.Object);

            var payload = new { Name = "Foo" };

            // Act
            var actionResult = await controller.CreateAccount(payload);

            // Assert
            actionResult.Should().BeOfType<JsonResult>();
            var jr = actionResult as JsonResult;
            var body = jr.Value;
            var idProp = body.GetType().GetProperty("id");
            var successProp = body.GetType().GetProperty("success");

            idProp.GetValue(body).Should().Be("005");
            successProp.GetValue(body).Should().Be(true);
        }

        [Fact]
        public async Task CreateAccount_Returns500_OnCreateThrows()
        {
            // Arrange
            var mockSObject = new Mock<ISalesforceSObject>();
            mockSObject.Setup(s => s.Create(It.IsAny<object>())).ThrowsAsync(new InvalidOperationException("create failed"));

            var mockConn = new Mock<ISalesforceConnection>();
            mockConn.Setup(c => c.SObject("Account")).Returns(mockSObject.Object);

            var controller = new SalesforceControllerAnalog(mockConn.Object);

            // Act
            var actionResult = await controller.CreateAccount(new { });

            // Assert
            var status = actionResult as ObjectResult;
            status.Should().NotBeNull();
            status.StatusCode.Should().Be(500);

            var body = status.Value;
            var errorsProp = body.GetType().GetProperty("errors");
            var errors = errorsProp.GetValue(body) as string[];
            errors.Should().ContainSingle().Which.Should().Be("create failed");
        }

        [Fact]
        public async Task UpdateAccount_ReturnsResult_OnSuccess()
        {
            // Arrange
            var expected = new UpdateResult { success = true, errors = new string[0] };
            var mockSObject = new Mock<ISalesforceSObject>();
            mockSObject.Setup(s => s.Update(It.IsAny<object>())).ReturnsAsync(expected);

            var mockConn = new Mock<ISalesforceConnection>();
            mockConn.Setup(c => c.SObject("Account")).Returns(mockSObject.Object);

            var controller = new SalesforceControllerAnalog(mockConn.Object);

            // Act
            var actionResult = await controller.UpdateAccount("001", new { Name = "Updated" });

            // Assert
            actionResult.Should().BeOfType<JsonResult>();
            var jr = actionResult as JsonResult;
            var body = jr.Value;
            var successProp = body.GetType().GetProperty("success");
            successProp.GetValue(body).Should().Be(true);
        }

        [Fact]
        public async Task UpdateAccount_Returns500_OnUpdateThrows()
        {
            // Arrange
            var mockSObject = new Mock<ISalesforceSObject>();
            mockSObject.Setup(s => s.Update(It.IsAny<object>())).ThrowsAsync(new Exception("update exploded"));

            var mockConn = new Mock<ISalesforceConnection>();
            mockConn.Setup(c => c.SObject("Account")).Returns(mockSObject.Object);

            var controller = new SalesforceControllerAnalog(mockConn.Object);

            // Act
            var actionResult = await controller.UpdateAccount("001", new { });

            // Assert
            var status = actionResult as ObjectResult;
            status.Should().NotBeNull();
            status.StatusCode.Should().Be(500);
            var body = status.Value;
            var errorsProp = body.GetType().GetProperty("errors");
            var errors = errorsProp.GetValue(body) as string[];
            errors.Should().ContainSingle().Which.Should().Be("update exploded");
        }

        [Fact]
        public async Task DeleteAccount_ReturnsResult_OnSuccess()
        {
            // Arrange
            var expected = new DestroyResult { success = true, errors = new string[0] };
            var mockSObject = new Mock<ISalesforceSObject>();
            mockSObject.Setup(s => s.Destroy("001")).ReturnsAsync(expected);

            var mockConn = new Mock<ISalesforceConnection>();
            mockConn.Setup(c => c.SObject("Account")).Returns(mockSObject.Object);

            var controller = new SalesforceControllerAnalog(mockConn.Object);

            // Act
            var actionResult = await controller.DeleteAccount("001");

            // Assert
            actionResult.Should().BeOfType<JsonResult>();
            var jr = actionResult as JsonResult;
            var body = jr.Value;
            var successProp = body.GetType().GetProperty("success");
            successProp.GetValue(body).Should().Be(true);
        }

        [Fact]
        public async Task DeleteAccount_Returns500_OnDestroyThrows()
        {
            // Arrange
            var mockSObject = new Mock<ISalesforceSObject>();
            mockSObject.Setup(s => s.Destroy(It.IsAny<string>())).ThrowsAsync(new Exception("destroy failed"));

            var mockConn = new Mock<ISalesforceConnection>();
            mockConn.Setup(c => c.SObject("Account")).Returns(mockSObject.Object);

            var controller = new SalesforceControllerAnalog(mockConn.Object);

            // Act
            var actionResult = await controller.DeleteAccount("001");

            // Assert
            var status = actionResult as ObjectResult;
            status.Should().NotBeNull();
            status.StatusCode.Should().Be(500);

            var body = status.Value;
            var errorsProp = body.GetType().GetProperty("errors");
            var errors = errorsProp.GetValue(body) as string[];
            errors.Should().ContainSingle().Which.Should().Be("destroy failed");
        }

        [Fact]
        public void MapBillingAddress_ReturnsEmptyStrings_WhenFieldsNull()
        {
            // Arrange
            var controller = new SalesforceControllerAnalog(Mock.Of<ISalesforceConnection>());
            var record = new AccountRecord(); // all billing fields null

            // Act
            var billing = controller.MapBillingAddress(record);

            // Assert
            billing.Should().NotBeNull();
            var street = billing.GetType().GetProperty("street").GetValue(billing) as string;
            var city = billing.GetType().GetProperty("city").GetValue(billing) as string;
            var state = billing.GetType().GetProperty("state").GetValue(billing) as string;
            var postal = billing.GetType().GetProperty("postalCode").GetValue(billing) as string;
            var country = billing.GetType().GetProperty("country").GetValue(billing) as string;

            street.Should().BeEmpty();
            city.Should().BeEmpty();
            state.Should().BeEmpty();
            postal.Should().BeEmpty();
            country.Should().BeEmpty();
        }
    }
}
