// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using System.Text.Json;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;

namespace zoho_project_csharp.Tests
{
    public class IZohoCrmServiceTests
    {
        [Fact]
        public async Task GetUsersAsync_ReturnsTuple_OnSuccess()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>();
            var users = new[] { new { id = "u1" } };
            var json = JsonSerializer.Serialize(users);
            mock.Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((200, json));

            // Act
            var result = await mock.Object.GetUsersAsync(CancellationToken.None);

            // Assert
            var status = result.StatusCode;
            var content = result.Content;
            Assert.Equal(200, status);
            Assert.Equal(json, content);
            mock.Verify(m => m.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsException_WhenConfigured()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>();
            mock.Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("err"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => mock.Object.GetUsersAsync(CancellationToken.None));
            mock.Verify(m => m.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsTuple_OnSuccess()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>();
            var request = new CreateUserRequest();
            var payload = new { created = true };
            var json = JsonSerializer.Serialize(payload);
            mock.Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(r => r == request), It.IsAny<CancellationToken>()))
                .ReturnsAsync((201, json));

            // Act
            var result = await mock.Object.CreateUserAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(201, result.StatusCode);
            Assert.Equal(json, result.Content);
            mock.Verify(m => m.CreateUserAsync(It.Is<CreateUserRequest>(r => r == request), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsException_WhenConfigured()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>();
            var request = new CreateUserRequest();
            mock.Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("bad"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => mock.Object.CreateUserAsync(request, CancellationToken.None));
            mock.Verify(m => m.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsTuple_OnSuccess()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>();
            var request = new UpdateUserRequest();
            var payload = new { updated = true };
            var json = JsonSerializer.Serialize(payload);
            var id = "123";
            mock.Setup(s => s.UpdateUserAsync(It.Is<string>(i => i == id), It.Is<UpdateUserRequest>(r => r == request), It.IsAny<CancellationToken>()))
                .ReturnsAsync((200, json));

            // Act
            var result = await mock.Object.UpdateUserAsync(id, request, CancellationToken.None);

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(json, result.Content);
            mock.Verify(m => m.UpdateUserAsync(It.Is<string>(i => i == id), It.Is<UpdateUserRequest>(r => r == request), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsException_WhenConfigured()
        {
            // Arrange
            var mock = new Mock<IZohoCrmService>();
            var request = new UpdateUserRequest();
            var id = "123";
            mock.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("nope"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => mock.Object.UpdateUserAsync(id, request, CancellationToken.None));
            mock.Verify(m => m.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
