using System;
using System.Linq;
using Xunit;

namespace HotelBookingService.Tests;

public sealed class ProgramTests
{
    [Fact]
    public void Program_Type_IsPresent()
    {
        var programType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => string.Equals(t.Name, "Program", StringComparison.Ordinal));

        Assert.NotNull(programType);
    }
}
