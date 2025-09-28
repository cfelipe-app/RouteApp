using RouteApp.Shared.Enums;
using RouteApp.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Entities
{
    public class CapacityRequest : IEntityWithId
    {
        public int Id { get; set; }
        public int? ProviderId { get; set; }
        public Provider? Provider { get; set; }
        public bool OnlyTargetProvider { get; set; } = false;
        public DateTime ServiceDate { get; set; }
        public string? Zone { get; set; }
        public double DemandWeightKg { get; set; }
        public double DemandVolumeM3 { get; set; }
        public int DemandStops { get; set; }
        public TimeSpan? WindowStart { get; set; }
        public TimeSpan? WindowEnd { get; set; }
        public CapacityReqStatus Status { get; set; } = CapacityReqStatus.Open;
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<VehicleOffer> Offers { get; set; } = new List<VehicleOffer>();
    }
}