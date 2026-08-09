using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class ScheduleServiceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 9, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private static (ApartmentsDbContext Db, ScheduleService Service, FixedClock Clock) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        var clock = new FixedClock();
        var service = new ScheduleService(db, clock, NullLogger<ScheduleService>.Instance);
        return (db, service, clock);
    }

    private static async Task<Unit> SeedUnitAsync(ApartmentsDbContext db, string number = "4")
    {
        var unit = new Unit { Id = Guid.NewGuid(), Number = number, SqFt = 600, Beds = 1, Baths = 1 };
        db.Units.Add(unit);
        await db.SaveChangesAsync();
        return unit;
    }

    [Fact]
    public async Task CreateAsync_PersistsItem_WithCategoryAndUnit()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var unit = await SeedUnitAsync(db);
            var start = new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc);

            var item = await service.CreateAsync(
                "Turnover clean",
                ScheduledItemCategory.Cleaning,
                start,
                endUtc: start.AddHours(2),
                dueUtc: start,
                reminderOffset: TimeSpan.FromDays(1),
                unitId: unit.Id,
                notes: "After vacancy");

            item.Title.Should().Be("Turnover clean");
            item.Category.Should().Be(ScheduledItemCategory.Cleaning);
            item.UnitId.Should().Be(unit.Id);
            item.ReminderOffset.Should().Be(TimeSpan.FromDays(1));
            item.IsCompleted.Should().BeFalse();
            item.IsDeleted.Should().BeFalse();
        }
    }

    [Fact]
    public async Task UpdateAsync_ChangesTitleAndCategory()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var start = new DateTime(2026, 8, 12, 14, 0, 0, DateTimeKind.Utc);
            var created = await service.CreateAsync(
                "Inspect unit",
                ScheduledItemCategory.Inspection,
                start);

            var updated = await service.UpdateAsync(
                created.Id,
                "Final walkthrough",
                ScheduledItemCategory.Vacancy,
                start.AddHours(1),
                notes: "Keys returned");

            updated.Title.Should().Be("Final walkthrough");
            updated.Category.Should().Be(ScheduledItemCategory.Vacancy);
            updated.Notes.Should().Be("Keys returned");
            updated.StartUtc.Should().Be(start.AddHours(1));
        }
    }

    [Fact]
    public async Task CompleteAsync_SetsCompletedFlagAndTimestamp()
    {
        var (db, service, clock) = Create();
        await using (db)
        {
            var created = await service.CreateAsync(
                "Pest check",
                ScheduledItemCategory.Other,
                clock.UtcNow);

            var completed = await service.CompleteAsync(created.Id);

            completed.IsCompleted.Should().BeTrue();
            completed.CompletedUtc.Should().Be(clock.UtcNow);
        }
    }

    [Fact]
    public async Task SoftDeleteAsync_HidesFromQueryAndGet()
    {
        var (db, service, clock) = Create();
        await using (db)
        {
            var created = await service.CreateAsync(
                "Temp",
                ScheduledItemCategory.Other,
                clock.UtcNow);

            await service.SoftDeleteAsync(created.Id);

            (await service.GetByIdAsync(created.Id)).Should().BeNull();
            (await service.QueryAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task QueryAsync_FiltersByUnitCategoryAndDateRange()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var unitA = await SeedUnitAsync(db, "A1");
            var unitB = await SeedUnitAsync(db, "B2");
            var day1 = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
            var day2 = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);
            var day3 = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

            await service.CreateAsync("Clean A", ScheduledItemCategory.Cleaning, day1, unitId: unitA.Id);
            await service.CreateAsync("Vacancy A", ScheduledItemCategory.Vacancy, day2, unitId: unitA.Id);
            await service.CreateAsync("Clean B", ScheduledItemCategory.Cleaning, day2, unitId: unitB.Id);
            await service.CreateAsync("Inspect late", ScheduledItemCategory.Inspection, day3, unitId: unitA.Id);

            var byUnit = await service.QueryAsync(unitId: unitA.Id);
            byUnit.Should().HaveCount(3);

            var cleaning = await service.QueryAsync(category: ScheduledItemCategory.Cleaning);
            cleaning.Should().HaveCount(2);

            var inSeptember = await service.QueryAsync(
                unitId: unitA.Id,
                rangeStartUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                rangeEndUtc: new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc));
            inSeptember.Select(i => i.Title).Should().BeEquivalentTo("Clean A", "Vacancy A");
        }
    }

    [Fact]
    public async Task QueryAsync_CanExcludeCompleted()
    {
        var (db, service, clock) = Create();
        await using (db)
        {
            var open = await service.CreateAsync("Open", ScheduledItemCategory.Other, clock.UtcNow);
            var done = await service.CreateAsync("Done", ScheduledItemCategory.Other, clock.UtcNow);
            await service.CompleteAsync(done.Id);

            var openOnly = await service.QueryAsync(includeCompleted: false);
            openOnly.Should().ContainSingle(i => i.Id == open.Id);
        }
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownUnit()
    {
        var (db, service, clock) = Create();
        await using (db)
        {
            var act = () => service.CreateAsync(
                "Bad link",
                ScheduledItemCategory.Other,
                clock.UtcNow,
                unitId: Guid.NewGuid());

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*was not found*");
        }
    }
}
