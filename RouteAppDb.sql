CREATE DATABASE db_routeapp
GO

USE db_routeapp
GO

/* =========================================================
   RouteApp • Esquema base (SQL Server)
   Tablas: Provider, Vehicle, [Order], [Route], RouteOrder,
           CapacityRequest, VehicleOffer
   ========================================================= */

-- Limpieza (orden de dependencias)
IF OBJECT_ID('dbo.VehicleOffer','U') IS NOT NULL DROP TABLE dbo.VehicleOffer;
IF OBJECT_ID('dbo.RouteOrder','U')    IS NOT NULL DROP TABLE dbo.RouteOrder;
IF OBJECT_ID('dbo.[Route]','U')       IS NOT NULL DROP TABLE dbo.[Route];
IF OBJECT_ID('dbo.[Order]','U')       IS NOT NULL DROP TABLE dbo.[Order];
IF OBJECT_ID('dbo.CapacityRequest','U') IS NOT NULL DROP TABLE dbo.CapacityRequest;
IF OBJECT_ID('dbo.Vehicle','U')       IS NOT NULL DROP TABLE dbo.Vehicle;
IF OBJECT_ID('dbo.Provider','U')      IS NOT NULL DROP TABLE dbo.Provider;
GO

/* =========================
   Tabla: Provider
   ========================= */
CREATE TABLE dbo.Provider
(
    Id              INT IDENTITY(1,1)      NOT NULL CONSTRAINT PK_Provider PRIMARY KEY,
    Name            NVARCHAR(120)          NOT NULL,
    TaxId           NVARCHAR(20)           NULL,     -- RUC u otro
    ContactName     NVARCHAR(120)          NULL,
    Phone           NVARCHAR(40)           NULL,
    Email           NVARCHAR(120)          NULL,
    Address         NVARCHAR(200)          NULL,
    IsActive        BIT                    NOT NULL  CONSTRAINT DF_Provider_IsActive DEFAULT(1),
    CreatedAt       DATETIME2(0)           NOT NULL  CONSTRAINT DF_Provider_CreatedAt DEFAULT(SYSDATETIME())
);
GO

-- Unicidad opcional de RUC
CREATE UNIQUE INDEX UX_Provider_TaxId ON dbo.Provider(TaxId) WHERE TaxId IS NOT NULL;
GO

/* =========================
   Tabla: Vehicle
   ========================= */
CREATE TABLE dbo.Vehicle
(
    Id              INT IDENTITY(1,1)      NOT NULL CONSTRAINT PK_Vehicle PRIMARY KEY,
    ProviderId      INT                    NULL,
    Plate           NVARCHAR(20)           NOT NULL,
    Model           NVARCHAR(60)           NULL,
    Brand           NVARCHAR(60)           NULL,
    CapacityKg      DECIMAL(10,2)          NOT NULL  CONSTRAINT DF_Vehicle_CapKg DEFAULT(0),
    CapacityVolM3   DECIMAL(10,3)          NOT NULL  CONSTRAINT DF_Vehicle_CapVol DEFAULT(0),
    Seats           INT                    NULL,
    [Type]          NVARCHAR(30)           NULL,     -- van, camión, moto, etc.
    IsActive        BIT                    NOT NULL  CONSTRAINT DF_Vehicle_IsActive DEFAULT(1)
);
GO

ALTER TABLE dbo.Vehicle
ADD CONSTRAINT FK_Vehicle_Provider
FOREIGN KEY (ProviderId) REFERENCES dbo.Provider(Id) ON DELETE SET NULL;
GO

-- Placa única
CREATE UNIQUE INDEX UX_Vehicle_Plate ON dbo.Vehicle(Plate);
-- Búsquedas comunes
CREATE INDEX IX_Vehicle_Provider ON dbo.Vehicle(ProviderId);
GO

/* =========================
   Tabla: Order
   ========================= */
CREATE TABLE dbo.[Order]
(
    Id              INT IDENTITY(1,1)      NOT NULL CONSTRAINT PK_Order PRIMARY KEY,
    ExternalOrderNo NVARCHAR(40)           NULL,
    CustomerName    NVARCHAR(160)          NOT NULL,
    CustomerTaxId   NVARCHAR(20)           NULL,
    Address         NVARCHAR(220)          NOT NULL,
    District        NVARCHAR(80)           NULL,
    Province        NVARCHAR(80)           NULL,
    Department      NVARCHAR(80)           NULL,
    WeightKg        DECIMAL(10,2)          NOT NULL  CONSTRAINT DF_Order_Weight DEFAULT(0),
    VolumeM3        DECIMAL(10,3)          NOT NULL  CONSTRAINT DF_Order_Volume DEFAULT(0),
    Packages        INT                    NOT NULL  CONSTRAINT DF_Order_Pack DEFAULT(0),
    AmountTotal     DECIMAL(12,2)          NOT NULL  CONSTRAINT DF_Order_Amt DEFAULT(0),
    PaymentMethod   NVARCHAR(30)           NULL,     -- Letras, Contado, etc.
    Latitude        DECIMAL(9,6)           NULL,
    Longitude       DECIMAL(9,6)           NULL,
    BillingDate     DATE                   NOT NULL,
    ScheduledDate   DATE                   NULL,
    [Status]        NVARCHAR(20)           NOT NULL  CONSTRAINT DF_Order_Status DEFAULT(N'Pending'),
    CreatedAt       DATETIME2(0)           NOT NULL  CONSTRAINT DF_Order_CreatedAt DEFAULT(SYSDATETIME())
);
GO

-- Restricción de estado
ALTER TABLE dbo.[Order] WITH NOCHECK
ADD CONSTRAINT CK_Order_Status CHECK ([Status] IN (N'Pending',N'Planned',N'Delivered',N'Cancelled'));
GO

-- Índices útiles
CREATE INDEX IX_Order_BillingDate   ON dbo.[Order](BillingDate);
CREATE INDEX IX_Order_Scheduled     ON dbo.[Order](ScheduledDate, District);
CREATE INDEX IX_Order_Geo           ON dbo.[Order](Latitude, Longitude) WHERE Latitude IS NOT NULL AND Longitude IS NOT NULL;
GO

/* =========================
   Tabla: Route
   ========================= */
CREATE TABLE dbo.[Route]
(
    Id              INT IDENTITY(1,1)      NOT NULL CONSTRAINT PK_Route PRIMARY KEY,
    ServiceDate     DATE                   NOT NULL,
    VehicleId       INT                    NULL,
    ProviderId      INT                    NULL,     -- redundante útil p/reportes
    [Code]          NVARCHAR(20)           NOT NULL, -- V1, V2, etc.
    [Status]        NVARCHAR(20)           NOT NULL  CONSTRAINT DF_Route_Status DEFAULT(N'Draft'),
    StartTime       TIME(0)                NULL,
    EndTime         TIME(0)                NULL,
    DistanceKm      DECIMAL(10,2)          NULL,
    DurationMin     DECIMAL(10,2)          NULL,
    ColorHex        CHAR(7)                NULL,     -- '#RRGGBB'
    CreatedAt       DATETIME2(0)           NOT NULL  CONSTRAINT DF_Route_CreatedAt DEFAULT(SYSDATETIME())
);
GO

ALTER TABLE dbo.[Route]
ADD CONSTRAINT FK_Route_Vehicle   FOREIGN KEY (VehicleId)  REFERENCES dbo.Vehicle(Id)  ON DELETE SET NULL,
    CONSTRAINT FK_Route_Provider  FOREIGN KEY (ProviderId) REFERENCES dbo.Provider(Id) ON DELETE SET NULL;
GO

ALTER TABLE dbo.[Route] WITH NOCHECK
ADD CONSTRAINT CK_Route_Status CHECK ([Status] IN (N'Draft',N'Planned',N'InProgress',N'Closed'));
GO

-- Evitar duplicados de código por fecha
CREATE UNIQUE INDEX UX_Route_ServiceDate_Code ON dbo.[Route](ServiceDate, [Code]);
CREATE INDEX IX_Route_ServiceDate_Vehicle ON dbo.[Route](ServiceDate, VehicleId);
GO

/* =========================
   Tabla puente: RouteOrder
   ========================= */
CREATE TABLE dbo.RouteOrder
(
    RouteId         INT                    NOT NULL,
    OrderId         INT                    NOT NULL,
    StopSequence    INT                    NOT NULL,   -- orden de visita
    ETA             TIME(0)                NULL,
    ETD             TIME(0)                NULL,
    DeliveryStatus  NVARCHAR(20)           NOT NULL  CONSTRAINT DF_RouteOrder_Status DEFAULT(N'Pending'),
    ProofPhotoUrl   NVARCHAR(300)          NULL,
    Notes           NVARCHAR(300)          NULL,
    CONSTRAINT PK_RouteOrder PRIMARY KEY (RouteId, OrderId)
);
GO

ALTER TABLE dbo.RouteOrder
ADD CONSTRAINT FK_RouteOrder_Route FOREIGN KEY (RouteId) REFERENCES dbo.[Route](Id) ON DELETE CASCADE,
    CONSTRAINT FK_RouteOrder_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order](Id) ON DELETE CASCADE;
GO

ALTER TABLE dbo.RouteOrder WITH NOCHECK
ADD CONSTRAINT CK_RouteOrder_Status CHECK (DeliveryStatus IN (N'Pending',N'Attempted',N'Delivered'));
GO

-- Unicidad de secuencia por ruta
CREATE UNIQUE INDEX UX_RouteOrder_Route_Seq ON dbo.RouteOrder(RouteId, StopSequence);
CREATE INDEX IX_RouteOrder_Order ON dbo.RouteOrder(OrderId);
GO

/* =========================
   Tabla: CapacityRequest
   ========================= */
CREATE TABLE dbo.CapacityRequest
(
    Id                INT IDENTITY(1,1)    NOT NULL CONSTRAINT PK_CapacityRequest PRIMARY KEY,
    ProviderId        INT                  NULL,     -- NULL = broadcast
    ServiceDate       DATE                 NOT NULL,
    Zone              NVARCHAR(120)        NULL,
    DemandWeightKg    DECIMAL(10,2)        NOT NULL  CONSTRAINT DF_CR_Weight DEFAULT(0),
    DemandVolumeM3    DECIMAL(10,3)        NOT NULL  CONSTRAINT DF_CR_Volume DEFAULT(0),
    DemandStops       INT                  NOT NULL  CONSTRAINT DF_CR_Stops DEFAULT(0),
    WindowStart       TIME(0)              NULL,
    WindowEnd         TIME(0)              NULL,
    [Status]          NVARCHAR(20)         NOT NULL  CONSTRAINT DF_CR_Status DEFAULT(N'Open'),
    CreatedBy         NVARCHAR(120)        NULL,
    CreatedAt         DATETIME2(0)         NOT NULL  CONSTRAINT DF_CR_CreatedAt DEFAULT(SYSDATETIME())
);
GO

ALTER TABLE dbo.CapacityRequest
ADD CONSTRAINT FK_CR_Provider FOREIGN KEY (ProviderId) REFERENCES dbo.Provider(Id) ON DELETE SET NULL;
GO

ALTER TABLE dbo.CapacityRequest WITH NOCHECK
ADD CONSTRAINT CK_CR_Status CHECK ([Status] IN (N'Open',N'Matching',N'Closed',N'Cancelled'));
GO

CREATE INDEX IX_CR_ServiceDate ON dbo.CapacityRequest(ServiceDate);
CREATE INDEX IX_CR_Status ON dbo.CapacityRequest([Status]);
GO

/* =========================
   Tabla: VehicleOffer
   ========================= */
CREATE TABLE dbo.VehicleOffer
(
    Id                  INT IDENTITY(1,1)  NOT NULL CONSTRAINT PK_VehicleOffer PRIMARY KEY,
    CapacityRequestId   INT                NOT NULL,
    ProviderId          INT                NOT NULL,
    VehicleId           INT                NOT NULL,
    OfferedWeightKg     DECIMAL(10,2)      NOT NULL  CONSTRAINT DF_VO_Weight DEFAULT(0),
    OfferedVolumeM3     DECIMAL(10,3)      NOT NULL  CONSTRAINT DF_VO_Volume DEFAULT(0),
    Price               DECIMAL(12,2)      NOT NULL  CONSTRAINT DF_VO_Price  DEFAULT(0),
    Currency            CHAR(3)            NOT NULL  CONSTRAINT DF_VO_Currency DEFAULT('PEN'),
    [Status]            NVARCHAR(20)       NOT NULL  CONSTRAINT DF_VO_Status DEFAULT(N'Offered'),
    Notes               NVARCHAR(300)      NULL,
    CreatedAt           DATETIME2(0)       NOT NULL  CONSTRAINT DF_VO_CreatedAt DEFAULT(SYSDATETIME()),
    DecisionAt          DATETIME2(0)       NULL
);
GO

ALTER TABLE dbo.VehicleOffer
ADD CONSTRAINT FK_VO_CR       FOREIGN KEY (CapacityRequestId) REFERENCES dbo.CapacityRequest(Id) ON DELETE CASCADE,
    CONSTRAINT FK_VO_Provider FOREIGN KEY (ProviderId)        REFERENCES dbo.Provider(Id)         ON DELETE CASCADE,
    CONSTRAINT FK_VO_Vehicle  FOREIGN KEY (VehicleId)         REFERENCES dbo.Vehicle(Id)          ON DELETE CASCADE;
GO

ALTER TABLE dbo.VehicleOffer WITH NOCHECK
ADD CONSTRAINT CK_VO_Status CHECK ([Status] IN (N'Offered',N'Accepted',N'Rejected',N'Expired'));
GO

-- Índices frecuentes
CREATE INDEX IX_VO_CR_Status    ON dbo.VehicleOffer(CapacityRequestId, [Status]);
CREATE INDEX IX_VO_Provider     ON dbo.VehicleOffer(ProviderId);
CREATE INDEX IX_VO_Vehicle      ON dbo.VehicleOffer(VehicleId);
GO

/* =========================
   Extras útiles
   ========================= */

-- Vista rápida de carga de rutas (resumen)
IF OBJECT_ID('dbo.vw_RouteSummary','V') IS NOT NULL DROP VIEW dbo.vw_RouteSummary;
GO
CREATE VIEW dbo.vw_RouteSummary AS
SELECT
    r.Id,
    r.ServiceDate,
    r.[Code],
    r.[Status],
    v.Plate,
    p.Name AS ProviderName,
    COUNT(ro.OrderId)        AS Stops,
    SUM(o.WeightKg)          AS TotalWeightKg,
    SUM(o.VolumeM3)          AS TotalVolumeM3,
    r.DistanceKm,
    r.DurationMin
FROM dbo.[Route] r
LEFT JOIN dbo.Vehicle v   ON r.VehicleId  = v.Id
LEFT JOIN dbo.Provider p  ON r.ProviderId = p.Id
LEFT JOIN dbo.RouteOrder ro ON ro.RouteId = r.Id
LEFT JOIN dbo.[Order] o     ON o.Id       = ro.OrderId
GROUP BY r.Id, r.ServiceDate, r.[Code], r.[Status], v.Plate, p.Name, r.DistanceKm, r.DurationMin;
GO

-- Trigger opcional: si cierras la ruta, bloquear nuevos pedidos en esa ruta
IF OBJECT_ID('dbo.tr_Route_CloseNoNewOrders','TR') IS NOT NULL DROP TRIGGER dbo.tr_Route_CloseNoNewOrders;
GO
CREATE TRIGGER dbo.tr_Route_CloseNoNewOrders
ON dbo.RouteOrder
INSTEAD OF INSERT
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.[Route] r ON r.Id = i.RouteId
        WHERE r.[Status] = N'Closed'
    )
    BEGIN
        RAISERROR('No se pueden agregar pedidos a una ruta cerrada.', 16, 1);
        RETURN;
    END
    INSERT dbo.RouteOrder (RouteId, OrderId, StopSequence, ETA, ETD, DeliveryStatus, ProofPhotoUrl, Notes)
    SELECT RouteId, OrderId, StopSequence, ETA, ETD, DeliveryStatus, ProofPhotoUrl, Notes
    FROM inserted;
END;
GO

/* =========================================================
   RouteApp • Datos demo
   ========================================================= */

-- Proveedores
INSERT INTO dbo.Provider (Name, TaxId, ContactName, Phone, Email, Address)
VALUES
(N'Transporte Andina S.A.', '20111111111', N'Carlos Gómez', '999111222', 'c.gomez@andina.pe', 'Av. Industrial 123, Lima'),
(N'Logística Norte EIRL', '20222222222', N'Lucía Rojas', '988222333', 'l.rojas@norte.com', 'Jr. Amazonas 45, Trujillo'),
(N'Express Sur SAC', '20333333333', N'Pedro Huamán', '977333444', 'p.huaman@expresssur.pe', 'Av. La Cultura 567, Cusco');

-- Vehículos
INSERT INTO dbo.Vehicle (ProviderId, Plate, Model, Brand, CapacityKg, CapacityVolM3, Seats, [Type])
VALUES
(1, 'ABC-123', 'Sprinter', 'Mercedes-Benz', 1500, 12, 2, 'Van'),
(1, 'XYZ-987', 'FH16', 'Volvo', 20000, 60, 3, 'Camión'),
(2, 'MNO-456', 'Daily', 'Iveco', 3500, 20, 3, 'Camión ligero'),
(3, 'JKL-789', 'Hilux', 'Toyota', 1000, 8, 2, 'Pick-up');

-- Pedidos
INSERT INTO dbo.[Order] (ExternalOrderNo, CustomerName, CustomerTaxId, Address, District, Province, Department,
                         WeightKg, VolumeM3, Packages, AmountTotal, PaymentMethod, Latitude, Longitude, BillingDate, ScheduledDate, [Status])
VALUES
('ORD-001', N'Supermercado Lima Norte', '20600011111', 'Av. Tupac Amaru 999', 'Comas', 'Lima', 'Lima',
 250, 2.5, 40, 5200.00, 'Contado', -11.9480, -77.0620, '2025-09-05', '2025-09-07', 'Pending'),

('ORD-002', N'Bodega San Pedro', '20600022222', 'Jr. Los Olivos 321', 'Los Olivos', 'Lima', 'Lima',
 80, 0.8, 10, 950.00, 'Letras', -11.9760, -77.0720, '2025-09-05', '2025-09-07', 'Pending'),

('ORD-003', N'Tienda El Sol', '20600033333', 'Av. Primavera 456', 'Surco', 'Lima', 'Lima',
 120, 1.2, 15, 1800.00, 'Contado', -12.1200, -77.0100, '2025-09-06', '2025-09-07', 'Pending'),

('ORD-004', N'Minimarket Central', '20600044444', 'Av. Grau 123', 'Trujillo', 'Trujillo', 'La Libertad',
 600, 5.0, 60, 7200.00, 'Contado', -8.1100, -79.0300, '2025-09-06', '2025-09-08', 'Pending');

-- Rutas
INSERT INTO dbo.[Route] (ServiceDate, VehicleId, ProviderId, [Code], [Status], DistanceKm, DurationMin, ColorHex)
VALUES
('2025-09-07', 1, 1, 'V1', 'Planned', 50.5, 120, '#FF5733'),
('2025-09-07', 3, 2, 'V2', 'Planned', 15.0, 60, '#33C1FF');

-- Asignación pedidos a rutas (RouteOrder)
INSERT INTO dbo.RouteOrder (RouteId, OrderId, StopSequence, DeliveryStatus)
VALUES
(1, 1, 1, 'Pending'), -- Supermercado Lima Norte
(1, 2, 2, 'Pending'), -- Bodega San Pedro
(1, 3, 3, 'Pending'), -- Tienda El Sol
(2, 4, 1, 'Pending'); -- Minimarket Central (Trujillo)

-- Solicitudes de capacidad (CapacityRequest)
INSERT INTO dbo.CapacityRequest (ProviderId, ServiceDate, Zone, DemandWeightKg, DemandVolumeM3, DemandStops, WindowStart, WindowEnd, [Status], CreatedBy)
VALUES
(NULL, '2025-09-09', N'Lima Metropolitana', 3000, 25, 20, '08:00', '18:00', 'Open', 'admin'),
(2,    '2025-09-09', N'Trujillo Centro',     500,  5,  5,  '09:00', '15:00', 'Open', 'planner');

-- Ofertas de vehículos (VehicleOffer)
INSERT INTO dbo.VehicleOffer (CapacityRequestId, ProviderId, VehicleId, OfferedWeightKg, OfferedVolumeM3, Price, Currency, [Status], Notes)
VALUES
(1, 1, 1, 1000, 10, 1500.00, 'PEN', 'Offered', N'Van disponible AM'),
(1, 1, 2, 5000, 30, 5000.00, 'PEN', 'Offered', N'Camión completo'),
(2, 3, 4, 800,  6,  900.00,  'PEN', 'Offered', N'Pick-up para reparto rápido');

/* =========================================================
   RouteApp • Consultas
   ========================================================= */

-- 1. Resumen por ruta (stops, peso, volumen, distancia, duración) 
SELECT
    r.Id            AS RouteId,
    r.ServiceDate,
    r.[Code],
    r.[Status],
    v.Plate,
    p.Name          AS ProviderName,
    COUNT(ro.OrderId)              AS Stops,
    SUM(o.WeightKg)                AS TotalWeightKg,
    SUM(o.VolumeM3)                AS TotalVolumeM3,
    r.DistanceKm,
    r.DurationMin
FROM dbo.[Route] r
LEFT JOIN dbo.Vehicle v   ON r.VehicleId  = v.Id
LEFT JOIN dbo.Provider p  ON r.ProviderId = p.Id
LEFT JOIN dbo.RouteOrder ro ON ro.RouteId = r.Id
LEFT JOIN dbo.[Order] o     ON o.Id       = ro.OrderId
WHERE r.ServiceDate = @ServiceDate
GROUP BY r.Id, r.ServiceDate, r.[Code], r.[Status], v.Plate, p.Name, r.DistanceKm, r.DurationMin
ORDER BY r.[Code];
