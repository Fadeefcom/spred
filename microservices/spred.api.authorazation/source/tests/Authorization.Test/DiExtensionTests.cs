using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.DiExtensions;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Services;
using Authorization.Test.Mocks;
using CloudinaryDotNet;
using Exception.Exceptions;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Authorization.Test;

public class DiExtensionTests
{
    [Fact]
    public void AddAvatarService_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Cloudinary:CloudName", "test"},
                {"Cloudinary:ApiKey", "key"},
                {"Cloudinary:ApiSecret", "secret"}
            })
            .Build();

        // Act
        services.AddAvatarService(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var cloudinary = provider.GetRequiredService<Cloudinary>();
        Assert.NotNull(cloudinary);

        var wrapper = provider.GetRequiredService<ICloudinaryWrapper>();
        Assert.IsType<CloudinaryWrapper>(wrapper);

        var avatarService = provider.GetRequiredService<IAvatarService>();
        Assert.IsType<AvatarService>(avatarService);
    }

    [Fact]
    public async Task InitTestUser_ShouldNotCreateUser_WhenExists()
    {
        // Arrange
        var userStoreMock = new Mock<IUserPlusStore>();
        var getTokenMock = new Mock<IGetToken>();
        var configMock = new Mock<IConfiguration>();

        // возвращаем существующего юзера
        userStoreMock
            .Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaseUser { Id = Guid.NewGuid(), UserName = "Existing" });

        var baseManager = MockBaseManagerServices.CreateMock(userStoreMock, getTokenMock, configMock);

        var services = new ServiceCollection();
        services.AddScoped(_ => baseManager);
        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        await scope.InitTestUser();

        // Assert: CreateAsyncByExternalIdAsync НЕ должен вызываться
        userStoreMock.Verify(s =>
            s.CreateAsync(It.IsAny<BaseUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public void AddServices_ShouldRegisterIdentityAndCustomServices()
    {
        var services = new ServiceCollection();

        // Act
        services.AddServices();
        
        Assert.NotEmpty(services);
    }
    
    
}