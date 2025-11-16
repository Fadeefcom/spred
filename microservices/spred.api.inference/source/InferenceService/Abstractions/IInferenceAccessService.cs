using InferenceService.Models.Dto;
using InferenceService.Models.Entities;

namespace InferenceService.Abstractions;

/// <summary>
/// Provides an interface for applying visibility rules on inference results and metadata.
/// </summary>
public interface IInferenceAccessService
{
    /// <summary>
    /// Applies visibility rules to the given inference result and metadata based on the user's attributes and subscriptions.
    /// </summary>
    /// <param name="userId">The unique identifier of the user for whom visibility rules are being applied.</param>
    /// <param name="isPremium">A boolean that indicates whether the user has a premium subscription.</param>
    /// <param name="inference">The inference result containing detailed information about the operation.</param>
    /// <returns>A boolean indicating whether any modifications were made to the metadata during the application of visibility rules.</returns>
    (List<InferenceMetadataDto> dtos, bool updated) ApplyVisibilityRulesAsync(
        Guid userId,
        bool isPremium,
        InferenceResult inference);
}