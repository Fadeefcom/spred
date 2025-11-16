using System.Text.RegularExpressions;
using Authorization.Configuration;
using Authorization.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Repository.Abstractions.Interfaces;

namespace Authorization.Validators;

/// <inheritdoc />
public class BaseRoleValidator : IRoleValidator<BaseRole>
{
    private readonly IPersistenceStore<BaseRole, Guid> _roles;
    private readonly ILookupNormalizer _normalizer;
    private readonly RoleValidationOptions _options;
    private readonly ILogger<BaseRoleValidator> _logger;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="roles"></param>
    /// <param name="normalizer"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public BaseRoleValidator(
        IPersistenceStore<BaseRole, Guid> roles,
        ILookupNormalizer normalizer,
        IOptions<RoleValidationOptions> options,
        ILogger<BaseRoleValidator> logger)
    {
        _roles = roles;
        _normalizer = normalizer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IdentityResult> ValidateAsync(RoleManager<BaseRole> manager, BaseRole role)
    {
        var errors = new List<IdentityError>();
        var name = role.Name ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            errors.Add(new IdentityError { Code = nameof(IdentityErrorDescriber.InvalidRoleName), Description = "Role name is required." });

        if (name.Length < _options.MinNameLength || name.Length > _options.MaxNameLength)
            errors.Add(new IdentityError { Code = nameof(IdentityErrorDescriber.InvalidRoleName), Description = $"Role name length must be between {_options.MinNameLength} and {_options.MaxNameLength}." });

        if (!string.IsNullOrEmpty(_options.AllowedNameRegex) && !Regex.IsMatch(name, _options.AllowedNameRegex))
            errors.Add(new IdentityError { Code = nameof(IdentityErrorDescriber.InvalidRoleName), Description = "Role name contains invalid characters." });

        if (errors.Count > 0)
            return Task.FromResult(IdentityResult.Failed(errors.ToArray()));

        var normalized = _normalizer.NormalizeName(name);
        role.NormalizedName = normalized;

        return Task.FromResult(IdentityResult.Success);
    }
}