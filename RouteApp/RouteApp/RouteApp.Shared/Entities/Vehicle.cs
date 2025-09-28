using RouteApp.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Entities
{
    public class Vehicle : IEntityWithId
    {
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public Provider Provider { get; set; } = default!;
        public string Plate { get; set; } = string.Empty;
        public string? Model { get; set; }
        public string? Brand { get; set; }
        public double CapacityKg { get; set; }
        public double CapacityVolM3 { get; set; }
        public int Seats { get; set; } = 2;
        public string? Type { get; set; }   // van, truck, moto
        public bool IsActive { get; set; } = true;
        public string? CapacityTonnageLabel { get; set; }
        public ICollection<RoutePlan> Routes { get; set; } = new List<RoutePlan>();
    }
}