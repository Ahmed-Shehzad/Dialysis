namespace Transponder.Persistence.EntityFramework.Tests;

public sealed class EntityFrameworkStorageOptionsTests
{
    [Fact]
    public void Constructor_Validates_Table_Names()
    {
        _ = Assert.Throws<ArgumentException>(() => new EntityFrameworkStorageOptions(outboxTableName: " "));
        _ = Assert.Throws<ArgumentException>(() => new EntityFrameworkStorageOptions(inboxTableName: " "));
        _ = Assert.Throws<ArgumentException>(() => new EntityFrameworkStorageOptions(scheduledMessagesTableName: " "));
        _ = Assert.Throws<ArgumentException>(() => new EntityFrameworkStorageOptions(sagaStatesTableName: " "));
    }

    [Fact]
    public void Constructor_Uses_Null_For_Empty_Schema()
    {
        var options = new EntityFrameworkStorageOptions(schema: " ");

        Assert.Null(options.Schema);
    }
}
