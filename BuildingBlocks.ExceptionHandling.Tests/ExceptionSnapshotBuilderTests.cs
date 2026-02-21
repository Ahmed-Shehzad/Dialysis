using BuildingBlocks.ExceptionHandling;

using Shouldly;

using Xunit;

namespace BuildingBlocks.ExceptionHandling.Tests;

public sealed class ExceptionSnapshotBuilderTests
{
    [Fact]
    public void Build_CapturesTypeMessageAndStackTrace()
    {
        var ex = new InvalidOperationException("Test message");

        ExceptionSnapshot snapshot = ExceptionSnapshotBuilder.Build(ex);

        snapshot.Type.ShouldContain("InvalidOperationException");
        snapshot.Message.ShouldBe("Test message");
        snapshot.ToStringOutput.ShouldContain("Test message");
    }

    [Fact]
    public void Build_CapturesInnerException()
    {
        var inner = new ArgumentException("Inner message");
        var ex = new InvalidOperationException("Outer message", inner);

        ExceptionSnapshot snapshot = ExceptionSnapshotBuilder.Build(ex);

        snapshot.InnerException.ShouldNotBeNull();
        snapshot.InnerException.Type.ShouldContain("ArgumentException");
        snapshot.InnerException.Message.ShouldBe("Inner message");
    }

    [Fact]
    public void Build_CapturesExceptionData()
    {
        var ex = new InvalidOperationException("Test");
        ex.Data["Key1"] = "Value1";
        ex.Data["Key2"] = 42;

        ExceptionSnapshot snapshot = ExceptionSnapshotBuilder.Build(ex);

        snapshot.Data.ShouldContainKey("Key1");
        snapshot.Data["Key1"].ShouldBe("Value1");
        snapshot.Data.ShouldContainKey("Key2");
        snapshot.Data["Key2"].ShouldBe("42");
    }

    [Fact]
    public void Build_ExceptionWithoutInner_InnerExceptionIsNull()
    {
        var ex = new InvalidOperationException("Test");

        ExceptionSnapshot snapshot = ExceptionSnapshotBuilder.Build(ex);

        snapshot.InnerException.ShouldBeNull();
    }
}
