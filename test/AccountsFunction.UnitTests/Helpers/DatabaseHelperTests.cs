using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using AccountsFunction.Helpers;
using AccountsFunction.Models;

namespace AccountsFunction.UnitTests.Helpers
{
    public class DatabaseHelperTests
    {
        [Fact]
        public void TryParseGuid_ValidGuid_ReturnsTrueAndOutputsGuid()
        {
            // Arrange
            var input = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

            // Act
            var result = DatabaseHelper.TryParseGuid(input, out var guid);

            // Assert
            Assert.True(result);
            Assert.Equal(Guid.Parse(input), guid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-guid")]
        public void TryParseGuid_InvalidInputs_ReturnsFalseAndGuidEmpty(string input)
        {
            // Act
            var result = DatabaseHelper.TryParseGuid(input, out var guid);

            // Assert
            Assert.False(result);
            Assert.Equal(Guid.Empty, guid);
        }

        [Fact]
        public async Task CreateAccountsAsync_AddsAndReturnsCreatedAccounts()
        {
            // Arrange
            var db = new DatabaseHelper();
            var a1 = new Account { Id = Guid.NewGuid(), Name = "A1", Email = "a1@example.com", Address = "Addr1" };
            var a2 = new Account { Id = Guid.NewGuid(), Name = "A2", Email = "a2@example.com", Address = "Addr2" };

            // Act
            var created = await db.CreateAccountsAsync(new[] { a1, a2 });

            // Assert
            Assert.Equal(2, created.Count);
            Assert.Contains(created, c => c.Id == a1.Id && c.Name == "A1");
            Assert.Contains(created, c => c.Id == a2.Id && c.Name == "A2");

            var fetched = await db.GetAccountByIdAsync(a1.Id);
            Assert.NotNull(fetched);
            Assert.Equal(a1.Id, fetched!.Id);
            Assert.Equal(a1.Name, fetched.Name);
            Assert.Equal(a1.Email, fetched.Email);
            Assert.Equal(a1.Address, fetched.Address);
        }

        [Fact]
        public async Task GetAccountByIdAsync_ReturnsNullForMissing()
        {
            // Arrange
            var db = new DatabaseHelper();
            var missingId = Guid.NewGuid();

            // Act
            var missing = await db.GetAccountByIdAsync(missingId);

            // Assert
            Assert.Null(missing);
        }

        [Fact]
        public async Task GetAccountByIdAsync_ReturnsCopy_NotReference()
        {
            // Arrange
            var db = new DatabaseHelper();
            var acc = new Account { Id = Guid.NewGuid(), Name = "Original", Email = "e@example.com", Address = "addr" };
            await db.CreateAccountsAsync(new[] { acc });

            // Act
            var fetched1 = await db.GetAccountByIdAsync(acc.Id);
            Assert.NotNull(fetched1);
            fetched1!.Name = "Modified"; // modify the returned copy

            var fetched2 = await db.GetAccountByIdAsync(acc.Id);

            // Assert - the original stored value should remain unchanged because GetAccountByIdAsync returns a copy
            Assert.Equal("Original", fetched2!.Name);
        }
    }
}
