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
        public async Task GetUsersAsync_InterfaceSignature_IsAvailable()
        {
            var mock = new Mock<IZohoCrmService>();
            var token = CancellationToken.None;
            var expected = new { ok = true };
            mock.Setup(m => m.GetUsersAsync(token)).ReturnsAsync(expected);

            var result = await mock.Object.GetUsersAsync(token);

            Assert.Same(expected, result);
            mock.Verify(m => m.GetUsersAsync(token), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_InterfaceSignature_IsAvailable()
        {
            var mock = new Mock<IZohoCrmService>();
            var token = CancellationToken.None;
            var request = new CreateUserRequest();
            var expected = new { created = true };
            mock.Setup(m => m.CreateUserAsync(request, token)).ReturnsAsync(expected);

            var result = await mock.Object.CreateUserAsync(request, token);

            Assert.Same(expected, result);
            mock.Verify(m => m.CreateUserAsync(request, token), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_InterfaceSignature_IsAvailable()
        {
            var mock = new Mock<IZohoCrmService>();
            var token = CancellationToken.None;
            var request = new UpdateUserRequest();
            var expected = new { updated = true };
            mock.Setup(m => m.UpdateUserAsync("123", request, token)).ReturnsAsync(expected);

            var result = await mock.Object.UpdateUserAsync("123", request, token);

            Assert.Same(expected, result);
            mock.Verify(m => m.UpdateUserAsync("123", request, token), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_InterfaceSignature_ThrowsOnSetup()
        {
            var mock = new Mock<IZohoCrmService>();
            var token = CancellationToken.None;
            var request = new UpdateUserRequest();
            mock.Setup(m => m.UpdateUserAsync("", request, token)).ThrowsAsync(new InvalidOperationException("bad"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => mock.Object.UpdateUserAsync("", request, token));
            mock.Verify(m => m.UpdateUserAsync("", request, token), Times.Once);
        }
    }
}
