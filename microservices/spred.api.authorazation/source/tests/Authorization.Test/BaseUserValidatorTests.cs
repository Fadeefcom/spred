using System;
using System.Linq;
using System.Threading.Tasks;
using Authorization.Models.Entities;
using Authorization.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Authorization.Test;

public class BaseUserValidatorTests
{
    private static (BaseUserValidator validator, Mock<ILogger<BaseUserValidator>> logger, UserManager<BaseUser> userManager) CreateSut()
    {
        var logger = new Mock<ILogger<BaseUserValidator>>();
        var store = new Mock<IUserStore<BaseUser>>();
        var userManager = new Mock<UserManager<BaseUser>>(store.Object, null, null, null, null, null, null, null, null).Object;
        var validator = new BaseUserValidator(logger.Object);
        return (validator, logger, userManager);
    }

    private static BaseUser MakeUser(Guid? id = null, string? name = "john_doe", string? email = "john@example.com", string? phone = "+123-456 789")
        => new BaseUser { Id = id ?? Guid.NewGuid(), UserName = name, Email = email, PhoneNumber = phone };

    [Fact]
    public async Task ValidateAsync_Should_Succeed_For_ValidUser()
    {
        var (sut, logger, um) = CreateSut();
        var user = MakeUser();
        var res = await sut.ValidateAsync(um, user);
        Assert.True(res.Succeeded);
        logger.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<System.Exception>(), (Func<It.IsAnyType, System.Exception?, string>)It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_IdEmpty()
    {
        var (sut, logger, um) = CreateSut();
        var user = MakeUser(Guid.Empty);
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        Assert.Contains(res.Errors, e => e.Code == "UserIdRequired");
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_UserNameMissing()
    {
        var (sut, _, um) = CreateSut();
        var user = MakeUser(name: " ");
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        Assert.Contains(res.Errors, e => e.Code == "UserNameRequired");
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_UserNameTooShort()
    {
        var (sut, _, um) = CreateSut();
        var user = MakeUser(name: "ab");
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        Assert.Contains(res.Errors, e => e.Code == "UserNameTooShort");
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_UserNameTooLong()
    {
        var (sut, _, um) = CreateSut();
        var longName = new string('x', 101);
        var user = MakeUser(name: longName);
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        Assert.Contains(res.Errors, e => e.Code == "UserNameTooLong");
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_EmailTooLong()
    {
        var (sut, _, um) = CreateSut();
        var email = new string('a', 257);
        var user = MakeUser(email: email);
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        Assert.Contains(res.Errors, e => e.Code == "EmailTooLong");
        Assert.DoesNotContain(res.Errors, e => e.Code == "InvalidEmail");
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("john@")]
    [InlineData("@example.com")]
    [InlineData("john@@example.com")]
    public async Task ValidateAsync_Should_Fail_When_EmailInvalid(string badEmail)
    {
        var (sut, _, um) = CreateSut();
        var user = MakeUser(email: badEmail);
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        Assert.Contains(res.Errors, e => e.Code == "InvalidEmail");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("john.doe+tag@example.co.uk")]
    [InlineData("\"John Doe\" <john@example.com>")] // MailAddress accepts display names
    public async Task ValidateAsync_Should_Pass_Email_When_ValidOrEmpty(string? okEmail)
    {
        var (sut, _, um) = CreateSut();
        var user = MakeUser(email: okEmail);
        var res = await sut.ValidateAsync(um, user);
        Assert.True(res.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_PhoneTooLong()
    {
        var (sut, _, um) = CreateSut();
        var phone = new string('1', 21);
        var user = MakeUser(phone: phone);
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        Assert.Contains(res.Errors, e => e.Code == "PhoneTooLong");
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("+12(34)567")]
    [InlineData("+12_34_56")]
    public async Task ValidateAsync_Should_Fail_When_PhoneInvalid(string badPhone)
    {
        var (sut, _, um) = CreateSut();
        var user = MakeUser(phone: badPhone);
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        Assert.Contains(res.Errors, e => e.Code == "InvalidPhone");
    }

    [Theory]
    [InlineData("+1234567890")]
    [InlineData("+123-456 789")]
    [InlineData("123 456 789")]
    public async Task ValidateAsync_Should_Pass_When_PhoneValid(string okPhone)
    {
        var (sut, _, um) = CreateSut();
        var user = MakeUser(phone: okPhone);
        var res = await sut.ValidateAsync(um, user);
        Assert.True(res.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_Should_Accumulate_MultipleErrors()
    {
        var (sut, _, um) = CreateSut();
        var user = MakeUser(Guid.Empty, name: "  ", email: "bad", phone: "abc");
        var res = await sut.ValidateAsync(um, user);
        Assert.False(res.Succeeded);
        var codes = res.Errors.Select(e => e.Code).ToArray();
        Assert.Contains("UserIdRequired", codes);
        Assert.Contains("UserNameRequired", codes);
        Assert.Contains("InvalidEmail", codes);
        Assert.Contains("InvalidPhone", codes);
    }
}