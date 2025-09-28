using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteApp.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalOrderNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CustomerTaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    District = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VolumeM3 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Packages = table.Column<int>(type: "int", nullable: false),
                    AmountTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BillingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceDoc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GuideDoc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GuideDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransportRuc = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    TransportName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    DeliveryDeptGuide = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_InvoiceDoc_GuideDoc",
                table: "Orders",
                columns: new[] { "InvoiceDoc", "GuideDoc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
