using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace Authorization.Configuration;

/// <summary>
/// Configures the <see cref="KeyManagementOptions"/> to use a custom XML repository for key storage.
/// </summary>
public class RedisKeyManagementOptionsSetup : IConfigureOptions<KeyManagementOptions>
{
    private readonly IXmlRepository _xmlRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisKeyManagementOptionsSetup"/> class.
    /// </summary>
    /// <param name="xmlRepository">The XML repository used for storing keys.</param>
    public RedisKeyManagementOptionsSetup(IXmlRepository xmlRepository)
    {
        _xmlRepository = xmlRepository;
    }

    /// <summary>
    /// Configures the <see cref="KeyManagementOptions"/> to use the provided XML repository.
    /// </summary>
    /// <param name="options">The <see cref="KeyManagementOptions"/> to configure.</param>
    public void Configure(KeyManagementOptions options)
    {
        options.XmlRepository = _xmlRepository;
    }
}
