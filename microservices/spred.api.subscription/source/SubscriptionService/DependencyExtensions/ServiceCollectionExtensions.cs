
using Extensions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Repositories;
using StackExchange.Redis;
using Stripe;
using Stripe.Checkout;
using SubscriptionService.Abstractions;
using SubscriptionService.Components;
using SubscriptionService.Configurations;
using SubscriptionService.Models.Entities;

namespace SubscriptionService.DependencyExtensions;

/// <summary>
/// Provides extension methods for registering all dependencies required by the <c>SubscriptionService</c> component.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services, persistence components, and Stripe SDK clients 
    /// necessary for handling subscription lifecycle operations.
    /// </summary>
    /// <param name="services">The service collection to which all dependencies will be added.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// Registers the Cosmos DB client and container for storing <see cref="UserSubscriptionStatus"/> entities.
    /// </item>
    /// <item>
    /// Configures the persistence layer using <see cref="IPersistenceStore{T, TKey}"/> and its implementation <see cref="PersistenceStore{T, TKey}"/>.
    /// </item>
    /// <item>
    /// Adds scoped services for subscription state management, Stripe API integration, and webhook handling.
    /// </item>
    /// <item>
    /// Registers singleton instances of Stripe SDK clients (<see cref="SessionService"/> and <see cref="Stripe.SubscriptionService"/>).
    /// </item>
    /// <item>
    /// After configuration binding, initializes the Stripe SDK with the secret key via <see cref="StripeConfiguration.ApiKey"/>.
    /// </item>
    /// </list>
    /// </remarks>
    public static void AddSubscriptionServices(this IServiceCollection services)
    {
        services.AddCosmosClient();
        services.AddContainer<SubscriptionSnapshot>([],
            excludedPaths:
            [
                new ExcludedPath()
                {
                    Path = "/RawJson/*"
                }
            ],
            uniqueKeys: [["/ExternalId", "/Kind"]],
            containerName: nameof(UserSubscriptionStatus));
        services.AddContainer<UserSubscriptionStatus>([], containerName: nameof(UserSubscriptionStatus));
        
        services.AddScoped<ISubscriptionStateStore, SubscriptionStateStore>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IWebhookHandler, StripeWebhookHandler>();
        services.AddScoped<IPersistenceStore<UserSubscriptionStatus, Guid>, PersistenceStore<UserSubscriptionStatus, Guid>>();
        services.AddScoped<IPersistenceStore<SubscriptionSnapshot, Guid>, PersistenceStore<SubscriptionSnapshot, Guid>>();
            
        services.AddSingleton<SessionService>();
        services.AddSingleton<Stripe.SubscriptionService>();
        services.AddSingleton<SessionLineItemService>();
        services.AddSingleton<RefundService>();
        services.AddSingleton<InvoiceService>();
        
        services.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
            var options = ConfigurationOptions.Parse(redisOptions.Value.ConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });
        
        services.PostConfigure<StripeOptions>(options =>
        {
            StripeConfiguration.ApiKey = options.SecretKey;
        });
    }
}