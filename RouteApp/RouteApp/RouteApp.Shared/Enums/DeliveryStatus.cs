using System.ComponentModel;

namespace RouteApp.Shared.Enums
{
    public enum DeliveryStatus
    {
        [Description("Pendiente")]
        Pending = 0,

        EnRoute = 1,

        [Description("Entregado")]
        Delivered = 2,

        [Description("Fallida")]
        Failed = 3
    }
}