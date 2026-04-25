// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using ZohoProject2.Models;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class IZohoCrmServiceTests
    {
        [Fact]
        public async Task GetUsersAsync_InterfaceContract_CanBeMockedForSuccess()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            var expected = new[] { new { id = "u1" } };
            mock.Setup(s => s.GetUsersAsync(cancellationToken)).ReturnsAsync(expected);

            // Act
            var result = await mock.Object.GetUsersAsync(cancellationToken);

            // Assert
            Assert.Same(expected, result);
            mock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsersAsync_InterfaceContract_CanBeMockedForException()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            mock.Setup(s => s.GetUsersAsync(cancellationToken)).ThrowsAsync(new InvalidOperationException("error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await mock.Object.GetUsersAsync(cancellationToken));
            mock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_InterfaceContract_CanBeMockedForSuccess()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            var request = new CreateUserRequest();
            var expected = new { id = "u2" };
            mock.Setup(s => s.CreateUserAsync(request, cancellationToken)).ReturnsAsync(expected);

            // Act
            var result = await mock.Object.CreateUserAsync(request, cancellationToken);

            // Assert
            Assert.Same(expected, result);
            mock.Verify(s => s.CreateUserAsync(request, cancellationToken), Times.Once);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_InterfaceContract_CanBeMockedForException()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            var request = new CreateUserRequest();
            mock.Setup(s => s.CreateUserAsync(request, cancellationToken)).ThrowsAsync(new InvalidOperationException("error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await mock.Object.CreateUserAsync(request, cancellationToken));
            mock.Verify(s => s.CreateUserAsync(request, cancellationToken), Times.Once);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateUserAsync_InterfaceContract_CanBeMockedForSuccess()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            var id = "u1";
            var request = new UpdateUserRequest();
            var expected = new { id = "u1", updated = true };
            mock.Setup(s => s.UpdateUserAsync(id, request, cancellationToken)).ReturnsAsync(expected);

            // Act
            var result = await mock.Object.UpdateUserAsync(id, request, cancellationToken);

            // Assert
            Assert.Same(expected, result);
            mock.Verify(s => s.UpdateUserAsync(id, request, cancellationToken), Times.Once);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateUserAsync_InterfaceContract_CanBeMockedForException()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            var id = "u1";
            var request = new UpdateUserRequest();
            mock.Setup(s => s.UpdateUserAsync(id, request, cancellationToken)).ThrowsAsync(new InvalidOperationException("error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await mock.Object.UpdateUserAsync(id, request, cancellationToken));
            mock.Verify(s => s.UpdateUserAsync(id, request, cancellationToken), Times.Once);
            mock.VerifyNoOtherCalls();
        }
    }
}
