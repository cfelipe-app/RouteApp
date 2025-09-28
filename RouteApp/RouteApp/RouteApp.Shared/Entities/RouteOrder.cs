using RouteApp.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Entities
{
    public class RouteOrder
    {
        public int RouteId { get; set; }
        public RoutePlan Route { get; set; } = default!;
        public int OrderId { get; set; }
        public Order Order { get; set; } = default!;
        public int StopSequence { get; set; }
        public DateTime? ETA { get; set; }
        public DateTime? ETD { get; set; }
        public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;
        public string? ProofPhotoUrl { get; set; }
        public string? Notes { get; set; }
    }
}