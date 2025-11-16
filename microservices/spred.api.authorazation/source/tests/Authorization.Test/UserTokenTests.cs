using System;
using System.Linq;
using Authorization.Models.Entities;

namespace Authorization.Test;

public sealed class UserTokenTests
{
    [Fact]
    public void Ctor_ShouldGenerateId()
    {
        var token = new UserToken { UserId = Guid.NewGuid(), LoginProvider = "Google", Name = "RefreshToken", Value = "v" };
        Assert.NotEqual(Guid.Empty, token.Id);
    }

    [Fact]
    public void Defaults_ShouldHaveNullEtag_AndZeroTimestamp()
    {
        var token = new UserToken { UserId = Guid.NewGuid(), LoginProvider = "Google", Name = "AccessToken", Value = "x" };
        Assert.Null(token.ETag);
        Assert.Equal(0, token.Timestamp);
    }

    [Fact]
    public void PartitionKeyAttribute_ShouldExist_OnUserId_WithIndex0()
    {
        var prop = typeof(UserToken).GetProperty(nameof(UserToken.UserId))!;
        var cad = prop.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "PartitionKeyAttribute");
        Assert.NotNull(cad);
        Assert.Equal(0, (int)cad!.ConstructorArguments[0].Value!);
    }

    [Fact]
    public void PartitionKeyAttribute_ShouldExist_OnLoginProvider_WithIndex1()
    {
        var prop = typeof(UserToken).GetProperty(nameof(UserToken.LoginProvider))!;
        var cad = prop.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "PartitionKeyAttribute");
        Assert.NotNull(cad);
        Assert.Equal(1, (int)cad!.ConstructorArguments[0].Value!);
    }

    [Fact]
    public void Id_Etag_Timestamp_ShouldHavePrivateSetters()
    {
        var idSet = typeof(UserToken).GetProperty(nameof(UserToken.Id))!.SetMethod;
        var etagSet = typeof(UserToken).GetProperty(nameof(UserToken.ETag))!.SetMethod;
        var tsSet = typeof(UserToken).GetProperty(nameof(UserToken.Timestamp))!.SetMethod;

        Assert.True(idSet?.IsPrivate == true);
        Assert.True(etagSet?.IsPrivate == true);
        Assert.True(tsSet?.IsPrivate == true);
    }

    [Fact]
    public void Initialization_ShouldKeepAssignedValues()
    {
        var userId = Guid.NewGuid();
        var token = new UserToken { UserId = userId, LoginProvider = "GitHub", Name = "ApiToken", Value = "secret" };
        Assert.Equal(userId, token.UserId);
        Assert.Equal("GitHub", token.LoginProvider);
        Assert.Equal("ApiToken", token.Name);
        Assert.Equal("secret", token.Value);
    }
}