using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Services;
using Extensions.Interfaces;
using Extensions.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using AutoMapper;
using MassTransit;
using StackExchange.Redis;

namespace Authorization.Test.Mocks;

public static class MockBaseManagerServices
{
    private static readonly Mock<ILogger<UserManager<BaseUser>>> _loggerMock = new();

    public static BaseManagerServices CreateMock(Mock<IUserPlusStore> userStoreMock, Mock<IGetToken> getTokenMock,
        Mock<IConfiguration> configurationMock)
    {
        var identityOptions = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var serviceOptions = Microsoft.Extensions.Options.Options.Create(new ServicesOuterOptions
        {
            AggregatorService = "https://example.com",
            PlaylistService = "https://example.com",
            AuthorizationService = "https://example.com",
            InferenceService = "https://example.com",
            TrackService = "https://example.com",
            UiEndpoint = "https://example.com",
            VectorService = "https://example.com",
            SubscriptionService = "https://example.com"
        });

        var providerMock = new Mock<IServiceProvider>();

        return new BaseManagerServices(
            userStoreMock.Object,
            getTokenMock.Object,
            identityOptions,
            serviceOptions,
            configurationMock.Object,
            Mock.Of<IPasswordHasher<BaseUser>>(),
            [],
            [],
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            providerMock.Object,
            _loggerMock.Object,
            Mock.Of<IPublishEndpoint>(),
            Mock.Of<IConnectionMultiplexer>(m =>
                m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()) == Mock.Of<IDatabase>()),
            Mock.Of<IRoleClaimStore<BaseRole>>(),
            Mock.Of<ILinkedAccountEventStore>(),
            Mock.Of<IMapper>()
        );
    }
}
