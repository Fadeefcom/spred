using Authorization.Configuration;

namespace Authorization.Test;

public sealed class RoleValidationOptionsTests
{
    [Fact]
    public void Properties_GetSet_Work()
    {
        var o = new RoleValidationOptions { MinNameLength = 2, MaxNameLength = 10, AllowedNameRegex = "^[A-Z]+$" };
        Assert.Equal(2, o.MinNameLength);
        Assert.Equal(10, o.MaxNameLength);
        Assert.Equal("^[A-Z]+$", o.AllowedNameRegex);
    }
}
