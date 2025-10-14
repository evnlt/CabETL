using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CabETL.CLI.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CabData",
                columns: table => new
                {
                    PickupDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DropoffDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PassengerCount = table.Column<int>(type: "int", nullable: false),
                    TripDistance = table.Column<float>(type: "real", nullable: false),
                    StoreAndFwd = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PickupLocationId = table.Column<int>(type: "int", nullable: false),
                    DropoffLocationId = table.Column<int>(type: "int", nullable: false),
                    FareAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CabData");
        }
    }
}
