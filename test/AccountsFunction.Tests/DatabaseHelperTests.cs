using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using AccountsFunction.Helpers;
using AccountsFunction.Models;

namespace AccountsFunction.Tests
{
    public class DatabaseHelperTests
    {
        [Fact]
        public void TryParseGuid_ValidGuid_ReturnsTrue()
        {
            var guid = Guid.NewGuid();
            var input = guid.ToString();

            var result = DatabaseHelper.TryParseGuid(input, out var parsed);

            Assert.True(result);
            Assert.Equal(guid, parsed);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-guid")]
        public void TryParseGuid_InvalidInputs_ReturnsFalse(string? input)
        {
            var result = DatabaseHelper.TryParseGuid(input, out var parsed);

            Assert.False(result);
            Assert.Equal(Guid.Empty, parsed);
        }

        [Fact]
        public async Task CreateAccountsAsync_AddsAndReturnsCreatedAccounts()
        {
            // Arrange
            var db = new DatabaseHelper();
            var a1 = new Account { Id = Guid.NewGuid(), Name = "A1", Email = "a1@example.com", Address = "addr1" };
            var a2 = new Account { Id = Guid.NewGuid(), Name = "A2", Email = "a2@example.com", Address = "addr2" };

            // Act
            var created = await db.CreateAccountsAsync(new[] { a1, a2 });

            // Assert
            Assert.NotNull(created);
            Assert.Equal(2, created.Count);
            Assert.Contains(created, c => c.Id == a1.Id);
            Assert.Contains(created, c => c.Id == a2.Id);

            // Verify that the account can be retrieved
            var fetched = await db.GetAccountByIdAsync(a1.Id);
            Assert.NotNull(fetched);
            Assert.Equal(a1.Id, fetched!.Id);
            Assert.Equal(a1.Name, fetched.Name);
            Assert.Equal(a1.Email, fetched.Email);
            Assert.Equal(a1.Address, fetched.Address);
        }

        [Fact]
        public async Task GetAccountByIdAsync_NonExisting_ReturnsNull()
        {
            var db = new DatabaseHelper();
            var id = Guid.NewGuid();
n            var result = await db.GetAccountByIdAsync(id);

            Assert.Null(result);
        }
    }
}
