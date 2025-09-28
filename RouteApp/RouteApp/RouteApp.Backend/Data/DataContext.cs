using Microsoft.EntityFrameworkCore;
using RouteApp.Shared.Entities;

namespace RouteApp.Backend.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<RoutePlan> RoutePlans => Set<RoutePlan>();
    public DbSet<RouteOrder> RouteOrders => Set<RouteOrder>();
    public DbSet<CapacityRequest> CapacityRequests => Set<CapacityRequest>();
    public DbSet<VehicleOffer> VehicleOffers => Set<VehicleOffer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ================= Provider =================
        modelBuilder.Entity<Provider>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.TaxId).HasMaxLength(20);
            e.Property(x => x.ContactName).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.Address).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.TaxId).IsUnique().HasFilter("[TaxId] IS NOT NULL");

            // Filtro global opcional para ocultar inactivos
            e.HasQueryFilter(p => p.IsActive);
        });

        // ================= Vehicle =================
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.Property(v => v.Plate).HasMaxLength(20).IsRequired();
            e.Property(v => v.Model).HasMaxLength(60);
            e.Property(v => v.Brand).HasMaxLength(60);
            e.Property(v => v.Type).HasMaxLength(40);
            e.Property(v => v.CapacityTonnageLabel).HasMaxLength(40);
            e.HasIndex(v => v.Plate).IsUnique();

            e.HasOne(v => v.Provider)
                .WithMany(p => p.Vehicles)
                .HasForeignKey(v => v.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Filtro global opcional para ocultar inactivos
            e.HasQueryFilter(v => v.IsActive);
        });

        // ================= Order =================
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.CustomerName).HasMaxLength(150).IsRequired();
            e.Property(o => o.CustomerTaxId).HasMaxLength(20);
            e.Property(o => o.Address).HasMaxLength(200);
            e.Property(o => o.District).HasMaxLength(100);
            e.Property(o => o.Province).HasMaxLength(100);
            e.Property(o => o.Department).HasMaxLength(100);
            e.Property(o => o.AmountTotal).HasPrecision(18, 2);
            e.Property(o => o.InvoiceDoc).HasMaxLength(40);
            e.Property(o => o.GuideDoc).HasMaxLength(40);
            e.Property(o => o.TransportRuc).HasMaxLength(20);
            e.Property(o => o.TransportName).HasMaxLength(120);
            e.Property(o => o.DeliveryDeptGuide).HasMaxLength(120);
            e.Property(o => o.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Índices útiles
            e.HasIndex(o => new { o.InvoiceDoc, o.GuideDoc });
            e.HasIndex(o => new { o.ScheduledDate, o.Status });
            e.HasIndex(o => new { o.Latitude, o.Longitude });

            // Enum como string
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        // ================= RoutePlan =================
        modelBuilder.Entity<RoutePlan>(e =>
        {
            e.Property(r => r.Code).HasMaxLength(30);
            e.Property(r => r.ColorHex).HasMaxLength(7);
            e.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            e.HasOne(r => r.Provider)
                .WithMany()
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Vehicle)
                .WithMany(v => v.Routes)
                .HasForeignKey(r => r.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(r => new { r.ServiceDate, r.Code }).IsUnique();
            e.HasIndex(r => r.ServiceDate);

            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        // ================= RouteOrder (PK compuesta) =================
        modelBuilder.Entity<RouteOrder>(e =>
        {
            // PK compuesta RouteId + OrderId
            e.HasKey(ro => new { ro.RouteId, ro.OrderId });

            e.HasOne(ro => ro.Route)
                .WithMany(r => r.Orders)
                .HasForeignKey(ro => ro.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ro => ro.Order)
                .WithMany(o => o.RouteOrders)
                .HasForeignKey(ro => ro.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(ro => ro.ProofPhotoUrl).HasMaxLength(300);
            e.Property(ro => ro.Notes).HasMaxLength(500);

            // Secuencia única por ruta
            e.HasIndex(ro => new { ro.RouteId, ro.StopSequence }).IsUnique();

            e.Property(x => x.DeliveryStatus).HasConversion<string>().HasMaxLength(20);
        });

        // ================= CapacityRequest =================
        modelBuilder.Entity<CapacityRequest>(e =>
        {
            e.Property(cr => cr.Zone).HasMaxLength(80);
            e.Property(cr => cr.CreatedBy).HasMaxLength(80);
            e.Property(cr => cr.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(cr => cr.WindowStart).HasColumnType("time");
            e.Property(cr => cr.WindowEnd).HasColumnType("time");

            e.HasOne(cr => cr.Provider)
                .WithMany()
                .HasForeignKey(cr => cr.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(cr => new { cr.ServiceDate, cr.ProviderId });

            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        // ================= VehicleOffer =================
        modelBuilder.Entity<VehicleOffer>(e =>
        {
            e.Property(vo => vo.Currency).HasMaxLength(10);
            e.Property(vo => vo.Notes).HasMaxLength(500);
            e.Property(vo => vo.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(vo => vo.Price).HasPrecision(18, 2);

            e.HasOne(vo => vo.CapacityRequest)
                .WithMany(cr => cr.Offers)
                .HasForeignKey(vo => vo.CapacityRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(vo => vo.Provider)
                .WithMany()
                .HasForeignKey(vo => vo.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(vo => vo.Vehicle)
                .WithMany()
                .HasForeignKey(vo => vo.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(vo => new { vo.CapacityRequestId, vo.VehicleId }).IsUnique();

            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        // Desactivar cascadas por defecto
        DisableCascadeDelete(modelBuilder);
    }

    private static void DisableCascadeDelete(ModelBuilder modelBuilder)
    {
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}