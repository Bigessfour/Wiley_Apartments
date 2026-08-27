using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class FacilityReservationServiceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 11, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private static ApartmentsDbContext CreateDb()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Confirm_RejectsOverlappingConfirmedBookings()
    {
        await using var db = CreateDb();
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Number = "CC",
            IsFacility = true,
            Status = UnitStatus.Vacant,
            RowVersion = Guid.NewGuid()
        };
        var renter = new FacilityRenter
        {
            Id = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B",
            Phone = "1",
            Email = "a@b.c",
            MailingAddress = "1 Main",
            RowVersion = Guid.NewGuid()
        };
        db.Units.Add(unit);
        db.FacilityRenters.Add(renter);
        await db.SaveChangesAsync();

        var service = new FacilityReservationService(db, new FixedClock(), NullLogger<FacilityReservationService>.Instance);
        var start = DateTime.UtcNow.Date.AddDays(1);
        var end = start.AddHours(4);

        await service.CreateAsync(new FacilityReservation
        {
            UnitId = unit.Id,
            FacilityRenterId = renter.Id,
            StartUtc = start,
            EndUtc = end,
            Status = FacilityReservationStatus.Confirmed,
            RentalFee = 100,
            DepositAmount = 50
        });

        var act = async () => await service.CreateAsync(new FacilityReservation
        {
            UnitId = unit.Id,
            FacilityRenterId = renter.Id,
            StartUtc = start.AddHours(1),
            EndUtc = end.AddHours(1),
            Status = FacilityReservationStatus.Confirmed,
            RentalFee = 100,
            DepositAmount = 50
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*overlaps*");
    }

    [Fact]
    public async Task Confirm_AllowsKitchenAndHallAtTheSameTime()
    {
        await using var db = CreateDb();
        var (unit, renter, service, start, end) = await SeedCcAsync(db);

        await service.CreateAsync(new FacilityReservation
        {
            UnitId = unit.Id,
            FacilityRenterId = renter.Id,
            StartUtc = start,
            EndUtc = end,
            Space = FacilitySpace.Kitchen,
            Status = FacilityReservationStatus.Confirmed,
            RentalFee = 75,
            DepositAmount = 75
        });

        var hall = await service.CreateAsync(new FacilityReservation
        {
            UnitId = unit.Id,
            FacilityRenterId = renter.Id,
            StartUtc = start,
            EndUtc = end,
            Space = FacilitySpace.MainHall,
            Status = FacilityReservationStatus.Confirmed,
            RentalFee = 150,
            DepositAmount = 100
        });

        hall.Space.Should().Be(FacilitySpace.MainHall);
        (await db.ScheduledItems.CountAsync(s =>
            s.FacilityReservationId != null && !s.IsDeleted)).Should().Be(2);
    }

    [Fact]
    public async Task Confirm_RejectsKitchenWhenWholeBuildingIsBooked()
    {
        await using var db = CreateDb();
        var (unit, renter, service, start, end) = await SeedCcAsync(db);

        await service.CreateAsync(new FacilityReservation
        {
            UnitId = unit.Id,
            FacilityRenterId = renter.Id,
            StartUtc = start,
            EndUtc = end,
            Space = FacilitySpace.WholeBuilding,
            Status = FacilityReservationStatus.Confirmed,
            RentalFee = 250,
            DepositAmount = 150
        });

        var act = async () => await service.CreateAsync(new FacilityReservation
        {
            UnitId = unit.Id,
            FacilityRenterId = renter.Id,
            StartUtc = start,
            EndUtc = end,
            Space = FacilitySpace.Kitchen,
            Status = FacilityReservationStatus.Confirmed,
            RentalFee = 75,
            DepositAmount = 75
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*overlaps*");
    }

    [Fact]
    public void CalendarLabel_UsesHallAndEntireFacility()
    {
        FacilitySpaceInfo.CalendarLabel(FacilitySpace.MainHall).Should().Be("Hall");
        FacilitySpaceInfo.CalendarLabel(FacilitySpace.WholeBuilding).Should().Be("Entire Facility");
        FacilitySpaceInfo.DisplayName(FacilitySpace.WholeBuilding).Should().Be("Entire Facility");
        FacilitySpaceInfo.Conflicts(FacilitySpace.Kitchen, FacilitySpace.MainHall).Should().BeFalse();
        FacilitySpaceInfo.Conflicts(FacilitySpace.Kitchen, FacilitySpace.Kitchen).Should().BeTrue();
        FacilitySpaceInfo.Conflicts(FacilitySpace.WholeBuilding, FacilitySpace.FireplaceRoom).Should().BeTrue();
    }

    private static async Task<(Unit unit, FacilityRenter renter, FacilityReservationService service, DateTime start, DateTime end)>
        SeedCcAsync(ApartmentsDbContext db)
    {
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Number = "CC",
            IsFacility = true,
            Status = UnitStatus.Vacant,
            RowVersion = Guid.NewGuid()
        };
        var renter = new FacilityRenter
        {
            Id = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B",
            Phone = "1",
            Email = "a@b.c",
            MailingAddress = "1 Main",
            RowVersion = Guid.NewGuid()
        };
        db.Units.Add(unit);
        db.FacilityRenters.Add(renter);
        await db.SaveChangesAsync();
        var service = new FacilityReservationService(db, new FixedClock(), NullLogger<FacilityReservationService>.Instance);
        var start = DateTime.UtcNow.Date.AddDays(1);
        var end = start.AddHours(4);
        return (unit, renter, service, start, end);
    }

    [Theory]
    [InlineData(FacilityReservationStatus.Completed, FacilityReservationStatus.Confirmed)]
    [InlineData(FacilityReservationStatus.Cancelled, FacilityReservationStatus.Confirmed)]
    [InlineData(FacilityReservationStatus.Draft, FacilityReservationStatus.Completed)]
    [InlineData(FacilityReservationStatus.Request, FacilityReservationStatus.Completed)]
    public void EnsureTransitionAllowed_RejectsIllegalMoves(
        FacilityReservationStatus from,
        FacilityReservationStatus to)
    {
        var act = () => FacilityReservationService.EnsureTransitionAllowed(from, to);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot change*");
    }

    [Theory]
    [InlineData(FacilityReservationStatus.Draft, FacilityReservationStatus.Confirmed)]
    [InlineData(FacilityReservationStatus.Request, FacilityReservationStatus.Confirmed)]
    [InlineData(FacilityReservationStatus.Confirmed, FacilityReservationStatus.Cancelled)]
    [InlineData(FacilityReservationStatus.Confirmed, FacilityReservationStatus.Completed)]
    [InlineData(FacilityReservationStatus.Completed, FacilityReservationStatus.Completed)]
    public void EnsureTransitionAllowed_AllowsLegalMoves(
        FacilityReservationStatus from,
        FacilityReservationStatus to)
    {
        var act = () => FacilityReservationService.EnsureTransitionAllowed(from, to);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SetStatusAsync_RejectsConfirmAfterCompleted()
    {
        await using var db = CreateDb();
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Number = "CC",
            IsFacility = true,
            Status = UnitStatus.Vacant,
            RowVersion = Guid.NewGuid()
        };
        var renter = new FacilityRenter
        {
            Id = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B",
            Phone = "1",
            Email = "a@b.c",
            MailingAddress = "1 Main",
            RowVersion = Guid.NewGuid()
        };
        var reservation = new FacilityReservation
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            FacilityRenterId = renter.Id,
            StartUtc = DateTime.UtcNow.Date.AddDays(2),
            EndUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(4),
            Status = FacilityReservationStatus.Completed,
            RentalFee = 100,
            DepositAmount = 50,
            RowVersion = Guid.NewGuid()
        };
        db.Units.Add(unit);
        db.FacilityRenters.Add(renter);
        db.FacilityReservations.Add(reservation);
        await db.SaveChangesAsync();

        var service = new FacilityReservationService(db, new FixedClock(), NullLogger<FacilityReservationService>.Instance);
        var act = async () => await service.SetStatusAsync(reservation.Id, FacilityReservationStatus.Confirmed);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot change*");
    }
}

public class FacilityRentalAgreementGeneratorTests
{
    [Fact]
    public void Generate_ProducesNonEmptyPdf()
    {
        var gen = new FacilityRentalAgreementGenerator();
        var bytes = gen.Generate(new FacilityRentalAgreementData(
            "Morgan Ellis",
            "Ellis event",
            "100 Demo St",
            "555",
            "a@b.c",
            "2026-08-01 10:00",
            "2026-08-01 14:00",
            "Main Space (Hall)",
            "5 x Banquet tables",
            "$150.00",
            "$100.00",
            "No alcohol",
            "2026-08-11 12:00"));
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
        var text = System.Text.Encoding.Latin1.GetString(bytes);
        text.Should().Contain("Community Center Rental Agreement");
        text.Should().Contain("Morgan Ellis");
        text.Should().Contain("No alcohol");
    }
}

public class FacilityInspectionServiceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 11, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Create_RequiresDamageNotesWhenUnsatisfactory()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();

        var unit = new Unit
        {
            Id = Guid.NewGuid(), Number = "CC", IsFacility = true, Status = UnitStatus.Vacant, RowVersion = Guid.NewGuid()
        };
        var renter = new FacilityRenter
        {
            Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Phone = "1", Email = "a@b.c",
            MailingAddress = "x", RowVersion = Guid.NewGuid()
        };
        var reservation = new FacilityReservation
        {
            Id = Guid.NewGuid(), UnitId = unit.Id, FacilityRenterId = renter.Id,
            StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(2),
            Status = FacilityReservationStatus.Confirmed, RowVersion = Guid.NewGuid()
        };
        db.Units.Add(unit);
        db.FacilityRenters.Add(renter);
        db.FacilityReservations.Add(reservation);
        await db.SaveChangesAsync();

        var service = new FacilityInspectionService(
            db, new FixedClock(), NullLogger<FacilityInspectionService>.Instance);

        var act = async () => await service.CreateAsync(new FacilityInspection
        {
            FacilityReservationId = reservation.Id,
            Type = FacilityInspectionType.PostRental,
            IsSatisfactory = false,
            InspectorDisplay = "Clerk"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Damage*");
    }
}

public class FacilityLedgerBalanceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 11, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetFacilityBalance_ScopesToRenterAndIgnoresOtherRentersOnSameUnit()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();

        var unit = new Unit
        {
            Id = Guid.NewGuid(), Number = "CC", IsFacility = true, Status = UnitStatus.Vacant, RowVersion = Guid.NewGuid()
        };
        var renterA = new FacilityRenter
        {
            Id = Guid.NewGuid(), FirstName = "A", LastName = "One", Phone = "1", Email = "a@b.c",
            MailingAddress = "x", RowVersion = Guid.NewGuid()
        };
        var renterB = new FacilityRenter
        {
            Id = Guid.NewGuid(), FirstName = "B", LastName = "Two", Phone = "2", Email = "b@b.c",
            MailingAddress = "y", RowVersion = Guid.NewGuid()
        };
        var resA = new FacilityReservation
        {
            Id = Guid.NewGuid(), UnitId = unit.Id, FacilityRenterId = renterA.Id,
            StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(2),
            Status = FacilityReservationStatus.Confirmed, RentalFee = 150, DepositAmount = 100,
            RowVersion = Guid.NewGuid()
        };
        var resB = new FacilityReservation
        {
            Id = Guid.NewGuid(), UnitId = unit.Id, FacilityRenterId = renterB.Id,
            StartUtc = DateTime.UtcNow.AddDays(1), EndUtc = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = FacilityReservationStatus.Confirmed, RentalFee = 200, DepositAmount = 50,
            RowVersion = Guid.NewGuid()
        };
        db.Units.Add(unit);
        db.FacilityRenters.AddRange(renterA, renterB);
        db.FacilityReservations.AddRange(resA, resB);
        await db.SaveChangesAsync();

        var lateFees = new LateFeeSettingsService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Wiley.Apartments.Web.Configuration.ClerkSuiteOptions()),
            NullLogger<LateFeeSettingsService>.Instance);
        var ledger = new LedgerService(db, lateFees, new FixedClock(), NullLogger<LedgerService>.Instance);

        await ledger.PostFacilityChargeAsync(renterA.Id, unit.Id, resA.Id, 100m, DateTime.UtcNow, isDeposit: true);
        await ledger.PostFacilityChargeAsync(renterA.Id, unit.Id, resA.Id, 150m, DateTime.UtcNow);
        await ledger.PostFacilityPaymentAsync(renterA.Id, unit.Id, resA.Id, 250m, DateTime.UtcNow, PaymentMethod.Cash);
        await ledger.PostFacilityChargeAsync(renterB.Id, unit.Id, resB.Id, 999m, DateTime.UtcNow);

        (await ledger.GetFacilityBalanceAsync(renterA.Id, resA.Id)).Should().Be(0m);
        (await ledger.GetFacilityBalanceAsync(renterA.Id)).Should().Be(0m);
        (await ledger.HasFacilityChargesAsync(resA.Id)).Should().BeTrue();
        (await ledger.HasFacilityPaymentsAsync(resA.Id)).Should().BeTrue();
        (await ledger.HasFacilityPaymentsAsync(resB.Id)).Should().BeFalse();
    }
}
