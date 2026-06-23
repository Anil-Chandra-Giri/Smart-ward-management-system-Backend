using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Smart_ward_management_system.Migrations
{
    /// <inheritdoc />
    public partial class zero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("32c79bd8-2187-40e8-8ab4-3b4374989d11"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("541f194d-aa3f-4fe0-8968-0fa9ce481896"));

            migrationBuilder.DeleteData(
                table: "WasteVehicles",
                keyColumn: "Id",
                keyValue: new Guid("0b792bf6-4e1c-4f10-9892-1c7b4a022a90"));

            migrationBuilder.DeleteData(
                table: "WasteVehicles",
                keyColumn: "Id",
                keyValue: new Guid("a5ca01f6-4f75-4dcd-b598-e9500ab8e4db"));

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstLogin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AssignedRouteDate", "Email", "IsAvailable", "LicenseNumber", "Name", "PhoneNumber" },
                values: new object[,]
                {
                    { new Guid("0ef08186-9c3c-4d13-9707-14eb1220b96b"), null, "jane@example.com", true, "DL-002", "Jane Smith", "0987654321" },
                    { new Guid("89b0d19e-2bcf-40e9-84c3-3ec24b5ddd5e"), null, "john@example.com", true, "DL-001", "John Doe", "1234567890" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "IsFirstLogin", "Role", "UpdatedAt" },
                values: new object[] { true, "admin", new DateTime(2026, 6, 22, 2, 27, 6, 508, DateTimeKind.Utc).AddTicks(9217) });

            migrationBuilder.InsertData(
                table: "WasteVehicles",
                columns: new[] { "Id", "Capacity", "CurrentFuelLevel", "IsActive", "LastMaintenanceDate", "LastUpdatedLocation", "Latitude", "Longitude", "NextMaintenanceDate", "Status", "VehicleName", "VehicleNumber", "VehicleType" },
                values: new object[,]
                {
                    { new Guid("a70ba268-5f70-45ab-8b3f-92b625ad99aa"), 5.0, 0.0, true, null, new DateTime(2026, 6, 22, 8, 12, 6, 512, DateTimeKind.Local).AddTicks(6488), 0.0, 0.0, null, 1, "Truck 1", "VH-001", "Compactor" },
                    { new Guid("c881c0cc-cfa7-46ea-9e63-5a2d8a608f41"), 3.0, 0.0, true, null, new DateTime(2026, 6, 22, 8, 12, 6, 512, DateTimeKind.Local).AddTicks(6499), 0.0, 0.0, null, 1, "Truck 2", "VH-002", "Dumper" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("0ef08186-9c3c-4d13-9707-14eb1220b96b"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("89b0d19e-2bcf-40e9-84c3-3ec24b5ddd5e"));

            migrationBuilder.DeleteData(
                table: "WasteVehicles",
                keyColumn: "Id",
                keyValue: new Guid("a70ba268-5f70-45ab-8b3f-92b625ad99aa"));

            migrationBuilder.DeleteData(
                table: "WasteVehicles",
                keyColumn: "Id",
                keyValue: new Guid("c881c0cc-cfa7-46ea-9e63-5a2d8a608f41"));

            migrationBuilder.DropColumn(
                name: "IsFirstLogin",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AssignedRouteDate", "Email", "IsAvailable", "LicenseNumber", "Name", "PhoneNumber" },
                values: new object[,]
                {
                    { new Guid("32c79bd8-2187-40e8-8ab4-3b4374989d11"), null, "jane@example.com", true, "DL-002", "Jane Smith", "0987654321" },
                    { new Guid("541f194d-aa3f-4fe0-8968-0fa9ce481896"), null, "john@example.com", true, "DL-001", "John Doe", "1234567890" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "Role", "UpdatedAt" },
                values: new object[] { "Staff", new DateTime(2026, 6, 20, 1, 47, 33, 354, DateTimeKind.Utc).AddTicks(8118) });

            migrationBuilder.InsertData(
                table: "WasteVehicles",
                columns: new[] { "Id", "Capacity", "CurrentFuelLevel", "IsActive", "LastMaintenanceDate", "LastUpdatedLocation", "Latitude", "Longitude", "NextMaintenanceDate", "Status", "VehicleName", "VehicleNumber", "VehicleType" },
                values: new object[,]
                {
                    { new Guid("0b792bf6-4e1c-4f10-9892-1c7b4a022a90"), 3.0, 0.0, true, null, new DateTime(2026, 6, 20, 7, 32, 33, 366, DateTimeKind.Local).AddTicks(1232), 0.0, 0.0, null, 1, "Truck 2", "VH-002", "Dumper" },
                    { new Guid("a5ca01f6-4f75-4dcd-b598-e9500ab8e4db"), 5.0, 0.0, true, null, new DateTime(2026, 6, 20, 7, 32, 33, 366, DateTimeKind.Local).AddTicks(1209), 0.0, 0.0, null, 1, "Truck 1", "VH-001", "Compactor" }
                });
        }
    }
}
