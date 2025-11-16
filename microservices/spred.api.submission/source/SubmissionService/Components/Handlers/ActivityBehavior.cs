using MediatR;
using Spred.Bus.Abstractions;
using SubmissionService.Abstractions;
using SubmissionService.Models;
using ActivityWriterExtensions = Spred.Bus.Extensions.ActivityWriterExtensions;

namespace SubmissionService.Components.Handlers;

/// <summary>
/// MediatR pipeline behavior that intercepts command execution
/// and writes activity records for commands implementing
/// <see cref="IAuditableCommand{TResult}"/>.
/// </summary>
/// <typeparam name="TRequest">
/// The type of the request handled by the pipeline.
/// Must implement <see cref="IRequest{TResponse}"/>.
/// </typeparam>
/// <typeparam name="TResponse">
/// The type of the response returned by the request handler.
/// </typeparam>
/// <remarks>
/// This behavior is executed after the request handler has completed
/// and uses <see cref="IAuditableCommand{TResult}.ToActivities"/> to produce
/// one or more <see cref="ActivityDescriptor"/> instances.
/// Each descriptor is written using <see cref="IActivityWriter"/>.
/// </remarks>
public sealed class ActivityBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IActorProvider _actor;
    private readonly IActivityWriter _audit;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="actor">The provider for retrieving actor and correlation metadata.</param>
    /// <param name="audit">The writer responsible for persisting activity records.</param>
    public ActivityBehavior(IActorProvider actor, IActivityWriter audit)
    {
        _actor = actor;
        _audit = audit;
    }

    /// <summary>
    /// Handles the execution of the pipeline behavior.
    /// If the request implements <see cref="IAuditableCommand{TResult}"/>,
    /// generates activity descriptors from the handler result and writes
    /// them using the configured activity writer.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The next delegate in the MediatR pipeline.</param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>
    /// The response produced by the next request handler in the pipeline.
    /// </returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (request is IAuditableCommand<TResponse> auditable)
        {
            foreach (var d in auditable.ToActivities(response))
            {
                await ActivityWriterExtensions.WriteAsync(
                    _audit,
                    _actor,
                    verb: d.Verb,
                    objectType: d.ObjectType,
                    objectId: d.ObjectId,
                    messageKey: d.MessageKey,
                    messageArgs: d.Args,
                    before: d.Before,
                    after: d.After,
                    service: nameof(SubmissionService),
                    importance: d.Importance,
                    ownerUserId: d.OwnerUserId,
                    otherPartyUserId: d.OtherPartyUserId,
                    tags: d.Tags,
                    cancellationToken: cancellationToken
                );
            }
        }

        return response;
    }
}