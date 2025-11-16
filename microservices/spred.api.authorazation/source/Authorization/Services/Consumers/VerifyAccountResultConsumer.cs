using Authorization.Abstractions;
using Authorization.Models.Entities;
using Extensions.Extensions;
using MassTransit;
using Spred.Bus.Contracts;

namespace Authorization.Services.Consumers;

/// <summary>
/// Consumes messages of type <see cref="VerifyAccountResult"/> and processes them
/// by verifying accounts and updating corresponding states based on verification results.
/// </summary>
/// <remarks>
/// This class interacts with user account data and linked account event stores to process verification results.
/// It appends events such as ProofAttached, AccountVerified, and AccountLinked based on the verification outcome,
/// or logs warnings when verification fails.
/// </remarks>
/// <example>
/// This consumer is typically used in a message-driven architecture to handle account verification results
/// published from other services.
/// </example>
public class VerifyAccountResultConsumer : IConsumer<VerifyAccountResult>
{
    private readonly BaseManagerServices _manager;
    private readonly ILogger<VerifyAccountResultConsumer> _logger;
    private readonly ILinkedAccountEventStore _store;

    /// <summary>
    /// Consumer that handles messages of type VerifyAccountResult. This class integrates with MassTransit
    /// to process incoming messages and manage account verification results.
    /// Dependencies:
    /// - BaseManagerServices: Provides methods to manage users and their authentication data.
    /// - ILogger: Used to log information during the message consumption process.
    /// Responsibilities:
    /// - Processes messages related to verifying account results.
    /// - Performs logic to act upon the received verification result, utilizing the BaseManagerServices.
    /// Constructor Parameters:
    /// - BaseManagerServices manager: Provides user management functionality.
    /// </summary>
    public VerifyAccountResultConsumer(ILinkedAccountEventStore store, BaseManagerServices manager, ILoggerFactory loggerFactory)
    {
        _manager = manager;
        _logger = loggerFactory.CreateLogger<VerifyAccountResultConsumer>();
        _store = store;
    }

    /// <summary>
    /// Consumes the <see cref="VerifyAccountResult"/> message and processes verification results
    /// for a user's linked account. It updates the account state and logs the outcome
    /// based on the verification status and provided proof.
    /// </summary>
    /// <param name="context">
    /// The context of the consumed message containing the verification result data.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public async Task Consume(ConsumeContext<VerifyAccountResult> context)
    {
        var result = context.Message;

        var user = await _manager.FindByIdAsync(result.UserId.ToString());
        if (user is null) return;

        var account = user.UserAccounts.FirstOrDefault(a => a.AccountId == result.AccountId);
        if (account is null) return;

        var state = await _store.GetCurrentState(account.AccountId, account.Platform, result.UserId, CancellationToken.None);
        
        if (state is null || state.Status is AccountStatus.Verified or AccountStatus.Deleted)
            return;
        
        if(result.Proof is not null)
            await _store.AppendAsync(account.AccountId, result.UserId, account.Platform, LinkedAccountEventType.ProofAttached, null, CancellationToken.None);

        if (result.Verified)
        {
            await _store.AppendAsync(account.AccountId, result.UserId, account.Platform, LinkedAccountEventType.AccountVerified, null, CancellationToken.None);
            await _store.AppendAsync(account.AccountId, result.UserId, account.Platform, LinkedAccountEventType.AccountLinked, null, CancellationToken.None);
            _logger.LogSpredInformation("AccountLinked", $"Account {result.AccountId} verified with proof.");
        }
        else
        {
            await _store.AppendAsync(account.AccountId, result.UserId, account.Platform, LinkedAccountEventType.ProofInvalid, null, CancellationToken.None);
            _logger.LogSpredWarning("AccountProofInvalid", $"Account {result.AccountId} verification failed: {result.Error}.");
        }
    }
}
