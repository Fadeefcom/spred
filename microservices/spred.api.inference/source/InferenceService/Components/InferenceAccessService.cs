using AutoMapper;
using InferenceService.Abstractions;
using InferenceService.Models.Dto;
using InferenceService.Models.Entities;

namespace InferenceService.Components;

/// <inheritdoc />
public class InferenceAccessService : IInferenceAccessService
{
    private readonly ILogger<IInferenceAccessService> _logger;
    private readonly IMapper _mapper;

    private const int FreeVisible = 5;
    private const int FreeHide = 5;
    private const int PremiumWeeklyUnlockMin = 5;
    private const int PremiumWeeklyUnlockMax = 10;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="mapper">Auto mapper.</param>
    public InferenceAccessService(ILoggerFactory loggerFactory, IMapper mapper)
    {
        _logger = loggerFactory.CreateLogger<IInferenceAccessService>();
        _mapper = mapper;
    }

    /// <inheritdoc />
    public (List<InferenceMetadataDto> dtos, bool updated) ApplyVisibilityRulesAsync(
        Guid userId,
        bool isPremium,
        InferenceResult inference)
    {
        bool updated = false;
        var metadata = inference.Metadata.OrderByDescending(x => x.Score).ToList();

        if (!isPremium)
        {
            List<InferenceMetadataDto> dtos = new();
            foreach(var m in metadata)
            {
                if (dtos.Count < FreeVisible)
                {
                    if (m.IsLocked)
                    {
                        m.IsLocked = false;
                        m.UnlockDate = DateTime.Now;
                        updated = true;
                    }
                    
                    dtos.Add(_mapper.Map<InferenceMetadataDto>(m));
                }
                else if (dtos.Count < FreeHide + FreeVisible)
                {
                    var mapped = _mapper.Map<InferenceMetadataDto>(m);
                    mapped.MetadataId = Guid.Empty;
                    mapped.MetadataOwner = Guid.Empty;
                    dtos.Add(mapped);
                }
            }

            return (dtos, updated);
        }
        
        var locked = metadata.Where(m => m.IsLocked).ToList();
        if (locked.Count == 0)
            return (_mapper.Map<List<InferenceMetadataDto>>(metadata), false);

        var now = DateTime.UtcNow;
        var createdAt = DateTimeOffset.FromUnixTimeSeconds(inference.Timestamp).UtcDateTime;
        var lastUnlock = inference.LastUnlock ?? createdAt;

        var weeksPassed = (int)Math.Floor((now - lastUnlock).TotalDays / 7);
        if (weeksPassed <= 0 && inference.LastUnlock != null)
            return (_mapper.Map<List<InferenceMetadataDto>>(metadata.Where(m => !m.IsLocked).ToList()), false);

        weeksPassed = Math.Max(weeksPassed, 1);
        var random = new Random();
        var totalToUnlock = 0;

        for (int i = 0; i < weeksPassed; i++)
            totalToUnlock += random.Next(PremiumWeeklyUnlockMin, PremiumWeeklyUnlockMax + 1);

        var toUnlock = locked
            .OrderBy(_ => random.Next())
            .Take(Math.Min(totalToUnlock, locked.Count))
            .ToList();

        if (toUnlock.Count == 0)
            return (_mapper.Map<List<InferenceMetadataDto>>(metadata.Where(m => !m.IsLocked).ToList()), false);

        foreach (var m in toUnlock)
        {
            m.IsLocked = false;
            m.WasUnlockedByPremium = true;
            m.UnlockDate = now;
            updated = true;
        }

        if (updated)
        {
            inference.LastUnlock = now;
            _logger.LogInformation(
                "Premium user {UserId}: unlocked {Count} metadata items after {Weeks} week(s)",
                userId,
                toUnlock.Count,
                weeksPassed);
        }

        return (_mapper.Map<List<InferenceMetadataDto>>(metadata.Where(m => !m.IsLocked).ToList()), updated);
    }
}