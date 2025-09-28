using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.DTOs
{
    public class OrderImportDto
    {
        public string? ExternalOrderNo { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? District { get; set; }
        public string? Province { get; set; }
        public string? Department { get; set; }
        public decimal WeightKg { get; set; }
        public decimal VolumeM3 { get; set; }
        public int Packages { get; set; }
        public decimal AmountTotal { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime BillingDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // opcionales de documentos
        public string? InvoiceDoc { get; set; }

        public DateTime? InvoiceDate { get; set; }
        public string? GuideDoc { get; set; }
        public DateTime? GuideDate { get; set; }
    }
}