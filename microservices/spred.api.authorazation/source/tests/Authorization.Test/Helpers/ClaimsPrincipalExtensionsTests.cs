using System.Security.Claims;
using Xunit;

namespace Authorization.Test.Helpers;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ShouldReturnNull_WhenClaimNotPresent()
    {
        var principal = new ClaimsPrincipal();
        var result = principal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.Null(result);
    }

    // Add more tests for claim extraction logic
}
