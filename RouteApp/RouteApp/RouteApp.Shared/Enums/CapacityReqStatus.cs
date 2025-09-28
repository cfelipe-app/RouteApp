using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Enums
{
    public enum CapacityReqStatus
    {
        [Description("Abierta")]
        Open = 0,

        Quoted = 1,

        Awarded = 2,

        [Description("Cerrada")]
        Closed = 3,

        [Description("Cancelada")]
        Cancelled = 9
    }
}