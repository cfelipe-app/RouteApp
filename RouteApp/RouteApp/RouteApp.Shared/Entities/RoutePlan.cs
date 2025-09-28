using RouteApp.Shared.Enums;
using RouteApp.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Entities
{
    public class RoutePlan : IEntityWithId
    {
        public int Id { get; set; }
        public DateTime ServiceDate { get; set; }
        public int? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        public int? ProviderId { get; set; }
        public Provider? Provider { get; set; }
        public string? Code { get; set; }       // e.g., "V1", "Z-02"
        public RouteStatus Status { get; set; } = RouteStatus.Draft;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double DistanceKm { get; set; }
        public double DurationMin { get; set; }
        public string? ColorHex { get; set; }
        public string? DriverUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RouteOrder> Orders { get; set; } = new List<RouteOrder>();
    }
}