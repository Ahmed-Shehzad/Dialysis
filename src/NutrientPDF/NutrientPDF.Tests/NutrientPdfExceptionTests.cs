using Shouldly;
using Xunit;

namespace NutrientPDF.Tests;

public sealed class NutrientPdfExceptionTests
{
    [Fact]
    public void FromStatus_creates_exception_with_message_and_status()
    {
        var ex = NutrientPdfException.FromStatus("TestOp", "SomeError");
        ex.Message.ShouldContain("TestOp");
        ex.Message.ShouldContain("SomeError");
        ex.GdPictureStatus.ShouldBe("SomeError");
    }

    [Fact]
    public void Constructor_with_message_sets_message()
    {
        var ex = new NutrientPdfException("Test message");
        ex.Message.ShouldBe("Test message");
        ex.GdPictureStatus.ShouldBeNull();
    }
}
