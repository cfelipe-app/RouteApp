CREATE DATABASE RouteAppDB
GO

USE RouteAppDB
GO

/* =========================================================
   RouteApp • Script TODO-EN-UNO (Esquema + Índices + Vistas
   + Triggers + SP + Datos de Simulación) — versión corregida
   SQL Server (T-SQL)
   ========================================================= */

-- OPCIONAL: usar tu BD
-- CREATE DATABASE RouteAppDB;
-- USE RouteAppDB;
-- GO

SET NOCOUNT ON;

------------------------------------------------------------
-- LIMPIEZA (orden seguro)
------------------------------------------------------------
IF OBJECT_ID('dbo.tr_VehicleOffer_Validate','TR') IS NOT NULL DROP TRIGGER dbo.tr_VehicleOffer_Validate;
IF OBJECT_ID('dbo.tr_Route_CloseNoNewOrders','TR')   IS NOT NULL DROP TRIGGER dbo.tr_Route_CloseNoNewOrders;

IF OBJECT_ID('dbo.usp_RouteSheet_PrintA4','P') IS NOT NULL DROP PROCEDURE dbo.usp_RouteSheet_PrintA4;

IF OBJECT_ID('dbo.vw_RouteTripAuto','V')    IS NOT NULL DROP VIEW dbo.vw_RouteTripAuto;
IF OBJECT_ID('dbo.vw_RouteSheetDetail','V') IS NOT NULL DROP VIEW dbo.vw_RouteSheetDetail;
IF OBJECT_ID('dbo.vw_RouteSheetHeader','V') IS NOT NULL DROP VIEW dbo.vw_RouteSheetHeader;
IF OBJECT_ID('dbo.vw_RouteSheet','V')       IS NOT NULL DROP VIEW dbo.vw_RouteSheet;
IF OBJECT_ID('dbo.vw_RouteSummary','V')     IS NOT NULL DROP VIEW dbo.vw_RouteSummary;

IF OBJECT_ID('dbo.VehicleOffer','U')   IS NOT NULL DROP TABLE dbo.VehicleOffer;
IF OBJECT_ID('dbo.RouteOrder','U')     IS NOT NULL DROP TABLE dbo.RouteOrder;
IF OBJECT_ID('dbo.[Route]','U')        IS NOT NULL DROP TABLE dbo.[Route];
IF OBJECT_ID('dbo.[Order]','U')        IS NOT NULL DROP TABLE dbo.[Order];
IF OBJECT_ID('dbo.CapacityRequest','U')IS NOT NULL DROP TABLE dbo.CapacityRequest;
IF OBJECT_ID('dbo.Vehicle','U')        IS NOT NULL DROP TABLE dbo.Vehicle;
IF OBJECT_ID('dbo.Provider','U')       IS NOT NULL DROP TABLE dbo.Provider;
GO

------------------------------------------------------------
-- TABLAS + ÍNDICES
------------------------------------------------------------
-- Provider
CREATE TABLE dbo.Provider
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Provider PRIMARY KEY,
    Name            NVARCHAR(120)     NOT NULL,
    TaxId           NVARCHAR(20)      NULL,
    ContactName     NVARCHAR(120)     NULL,
    Phone           NVARCHAR(40)      NULL,
    Email           NVARCHAR(120)     NULL,
    Address         NVARCHAR(200)     NULL,
    IsActive        BIT               NOT NULL CONSTRAINT DF_Provider_IsActive DEFAULT(1),
    CreatedAt       DATETIME2(0)      NOT NULL CONSTRAINT DF_Provider_CreatedAt DEFAULT(SYSDATETIME())
);
CREATE UNIQUE INDEX UX_Provider_TaxId ON dbo.Provider(TaxId) WHERE TaxId IS NOT NULL;

-- Vehicle (incluye etiqueta de tonelaje)
CREATE TABLE dbo.Vehicle
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vehicle PRIMARY KEY,
    ProviderId      INT               NULL,
    Plate           NVARCHAR(20)      NOT NULL,
    Model           NVARCHAR(60)      NULL,
    Brand           NVARCHAR(60)      NULL,
    CapacityKg      DECIMAL(10,2)     NOT NULL CONSTRAINT DF_Vehicle_CapKg DEFAULT(0),
    CapacityVolM3   DECIMAL(10,3)     NOT NULL CONSTRAINT DF_Vehicle_CapVol DEFAULT(0),
    Seats           INT               NULL,
    [Type]          NVARCHAR(30)      NULL,
    IsActive        BIT               NOT NULL CONSTRAINT DF_Vehicle_IsActive DEFAULT(1),
    CapacityTonnageLabel AS (CONVERT(NVARCHAR(10), CEILING(CAST([CapacityKg] AS DECIMAL(18,4))/1000.0)) + N'tn') PERSISTED
);
ALTER TABLE dbo.Vehicle
  ADD CONSTRAINT FK_Vehicle_Provider FOREIGN KEY (ProviderId) REFERENCES dbo.Provider(Id) ON DELETE SET NULL;
CREATE UNIQUE INDEX UX_Vehicle_Plate ON dbo.Vehicle(Plate);
CREATE INDEX IX_Vehicle_Provider ON dbo.Vehicle(ProviderId);
CREATE INDEX IX_Vehicle_Tonnage  ON dbo.Vehicle(CapacityTonnageLabel);

-- Order (incluye comprobante/guía)
CREATE TABLE dbo.[Order]
(
    Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Order PRIMARY KEY,
    ExternalOrderNo    NVARCHAR(40)      NULL,
    CustomerName       NVARCHAR(160)     NOT NULL,
    CustomerTaxId      NVARCHAR(20)      NULL,
    Address            NVARCHAR(220)     NOT NULL,
    District           NVARCHAR(80)      NULL,
    Province           NVARCHAR(80)      NULL,
    Department         NVARCHAR(80)      NULL,
    WeightKg           DECIMAL(10,2)     NOT NULL CONSTRAINT DF_Order_Weight DEFAULT(0),
    VolumeM3           DECIMAL(10,3)     NOT NULL CONSTRAINT DF_Order_Volume DEFAULT(0),
    Packages           INT               NOT NULL CONSTRAINT DF_Order_Pack DEFAULT(0),
    AmountTotal        DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Order_Amt DEFAULT(0),
    PaymentMethod      NVARCHAR(30)      NULL,
    Latitude           DECIMAL(9,6)      NULL,
    Longitude          DECIMAL(9,6)      NULL,
    BillingDate        DATE              NOT NULL,
    ScheduledDate      DATE              NULL,
    [Status]           NVARCHAR(20)      NOT NULL CONSTRAINT DF_Order_Status DEFAULT(N'Pending'),
    CreatedAt          DATETIME2(0)      NOT NULL CONSTRAINT DF_Order_CreatedAt DEFAULT(SYSDATETIME()),
    InvoiceDoc         NVARCHAR(20)      NULL,
    InvoiceDate        DATE              NULL,
    GuideDoc           NVARCHAR(20)      NULL,
    GuideDate          DATE              NULL,
    TransportRuc       NVARCHAR(11)      NULL,
    TransportName      NVARCHAR(160)     NULL,
    DeliveryDeptGuide  NVARCHAR(80)      NULL
);
ALTER TABLE dbo.[Order] WITH NOCHECK
  ADD CONSTRAINT CK_Order_Status CHECK ([Status] IN (N'Pending',N'Planned',N'Delivered',N'Cancelled'));
CREATE INDEX IX_Order_BillingDate ON dbo.[Order](BillingDate);
CREATE INDEX IX_Order_Scheduled   ON dbo.[Order](ScheduledDate, District);
CREATE INDEX IX_Order_Geo         ON dbo.[Order](Latitude, Longitude) WHERE Latitude IS NOT NULL AND Longitude IS NOT NULL;
CREATE INDEX IX_Order_InvoiceDate ON dbo.[Order](InvoiceDate) WHERE InvoiceDate IS NOT NULL;
CREATE INDEX IX_Order_GuideDate   ON dbo.[Order](GuideDate)   WHERE GuideDate   IS NOT NULL;

-- Route
CREATE TABLE dbo.[Route]
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Route PRIMARY KEY,
    ServiceDate     DATE              NOT NULL,
    VehicleId       INT               NULL,
    ProviderId      INT               NULL,
    [Code]          NVARCHAR(20)      NOT NULL,  -- V1, V2, …
    [Status]        NVARCHAR(20)      NOT NULL CONSTRAINT DF_Route_Status DEFAULT(N'Draft'),
    StartTime       TIME(0)           NULL,
    EndTime         TIME(0)           NULL,
    DistanceKm      DECIMAL(10,2)     NULL,
    DurationMin     DECIMAL(10,2)     NULL,
    ColorHex        CHAR(7)           NULL,
    CreatedAt       DATETIME2(0)      NOT NULL CONSTRAINT DF_Route_CreatedAt DEFAULT(SYSDATETIME())
);
ALTER TABLE dbo.[Route]
  ADD CONSTRAINT FK_Route_Vehicle  FOREIGN KEY (VehicleId)  REFERENCES dbo.Vehicle(Id) ON DELETE SET NULL,
      CONSTRAINT FK_Route_Provider FOREIGN KEY (ProviderId) REFERENCES dbo.Provider(Id) ON DELETE SET NULL;
ALTER TABLE dbo.[Route] WITH NOCHECK
  ADD CONSTRAINT CK_Route_Status CHECK ([Status] IN (N'Draft',N'Planned',N'InProgress',N'Closed'));
CREATE UNIQUE INDEX UX_Route_ServiceDate_Code ON dbo.[Route](ServiceDate, [Code]);
CREATE INDEX IX_Route_ServiceDate_Vehicle     ON dbo.[Route](ServiceDate, VehicleId);

-- RouteOrder (puente)
CREATE TABLE dbo.RouteOrder
(
    RouteId        INT           NOT NULL,
    OrderId        INT           NOT NULL,
    StopSequence   INT           NOT NULL,
    ETA            TIME(0)       NULL,
    ETD            TIME(0)       NULL,
    DeliveryStatus NVARCHAR(20)  NOT NULL CONSTRAINT DF_RouteOrder_Status DEFAULT(N'Pending'),
    ProofPhotoUrl  NVARCHAR(300) NULL,
    Notes          NVARCHAR(300) NULL,
    CONSTRAINT PK_RouteOrder PRIMARY KEY (RouteId, OrderId)
);
ALTER TABLE dbo.RouteOrder
  ADD CONSTRAINT FK_RouteOrder_Route FOREIGN KEY (RouteId) REFERENCES dbo.[Route](Id) ON DELETE CASCADE,
      CONSTRAINT FK_RouteOrder_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order](Id) ON DELETE CASCADE;
ALTER TABLE dbo.RouteOrder WITH NOCHECK
  ADD CONSTRAINT CK_RouteOrder_Status CHECK (DeliveryStatus IN (N'Pending',N'Attempted',N'Delivered'));
CREATE UNIQUE INDEX UX_RouteOrder_Route_Seq ON dbo.RouteOrder(RouteId, StopSequence);
CREATE INDEX IX_RouteOrder_Order            ON dbo.RouteOrder(OrderId);

-- CapacityRequest (con exclusividad)
CREATE TABLE dbo.CapacityRequest
(
    Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CapacityRequest PRIMARY KEY,
    ProviderId         INT               NULL, -- NULL = broadcast
    OnlyTargetProvider BIT               NOT NULL CONSTRAINT DF_CR_OnlyTarget DEFAULT(0),
    ServiceDate        DATE              NOT NULL,
    Zone               NVARCHAR(120)     NULL,
    DemandWeightKg     DECIMAL(10,2)     NOT NULL CONSTRAINT DF_CR_Weight DEFAULT(0),
    DemandVolumeM3     DECIMAL(10,3)     NOT NULL CONSTRAINT DF_CR_Volume DEFAULT(0),
    DemandStops        INT               NOT NULL CONSTRAINT DF_CR_Stops DEFAULT(0),
    WindowStart        TIME(0)           NULL,
    WindowEnd          TIME(0)           NULL,
    [Status]           NVARCHAR(20)      NOT NULL CONSTRAINT DF_CR_Status DEFAULT(N'Open'),
    CreatedBy          NVARCHAR(120)     NULL,
    CreatedAt          DATETIME2(0)      NOT NULL CONSTRAINT DF_CR_CreatedAt DEFAULT(SYSDATETIME())
);
ALTER TABLE dbo.CapacityRequest
  ADD CONSTRAINT FK_CR_Provider FOREIGN KEY (ProviderId) REFERENCES dbo.Provider(Id) ON DELETE SET NULL;
ALTER TABLE dbo.CapacityRequest WITH NOCHECK
  ADD CONSTRAINT CK_CR_Status CHECK ([Status] IN (N'Open',N'Matching',N'Closed',N'Cancelled'));
CREATE INDEX IX_CR_ServiceDate ON dbo.CapacityRequest(ServiceDate);
CREATE INDEX IX_CR_Status      ON dbo.CapacityRequest([Status]);
CREATE INDEX IX_CR_Target      ON dbo.CapacityRequest(ProviderId, OnlyTargetProvider, ServiceDate);

-- VehicleOffer
CREATE TABLE dbo.VehicleOffer
(
    Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleOffer PRIMARY KEY,
    CapacityRequestId  INT               NOT NULL,
    ProviderId         INT               NOT NULL,
    VehicleId          INT               NOT NULL,
    OfferedWeightKg    DECIMAL(10,2)     NOT NULL CONSTRAINT DF_VO_Weight DEFAULT(0),
    OfferedVolumeM3    DECIMAL(10,3)     NOT NULL CONSTRAINT DF_VO_Volume DEFAULT(0),
    Price              DECIMAL(12,2)     NOT NULL CONSTRAINT DF_VO_Price  DEFAULT(0),
    Currency           CHAR(3)           NOT NULL CONSTRAINT DF_VO_Currency DEFAULT('PEN'),
    [Status]           NVARCHAR(20)      NOT NULL CONSTRAINT DF_VO_Status DEFAULT(N'Offered'),
    Notes              NVARCHAR(300)     NULL,
    CreatedAt          DATETIME2(0)      NOT NULL CONSTRAINT DF_VO_CreatedAt DEFAULT(SYSDATETIME()),
    DecisionAt         DATETIME2(0)      NULL
);
ALTER TABLE dbo.VehicleOffer
  ADD CONSTRAINT FK_VO_CR       FOREIGN KEY (CapacityRequestId) REFERENCES dbo.CapacityRequest(Id) ON DELETE CASCADE,
      CONSTRAINT FK_VO_Provider FOREIGN KEY (ProviderId)        REFERENCES dbo.Provider(Id)        ON DELETE CASCADE,
      CONSTRAINT FK_VO_Vehicle  FOREIGN KEY (VehicleId)         REFERENCES dbo.Vehicle(Id)         ON DELETE CASCADE;
ALTER TABLE dbo.VehicleOffer WITH NOCHECK
  ADD CONSTRAINT CK_VO_Status CHECK ([Status] IN (N'Offered',N'Accepted',N'Rejected',N'Expired'));
CREATE INDEX IX_VO_CR_Status ON dbo.VehicleOffer(CapacityRequestId, [Status]);
CREATE INDEX IX_VO_Provider  ON dbo.VehicleOffer(ProviderId);
CREATE INDEX IX_VO_Vehicle   ON dbo.VehicleOffer(VehicleId);
GO

------------------------------------------------------------
-- TRIGGERS (cada uno en su batch)
------------------------------------------------------------
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
        RAISERROR(N'No se pueden agregar pedidos a una ruta cerrada.', 16, 1);
        RETURN;
    END

    INSERT dbo.RouteOrder (RouteId, OrderId, StopSequence, ETA, ETD, DeliveryStatus, ProofPhotoUrl, Notes)
    SELECT RouteId, OrderId, StopSequence, ETA, ETD, DeliveryStatus, ProofPhotoUrl, Notes
    FROM inserted;
END;
GO

CREATE TRIGGER dbo.tr_VehicleOffer_Validate
ON dbo.VehicleOffer
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Exclusividad y pertenencia del vehículo
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.CapacityRequest cr ON cr.Id = i.CapacityRequestId
        JOIN dbo.Vehicle v ON v.Id = i.VehicleId
        WHERE
            (cr.OnlyTargetProvider = 1 AND cr.ProviderId IS NOT NULL AND i.ProviderId <> cr.ProviderId)
            OR
            (v.ProviderId IS NULL OR v.ProviderId <> i.ProviderId)
    )
    BEGIN
        RAISERROR(N'Oferta inválida: proveedor no coincide con solicitud dirigida y/o el vehículo no pertenece al proveedor.', 16, 1);
        RETURN;
    END

    -- Capacidad ofrecida no debe exceder la del vehículo
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Vehicle v ON v.Id = i.VehicleId
        WHERE (i.OfferedWeightKg  > v.CapacityKg)
           OR (i.OfferedVolumeM3 > v.CapacityVolM3)
    )
    BEGIN
        RAISERROR(N'Oferta inválida: la capacidad ofrecida excede la capacidad del vehículo.', 16, 1);
        RETURN;
    END

    INSERT dbo.VehicleOffer (CapacityRequestId, ProviderId, VehicleId, OfferedWeightKg, OfferedVolumeM3, Price, Currency, [Status], Notes, CreatedAt, DecisionAt)
    SELECT CapacityRequestId, ProviderId, VehicleId, OfferedWeightKg, OfferedVolumeM3, Price, Currency, [Status], Notes, COALESCE(CreatedAt, SYSDATETIME()), DecisionAt
    FROM inserted;
END;
GO

------------------------------------------------------------
-- VISTAS (cada una en su batch)
------------------------------------------------------------
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

CREATE VIEW dbo.vw_RouteSheet AS
SELECT
    r.ServiceDate,
    r.[Code]              AS TripCode,
    r.[Status]            AS RouteStatus,
    r.StartTime, r.EndTime,
    v.Plate, v.Brand, v.Model, v.CapacityKg, v.CapacityVolM3, v.CapacityTonnageLabel,
    p.Name                AS ProviderName,
    ro.StopSequence,
    o.ExternalOrderNo,
    o.CustomerName,
    o.Address, o.District, o.Province, o.Department,
    o.WeightKg, o.VolumeM3, o.Packages, o.AmountTotal, o.PaymentMethod,
    o.InvoiceDoc, o.InvoiceDate,
    o.GuideDoc,   o.GuideDate,
    o.TransportRuc, o.TransportName, o.DeliveryDeptGuide,
    o.Latitude, o.Longitude,
    ro.ETA, ro.ETD,
    ro.DeliveryStatus
FROM dbo.[Route] r
LEFT JOIN dbo.Vehicle v   ON v.Id = r.VehicleId
LEFT JOIN dbo.Provider p  ON p.Id = r.ProviderId
JOIN dbo.RouteOrder ro    ON ro.RouteId = r.Id
JOIN dbo.[Order] o        ON o.Id = ro.OrderId;
GO

CREATE VIEW dbo.vw_RouteSheetHeader AS
SELECT
    r.Id AS RouteId,
    r.ServiceDate,
    r.[Code]            AS TripCode,
    r.[Status]          AS RouteStatus,
    r.StartTime, r.EndTime,
    r.DistanceKm, r.DurationMin,
    v.Id AS VehicleId,
    v.Plate, v.Brand, v.Model, v.CapacityKg, v.CapacityVolM3, v.CapacityTonnageLabel,
    p.Id AS ProviderId,
    p.Name AS ProviderName, p.TaxId AS ProviderRuc,
    p.ContactName, p.Phone, p.Email
FROM dbo.[Route] r
LEFT JOIN dbo.Vehicle v  ON v.Id = r.VehicleId
LEFT JOIN dbo.Provider p ON p.Id = r.ProviderId;
GO

CREATE VIEW dbo.vw_RouteSheetDetail AS
SELECT
    r.Id AS RouteId,
    r.ServiceDate,
    r.[Code]            AS TripCode,
    ro.StopSequence,
    o.ExternalOrderNo,
    o.CustomerName,
    o.Address, o.District, o.Province, o.Department,
    o.InvoiceDoc, o.InvoiceDate,
    o.GuideDoc,   o.GuideDate,
    o.TransportRuc, o.TransportName, o.DeliveryDeptGuide,
    o.WeightKg, o.VolumeM3, o.Packages, o.AmountTotal, o.PaymentMethod,
    ro.ETA, ro.ETD, ro.DeliveryStatus
FROM dbo.[Route] r
JOIN dbo.RouteOrder ro ON ro.RouteId = r.Id
JOIN dbo.[Order] o     ON o.Id = ro.OrderId;
GO

-- Versión sin CTE para evitar el error del ;WITH
CREATE VIEW dbo.vw_RouteTripAuto AS
SELECT *
FROM (
    SELECT r.*,
           'V' + CAST(ROW_NUMBER() OVER (
                PARTITION BY r.ServiceDate, r.VehicleId ORDER BY r.Id
           ) AS NVARCHAR(10)) AS TripCodeAuto
    FROM dbo.[Route] r
) AS ranked;
GO

------------------------------------------------------------
-- PROCEDIMIENTO (batch propio)
------------------------------------------------------------
CREATE PROCEDURE dbo.usp_RouteSheet_PrintA4
    @ServiceDate DATE,
    @VehicleId   INT,
    @TripCode    NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    -- Encabezado
    SELECT TOP(1)
        r.ServiceDate,
        r.[Code]          AS TripCode,
        r.[Status]        AS RouteStatus,
        r.StartTime, r.EndTime,
        r.DistanceKm, r.DurationMin,
        v.Plate, v.Brand, v.Model, v.CapacityKg, v.CapacityVolM3, v.CapacityTonnageLabel,
        p.Name            AS ProviderName,
        p.TaxId           AS ProviderRuc,
        p.ContactName, p.Phone, p.Email
    FROM dbo.[Route] r
    LEFT JOIN dbo.Vehicle v   ON v.Id = r.VehicleId
    LEFT JOIN dbo.Provider p  ON p.Id = r.ProviderId
    WHERE r.ServiceDate = @ServiceDate
      AND r.VehicleId   = @VehicleId
      AND r.[Code]      = @TripCode;

    -- Detalle
    SELECT
        ro.StopSequence,
        o.ExternalOrderNo,
        o.CustomerName,
        o.Address, o.District, o.Province, o.Department,
        o.InvoiceDoc, o.InvoiceDate,
        o.GuideDoc,   o.GuideDate,
        o.TransportRuc, o.TransportName, o.DeliveryDeptGuide,
        o.WeightKg, o.VolumeM3, o.Packages, o.AmountTotal, o.PaymentMethod,
        ro.ETA, ro.ETD, ro.DeliveryStatus,
        o.Latitude, o.Longitude
    FROM dbo.[Route] r
    JOIN dbo.RouteOrder ro ON ro.RouteId = r.Id
    JOIN dbo.[Order] o     ON o.Id = ro.OrderId
    WHERE r.ServiceDate = @ServiceDate
      AND r.VehicleId   = @VehicleId
      AND r.[Code]      = @TripCode
    ORDER BY ro.StopSequence;
END;
GO

------------------------------------------------------------
-- DATOS DE SIMULACIÓN
------------------------------------------------------------
INSERT INTO dbo.Provider (Name, TaxId, ContactName, Phone, Email, Address)
VALUES
(N'Transporte Andina S.A.', '20111111111', N'Carlos Gómez', '999111222', 'c.gomez@andina.pe', 'Av. Industrial 123, Lima'),
(N'Logística Norte EIRL',   '20222222222', N'Lucía Rojas',  '988222333', 'l.rojas@norte.com', 'Jr. Amazonas 45, Trujillo'),
(N'Express Sur SAC',        '20333333333', N'Pedro Huamán', '977333444', 'p.huaman@expresssur.pe', 'Av. La Cultura 567, Cusco');

INSERT INTO dbo.Vehicle (ProviderId, Plate, Model, Brand, CapacityKg, CapacityVolM3, Seats, [Type])
VALUES
(1, 'ABC-123', 'Sprinter', 'Mercedes-Benz', 1500, 12, 2, 'Van'),           -- 2tn
(1, 'XYZ-987', 'FH16',     'Volvo',        20000, 60, 3, 'Camión'),        -- 20tn
(2, 'MNO-456', 'Daily',    'Iveco',         3500, 20, 3, 'Camión ligero'), -- 4tn
(3, 'JKL-789', 'Hilux',    'Toyota',        1000,  8, 2, 'Pick-up');       -- 1tn

INSERT INTO dbo.[Order] (ExternalOrderNo, CustomerName, CustomerTaxId, Address, District, Province, Department,
                         WeightKg, VolumeM3, Packages, AmountTotal, PaymentMethod, Latitude, Longitude,
                         BillingDate, ScheduledDate, [Status],
                         InvoiceDoc, InvoiceDate, GuideDoc, GuideDate, TransportRuc, TransportName, DeliveryDeptGuide)
VALUES
('ORD-001', N'Supermercado Lima Norte', '20600011111', 'Av. Tupac Amaru 999', 'Comas', 'Lima', 'Lima',
 250, 2.5, 40, 5200.00, 'Contado', -11.9480, -77.0620, '2025-09-05', '2025-09-07', 'Pending',
 'F002-53335', '2025-09-05', 'T002-21997', '2025-09-07', '20601640148', N'TRANSPORTES VIA T Y T E.I.R.L', N'Lima'),

('ORD-002', N'Bodega San Pedro', '20600022222', 'Jr. Los Olivos 321', 'Los Olivos', 'Lima', 'Lima',
 80, 0.8, 10, 950.00, 'Letras', -11.9760, -77.0720, '2025-09-05', '2025-09-07', 'Pending',
 'F002-53336', '2025-09-05', 'T002-21998', '2025-09-07', '20601052670', N'SERVITRANS DEL CENTRO S.R.L.', N'Lima'),

('ORD-003', N'Tienda El Sol', '20600033333', 'Av. Primavera 456', 'Surco', 'Lima', 'Lima',
 120, 1.2, 15, 1800.00, 'Contado', -12.1200, -77.0100, '2025-09-06', '2025-09-07', 'Pending',
 'F002-53340', '2025-09-06', 'T002-21999', '2025-09-07', '20111111111', N'TRANSPORTE ANDINA S.A.', N'Lima'),

('ORD-004', N'Minimarket Central', '20600044444', 'Av. Grau 123', 'Trujillo', 'Trujillo', 'La Libertad',
 600, 5.0, 60, 7200.00, 'Contado', -8.1100, -79.0300, '2025-09-06', '2025-09-08', 'Pending',
 'F002-54000', '2025-09-06', 'T050-00011', '2025-09-08', '20222222222', N'LOGÍSTICA NORTE EIRL', N'La Libertad');

INSERT INTO dbo.[Route] (ServiceDate, VehicleId, ProviderId, [Code], [Status], DistanceKm, DurationMin, ColorHex, StartTime, EndTime)
VALUES
('2025-09-07', 1, 1, 'V1', 'Planned', 50.5, 120, '#FF5733', '08:00', '10:20'),
('2025-09-07', 3, 2, 'V2', 'Planned', 15.0,  60, '#33C1FF', '09:00', '10:00');

INSERT INTO dbo.RouteOrder (RouteId, OrderId, StopSequence, DeliveryStatus)
VALUES
(1, 1, 1, 'Pending'),
(1, 2, 2, 'Pending'),
(1, 3, 3, 'Pending'),
(2, 4, 1, 'Pending');

INSERT INTO dbo.CapacityRequest (ProviderId, OnlyTargetProvider, ServiceDate, Zone, DemandWeightKg, DemandVolumeM3, DemandStops, WindowStart, WindowEnd, [Status], CreatedBy)
VALUES
(NULL, 0, '2025-09-09', N'Lima Metropolitana', 3000, 25, 20, '08:00', '18:00', 'Open', 'admin'),   -- broadcast
(2,    0, '2025-09-09', N'Trujillo Centro',      500,  5,  5, '09:00', '15:00', 'Open', 'planner'), -- preferido (no exclusivo)
(1,    1, '2025-09-10', N'Lima Metropolitana',  2000, 15, 10, '08:00', '18:00', 'Open', 'planner'); -- exclusivo proveedor 1

INSERT INTO dbo.VehicleOffer (CapacityRequestId, ProviderId, VehicleId, OfferedWeightKg, OfferedVolumeM3, Price, Currency, [Status], Notes)
VALUES
((SELECT MIN(Id) FROM dbo.CapacityRequest WHERE ServiceDate='2025-09-09' AND OnlyTargetProvider=0 AND ProviderId IS NULL),
  1, 1, 1000, 10, 1500.00, 'PEN', 'Offered', N'Andina: van para Lima'),
((SELECT MIN(Id) FROM dbo.CapacityRequest WHERE ServiceDate='2025-09-09' AND OnlyTargetProvider=0 AND ProviderId IS NULL),
  1, 2, 5000, 30, 5000.00, 'PEN', 'Offered', N'Andina: camión completo'),
((SELECT MIN(Id) FROM dbo.CapacityRequest WHERE ServiceDate='2025-09-09' AND ProviderId=2 AND OnlyTargetProvider=0),
  2, 3,  700,  5,  850.00, 'PEN', 'Offered', N'Norte: Trujillo'),
((SELECT MIN(Id) FROM dbo.CapacityRequest WHERE ServiceDate='2025-09-10' AND ProviderId=1 AND OnlyTargetProvider=1),
  1, 1, 1200,  9, 1400.00, 'PEN', 'Offered', N'Andina: van exclusiva');
GO

------------------------------------------------------------
-- PRUEBAS RÁPIDAS (seguras)
------------------------------------------------------------
DECLARE @ServiceDate DATE = '2025-09-07';

SELECT * FROM dbo.vw_RouteSummary WHERE ServiceDate = @ServiceDate ORDER BY [Code];

EXEC dbo.usp_RouteSheet_PrintA4 @ServiceDate=@ServiceDate, @VehicleId=1, @TripCode=N'V1';

SELECT Id, Plate, CapacityKg, CapacityTonnageLabel FROM dbo.Vehicle ORDER BY CapacityKg;

SELECT
  vo.Id, vo.CapacityRequestId, cr.OnlyTargetProvider, cr.ProviderId AS TargetProvider,
  vo.ProviderId, p.Name AS ProviderName,
  vo.VehicleId, v.Plate, v.CapacityKg, v.CapacityTonnageLabel,
  vo.OfferedWeightKg, vo.OfferedVolumeM3, vo.Price, vo.[Status], vo.Notes
FROM dbo.VehicleOffer vo
JOIN dbo.CapacityRequest cr ON cr.Id = vo.CapacityRequestId
JOIN dbo.Provider p ON p.Id = vo.ProviderId
JOIN dbo.Vehicle  v ON v.Id = vo.VehicleId
ORDER BY vo.CapacityRequestId, vo.Price;
