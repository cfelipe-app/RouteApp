using RouteApp.Shared.Enums;
using RouteApp.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Entities
{
    public class VehicleOffer : IEntityWithId
    {
        public int Id { get; set; }
        public int CapacityRequestId { get; set; }
        public CapacityRequest CapacityRequest { get; set; } = default!;
        public int ProviderId { get; set; }
        public Provider Provider { get; set; } = default!;
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = default!;
        public double OfferedWeightKg { get; set; }
        public double OfferedVolumeM3 { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "PEN";
        public VehicleOfferStatus Status { get; set; } = VehicleOfferStatus.Draft;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? DecisionAt { get; set; }
    }
}