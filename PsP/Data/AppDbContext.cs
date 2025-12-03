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
            e.HasMany(x => x.Employees)
             .WithOne(x => x.Business)
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ==== Employee ====
        mb.Entity<Employee>(e =>
        {
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.HasIndex(x => new { x.BusinessId, x.Role });
        });

        // ==== CatalogItem ====
        mb.Entity<CatalogItem>(e =>
        {
            e.HasKey(x => x.CatalogItemId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Type).HasMaxLength(16).IsRequired();      // "Product" | "Service"
            e.Property(x => x.Status).HasMaxLength(16).IsRequired();    // "Draft" | "Active" | "Archived"
            e.Property(x => x.TaxClass).HasMaxLength(32).IsRequired();  // "Food" | "Service" | ...
            e.Property(x => x.BasePrice).HasColumnType("numeric(18,2)");
            e.HasIndex(x => new { x.BusinessId, x.Code }).IsUnique();
            e.HasOne(x => x.Business!)
             .WithMany(x => x.CatalogItems)
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Restrict);
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

        // ==== Discount & DiscountEligibility (M:N for line-scoped eligibility) ====
        mb.Entity( (Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Discount>>)(e =>
        {
            e.HasKey(x => x.DiscountId);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Type).HasMaxLength(16).IsRequired();   // "Percent" | "Amount"
            e.Property(x => x.Scope).HasMaxLength(16).IsRequired();  // "Order" | "Line"
            e.Property(x => x.Status).HasMaxLength(16).IsRequired(); // "Active" | "Inactive"
            e.Property(x => x.Value).HasColumnType("numeric(18,2)");
            e.HasIndex(x => new { x.BusinessId, x.Code }).IsUnique();

            e.HasOne(x => x.Business!)
             .WithMany(x => x.Discounts)
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Eligibilities)
             .WithOne(x => x.Discount!)
             .HasForeignKey(x => x.DiscountId)
             .OnDelete(DeleteBehavior.Cascade);
        }));

        mb.Entity<DiscountEligibility>(e =>
        {
            e.HasKey(x => new { x.DiscountId, x.CatalogItemId }); // composite
            e.HasOne(x => x.CatalogItem!)
             .WithMany(x => x.DiscountEligibilities)
             .HasForeignKey(x => x.CatalogItemId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ==== Order ====
        mb.Entity<Order>(e =>
        {
            e.HasKey(x => x.OrderId);
            e.Property(x => x.Status).HasMaxLength(16).IsRequired(); // "Open" | "Closed" | "Cancelled" | "Refunded"
            e.Property(x => x.TableOrArea).HasMaxLength(64);
            e.Property(x => x.TipAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.OrderDiscountSnapshot).HasColumnType("text");
            // e.Property(x => x.OrderDiscountValueSnapshot).HasColumnType("numeric(5,2)");

            
            e.HasOne<Business>(x => x.Business)
             .WithMany(x => x.Orders)
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Employee>(x => x.Employee)
             .WithMany(x => x.Orders)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Reservation>(x => x.Reservation)
             .WithOne(r => r.Order)
             .HasForeignKey<Order>(x => x.ReservationId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.BusinessId, x.CreatedAt });
        });

        // ==== OrderLine ====
        mb.Entity<OrderLine>(e =>
        {
            e.HasKey(x => x.OrderLineId);
            e.Property(x => x.Qty).HasColumnType("numeric(12,3)");
            e.Property(x => x.UnitPriceSnapshot).HasColumnType("numeric(18,2)");
            e.Property(x => x.UnitDiscountSnapshot).HasColumnType("text");
            e.Property(x => x.TaxClassSnapshot).HasMaxLength(32).IsRequired();
            e.Property(x => x.TaxRateSnapshotPct).HasColumnType("numeric(6,3)");

            e.HasOne<Order>(x => x.Order)
             .WithMany(x => x.Lines)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<CatalogItem>(x => x.CatalogItem)
             .WithMany()
             .HasForeignKey(x => x.CatalogItemId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new {  x.BusinessId,x.OrderId });
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
        {
            e.HasKey(x => x.GiftCardId);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasMaxLength(16).IsRequired(); // "Active" | "Blocked" | "Expired"
            e.Property(x => x.InitialValue).HasColumnType("numeric(18,2)");
            e.Property(x => x.Balance).HasColumnType("numeric(18,2)");

            e.HasOne(x => x.Business!)
             .WithMany(x => x.GiftCards)
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.BusinessId, x.Code }).IsUnique();
        });

        // ==== Payment (even if not implementing flows yet) ====
        mb.Entity<Payment>(e =>
        {
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.AmountCents).IsRequired(); // negative for refunds
            e.Property(x => x.TipCents).HasDefaultValue(0);
            e.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            e.Property(x => x.Method).HasMaxLength(16).IsRequired(); // Cash / CardDebit / CardCredit / GiftCard
    
            e.HasOne<Order>(x => x.Order)
             .WithMany(x => x.Payments)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Business>(x => x.Business!)
             .WithMany(x => x.Payments)
             .HasForeignKey(x => x.BusinessId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.BusinessId, x.OrderId, x.CreatedAt });
        });

        // ==== StockItem (per CatalogItem) ====
        mb.Entity<StockItem>(e =>
        {
            e.HasKey(x => x.StockItemId);
            e.Property(x => x.Unit).HasMaxLength(16).IsRequired(); // pcs/ml/g
            e.Property(x => x.QtyOnHand).HasColumnType("numeric(18,3)");
            e.Property(x => x.AverageUnitCost).HasColumnType("numeric(18,4)");

            e.HasOne(x => x.CatalogItem!)
             .WithOne(x => x.StockItem)
             .HasForeignKey<StockItem>(x => x.CatalogItemId)
             .OnDelete(DeleteBehavior.Cascade);

            // optional scoping by Business if present on your class:
            if (typeof(StockItem).GetProperty("BusinessId") != null)
            {
                e.HasIndex("BusinessId");
            }

            e.HasIndex(x => x.CatalogItemId).IsUnique();
        });

        // ==== StockMovement (audit; link to OrderLine when sale/refund) ====
        mb.Entity<StockMovement>(e =>
        {
            e.HasKey(x => x.StockMovementId);
            e.Property(x => x.Type).HasMaxLength(16).IsRequired(); // Receive / Sale / RefundReturn / Waste / Adjust
            e.Property(x => x.Delta).HasColumnType("numeric(18,3)");
            e.Property(x => x.UnitCostSnapshot).HasColumnType("numeric(18,4)");

            e.HasOne(x => x.StockItem!)
             .WithMany(x => x.StockMovement)
             .HasForeignKey(x => x.StockItemId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.OrderLine)
             .WithMany(x => x.StockMovement)
             .HasForeignKey(x => x.OrderLineId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.StockItemId, x.At });
        });
    }
}
