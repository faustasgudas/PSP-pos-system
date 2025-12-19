using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Reservations;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;

namespace TestProject1;

public class ReservationTests
{
   
    private static (
        AppDbContext db,
        Business biz,
        Employee owner,
        Employee manager,
        Employee staff,
        CatalogItem service60,
        CatalogItem service45,
        CatalogItem inactiveService,
        CatalogItem product
    ) Boot()
    {
        var db = TestHelpers.NewInMemoryContext();

        var biz = new Business
        {
            Name = "Test Biz",
            Address = "Addr",
            Phone = "1",
            Email = "biz@test.local",
            CountryCode = "LT",
            BusinessType = "Catering",
            PriceIncludesTax = false
        };
        db.Businesses.Add(biz);
        db.SaveChanges();

        var owner = new Employee
        {
            BusinessId = biz.BusinessId,
            Name = "Owner",
            Role = "Owner",
            Status = "Active",
            Email = "owner@test.local",
            PasswordHash = "x"
        };

        var manager = new Employee
        {
            BusinessId = biz.BusinessId,
            Name = "Manager",
            Role = "Manager",
            Status = "Active",
            Email = "manager@test.local",
            PasswordHash = "x"
        };

        var staff = new Employee
        {
            BusinessId = biz.BusinessId,
            Name = "Staff",
            Role = "Staff",
            Status = "Active",
            Email = "staff@test.local",
            PasswordHash = "x"
        };

        db.Employees.AddRange(owner, manager, staff);

        var service60 = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Private Table",
            Code = "TABLE60",
            Type = "Service",
            BasePrice = 100m,
            Status = "Active",
            DefaultDurationMin = 60,
            TaxClass = "Standard"
        };

        var service45 = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Haircut",
            Code = "SERV45",
            Type = "Service",
            BasePrice = 50m,
            Status = "Active",
            DefaultDurationMin = 45,
            TaxClass = "Standard"
        };

        var inactiveService = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Inactive Service",
            Code = "INACT",
            Type = "Service",
            BasePrice = 10m,
            Status = "Inactive",
            DefaultDurationMin = 30,
            TaxClass = "Standard"
        };

        var product = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Coffee",
            Code = "COF",
            Type = "Product",
            BasePrice = 3m,
            Status = "Active",
            DefaultDurationMin = 0,
            TaxClass = "Food"
        };

        db.CatalogItems.AddRange(service60, service45, inactiveService, product);
        db.SaveChanges();

        return (db, biz, owner, manager, staff, service60, service45, inactiveService, product);
    }

    private static async Task<ReservationDetailResponse> CreateBookedAsync(
        ReservationService svc,
        int businessId,
        int callerId,
        int catalogItemId,
        int employeeId,
        DateTime start,
        string? notes = null)
    {
        return await svc.CreateAsync(
            businessId,
            callerId,
            new CreateReservationRequest
            {
                CatalogItemId = catalogItemId,
                EmployeeId = employeeId,
                AppointmentStart = start,
                Notes = notes
            });
    }

    

    [Fact]
    public async Task CreateAsync_SetsBookedStatus_Start_Notes_Table_AndDurationFromCatalog()
    {
        var (db, biz, owner, _, _, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var start = DateTime.UtcNow.AddHours(2);

        var created = await svc.CreateAsync(
            biz.BusinessId,
            owner.EmployeeId,
            new CreateReservationRequest
            {
                CatalogItemId = service60.CatalogItemId,
                EmployeeId = owner.EmployeeId,
                AppointmentStart = start,
                Notes = "VIP",
                TableOrArea = "A1"
            });

        Assert.Equal("Booked", created.Status);
        Assert.Equal(start, created.AppointmentStart);
        Assert.Equal(service60.DefaultDurationMin, created.PlannedDurationMin);
        Assert.Equal("VIP", created.Notes);
        Assert.Equal("A1", created.TableOrArea);

        Assert.NotEqual(default, created.BookedAt);
    }

    [Fact]
    public async Task CreateAsync_Rejects_DefaultDateTime_Start()
    {
        var (db, biz, owner, _, _, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(
                biz.BusinessId,
                owner.EmployeeId,
                new CreateReservationRequest
                {
                    CatalogItemId = service60.CatalogItemId,
                    EmployeeId = owner.EmployeeId,
                    AppointmentStart = default
                }));

        Assert.Contains("start", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Rejects_NonService_CatalogItem()
    {
        var (db, biz, owner, _, _, _, _, _, product) = Boot();
        var svc = new ReservationService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(
                biz.BusinessId,
                owner.EmployeeId,
                new CreateReservationRequest
                {
                    CatalogItemId = product.CatalogItemId,
                    EmployeeId = owner.EmployeeId,
                    AppointmentStart = DateTime.UtcNow.AddHours(1)
                }));

        Assert.True(
            ex.Message.Contains("service", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("not_service", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_Rejects_Inactive_Service()
    {
        var (db, biz, owner, _, _, _, _, inactiveService, _) = Boot();
        var svc = new ReservationService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(
                biz.BusinessId,
                owner.EmployeeId,
                new CreateReservationRequest
                {
                    CatalogItemId = inactiveService.CatalogItemId,
                    EmployeeId = owner.EmployeeId,
                    AppointmentStart = DateTime.UtcNow.AddHours(1)
                }));

        Assert.Contains("active", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Rejects_Service_WithInvalidCatalogDuration()
    {
        var (db, biz, owner, _, _, _, _, _, _) = Boot();

       
        var bad = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Bad Service",
            Code = "BAD",
            Type = "Service",
            BasePrice = 1m,
            Status = "Active",
            DefaultDurationMin = 0,
            TaxClass = "Standard"
        };
        db.CatalogItems.Add(bad);
        db.SaveChanges();

        var svc = new ReservationService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(
                biz.BusinessId,
                owner.EmployeeId,
                new CreateReservationRequest
                {
                    CatalogItemId = bad.CatalogItemId,
                    EmployeeId = owner.EmployeeId,
                    AppointmentStart = DateTime.UtcNow.AddHours(1)
                }));

        Assert.Contains("duration", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_OnlyOwnerOrManager_CanCreate_StaffForbidden()
    {
        var (db, biz, owner, _, staff, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(
                biz.BusinessId,
                staff.EmployeeId, 
                new CreateReservationRequest
                {
                    CatalogItemId = service60.CatalogItemId,
                    EmployeeId = owner.EmployeeId,
                    AppointmentStart = DateTime.UtcNow.AddHours(1)
                }));

        Assert.True(
            ex.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("owner", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("manager", StringComparison.OrdinalIgnoreCase));
    }

 

    [Fact]
    public async Task UpdateAsync_Updates_Start_AndKeepsDurationFromCatalog()
    {
        var (db, biz, owner, _, _, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var created = await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, owner.EmployeeId,
            DateTime.UtcNow.AddHours(1));

        var newStart = DateTime.UtcNow.AddDays(1).AddHours(2);

        var updated = await svc.UpdateAsync(
            biz.BusinessId,
            created.ReservationId,
            owner.EmployeeId,
            new UpdateReservationRequest
            {
                AppointmentStart = newStart,
                Notes = "Updated",
                TableOrArea = "B2"
            });

        Assert.Equal(newStart, updated.AppointmentStart);
        Assert.Equal(service60.DefaultDurationMin, updated.PlannedDurationMin);
        Assert.Equal("Updated", updated.Notes);
        Assert.Equal("B2", updated.TableOrArea);
    }

    [Fact]
    public async Task UpdateAsync_WhenCatalogChanges_RecomputesDurationFromNewCatalog()
    {
        var (db, biz, owner, _, _, service60, service45, _, _) = Boot();
        var svc = new ReservationService(db);

        var created = await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, owner.EmployeeId,
            DateTime.UtcNow.AddHours(1));

        var updated = await svc.UpdateAsync(
            biz.BusinessId,
            created.ReservationId,
            owner.EmployeeId,
            new UpdateReservationRequest
            {
                CatalogItemId = service45.CatalogItemId 
            });

        Assert.Equal(service45.CatalogItemId, updated.CatalogItemId);
        Assert.Equal(service45.DefaultDurationMin, updated.PlannedDurationMin);
    }

    [Fact]
    public async Task UpdateAsync_Ignores_Null_AppointmentStart()
    {
        var (db, biz, owner, _, _, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var created = await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, owner.EmployeeId,
            DateTime.UtcNow.AddHours(1));

        var updated = await svc.UpdateAsync(
            biz.BusinessId,
            created.ReservationId,
            owner.EmployeeId,
            new UpdateReservationRequest
            {
                AppointmentStart = null,
                Notes = "Still valid"
            });

        Assert.Equal(created.AppointmentStart, updated.AppointmentStart);
        Assert.Equal("Still valid", updated.Notes);
    }

    [Fact]
    public async Task UpdateAsync_Rejects_NonBookedReservations()
    {
        var (db, biz, owner, _, _, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        
        var entity = new Reservation
        {
            BusinessId = biz.BusinessId,
            EmployeeId = owner.EmployeeId,
            CatalogItemId = service60.CatalogItemId,
            AppointmentStart = DateTime.UtcNow.AddHours(1),
            PlannedDurationMin = service60.DefaultDurationMin,
            Status = "Cancelled",
            BookedAt = DateTime.UtcNow
        };
        db.Reservations.Add(entity);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateAsync(
                biz.BusinessId,
                entity.ReservationId,
                owner.EmployeeId,
                new UpdateReservationRequest { Notes = "Try update" }));

        Assert.Contains("not", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_OnlyOwnerOrManager_CanUpdate_StaffForbidden()
    {
        var (db, biz, owner, _, staff, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var created = await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, owner.EmployeeId,
            DateTime.UtcNow.AddHours(1));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateAsync(
                biz.BusinessId,
                created.ReservationId,
                staff.EmployeeId,
                new UpdateReservationRequest { Notes = "Hack" }));

        Assert.True(
            ex.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("owner", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("manager", StringComparison.OrdinalIgnoreCase));
    }

 
   
    [Fact]
    public async Task CancelAsync_SetsStatusCancelled_WhenBooked()
    {
        var (db, biz, owner, _, _, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var created = await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, owner.EmployeeId,
            DateTime.UtcNow.AddHours(1));

        var cancelled = await svc.CancelAsync(biz.BusinessId, created.ReservationId, owner.EmployeeId);

        Assert.Equal("Cancelled", cancelled.Status);
    }

    [Fact]
    public async Task CancelAsync_Rejects_WhenNotBooked()
    {
        var (db, biz, owner, _, _, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var entity = new Reservation
        {
            BusinessId = biz.BusinessId,
            EmployeeId = owner.EmployeeId,
            CatalogItemId = service60.CatalogItemId,
            AppointmentStart = DateTime.UtcNow.AddHours(1),
            PlannedDurationMin = service60.DefaultDurationMin,
            Status = "Cancelled",
            BookedAt = DateTime.UtcNow
        };
        db.Reservations.Add(entity);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CancelAsync(biz.BusinessId, entity.ReservationId, owner.EmployeeId));

        Assert.Contains("cancel", ex.Message, StringComparison.OrdinalIgnoreCase);
    }



    [Fact]
    public async Task GetAsync_StaffCanGetOnlyOwn_ForbiddenForOthers()
    {
        var (db, biz, owner, _, staff, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        var own = await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, staff.EmployeeId,
            DateTime.UtcNow.AddHours(1),
            notes: "staff own");

        
        var ok = await svc.GetAsync(biz.BusinessId, own.ReservationId, staff.EmployeeId);
        Assert.Equal(own.ReservationId, ok.ReservationId);

     
        var owners = await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, owner.EmployeeId,
            DateTime.UtcNow.AddHours(2));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.GetAsync(biz.BusinessId, owners.ReservationId, staff.EmployeeId));

        Assert.Contains("forbidden", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListAsync_StaffSeesOnlyTheirReservations()
    {
        var (db, biz, owner, _, staff, service60, _, _, _) = Boot();
        var svc = new ReservationService(db);

        await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, staff.EmployeeId,
            DateTime.UtcNow.AddHours(1));

        await CreateBookedAsync(
            svc, biz.BusinessId, owner.EmployeeId,
            service60.CatalogItemId, owner.EmployeeId,
            DateTime.UtcNow.AddHours(2));

        var staffList = await svc.ListAsync(
            biz.BusinessId,
            staff.EmployeeId,
            status: null,
            dateFrom: null,
            dateTo: null,
            employeeId: null,
            catalogItemId: null);

        Assert.Single(staffList);
        Assert.Equal(staff.EmployeeId, staffList.First().EmployeeId);
    }

    [Fact]
    public async Task ListAsync_ManagerCanFilter_ByStatus_DateRange_Employee_Catalog()
    {
        var (db, biz, owner, manager, staff, service60, service45, _, _) = Boot();
        var svc = new ReservationService(db);

        var now = DateTime.UtcNow;

        
        var r1 = await CreateBookedAsync(svc, biz.BusinessId, owner.EmployeeId, service60.CatalogItemId, staff.EmployeeId, now.AddHours(1));
        var r2 = await CreateBookedAsync(svc, biz.BusinessId, owner.EmployeeId, service45.CatalogItemId, staff.EmployeeId, now.AddHours(2));
        var r3 = await CreateBookedAsync(svc, biz.BusinessId, owner.EmployeeId, service60.CatalogItemId, owner.EmployeeId, now.AddHours(3));

       
        await svc.CancelAsync(biz.BusinessId, r2.ReservationId, owner.EmployeeId);

        
        var cancelled = await svc.ListAsync(
            biz.BusinessId,
            manager.EmployeeId,
            status: "Cancelled",
            dateFrom: null,
            dateTo: null,
            employeeId: null,
            catalogItemId: null);

        Assert.Single(cancelled);

        
        var filtered = await svc.ListAsync(
            biz.BusinessId,
            manager.EmployeeId,
            status: "Booked",
            dateFrom: now,
            dateTo: now.AddHours(10),
            employeeId: staff.EmployeeId,
            catalogItemId: service60.CatalogItemId);

        Assert.Single(filtered);
        Assert.Equal(staff.EmployeeId, filtered.First().EmployeeId);
        Assert.Equal(service60.CatalogItemId, filtered.First().CatalogItemId);
    }
}
