using Microsoft.EntityFrameworkCore;
using PsP.Models;

namespace PsP.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    // ===== DbSets =====
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<TaxRule> TaxRules => Set<TaxRule>();

    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<DiscountEligibility> DiscountEligibilities => Set<DiscountEligibility>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<GiftCard> GiftCards => Set<GiftCard>();

    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ==== Business ====
        mb.Entity<Business>(e =>
        {
            e.HasKey(x => x.BusinessId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();

            e.HasMany(x => x.Employees)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.CatalogItems)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Orders)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Reservations)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.GiftCards)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Payments)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Discounts)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==== Employee ====
        mb.Entity<Employee>(e =>
        {
            e.HasKey(x => x.EmployeeId);

       
            e.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Email)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.PasswordHash)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Role)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.Status)
                .HasMaxLength(32)
                .IsRequired();

           
            e.HasOne(x => x.Business)
                .WithMany(b => b.Employees)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Orders)
                .WithOne(o => o.Employee)
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
           

          
            e.HasMany(x => x.Reservations)
                .WithOne(r => r.Employee)
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
       
            e.HasIndex(x => new { x.BusinessId, x.Role });
    
         
            e.HasIndex(x => new { x.BusinessId, x.Email })
                .IsUnique(); 
        });

        // ==== CatalogItem ====
        mb.Entity<CatalogItem>(e =>
        {
            e.HasKey(x => x.CatalogItemId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Type).HasMaxLength(16).IsRequired();
            e.Property(x => x.Status).HasMaxLength(16).IsRequired();
            e.Property(x => x.TaxClass).HasMaxLength(32).IsRequired();
            e.Property(x => x.BasePrice).HasColumnType("numeric(18,2)");

            e.HasIndex(x => new { x.BusinessId, x.Code }).IsUnique();

            e.HasOne(x => x.Business)
                .WithMany(b => b.CatalogItems)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.DiscountEligibilities)
                .WithOne(de => de.CatalogItem)
                .HasForeignKey(de => de.CatalogItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==== TaxRule (read-only/reference; kept simple) ====
        mb.Entity<TaxRule>(e =>
        {
            e.HasKey(x => x.TaxRuleId);
            e.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            e.Property(x => x.TaxClass).HasMaxLength(32).IsRequired();
            e.Property(x => x.RatePercent).HasColumnType("numeric(5,2)");

            e.HasIndex(x => new { x.CountryCode, x.TaxClass, x.ValidFrom, x.ValidTo });

        });
        
        mb.Entity<Discount>(e =>
        {
            
            e.HasKey(x => x.DiscountId);

            e.Property(x => x.Code)
                .HasMaxLength(64)
                .IsRequired();

            e.Property(x => x.Type)
                .HasMaxLength(16)
                .IsRequired();   // "Percent" | "Amount"

            e.Property(x => x.Scope)
                .HasMaxLength(16)
                .IsRequired();  // "Order" | "Line"

            e.Property(x => x.Status)
                .HasMaxLength(16)
                .IsRequired();

            e.Property(x => x.Value)
                .HasColumnType("numeric(18,2)");

            e.Property(x => x.StartsAt)
                .IsRequired();

            e.Property(x => x.EndsAt)
                .IsRequired();

            
            e.HasIndex(x => new { x.BusinessId, x.Code })
                .IsUnique();

           

            e.HasOne(x => x.Business)
                .WithMany(b => b.Discounts)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
          

            e.HasMany(x => x.Eligibilities)
                .WithOne(de => de.Discount)
                .HasForeignKey(de => de.DiscountId)
                .OnDelete(DeleteBehavior.Cascade);
           
        });

        mb.Entity<DiscountEligibility>(e =>
        {
            e.HasKey(x => new { x.DiscountId, x.CatalogItemId });


            e.HasOne(x => x.Discount)
                .WithMany(d => d.Eligibilities)
                .HasForeignKey(x => x.DiscountId)
                .OnDelete(DeleteBehavior.Cascade); 
            
            e.HasOne(x => x.CatalogItem)
                .WithMany(ci => ci.DiscountEligibilities)
                .HasForeignKey(x => x.CatalogItemId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

   
        mb.Entity<Order>(e =>
        {
            e.HasKey(x => x.OrderId);

            
            e.Property(x => x.Status)
                .HasMaxLength(16)
                .IsRequired();
            
            e.Property(x => x.TableOrArea)
                .HasMaxLength(64);

            e.Property(x => x.TipAmount)
                .HasColumnType("numeric(18,2)");

            e.Property(x => x.OrderDiscountSnapshot)
                .HasColumnType("text");

            e.Property(x => x.CreatedAt)
                .IsRequired();


            e.HasOne(x => x.Business)
                .WithMany(b => b.Orders)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

       
            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.Orders)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            
            e.HasOne(x => x.Reservation)
                .WithOne()
                .HasForeignKey<Order>(x => x.ReservationId)
                .OnDelete(DeleteBehavior.SetNull);
            
            e.HasMany(x => x.Lines)
                .WithOne(l => l.Order)
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
          

         
            e.HasMany(x => x.Payments)
                .WithOne(p => p.Order)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
 
            e.HasIndex(x => new { x.BusinessId, x.CreatedAt });
        });

       
        mb.Entity<OrderLine>(e =>
        {
            e.HasKey(x => x.OrderLineId);

            
            e.Property(x => x.Qty).HasColumnType("numeric(12,3)");

            e.Property(x => x.UnitPriceSnapshot)
                .HasColumnType("numeric(18,2)");

            e.Property(x => x.UnitDiscountSnapshot)
                .HasColumnType("text");

            e.Property(x => x.ItemNameSnapshot)
                .HasMaxLength(256) 
                .IsRequired();

            e.Property(x => x.TaxClassSnapshot)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.CatalogTypeSnapshot)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.TaxRateSnapshotPct)
                .HasColumnType("numeric(6,3)");

           
            e.Property(x => x.PerformedAt)
                .IsRequired();
            
            e.HasOne(x => x.Order)
                .WithMany(o => o.Lines)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CatalogItem)
                .WithMany()
                .HasForeignKey(x => x.CatalogItemId)
                .OnDelete(DeleteBehavior.Restrict);
            
            e.HasMany(x => x.StockMovements)
                .WithOne(sm => sm.OrderLine)
                .HasForeignKey(sm => sm.OrderLineId)
                .OnDelete(DeleteBehavior.Restrict);
            
            e.HasIndex(x => new { x.BusinessId, x.OrderId });
        });

        // ==== Reservation ====
        mb.Entity<Reservation>(e =>
        {
            e.HasKey(x => x.ReservationId);
            e.Property(x => x.Status).HasMaxLength(16).IsRequired();
            e.Property(x => x.TableOrArea).HasMaxLength(64);

            e.HasOne(x => x.Business!)
             .WithMany(x => x.Reservations)
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Employee!)
             .WithMany(x => x.Reservations)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CatalogItem!)
             .WithMany()
             .HasForeignKey(x => x.CatalogItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ==== GiftCard ====
        mb.Entity<GiftCard>(e =>
        {e.HasKey(x => x.GiftCardId);

            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasMaxLength(16).IsRequired(); 
            e.Property(x => x.InitialValue).HasColumnType("bigint");
            e.Property(x => x.Balance).HasColumnType("bigint");

            e.HasOne(x => x.Business)
                .WithMany(b => b.GiftCards)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Payments)
                .WithOne(p => p.GiftCard)
                .HasForeignKey(p => p.GiftCardId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.BusinessId, x.Code }).IsUnique();
        });

        
        mb.Entity<Payment>(e =>
        {e.HasKey(x => x.PaymentId);

   
    e.Property(x => x.AmountCents)
        .IsRequired();

    e.Property(x => x.TipCents)
        .HasDefaultValue(0);

    e.Property(x => x.GiftCardPlannedCents)
        .IsRequired();


    e.Property(x => x.Currency)
        .HasMaxLength(8)
        .IsRequired(); // e.g. "EUR", "USD"

    e.Property(x => x.Method)
        .HasMaxLength(16)
        .IsRequired(); 

    e.Property(x => x.Status)
        .HasMaxLength(32)
        .IsRequired(); 

    e.Property(x => x.StripeSessionId)
        .HasMaxLength(128);
    e.HasOne(p => p.Order)
        .WithMany(o => o.Payments)
        .HasForeignKey(p => p.OrderId)
        .OnDelete(DeleteBehavior.Restrict);


    e.HasOne(p => p.Business)
        .WithMany(b => b.Payments)
        .HasForeignKey(p => p.BusinessId)
        .OnDelete(DeleteBehavior.Restrict);


    
    e.HasOne(p => p.Employee)
        .WithMany()
        .HasForeignKey(p => p.EmployeeId)
        .OnDelete(DeleteBehavior.SetNull);
        

    
    e.HasOne(p => p.GiftCard)
        .WithMany(g => g.Payments)
        .HasForeignKey(p => p.GiftCardId)
        .OnDelete(DeleteBehavior.SetNull);
        
    e.HasIndex(x => new { x.BusinessId, x.OrderId, x.CreatedAt });
        });

        // ==== StockItem (per CatalogItem) ====
        mb.Entity<StockItem>(e =>
        {
            e.HasKey(x => x.StockItemId);

            e.Property(x => x.Unit)
                .HasMaxLength(16)
                .IsRequired();

            e.Property(x => x.QtyOnHand)
                .HasColumnType("numeric(18,3)");

            e.Property(x => x.AverageUnitCost)
                .HasColumnType("numeric(18,4)");

            
            e.HasOne(x => x.CatalogItem)
                .WithOne()
                .HasForeignKey<StockItem>(x => x.CatalogItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==== StockMovement (audit; link to OrderLine when sale/refund) ====
        mb.Entity<StockMovement>(e =>
        {
            e.HasKey(x => x.StockMovementId);
            e.Property(x => x.Type).HasMaxLength(16).IsRequired(); // Receive / Sale / RefundReturn / Waste / Adjust
            e.Property(x => x.Delta).HasColumnType("numeric(18,3)");
            e.Property(x => x.UnitCostSnapshot).HasColumnType("numeric(18,4)");

            e.HasOne(x => x.StockItem!)
             .WithMany(x => x.StockMovements)
             .HasForeignKey(x => x.StockItemId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.OrderLine)
             .WithMany(x => x.StockMovements)
             .HasForeignKey(x => x.OrderLineId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.StockItemId, x.At });
        });
    }
}
