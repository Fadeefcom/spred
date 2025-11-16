using System.Net.Mail;
using System.Text.RegularExpressions;
using Authorization.Models.Entities;
using Extensions.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Authorization.Validators;

/// <summary>
/// User entity validator
/// </summary>
public sealed class BaseUserValidator : IUserValidator<BaseUser>
{
    private readonly ILogger<BaseUserValidator> _logger;
    private readonly int _maxUserNameLength;
    private readonly int _minUserNameLength;
    private readonly int _maxEmailLength;
    private readonly int _maxPhoneLength;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="logger"></param>
    public BaseUserValidator(
        ILogger<BaseUserValidator> logger)
    {
        _logger = logger;
        _maxUserNameLength = 100;
        _minUserNameLength = 3;
        _maxEmailLength = 256;
        _maxPhoneLength = 20;
    }

    /// <inheritdoc />
    public Task<IdentityResult> ValidateAsync(UserManager<BaseUser> manager, BaseUser user)
    {
        var errors = new List<IdentityError>();
        
        if(user.Id == Guid.Empty)
            errors.Add(new IdentityError { Code = "UserIdRequired", Description = "User id is required." });

        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            errors.Add(new IdentityError { Code = "UserNameRequired", Description = "User name is required." });
        }
        else
        {
            if (user.UserName.Length < _minUserNameLength)
                errors.Add(new IdentityError { Code = "UserNameTooShort", Description = $"User name must be at least {_minUserNameLength} characters." });
            if (user.UserName.Length > _maxUserNameLength)
                errors.Add(new IdentityError { Code = "UserNameTooLong", Description = $"User name must be at most {_maxUserNameLength} characters." });
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            if (user.Email.Length > _maxEmailLength)
                errors.Add(new IdentityError { Code = "EmailTooLong", Description = $"Email must be at most {_maxEmailLength} characters." });
            else if (!IsValidEmail(user.Email))
                errors.Add(new IdentityError { Code = "InvalidEmail", Description = "Email format is invalid." });
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            if (user.PhoneNumber.Length > _maxPhoneLength)
                errors.Add(new IdentityError { Code = "PhoneTooLong", Description = $"Phone number must be at most {_maxPhoneLength} characters." });
            else if (!Regex.IsMatch(user.PhoneNumber, @"^\+?[0-9\- ]+$"))
                errors.Add(new IdentityError { Code = "InvalidPhone", Description = "Phone number format is invalid." });
        }

        if (errors.Count > 0)
        {
            _logger.LogSpredWarning("Base user validator", $"User validation failed for {user.Id}: {string.Join(',', errors)}");
            return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
        }

        return Task.FromResult(IdentityResult.Success);
    }

    private static bool IsValidEmail(string email)
    {
        try { var _ = new MailAddress(email); return true; }
        catch { return false; }
    }
}