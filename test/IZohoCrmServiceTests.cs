// GENERATED_BY_AI_TEST_ENGINE
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
        public async Task GetUsersAsync_ReturnsObject_WhenMockIsConfigured()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            var expected = new { users = new[] { new { id = "u1" } } };
            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ReturnsAsync(expected);

            // Act
            var result = await serviceMock.Object.GetUsersAsync(cancellationToken);

            // Assert
            Assert.Same(expected, result);
            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_Throws_WhenNotConfigured()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);

            // Act & Assert
            await Assert.ThrowsAsync<MockException>(async () =>
            {
                await serviceMock.Object.GetUsersAsync(CancellationToken.None);
            });
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsObject_WhenMockIsConfigured()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            var request = new CreateUserRequest();
            var expected = new { id = "u1", status = "created" };
            serviceMock
                .Setup(s => s.CreateUserAsync(request, cancellationToken))
                .ReturnsAsync(expected);

            // Act
            var result = await serviceMock.Object.CreateUserAsync(request, cancellationToken);

            // Assert
            Assert.Same(expected, result);
            serviceMock.Verify(s => s.CreateUserAsync(request, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_Throws_WhenNotConfigured()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);

            // Act & Assert
            await Assert.ThrowsAsync<MockException>(async () =>
            {
                await serviceMock.Object.CreateUserAsync(new CreateUserRequest(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsObject_WhenMockIsConfigured()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var cancellationToken = CancellationToken.None;
            var request = new UpdateUserRequest();
            var expected = new { id = "u1", status = "updated" };
            serviceMock
                .Setup(s => s.UpdateUserAsync("u1", request, cancellationToken))
                .ReturnsAsync(expected);

            // Act
            var result = await serviceMock.Object.UpdateUserAsync("u1", request, cancellationToken);

            // Assert
            Assert.Same(expected, result);
            serviceMock.Verify(s => s.UpdateUserAsync("u1", request, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_Throws_WhenNotConfigured()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);

            // Act & Assert
            await Assert.ThrowsAsync<MockException>(async () =>
            {
                await serviceMock.Object.UpdateUserAsync("u1", new UpdateUserRequest(), CancellationToken.None);
            });
        }
    }
}