using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RouteApp.Shared.Entities;
using RouteApp.Shared.Enums;

namespace RouteApp.Backend.Data;

public class SeedDb
{
    private readonly DataContext _context;
    private readonly ILogger<SeedDb> _logger;

    public SeedDb(DataContext context, ILogger<SeedDb> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _context.Database.MigrateAsync(ct);

        await EnsureProvidersAsync(ct);
        await EnsureVehiclesAsync(ct);
        await EnsureOrdersAsync(ct);

        _logger.LogInformation("Seed OK: Providers={P}, Vehicles={V}, Orders={O}",
            await _context.Providers.CountAsync(ct),
            await _context.Vehicles.CountAsync(ct),
            await _context.Orders.CountAsync(ct));
    }

    private async Task EnsureProvidersAsync(CancellationToken ct)
    {
        var p1Tax = "20123456789";
        var p2Tax = "20654321098";

        if (!await _context.Providers.AnyAsync(p => p.TaxId == p1Tax || p.TaxId == p2Tax, ct))
        {
            _context.Providers.AddRange(
                new Provider { Name = "Transporte Lima SAC", TaxId = p1Tax, ContactName = "María Torres", Phone = "(01) 555-0101", Email = "contacto@tlima.pe", Address = "Av. República 123, Lima", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Provider { Name = "Logística Andina SRL", TaxId = p2Tax, ContactName = "Luis Alvarado", Phone = "(01) 555-0202", Email = "ventas@landina.pe", Address = "Av. Industrial 456, Lima", IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task EnsureVehiclesAsync(CancellationToken ct)
    {
        var p1 = await _context.Providers.AsNoTracking().FirstAsync(ct);
        var p2 = await _context.Providers.AsNoTracking().OrderBy(p => p.Id).Skip(1).FirstOrDefaultAsync(ct) ?? p1;

        // evita duplicados por placa
        if (!await _context.Vehicles.AnyAsync(v => v.Plate == "ABC-123", ct))
            _context.Vehicles.Add(new Vehicle { ProviderId = p1.Id, Plate = "ABC-123", Brand = "Hyundai", Model = "H100", CapacityKg = 1500, CapacityVolM3 = 12, Seats = 3, Type = "van", IsActive = true, CapacityTonnageLabel = "1.5T" });

        if (!await _context.Vehicles.AnyAsync(v => v.Plate == "XYZ-987", ct))
            _context.Vehicles.Add(new Vehicle { ProviderId = p1.Id, Plate = "XYZ-987", Brand = "Toyota", Model = "Hiace", CapacityKg = 1200, CapacityVolM3 = 10, Seats = 3, Type = "van", IsActive = true, CapacityTonnageLabel = "1.2T" });

        if (!await _context.Vehicles.AnyAsync(v => v.Plate == "FRT-456", ct))
            _context.Vehicles.Add(new Vehicle { ProviderId = p2.Id, Plate = "FRT-456", Brand = "Volvo", Model = "VM", CapacityKg = 3500, CapacityVolM3 = 24, Seats = 2, Type = "truck", IsActive = true, CapacityTonnageLabel = "3.5T" });

        await _context.SaveChangesAsync(ct);
    }

    private async Task EnsureOrdersAsync(CancellationToken ct)
    {
        if (await _context.Orders.AnyAsync(ct)) return;

        var rnd = new Random(7);
        DateTime hoy = DateTime.Today;
        var fechas = new[] { hoy, hoy, hoy.AddDays(1), hoy.AddDays(2) }; // mayoría hoy

        // usa DECIMAL en coords para calzar con Order.Lat/Long (decimal?)
        var zonas = new (string District, decimal Lat, decimal Lon)[]
        {
            ("Miraflores", -12.121m, -77.030m),
            ("San Isidro", -12.097m, -77.037m),
            ("Santiago de Surco", -12.149m, -76.990m),
            ("San Borja", -12.104m, -76.999m),
            ("La Molina", -12.089m, -76.946m),
            ("Lince", -12.090m, -77.037m),
            ("San Miguel", -12.075m, -77.095m),
            ("Callao", -12.054m, -77.118m),
            ("Ate", -12.042m, -76.940m),
            ("SJL", -12.000m, -76.990m),
            ("Villa El Salvador", -12.209m, -76.941m),
        };

        string[] mediosPago = { "CONTADO", "CREDITO", "LETRAS" };

        decimal Jitter() => ((decimal)rnd.NextDouble() - 0.5m) * 0.01m; // ±~500m
        string NextRuc() => $"{rnd.Next(10, 99)}{rnd.Next(100000000, 999999999)}";

        var orders = new List<Order>();
        int n = 40;
        for (int i = 0; i < n; i++)
        {
            var baseZone = zonas[rnd.Next(zonas.Length)];
            var lat = baseZone.Lat + Jitter();
            var lon = baseZone.Lon + Jitter();
            var fecha = fechas[rnd.Next(fechas.Length)];

            int packs = rnd.Next(1, 10);

            // Cálculos en DECIMAL (nota sufijo m)
            decimal weight = Math.Round(packs * (5m + (decimal)rnd.NextDouble() * 10m), 1);
            decimal volume = Math.Round(packs * (0.02m + (decimal)rnd.NextDouble() * 0.05m), 3);
            decimal amount = Math.Round(packs * (50m + (decimal)rnd.NextDouble() * 250m), 2);

            orders.Add(new Order
            {
                ExternalOrderNo = $"PO-{1000 + i}",
                CustomerName = $"Cliente {i + 1}",
                CustomerTaxId = NextRuc(),
                Address = $"Av. Ejemplo {100 + i}",
                District = baseZone.District,
                Province = "Lima",
                Department = "Lima",

                WeightKg = weight,             // decimal ✔
                VolumeM3 = volume,             // decimal ✔
                Packages = packs,
                AmountTotal = amount,          // decimal ✔
                PaymentMethod = mediosPago[rnd.Next(mediosPago.Length)],

                Latitude = lat,                // decimal? ✔
                Longitude = lon,               // decimal? ✔

                BillingDate = fecha,           // obligatorio (no-nullable) ✔
                ScheduledDate = fecha,         // puede ser null; aquí asignamos

                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,   // por si no hay default en DB

                // Estos son opcionales (pueden quedar null perfectamente)
                InvoiceDoc = null,
                InvoiceDate = null,
                GuideDoc = null,
                GuideDate = null,
                TransportRuc = null,
                TransportName = null,
                DeliveryDeptGuide = null
            });
        }

        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync(ct);
    }
}