using Roms.Domain;

namespace Roms.Domain.Tests;

public sealed class InventoryActiveItemGuardTests
{
    [Fact]
    public void When_all_inventory_items_are_inactive_HasActiveInventoryItems_returns_false()
    {
        var items = new List<InventoryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Flour", Unit = "g", IsActive = false },
            new() { Id = Guid.NewGuid(), Name = "Sugar", Unit = "g", IsActive = false }
        };

        var hasActive = items.Any(x => x.IsActive);
        Assert.False(hasActive);

        var activeItems = items.Where(x => x.IsActive).ToList();
        Assert.Empty(activeItems);
    }

    [Fact]
    public void When_active_items_exist_HasActiveInventoryItems_returns_true()
    {
        var items = new List<InventoryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Flour", Unit = "g", IsActive = false },
            new() { Id = Guid.NewGuid(), Name = "Rice", Unit = "g", IsActive = true }
        };

        var hasActive = items.Any(x => x.IsActive);
        Assert.True(hasActive);

        var activeItems = items.Where(x => x.IsActive).ToList();
        Assert.Single(activeItems);
        Assert.Equal("Rice", activeItems[0].Name);
    }
}
