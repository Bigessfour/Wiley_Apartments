using System.Text;
using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Web.Services;

/// <summary>
/// Seeds a full pseudo-resident (24 months of occupancy/ledger) plus Community Center event renters
/// so clerks can exercise every surface. Marked with [DEMO] notes for safe identification.
/// </summary>
public sealed class DemoDataSeeder : IDemoDataSeeder
{
    public const string DemoTag = "[DEMO]";
    public const string DemoCcTag = "[DEMO-CC]";
    public const string PrimaryEmail = "jordan.reyes@wiley-demo.local";

    private readonly ApartmentsDbContext _db;
    private readonly IDocumentPathResolver _paths;
    private readonly IDateTimeService _clock;
    private readonly IUnitSeeder _unitSeeder;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        ApartmentsDbContext db,
        IDocumentPathResolver paths,
        IDateTimeService clock,
        IUnitSeeder unitSeeder,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _paths = paths;
        _clock = clock;
        _unitSeeder = unitSeeder;
        _logger = logger;
    }

    public Task<bool> IsDemoLoadedAsync(CancellationToken cancellationToken = default) =>
        _db.Tenants.AnyAsync(t => !t.IsDeleted && t.Email == PrimaryEmail, cancellationToken);

    public async Task<DemoSeedResult> SeedAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        await _unitSeeder.SeedAsync(cancellationToken);

        if (!force && await IsDemoLoadedAsync(cancellationToken))
        {
            var existing = await _db.Tenants.AsNoTracking()
                .FirstAsync(t => t.Email == PrimaryEmail, cancellationToken);
            var unitId = await _db.Occupancies.AsNoTracking()
                .Where(o => o.TenantId == existing.Id && o.EndUtc == null)
                .Select(o => o.UnitId)
                .FirstOrDefaultAsync(cancellationToken);
            return new DemoSeedResult(
                AlreadyLoaded: true,
                Forced: false,
                PrimaryTenantName: $"{existing.FirstName} {existing.LastName}",
                PrimaryTenantId: existing.Id,
                PrimaryUnitId: unitId,
                CommunityCenterRenters: await _db.Tenants.CountAsync(
                    t => !t.IsDeleted && t.Notes != null && t.Notes.Contains(DemoCcTag),
                    cancellationToken),
                LedgerEntries: await _db.LedgerEntries.CountAsync(
                    e => !e.IsDeleted && e.Notes != null && e.Notes.Contains(DemoTag),
                    cancellationToken),
                Documents: await _db.Documents.CountAsync(
                    d => !d.IsDeleted && d.OriginalFileName.Contains("demo"),
                    cancellationToken),
                Maintenance: await _db.MaintenanceRequests.CountAsync(
                    m => !m.IsDeleted && m.Notes != null && m.Notes.Contains(DemoTag),
                    cancellationToken),
                ScheduleItems: await _db.ScheduledItems.CountAsync(
                    s => !s.IsDeleted && s.Notes != null && s.Notes.Contains(DemoTag),
                    cancellationToken),
                Message: "Demo data already loaded. Use force to wipe demo rows and reseed.");
        }

        if (force)
        {
            await WipeDemoAsync(cancellationToken);
        }

        var root = await _paths.GetDocumentRootAsync(cancellationToken);
        EnsureDemoFolders(root);

        var units = await _db.Units.OrderBy(u => u.Number).ToListAsync(cancellationToken);
        var unit1 = units.First(u => u.Number == "1");
        var unit2 = units.First(u => u.Number == "2");
        var unit3 = units.FirstOrDefault(u => u.Number == "3");
        var cc = units.FirstOrDefault(u => u.IsFacility)
                 ?? throw new InvalidOperationException("Community Center facility unit missing. Restart app to seed CC.");

        EnrichResidentialUnits(units);

        // --- Primary 24-month resident ---
        var now = _clock.UtcNow;
        var residencyStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-23);
        var priorStart = residencyStart.AddMonths(-14);
        var priorEnd = residencyStart.AddDays(-1);

        var jordan = new Tenant
        {
            Id = Guid.NewGuid(),
            FirstName = "Jordan",
            LastName = "Reyes",
            Phone = "(719) 555-0142",
            Email = PrimaryEmail,
            EmergencyContact = "Alex Reyes — (719) 555-0199 (spouse)",
            Notes = $"{DemoTag} Pseudo resident for full-stack validation. 24-month residency + prior unit history.",
            RowVersion = Guid.NewGuid()
        };
        _db.Tenants.Add(jordan);
        _db.HouseholdMembers.Add(new HouseholdMember
        {
            Id = Guid.NewGuid(),
            TenantId = jordan.Id,
            FullName = "Alex Reyes",
            Relationship = "Spouse",
            DateOfBirth = new DateOnly(1988, 4, 12)
        });
        _db.HouseholdMembers.Add(new HouseholdMember
        {
            Id = Guid.NewGuid(),
            TenantId = jordan.Id,
            FullName = "Sam Reyes",
            Relationship = "Child",
            DateOfBirth = new DateOnly(2016, 9, 3)
        });
        _db.Vehicles.Add(new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = jordan.Id,
            Make = "Toyota",
            Model = "RAV4",
            Color = "Silver",
            Plate = "CO-DEMO1"
        });
        _db.Pets.Add(new Pet
        {
            Id = Guid.NewGuid(),
            TenantId = jordan.Id,
            Name = "Mochi",
            Type = "Dog",
            Breed = "Corgi mix",
            Notes = $"{DemoTag} Vaccinated; pet addendum on file."
        });

        // Prior residency (unit 2) for history
        _db.Occupancies.Add(new Occupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit2.Id,
            TenantId = jordan.Id,
            StartUtc = priorStart,
            EndUtc = priorEnd
        });

        // Current residency unit 1
        _db.Occupancies.Add(new Occupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            TenantId = jordan.Id,
            StartUtc = residencyStart,
            EndUtc = null
        });
        unit1.Status = UnitStatus.Occupied;
        unit1.CurrentTenantId = jordan.Id;
        unit1.SqFt = 980;
        unit1.Beds = 2;
        unit1.Baths = 1;
        unit1.Notes = $"{DemoTag} Layout: living/kitchen open; washer/dryer closet. Primary demo unit.";
        ConcurrencyHelper.BumpRowVersion(unit1);

        unit2.Status = UnitStatus.Vacant;
        unit2.CurrentTenantId = null;
        unit2.SqFt = 720;
        unit2.Beds = 1;
        unit2.Baths = 1;
        unit2.Notes = $"{DemoTag} Prior unit for Jordan Reyes history.";
        ConcurrencyHelper.BumpRowVersion(unit2);

        if (unit3 is not null)
        {
            unit3.SqFt = 1100;
            unit3.Beds = 3;
            unit3.Baths = 2;
            unit3.Status = UnitStatus.MakeReady;
            unit3.Notes = $"{DemoTag} Make-ready example unit.";
            ConcurrencyHelper.BumpRowVersion(unit3);
        }

        // Assets + flooring on unit 1
        var fridge = new Asset
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            Type = "Refrigerator",
            Make = "Whirlpool",
            Model = "WRT318FZDM",
            Serial = "DEMO-FRIDGE-001",
            InstallDate = DateOnly.FromDateTime(residencyStart.AddMonths(-6)),
            WarrantyStart = DateOnly.FromDateTime(residencyStart.AddMonths(-6)),
            WarrantyEnd = DateOnly.FromDateTime(residencyStart.AddMonths(-6).AddYears(1)),
            Condition = "Good",
            Notes = $"{DemoTag} Inventory appliance",
            PhotoPaths = null
        };
        var hvac = new Asset
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            Type = "HVAC",
            Make = "Carrier",
            Model = "24ACC636A003",
            Serial = "DEMO-HVAC-001",
            InstallDate = new DateOnly(2022, 5, 1),
            WarrantyStart = new DateOnly(2022, 5, 1),
            WarrantyEnd = new DateOnly(2032, 5, 1),
            Condition = "Fair",
            Notes = $"{DemoTag} Filter due every 90 days"
        };
        _db.Assets.AddRange(fridge, hvac);
        _db.Floorings.Add(new Flooring
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            Type = "Carpet — living/bedrooms",
            InstallDate = new DateOnly(2021, 8, 15),
            Condition = "Worn traffic paths",
            ReplacedDate = null,
            Notes = $"{DemoTag} Replacement planned next turnover."
        });
        _db.Floorings.Add(new Flooring
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            Type = "Vinyl plank — kitchen/bath",
            InstallDate = new DateOnly(2023, 3, 1),
            Condition = "Good",
            Notes = $"{DemoTag}"
        });

        const decimal rent = 850m;
        const decimal deposit = 850m;
        var leaseEnd = residencyStart.AddYears(2).AddDays(-1);
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            TenantId = jordan.Id,
            StartUtc = residencyStart,
            EndUtc = leaseEnd,
            Rent = rent,
            Deposit = deposit,
            Status = LeaseStatus.Active,
            TemplateUsed = "demo-year-lease.pdf",
            CustomClauses = $"{DemoTag} Pet deposit waived with vaccination records on file.",
            LifecycleNote = $"{DemoTag} Seeded active lease — not generated from NAS template.",
            GeneratedPdfRelativePath = $"leases/{unit1.Number}/demo-lease-active.pdf",
            RowVersion = Guid.NewGuid()
        };
        _db.Leases.Add(lease);

        // Deposit + 24 months rent charges/payments
        var ledgerCount = 0;
        _db.LedgerEntries.Add(MakeCharge(jordan.Id, unit1.Id, lease.Id, deposit, residencyStart,
            $"{DemoTag} Security deposit"));
        ledgerCount++;
        _db.LedgerEntries.Add(MakePayment(jordan.Id, unit1.Id, lease.Id, deposit, residencyStart.AddDays(1),
            PaymentMethod.Check, $"{DemoTag} Deposit check #1042"));
        ledgerCount++;

        for (var m = 0; m < 24; m++)
        {
            var month = residencyStart.AddMonths(m);
            var chargeDate = new DateTime(month.Year, month.Month, 1, 12, 0, 0, DateTimeKind.Utc);
            _db.LedgerEntries.Add(MakeCharge(jordan.Id, unit1.Id, lease.Id, rent, chargeDate,
                $"{DemoTag} Monthly rent {month:yyyy-MM}"));
            ledgerCount++;

            // Month 6: late payment + late fee
            if (m == 5)
            {
                _db.LedgerEntries.Add(MakeCharge(jordan.Id, unit1.Id, lease.Id, 50m, chargeDate.AddDays(10),
                    $"{DemoTag} Late fee", isLateFee: true));
                ledgerCount++;
                _db.LedgerEntries.Add(MakePayment(jordan.Id, unit1.Id, lease.Id, rent + 50m, chargeDate.AddDays(12),
                    PaymentMethod.OnlineReference,
                    $"{DemoTag} PayStar conf PS-DEMO-{month:yyyyMM}-LATE"));
                ledgerCount++;
            }
            else
            {
                var payMethod = m % 3 == 0 ? PaymentMethod.Cash
                    : m % 3 == 1 ? PaymentMethod.Check
                    : PaymentMethod.OnlineReference;
                var note = payMethod == PaymentMethod.OnlineReference
                    ? $"{DemoTag} PayStar conf PS-DEMO-{month:yyyyMM}-RENT"
                    : payMethod == PaymentMethod.Check
                        ? $"{DemoTag} Check #{1100 + m}"
                        : $"{DemoTag} Cash receipt";
                _db.LedgerEntries.Add(MakePayment(jordan.Id, unit1.Id, lease.Id, rent, chargeDate.AddDays(2),
                    payMethod, note));
                ledgerCount++;
            }
        }

        // Maintenance
        var openWo = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            AssetId = hvac.Id,
            Description = "HVAC not cooling adequately in afternoon",
            Status = MaintenanceStatus.Open,
            Priority = MaintenancePriority.High,
            CreatedUtc = now.AddDays(-3),
            Notes = $"{DemoTag} Open WO for dashboard"
        };
        var doneWo = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            AssetId = fridge.Id,
            Description = "Ice maker jammed",
            Status = MaintenanceStatus.Completed,
            Priority = MaintenancePriority.Normal,
            Cost = 85m,
            CreatedUtc = now.AddMonths(-2),
            CompletedUtc = now.AddMonths(-2).AddDays(2),
            Notes = $"{DemoTag} Completed WO"
        };
        var ops = new UnitOperatingCost
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            Category = OperatingCostCategory.Repair,
            Amount = 85m,
            IncurredUtc = doneWo.CompletedUtc!.Value,
            Vendor = "Valley Appliance",
            Notes = $"{DemoTag} Linked to completed WO",
            MaintenanceRequestId = doneWo.Id
        };
        doneWo.OperatingCostId = ops.Id;
        _db.MaintenanceRequests.AddRange(openWo, doneWo);
        _db.UnitOperatingCosts.Add(ops);
        _db.UnitOperatingCosts.Add(new UnitOperatingCost
        {
            Id = Guid.NewGuid(),
            UnitId = null,
            Category = OperatingCostCategory.CommonUpkeep,
            Amount = 220m,
            IncurredUtc = now.AddDays(-20),
            Vendor = "Wiley Grounds",
            Notes = $"{DemoTag} Building common upkeep"
        });
        _db.UnitOperatingCosts.Add(new UnitOperatingCost
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            Category = OperatingCostCategory.Utility,
            Amount = 45m,
            IncurredUtc = now.AddDays(-10),
            Vendor = "SECOM",
            Notes = $"{DemoTag} Unit utility allocation"
        });

        // Schedule
        _db.ScheduledItems.Add(new ScheduledItem
        {
            Id = Guid.NewGuid(),
            Title = "Annual inspection — Unit 1",
            Category = ScheduledItemCategory.Inspection,
            UnitId = unit1.Id,
            TenantId = jordan.Id,
            LeaseId = lease.Id,
            StartUtc = now.AddDays(5).Date.AddHours(10),
            EndUtc = now.AddDays(5).Date.AddHours(11),
            DueUtc = now.AddDays(5).Date.AddHours(10),
            ReminderOffset = TimeSpan.FromDays(1),
            Notes = $"{DemoTag} Upcoming reminder"
        });
        _db.ScheduledItems.Add(new ScheduledItem
        {
            Id = Guid.NewGuid(),
            Title = "Filter change — Unit 1 HVAC",
            Category = ScheduledItemCategory.Other,
            UnitId = unit1.Id,
            StartUtc = now.AddDays(-14).Date.AddHours(9),
            EndUtc = now.AddDays(-14).Date.AddHours(10),
            IsCompleted = true,
            CompletedUtc = now.AddDays(-14),
            Notes = $"{DemoTag} Completed schedule item"
        });

        // Documents (stub PDFs on disk + metadata)
        var docCount = 0;
        docCount += await WriteStubDocumentAsync(
            root, DocumentEntityType.Tenant, jordan.Id, DocumentCategory.Screening,
            $"tenants/{jordan.Id:N}", "demo-screening-jordan-reyes.pdf",
            "Jordan Reyes — screening packet (demo)", "seed@clerksuite", cancellationToken);
        var signedDocId = Guid.NewGuid();
        docCount += await WriteStubDocumentAsync(
            root, DocumentEntityType.Lease, lease.Id, DocumentCategory.SignedLease,
            $"leases/{unit1.Number}/signed", "demo-signed-lease-unit1.pdf",
            "Signed year lease — Unit 1 / Reyes (demo)", "seed@clerksuite", cancellationToken,
            forcedId: signedDocId);
        lease.SignedDocumentId = signedDocId;
        docCount += await WriteStubDocumentAsync(
            root, DocumentEntityType.Unit, unit1.Id, DocumentCategory.Manual,
            $"appliances/{unit1.Number}", "demo-fridge-manual.pdf",
            "Whirlpool fridge manual (demo)", "seed@clerksuite", cancellationToken);
        docCount += await WriteStubDocumentAsync(
            root, DocumentEntityType.Asset, fridge.Id, DocumentCategory.Warranty,
            $"appliances/{unit1.Number}", "demo-fridge-warranty.pdf",
            "Fridge warranty card (demo)", "seed@clerksuite", cancellationToken);
        docCount += await WriteStubDocumentAsync(
            root, DocumentEntityType.MaintenanceRequest, doneWo.Id, DocumentCategory.Receipt,
            $"uploads/maintenance", "demo-wo-receipt.pdf",
            "Valley Appliance receipt (demo)", "seed@clerksuite", cancellationToken);

        // Also write generated lease path stub
        var genPath = Path.Combine(root, lease.GeneratedPdfRelativePath!.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(genPath)!);
        await File.WriteAllBytesAsync(genPath, MinimalPdfBytes("Demo active lease PDF"), cancellationToken);

        // --- Secondary light tenant for rent roll variety ---
        if (unit3 is not null)
        {
            // leave make-ready; add soft-deleted tenant for soft-delete validation
            _db.Tenants.Add(new Tenant
            {
                Id = Guid.NewGuid(),
                FirstName = "Casey",
                LastName = "Nguyen",
                Phone = "(719) 555-0177",
                Email = "casey.nguyen@wiley-demo.local",
                EmergencyContact = "Pat Nguyen — (719) 555-0178",
                Notes = $"{DemoTag} Soft-deleted sample tenant",
                IsDeleted = true,
                RowVersion = Guid.NewGuid()
            });
        }

        // --- Community Center renters ---
        cc.Status = UnitStatus.Vacant;
        cc.SqFt = 2400;
        cc.Beds = 0;
        cc.Baths = 2;
        cc.Notes = $"{DemoTag} Community Center facility — event rentals. Demo renters attached.";
        ConcurrencyHelper.BumpRowVersion(cc);

        var renterSpecs = new (string First, string Last, string Phone, string Email, int DaysAgo, int DurationDays, decimal Fee, decimal Dep)[]
        {
            ("Morgan", "Ellis", "(719) 555-0201", "morgan.ellis@wiley-demo.local", 60, 1, 150m, 100m),
            ("Taylor", "Brooks", "(719) 555-0202", "taylor.brooks@wiley-demo.local", 35, 2, 275m, 150m),
            ("Riley", "Santos", "(719) 555-0203", "riley.santos@wiley-demo.local", 12, 1, 150m, 100m),
            ("Quinn", "Patel", "(719) 555-0204", "quinn.patel@wiley-demo.local", 5, 3, 400m, 200m),
            ("Avery", "Kim", "(719) 555-0205", "avery.kim@wiley-demo.local", -10, 1, 150m, 100m) // upcoming
        };

        var ccRenterCount = 0;
        var schedCount = 2; // already added residential
        var maintCount = 2;

        foreach (var r in renterSpecs)
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                FirstName = r.First,
                LastName = r.Last,
                Phone = r.Phone,
                Email = r.Email,
                EmergencyContact = $"{r.First} emergency — (719) 555-0299",
                Notes = $"{DemoCcTag} {DemoTag} Community Center event renter. Address on agreement: 100 Demo St, Wiley CO 81092.",
                RowVersion = Guid.NewGuid()
            };
            _db.Tenants.Add(tenant);
            ccRenterCount++;

            var start = DateTime.SpecifyKind(now.Date.AddDays(-r.DaysAgo), DateTimeKind.Utc);

            var end = start.AddDays(r.DurationDays);
            var status = r.DaysAgo < 0 ? LeaseStatus.Draft
                : end < now ? LeaseStatus.Expired
                : LeaseStatus.Active;

            var ccLease = new Lease
            {
                Id = Guid.NewGuid(),
                UnitId = cc.Id,
                TenantId = tenant.Id,
                StartUtc = start,
                EndUtc = end,
                Rent = r.Fee,
                Deposit = r.Dep,
                Status = status,
                TemplateUsed = "demo-cc-rental-agreement.pdf",
                CustomClauses = $"{DemoCcTag} No alcohol without permit. Clean-up by 10 PM.",
                LifecycleNote = $"{DemoTag} Community Center rental agreement (seeded).",
                GeneratedPdfRelativePath = $"community-center/rentals/{tenant.Id:N}/agreement.pdf",
                RowVersion = Guid.NewGuid()
            };
            _db.Leases.Add(ccLease);

            // Occupancy-style history: short window (ended if past)
            _db.Occupancies.Add(new Occupancy
            {
                Id = Guid.NewGuid(),
                UnitId = cc.Id,
                TenantId = tenant.Id,
                StartUtc = start,
                EndUtc = r.DaysAgo < 0 ? null : end
            });

            // Deposit + rental fee + PayStar payment
            _db.LedgerEntries.Add(MakeCharge(tenant.Id, cc.Id, ccLease.Id, r.Dep, start.AddDays(-7),
                $"{DemoCcTag} {DemoTag} CC deposit — {r.First} {r.Last}"));
            ledgerCount++;
            _db.LedgerEntries.Add(MakeCharge(tenant.Id, cc.Id, ccLease.Id, r.Fee, start.AddDays(-7),
                $"{DemoCcTag} {DemoTag} CC rental fee {start:yyyy-MM-dd}"));
            ledgerCount++;
            if (r.DaysAgo >= 0)
            {
                _db.LedgerEntries.Add(MakePayment(tenant.Id, cc.Id, ccLease.Id, r.Dep + r.Fee, start.AddDays(-5),
                    PaymentMethod.OnlineReference,
                    $"{DemoCcTag} {DemoTag} PayStar conf PS-CC-{start:yyyyMMdd}-{r.Last.ToUpperInvariant()}"));
                ledgerCount++;
            }

            // Prep/clean schedule
            _db.ScheduledItems.Add(new ScheduledItem
            {
                Id = Guid.NewGuid(),
                Title = $"CC setup — {r.First} {r.Last}",
                Category = ScheduledItemCategory.Cleaning,
                UnitId = cc.Id,
                TenantId = tenant.Id,
                LeaseId = ccLease.Id,
                StartUtc = start.AddHours(-2),
                EndUtc = start.AddHours(-1),
                DueUtc = start.AddHours(-2),
                ReminderOffset = TimeSpan.FromHours(24),
                IsCompleted = r.DaysAgo >= 0,
                CompletedUtc = r.DaysAgo >= 0 ? start.AddHours(-1) : null,
                Notes = $"{DemoTag} {DemoCcTag} Event setup"
            });
            schedCount++;
            _db.ScheduledItems.Add(new ScheduledItem
            {
                Id = Guid.NewGuid(),
                Title = $"CC turnover clean — {r.Last}",
                Category = ScheduledItemCategory.Cleaning,
                UnitId = cc.Id,
                TenantId = tenant.Id,
                StartUtc = end.AddHours(1),
                EndUtc = end.AddHours(3),
                DueUtc = end.AddHours(1),
                IsCompleted = end < now,
                CompletedUtc = end < now ? end.AddHours(3) : null,
                Notes = $"{DemoTag} {DemoCcTag} Post-event clean"
            });
            schedCount++;

            var signedId = Guid.NewGuid();
            docCount += await WriteStubDocumentAsync(
                root, DocumentEntityType.Lease, ccLease.Id, DocumentCategory.SignedLease,
                $"community-center/rentals/{tenant.Id:N}", "demo-cc-signed-agreement.pdf",
                $"CC rental agreement signed — {r.First} {r.Last}", "seed@clerksuite", cancellationToken,
                forcedId: signedId);
            ccLease.SignedDocumentId = signedId;
            docCount += await WriteStubDocumentAsync(
                root, DocumentEntityType.Tenant, tenant.Id, DocumentCategory.Correspondence,
                $"community-center/renters/{tenant.Id:N}", "demo-cc-renter-info.pdf",
                $"Renter info sheet — {r.First} {r.Last}", "seed@clerksuite", cancellationToken);

            var gen = Path.Combine(root, ccLease.GeneratedPdfRelativePath!.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(gen)!);
            await File.WriteAllBytesAsync(gen, MinimalPdfBytes($"CC agreement {r.First} {r.Last}"), cancellationToken);
        }

        // CC maintenance + ops
        _db.MaintenanceRequests.Add(new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            UnitId = cc.Id,
            Description = "Replace burnt stage light bulbs",
            Status = MaintenanceStatus.InProgress,
            Priority = MaintenancePriority.Normal,
            CreatedUtc = now.AddDays(-4),
            Notes = $"{DemoTag} {DemoCcTag} Facility WO"
        });
        maintCount++;
        _db.UnitOperatingCosts.Add(new UnitOperatingCost
        {
            Id = Guid.NewGuid(),
            UnitId = cc.Id,
            Category = OperatingCostCategory.Utility,
            Amount = 180m,
            IncurredUtc = now.AddDays(-15),
            Vendor = "SECOM",
            Notes = $"{DemoTag} {DemoCcTag} CC utilities"
        });
        _db.UnitOperatingCosts.Add(new UnitOperatingCost
        {
            Id = Guid.NewGuid(),
            UnitId = cc.Id,
            Category = OperatingCostCategory.Replace,
            Amount = 320m,
            IncurredUtc = now.AddDays(-40),
            Vendor = "Furniture Plus",
            Notes = $"{DemoTag} {DemoCcTag} Table replacements"
        });
        _db.Assets.Add(new Asset
        {
            Id = Guid.NewGuid(),
            UnitId = cc.Id,
            Type = "Sound system",
            Make = "Yamaha",
            Model = "MG10XU",
            Serial = "DEMO-CC-SOUND-1",
            InstallDate = new DateOnly(2024, 1, 10),
            WarrantyStart = new DateOnly(2024, 1, 10),
            WarrantyEnd = new DateOnly(2027, 1, 10),
            Condition = "Good",
            Notes = $"{DemoTag} {DemoCcTag}"
        });
        _db.Floorings.Add(new Flooring
        {
            Id = Guid.NewGuid(),
            UnitId = cc.Id,
            Type = "Commercial vinyl",
            InstallDate = new DateOnly(2020, 6, 1),
            Condition = "Good",
            Notes = $"{DemoTag} {DemoCcTag}"
        });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Demo seed complete: primary {Email}, {Cc} CC renters, {Ledger} ledger lines, {Docs} docs.",
            PrimaryEmail, ccRenterCount, ledgerCount, docCount);

        return new DemoSeedResult(
            AlreadyLoaded: false,
            Forced: force,
            PrimaryTenantName: "Jordan Reyes",
            PrimaryTenantId: jordan.Id,
            PrimaryUnitId: unit1.Id,
            CommunityCenterRenters: ccRenterCount,
            LedgerEntries: ledgerCount,
            Documents: docCount,
            Maintenance: maintCount,
            ScheduleItems: schedCount,
            Message: "Demo portfolio loaded: 24-month resident + Community Center renters with PayStar-style payments.");
    }

    public async Task<DemoValidationReport> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<DemoValidationCheck>();

        async Task Check(string area, Func<Task<(bool pass, string detail, int count)>> fn)
        {
            try
            {
                var (pass, detail, count) = await fn();
                checks.Add(new DemoValidationCheck(area, pass, detail, count));
            }
            catch (Exception ex)
            {
                checks.Add(new DemoValidationCheck(area, false, ex.Message));
            }
        }

        await Check("Units", async () =>
        {
            var n = await _db.Units.CountAsync(cancellationToken);
            var fac = await _db.Units.CountAsync(u => u.IsFacility, cancellationToken);
            var ok = n >= 17 && fac >= 1;
            return (ok, $"{n} units ({fac} facility)", n);
        });

        await Check("Primary demo tenant", async () =>
        {
            var t = await _db.Tenants.Include(x => x.HouseholdMembers).Include(x => x.Vehicles).Include(x => x.Pets)
                .FirstOrDefaultAsync(x => x.Email == PrimaryEmail, cancellationToken);
            if (t is null)
            {
                return (false, "Jordan Reyes not found — run Load demo data", 0);
            }

            var ok = t.HouseholdMembers.Count >= 2 && t.Vehicles.Count >= 1 && t.Pets.Count >= 1
                     && !string.IsNullOrWhiteSpace(t.Phone) && !string.IsNullOrWhiteSpace(t.EmergencyContact);
            return (ok, $"Tenant {t.FirstName} {t.LastName}: HH={t.HouseholdMembers.Count}, vehicles={t.Vehicles.Count}, pets={t.Pets.Count}", 1);
        });

        await Check("Occupancy history", async () =>
        {
            var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Email == PrimaryEmail, cancellationToken);
            if (t is null)
            {
                return (false, "No primary tenant", 0);
            }

            var hist = await _db.Occupancies.CountAsync(o => o.TenantId == t.Id, cancellationToken);
            var active = await _db.Occupancies.CountAsync(o => o.TenantId == t.Id && o.EndUtc == null, cancellationToken);
            return (hist >= 2 && active == 1, $"{hist} occupancy rows ({active} active)", hist);
        });

        await Check("24-month ledger", async () =>
        {
            var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Email == PrimaryEmail, cancellationToken);
            if (t is null)
            {
                return (false, "No primary tenant", 0);
            }

            var charges = await _db.LedgerEntries.CountAsync(
                e => !e.IsDeleted && e.TenantId == t.Id && e.EntryType == LedgerEntryType.Charge, cancellationToken);
            var payments = await _db.LedgerEntries.CountAsync(
                e => !e.IsDeleted && e.TenantId == t.Id && e.EntryType == LedgerEntryType.Payment, cancellationToken);
            var paystar = await _db.LedgerEntries.CountAsync(
                e => !e.IsDeleted && e.TenantId == t.Id && e.Method == PaymentMethod.OnlineReference, cancellationToken);
            var ok = charges >= 25 && payments >= 24 && paystar >= 5;
            return (ok, $"Charges={charges}, payments={payments}, PayStar/online={paystar}", charges + payments);
        });

        await Check("Active lease + signed doc", async () =>
        {
            var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Email == PrimaryEmail, cancellationToken);
            if (t is null)
            {
                return (false, "No primary tenant", 0);
            }

            var lease = await _db.Leases.AsNoTracking()
                .FirstOrDefaultAsync(l => l.TenantId == t.Id && l.Status == LeaseStatus.Active && !l.IsDeleted, cancellationToken);
            if (lease is null)
            {
                return (false, "No active lease", 0);
            }

            var hasSigned = lease.SignedDocumentId is Guid;
            var docOk = hasSigned && await _db.Documents.AnyAsync(d => d.Id == lease.SignedDocumentId && !d.IsDeleted, cancellationToken);
            return (docOk, $"Lease {lease.Id:N} rent={lease.Rent:C} signedDoc={docOk}", 1);
        });

        await Check("Unit assets & flooring", async () =>
        {
            var unit1 = await _db.Units.AsNoTracking().FirstAsync(u => u.Number == "1", cancellationToken);
            var assets = await _db.Assets.CountAsync(a => a.UnitId == unit1.Id, cancellationToken);
            var floors = await _db.Floorings.CountAsync(f => f.UnitId == unit1.Id, cancellationToken);
            return (assets >= 2 && floors >= 1, $"Unit 1 assets={assets} flooring={floors}", assets + floors);
        });

        await Check("Maintenance + ops costs", async () =>
        {
            var m = await _db.MaintenanceRequests.CountAsync(x => !x.IsDeleted && x.Notes != null && x.Notes.Contains(DemoTag), cancellationToken);
            var o = await _db.UnitOperatingCosts.CountAsync(x => !x.IsDeleted && x.Notes != null && x.Notes.Contains(DemoTag), cancellationToken);
            return (m >= 2 && o >= 3, $"Maintenance={m} ops costs={o}", m + o);
        });

        await Check("Schedule items", async () =>
        {
            var n = await _db.ScheduledItems.CountAsync(s => !s.IsDeleted && s.Notes != null && s.Notes.Contains(DemoTag), cancellationToken);
            return (n >= 4, $"{n} scheduled items", n);
        });

        await Check("Community Center renters", async () =>
        {
            var cc = await _db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.IsFacility, cancellationToken);
            if (cc is null)
            {
                return (false, "No facility unit", 0);
            }

            var renters = await _db.Tenants.CountAsync(
                t => !t.IsDeleted && t.Notes != null && t.Notes.Contains(DemoCcTag), cancellationToken);
            var leases = await _db.Leases.CountAsync(l => !l.IsDeleted && l.UnitId == cc.Id, cancellationToken);
            var paystar = await _db.LedgerEntries.CountAsync(
                e => !e.IsDeleted && e.UnitId == cc.Id && e.Method == PaymentMethod.OnlineReference, cancellationToken);
            var ok = renters >= 4 && leases >= 4 && paystar >= 3;
            return (ok, $"CC renters={renters} leases={leases} PayStar payments={paystar}", renters);
        });

        await Check("Documents on disk", async () =>
        {
            var root = await _paths.GetDocumentRootAsync(cancellationToken);
            var docs = await _db.Documents.AsNoTracking()
                .Where(d => !d.IsDeleted && d.OriginalFileName.Contains("demo"))
                .ToListAsync(cancellationToken);
            var missing = 0;
            foreach (var d in docs)
            {
                var abs = Path.Combine(root, d.FilePathOnNas.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(abs))
                {
                    missing++;
                }
            }

            return (docs.Count >= 8 && missing == 0, $"Metadata={docs.Count}, missing files={missing}, root={root}", docs.Count);
        });

        await Check("Document root writable", async () =>
        {
            var root = await _paths.GetDocumentRootAsync(cancellationToken);
            try
            {
                DocumentRootAvailability.EnsureWritable(root);
                return (true, root, 1);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, 0);
            }
        });

        return new DemoValidationReport(checks.All(c => c.Pass), checks);
    }

    private async Task WipeDemoAsync(CancellationToken cancellationToken)
    {
        var demoTenantIds = await _db.Tenants
            .Where(t => t.Email == PrimaryEmail
                        || (t.Notes != null && (t.Notes.Contains(DemoTag) || t.Notes.Contains(DemoCcTag))))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (demoTenantIds.Count == 0)
        {
            return;
        }

        var leaseIds = await _db.Leases.Where(l => demoTenantIds.Contains(l.TenantId)).Select(l => l.Id).ToListAsync(cancellationToken);

        _db.LedgerEntries.RemoveRange(_db.LedgerEntries.Where(e => demoTenantIds.Contains(e.TenantId)));
        _db.ScheduledItems.RemoveRange(_db.ScheduledItems.Where(s =>
            (s.TenantId != null && demoTenantIds.Contains(s.TenantId.Value))
            || (s.Notes != null && s.Notes.Contains(DemoTag))));
        _db.Occupancies.RemoveRange(_db.Occupancies.Where(o => demoTenantIds.Contains(o.TenantId)));
        _db.Documents.RemoveRange(_db.Documents.Where(d =>
            d.OriginalFileName.Contains("demo")
            || (d.EntityType == DocumentEntityType.Tenant && demoTenantIds.Contains(d.EntityId))
            || (d.EntityType == DocumentEntityType.Lease && leaseIds.Contains(d.EntityId))));
        _db.Leases.RemoveRange(_db.Leases.Where(l => demoTenantIds.Contains(l.TenantId)));
        _db.HouseholdMembers.RemoveRange(_db.HouseholdMembers.Where(h => demoTenantIds.Contains(h.TenantId)));
        _db.Vehicles.RemoveRange(_db.Vehicles.Where(v => demoTenantIds.Contains(v.TenantId)));
        _db.Pets.RemoveRange(_db.Pets.Where(p => demoTenantIds.Contains(p.TenantId)));
        _db.MaintenanceRequests.RemoveRange(_db.MaintenanceRequests.Where(m =>
            m.Notes != null && m.Notes.Contains(DemoTag)));
        _db.UnitOperatingCosts.RemoveRange(_db.UnitOperatingCosts.Where(c =>
            c.Notes != null && c.Notes.Contains(DemoTag)));

        // Clear unit links for demo units
        foreach (var u in await _db.Units.Where(u => u.Notes != null && u.Notes.Contains(DemoTag)).ToListAsync(cancellationToken))
        {
            if (u.CurrentTenantId is Guid tid && demoTenantIds.Contains(tid))
            {
                u.CurrentTenantId = null;
                u.Status = u.IsFacility ? UnitStatus.Vacant : UnitStatus.Vacant;
            }
        }

        // Remove demo-only assets/flooring on unit 1 / CC (by serial/notes)
        _db.Assets.RemoveRange(_db.Assets.Where(a =>
            a.Serial.StartsWith("DEMO-") || (a.Notes != null && a.Notes.Contains(DemoTag))));
        _db.Floorings.RemoveRange(_db.Floorings.Where(f => f.Notes != null && f.Notes.Contains(DemoTag)));

        _db.Tenants.RemoveRange(_db.Tenants.Where(t => demoTenantIds.Contains(t.Id)));
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Wiped {Count} demo tenants and related rows.", demoTenantIds.Count);
    }

    private static void EnrichResidentialUnits(List<Unit> units)
    {
        foreach (var u in units.Where(u => !u.IsFacility && u.Number is "4" or "5" or "6"))
        {
            if (u.SqFt == 0)
            {
                u.SqFt = 800 + int.Parse(u.Number) * 10;
                u.Beds = 2;
                u.Baths = 1;
                u.Notes = string.IsNullOrWhiteSpace(u.Notes)
                    ? $"{DemoTag} Placeholder layout filled for demo reports."
                    : u.Notes;
            }
        }
    }

    private static LedgerEntry MakeCharge(
        Guid tenantId, Guid unitId, Guid leaseId, decimal amount, DateTime dateUtc, string notes, bool isLateFee = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntryType = LedgerEntryType.Charge,
            TenantId = tenantId,
            UnitId = unitId,
            LeaseId = leaseId,
            Amount = amount,
            DateUtc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc),
            Notes = notes,
            IsLateFee = isLateFee
        };

    private static LedgerEntry MakePayment(
        Guid tenantId, Guid unitId, Guid leaseId, decimal amount, DateTime dateUtc, PaymentMethod method, string notes) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntryType = LedgerEntryType.Payment,
            TenantId = tenantId,
            UnitId = unitId,
            LeaseId = leaseId,
            Amount = amount,
            DateUtc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc),
            Method = method,
            Notes = notes
        };

    private async Task<int> WriteStubDocumentAsync(
        string root,
        DocumentEntityType entityType,
        Guid entityId,
        DocumentCategory category,
        string relativeDirectory,
        string fileName,
        string title,
        string uploadedBy,
        CancellationToken cancellationToken,
        Guid? forcedId = null)
    {
        var relDir = relativeDirectory.Replace('\\', '/').Trim('/');
        var relPath = $"{relDir}/{fileName}";
        var abs = Path.Combine(root, relPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
        await File.WriteAllBytesAsync(abs, MinimalPdfBytes(title), cancellationToken);

        _db.Documents.Add(new Document
        {
            Id = forcedId ?? Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            FilePathOnNas = relPath,
            OriginalFileName = fileName,
            ContentType = "application/pdf",
            Category = category,
            UploadedBy = uploadedBy,
            UploadedAtUtc = _clock.UtcNow
        });
        return 1;
    }

    private static void EnsureDemoFolders(string root)
    {
        foreach (var sub in new[]
                 {
                     "templates", "leases", "uploads", "appliances", "tenants",
                     "community-center/rentals", "community-center/renters", "uploads/maintenance"
                 })
        {
            Directory.CreateDirectory(Path.Combine(root, sub.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    /// <summary>Tiny valid-enough PDF for vault viewer smoke (not a real legal form).</summary>
    private static byte[] MinimalPdfBytes(string title)
    {
        var safe = new string(title.Where(c => c >= 32 && c < 127).ToArray());
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "ClerkSuite demo document";
        }

        var content = $"BT /F1 12 Tf 72 720 Td ({safe}) Tj ET";
        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n");
        sb.Append("2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n");
        sb.Append("3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources<< /Font<< /F1 5 0 R >> >> >>endobj\n");
        sb.Append($"4 0 obj<< /Length {content.Length} >>stream\n{content}\nendstream endobj\n");
        sb.Append("5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n");
        sb.Append("xref\n0 6\n0000000000 65535 f \n");
        sb.Append("trailer<< /Size 6 /Root 1 0 R >>\nstartxref\n0\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
