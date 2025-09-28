using RouteApp.Shared.Enums;
using RouteApp.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Entities;

public class Order : IEntityWithId
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string? ExternalOrderNo { get; set; }

    [Required, MaxLength(160)]
    [Display(Name = "Nombre del Cliente")]
    public string CustomerName { get; set; } = null!;

    [MaxLength(20)]
    public string? CustomerTaxId { get; set; }

    [Required, MaxLength(220)]
    public string Address { get; set; } = null!;

    [MaxLength(80)] public string? District { get; set; }
    [MaxLength(80)] public string? Province { get; set; }
    [MaxLength(80)] public string? Department { get; set; }

    public decimal WeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
    public int Packages { get; set; }
    public decimal AmountTotal { get; set; }

    [MaxLength(30)]
    public string? PaymentMethod { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public DateTime BillingDate { get; set; }
    public DateTime? ScheduledDate { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; }

    // Documentos logísticos
    [MaxLength(20)] public string? InvoiceDoc { get; set; }

    public DateTime? InvoiceDate { get; set; }
    [MaxLength(20)] public string? GuideDoc { get; set; }
    public DateTime? GuideDate { get; set; }
    [MaxLength(11)] public string? TransportRuc { get; set; }
    [MaxLength(160)] public string? TransportName { get; set; }
    [MaxLength(80)] public string? DeliveryDeptGuide { get; set; }

    // Navegaciones
    public ICollection<RouteOrder> RouteOrders { get; set; } = new List<RouteOrder>();
}