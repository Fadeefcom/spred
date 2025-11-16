using System;
using System.Threading.Tasks;
using Authorization.Configuration;
using Authorization.Models.Entities;
using Authorization.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;

namespace Authorization.Test;

public sealed class BaseRoleValidatorTests
{
    private static BaseRoleValidator CreateSut(RoleValidationOptions opts, out Mock<ILookupNormalizer> normalizerMock)
    {
        var roles = new Mock<IPersistenceStore<BaseRole, Guid>>().Object;
        normalizerMock = new Mock<ILookupNormalizer>();
        normalizerMock
            .Setup(n => n.NormalizeName(It.IsAny<string>()))
            .Returns<string>(s => s.ToUpperInvariant());

        var options = Microsoft.Extensions.Options.Options.Create(opts);
        var logger = new Mock<ILogger<BaseRoleValidator>>().Object;

        return new BaseRoleValidator(roles, normalizerMock.Object, options, logger);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_ShouldFail_WhenNameMissingOrWhitespace(string? name)
    {
        var opts = new RoleValidationOptions { MinNameLength = 3, MaxNameLength = 20, AllowedNameRegex = "^[A-Za-z0-9_-]+$" };
        var sut = CreateSut(opts, out _);
        var role = new BaseRole { Name = name };

        var result = await sut.ValidateAsync(null!, role);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == nameof(IdentityErrorDescriber.InvalidRoleName));
    }

    [Theory]
    [InlineData("A", 3, 20)]
    [InlineData("AB", 3, 20)]
    public async Task Validate_ShouldFail_WhenNameTooShort(string name, int min, int max)
    {
        var opts = new RoleValidationOptions { MinNameLength = min, MaxNameLength = max, AllowedNameRegex = "^[A-Za-z0-9_-]+$" };
        var sut = CreateSut(opts, out _);
        var role = new BaseRole { Name = name };

        var result = await sut.ValidateAsync(null!, role);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == nameof(IdentityErrorDescriber.InvalidRoleName));
    }

    [Theory]
    [InlineData(21)]
    [InlineData(50)]
    public async Task Validate_ShouldFail_WhenNameTooLong(int length)
    {
        var name = new string('A', length);
        var opts = new RoleValidationOptions { MinNameLength = 3, MaxNameLength = 20, AllowedNameRegex = "^[A-Za-z0-9_-]+$" };
        var sut = CreateSut(opts, out _);
        var role = new BaseRole { Name = name };

        var result = await sut.ValidateAsync(null!, role);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == nameof(IdentityErrorDescriber.InvalidRoleName));
    }

    [Theory]
    [InlineData("Admin!")]
    [InlineData("Role Name With Space")]
    public async Task Validate_ShouldFail_WhenRegexDoesNotMatch(string name)
    {
        var opts = new RoleValidationOptions { MinNameLength = 3, MaxNameLength = 20, AllowedNameRegex = "^[A-Za-z0-9_-]+$" };
        var sut = CreateSut(opts, out _);
        var role = new BaseRole { Name = name };

        var result = await sut.ValidateAsync(null!, role);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == nameof(IdentityErrorDescriber.InvalidRoleName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_ShouldIgnoreRegex_WhenPatternNullOrEmpty(string? pattern)
    {
        var opts = new RoleValidationOptions { MinNameLength = 3, MaxNameLength = 20, AllowedNameRegex = pattern };
        var sut = CreateSut(opts, out var normalizer);
        var role = new BaseRole { Name = "Admin_01" };

        var result = await sut.ValidateAsync(null!, role);

        Assert.True(result.Succeeded);
        normalizer.Verify(n => n.NormalizeName("Admin_01"), Times.Once);
        Assert.Equal("ADMIN_01", role.NormalizedName);
    }

    [Fact]
    public async Task Validate_ShouldSucceed_AndSetNormalizedName()
    {
        var opts = new RoleValidationOptions { MinNameLength = 3, MaxNameLength = 20, AllowedNameRegex = "^[A-Za-z0-9_-]+$" };
        var sut = CreateSut(opts, out var normalizer);
        var role = new BaseRole { Name = "Admin" };

        var result = await sut.ValidateAsync(null!, role);

        Assert.True(result.Succeeded);
        normalizer.Verify(n => n.NormalizeName("Admin"), Times.Once);
        Assert.Equal("ADMIN", role.NormalizedName);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    public async Task Validate_ShouldSucceed_OnBoundaryLengths(int boundary)
    {
        var name = new string('A', boundary);
        var opts = new RoleValidationOptions { MinNameLength = 3, MaxNameLength = 20, AllowedNameRegex = "^[A-Za-z0-9_-]+$" };
        var sut = CreateSut(opts, out _);
        var role = new BaseRole { Name = name };

        var result = await sut.ValidateAsync(null!, role);

        Assert.True(result.Succeeded);
        Assert.Equal(name.ToUpperInvariant(), role.NormalizedName);
    }
}
