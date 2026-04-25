// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZohoProject2.Models;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class IZohoCrmServiceTests
    {
        [Fact]
        public async Task GetUsersAsync_CanBeImplementedForSuccessPath()
        {
            // Arrange
            var requestToken = CancellationToken.None;
            var expected = new { data = new[] { new { id = "u1" } } };
            var service = new StubService((c) => Task.FromResult<object>(expected));

            // Act
            var result = await service.GetUsersAsync(requestToken);

            // Assert
            Assert.Same(expected, result);
        }

        [Fact]
        public async Task GetUsersAsync_CanBeImplementedForFailurePath()
        {
            // Arrange
            var requestToken = CancellationToken.None;
            var service = new StubService((c) => Task.FromException<object>(new InvalidOperationException("bad")));

            // Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(requestToken));

            // Assert
            Assert.Equal("bad", thrown.Message);
        }

        [Fact]
        public async Task CreateUserAsync_CanBeImplementedForSuccessPath()
        {
            // Arrange
            var request = new CreateUserRequest();
            var requestToken = CancellationToken.None;
            var expected = new { id = "u2" };
            var service = new StubService(null, (r, c) => Task.FromResult<object>(expected));

            // Act
            var result = await service.CreateUserAsync(request, requestToken);

            // Assert
            Assert.Same(expected, result);
        }

        [Fact]
        public async Task CreateUserAsync_CanBeImplementedForFailurePath()
        {
            // Arrange
            var request = new CreateUserRequest();
            var requestToken = CancellationToken.None;
            var service = new StubService(null, (r, c) => Task.FromException<object>(new ArgumentNullException("request")));

            // Act
            var thrown = await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateUserAsync(request, requestToken));

            // Assert
            Assert.Equal("request", thrown.ParamName);
        }

        [Fact]
        public async Task UpdateUserAsync_CanBeImplementedForSuccessPath()
        {
            // Arrange
            var id = "123";
            var request = new UpdateUserRequest();
            var requestToken = CancellationToken.None;
            var expected = new { updated = true };
            var service = new StubService(null, null, (i, r, c) => Task.FromResult<object>(expected));

            // Act
            var result = await service.UpdateUserAsync(id, request, requestToken);

            // Assert
            Assert.Same(expected, result);
        }

        [Fact]
        public async Task UpdateUserAsync_CanBeImplementedForFailurePath()
        {
            // Arrange
            var id = "123";
            var request = new UpdateUserRequest();
            var requestToken = CancellationToken.None;
            var service = new StubService(null, null, (i, r, c) => Task.FromException<object>(new InvalidOperationException("update failed")));

            // Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUserAsync(id, request, requestToken));

            // Assert
            Assert.Equal("update failed", thrown.Message);
        }

        private sealed class StubService : IZohoCrmService
        {
            private readonly Func<CancellationToken, Task<object>>? _getUsers;
            private readonly Func<CreateUserRequest, CancellationToken, Task<object>>? _createUser;
            private readonly Func<string, UpdateUserRequest, CancellationToken, Task<object>>? _updateUser;

            public StubService(
                Func<CancellationToken, Task<object>>? getUsers = null,
                Func<CreateUserRequest, CancellationToken, Task<object>>? createUser = null,
                Func<string, UpdateUserRequest, CancellationToken, Task<object>>? updateUser = null)
            {
                _getUsers = getUsers;
                _createUser = createUser;
                _updateUser = updateUser;
            }

            public Task<object> GetUsersAsync(CancellationToken cancellationToken)
            {
                if (_getUsers == null)
                {
                    throw new NotImplementedException();
                }

                return _getUsers(cancellationToken);
            }

            public Task<object> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
            {
                if (_createUser == null)
                {
                    throw new NotImplementedException();
                }

                return _createUser(request, cancellationToken);
            }

            public Task<object> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
            {
                if (_updateUser == null)
                {
                    throw new NotImplementedException();
                }

                return _updateUser(id, request, cancellationToken);
            }
        }
    }
}