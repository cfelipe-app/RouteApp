using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Enums
{
    public enum RouteStatus
    {
        [Description("Inactiva")]
        Draft = 0,

        [Description("Planificada")]
        Planned = 1,

        [Description("En progreso")]
        InProgress = 2,

        [Description("Completado")]
        Completed = 3,

        [Description("Cancelado")]
        Cancelled = 9
    }
}