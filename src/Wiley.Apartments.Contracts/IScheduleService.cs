using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IScheduleService
{
    Task<ScheduledItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledItem>> QueryAsync(
        Guid? unitId = null,
        ScheduledItemCategory? category = null,
        DateTime? rangeStartUtc = null,
        DateTime? rangeEndUtc = null,
        bool includeCompleted = true,
        CancellationToken cancellationToken = default);

    Task<ScheduledItem> CreateAsync(
        string title,
        ScheduledItemCategory category,
        DateTime startUtc,
        DateTime? endUtc = null,
        DateTime? dueUtc = null,
        TimeSpan? reminderOffset = null,
        Guid? unitId = null,
        Guid? tenantId = null,
        Guid? leaseId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ScheduledItem> UpdateAsync(
        Guid id,
        string title,
        ScheduledItemCategory category,
        DateTime startUtc,
        DateTime? endUtc = null,
        DateTime? dueUtc = null,
        TimeSpan? reminderOffset = null,
        Guid? unitId = null,
        Guid? tenantId = null,
        Guid? leaseId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ScheduledItem> CompleteAsync(
        Guid id,
        DateTime? completedUtc = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
