using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace YachtSharing.BookingService.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "clients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_clients", x => x.Id));

        migrationBuilder.CreateTable(
            name: "bookings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                YachtId = table.Column<int>(type: "integer", nullable: false),
                StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Passport_Series = table.Column<string>(type: "text", nullable: false),
                Passport_Number = table.Column<string>(type: "text", nullable: false),
                IsGimsVerified = table.Column<bool>(type: "boolean", nullable: false),
                FinalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                IsRefundEligible = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bookings", x => x.Id);
                table.ForeignKey(
                    name: "FK_bookings_clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "crew_members",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                InsuranceExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_crew_members", x => x.Id);
                table.ForeignKey(
                    name: "FK_crew_members_bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TransactionId = table.Column<string>(type: "text", nullable: false),
                PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_payments_bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bookings_ClientId",
            table: "bookings",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_crew_members_BookingId",
            table: "crew_members",
            column: "BookingId");

        migrationBuilder.CreateIndex(
            name: "IX_payments_BookingId",
            table: "payments",
            column: "BookingId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_payments_TransactionId",
            table: "payments",
            column: "TransactionId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "crew_members");

        migrationBuilder.DropTable(
            name: "payments");

        migrationBuilder.DropTable(
            name: "bookings");

        migrationBuilder.DropTable(
            name: "clients");
    }
}
