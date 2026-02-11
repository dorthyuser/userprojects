using System;
using Xunit;
using AccountsFunction.Helpers;

namespace AccountsFunction.Tests.Helpers
{
    public class TryParseGuidTests
    {
        [Fact]
        public void TryParseGuid_ValidGuid_ReturnsTrueAndSetsOut()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var input = guid.ToString();

            // Act
            var result = DatabaseHelper.TryParseGuid(input, out var parsed);

            // Assert
            Assert.True(result);
            Assert.Equal(guid, parsed);
        }

        [Fact]
        public void TryParseGuid_InvalidGuid_ReturnsFalseAndReturnsEmptyGuid()
        {
            // Arrange
            var input = "not-a-guid";

            // Act
            var result = DatabaseHelper.TryParseGuid(input, out var parsed);

            // Assert
            Assert.False(result);
            Assert.Equal(Guid.Empty, parsed);
        }

        [Fact]
        public void TryParseGuid_NullOrWhitespace_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(DatabaseHelper.TryParseGuid(null!, out _));
            Assert.False(DatabaseHelper.TryParseGuid(string.Empty, out _));
            Assert.False(DatabaseHelper.TryParseGuid("   ", out _));
        }
    }
}
