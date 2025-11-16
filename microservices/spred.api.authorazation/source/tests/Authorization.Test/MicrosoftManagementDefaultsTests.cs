using Authorization.Options.AuthenticationSchemes;
using FluentAssertions;

namespace Authorization.Test;

public class MicrosoftManagementDefaultsTests
{
    [Fact]
    public void AuthenticationScheme_Should_Be_Correct()
    {
        MicrosoftManagementDefaults.AuthenticationScheme.Should().Be("MicrosoftManagement");
    }

    [Fact]
    public void AuthorizationEndpoint_Should_Be_Correct()
    {
        MicrosoftManagementDefaults.AuthorizationEndpoint.Should().Be(
            "https://login.microsoftonline.com/9af483bb-489a-4f75-bfb4-8de391e183a5/oauth2/v2.0/authorize");
    }

    [Fact]
    public void TokenEndpoint_Should_Be_Correct()
    {
        MicrosoftManagementDefaults.TokenEndpoint.Should().Be(
            "https://login.microsoftonline.com/9af483bb-489a-4f75-bfb4-8de391e183a5/oauth2/v2.0/token");
    }

    [Fact]
    public void UserInformationEndpoint_Should_Be_Correct()
    {
        MicrosoftManagementDefaults.UserInformationEndpoint.Should().Be(
            "https://graph.microsoft.com/v1.0/me");
    }

    [Fact]
    public void AllowedEmails_Should_Contain_Specified_Emails()
    {
        MicrosoftManagementDefaults.AllowedEmails.Should().Contain("gleb@spred.com");
    }
}