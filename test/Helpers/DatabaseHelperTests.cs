using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using AccountsFunction.Helpers;
using AccountsFunction.Models;

namespace AccountsFunction.Tests.Helpers
{
    public class DatabaseHelperTests
    {
        [Fact]
        public async Task CreateAndRetrieveAccountAsync_ShouldReturnCreatedAccount()
        {
            // Arrange
            var db = new DatabaseHelper();
            var id = Guid.NewGuid();
            var account = new Account
            {
                Id = id,
                Name = "Test User",
                Email = "test@example.com",
                Address = "123 Test Lane"
            };

            // Act
            var created = await db.CreateAccountsAsync(new[] { account });
            var fetched = await db.GetAccountByIdAsync(id);

            // Assert
            Assert.NotNull(created);
            Assert.Single(created);
            Assert.Equal(id, created[0].Id);
            Assert.Equal("Test User", created[0].Name);

            Assert.NotNull(fetched);
            Assert.Equal(id, fetched!.Id);
            Assert.Equal("test@example.com", fetched.Email);
            Assert.Equal("123 Test Lane", fetched.Address);
        }

        [Fact]
        public async Task CreateAccountsAsync_ShouldUpsertExistingAccount()
        {
            // Arrange
            var db = new DatabaseHelper();
            var id = Guid.NewGuid();
            var original = new Account { Id = id, Name = "Orig", Email = "orig@x.com", Address = "A" };
            var updated = new Account { Id = id, Name = "Updated", Email = "up@x.com", Address = "B" };

            // Act
            await db.CreateAccountsAsync(new[] { original });
            await db.CreateAccountsAsync(new[] { updated });
            var fetched = await db.GetAccountByIdAsync(id);

            // Assert
            Assert.NotNull(fetched);
            Assert.Equal("Updated", fetched!.Name);
            Assert.Equal("up@x.com", fetched.Email);
            Assert.Equal("B", fetched.Address);
        }

        [Fact]
        public async Task CreateAccountsAsync_ShouldIgnoreNullEntries()
        {
            // Arrange
            var db = new DatabaseHelper();
            var id = Guid.NewGuid();
            var account = new Account { Id = id, Name = "NullSafe", Email = "null@x.com", Address = "N" };
            var list = new List<Account?> { null, account };

            // Act
            var created = await db.CreateAccountsAsync(list!);
            var fetched = await db.GetAccountByIdAsync(id);

            // Assert
            Assert.NotNull(created);
            Assert.Single(created);
            Assert.Equal(id, created[0].Id);

            Assert.NotNull(fetched);
            Assert.Equal("NullSafe", fetched!.Name);
        }

        [Fact]
        public async Task GetAccountByIdAsync_NotFound_ReturnsNull()
        {
            // Arrange
            var db = new DatabaseHelper();
            var randomId = Guid.NewGuid(); // unlikely to collide with seeded/sample

            // Act
            var fetched = await db.GetAccountByIdAsync(randomId);

            // Assert
            Assert.Null(fetched);
        }
    }
}
