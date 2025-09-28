using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Enums
{
    public enum OrderStatus
    {
        [Description("Pendiente")]
        Pending = 0,

        Scheduled = 1,

        Assigned = 2,

        [Description("Entregado")]
        Delivered = 3,

        [Description("Cancelado")]
        Cancelled = 9
    }
}