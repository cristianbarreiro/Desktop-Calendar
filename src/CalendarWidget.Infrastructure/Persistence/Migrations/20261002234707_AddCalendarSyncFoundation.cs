using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarWidget.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCalendarSyncFoundation : Migration
{
    private static readonly string[] CalendarSeedColumns = ["Id", "AccountId", "CreatedAt", "ExternalId", "Name", "Provider"];
    private static readonly string[] AccountUniqueIndexColumns = ["Provider", "ProviderAccountId"];
    private static readonly string[] EventMappingUniqueIndexColumns = ["Provider", "AccountId", "CalendarId", "ExternalEventId"];
    private static readonly string[] CalendarUniqueIndexColumns = ["Provider", "AccountId", "ExternalId"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CalendarId",
            table: "CalendarEvents",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

        migrationBuilder.AddColumn<string>(
            name: "Location",
            table: "CalendarEvents",
            type: "TEXT",
            maxLength: 500,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "CalendarAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                ProviderAccountId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalendarAccounts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Calendars",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                AccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ExternalId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Calendars", x => x.Id);
                table.ForeignKey(
                    name: "FK_Calendars_CalendarAccounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "CalendarAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CalendarEventMappings",
            columns: table => new
            {
                InternalEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                CalendarId = table.Column<Guid>(type: "TEXT", nullable: false),
                ExternalEventId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                ExternalVersion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalendarEventMappings", x => new { x.InternalEventId, x.Provider, x.AccountId, x.CalendarId });
                table.ForeignKey(
                    name: "FK_CalendarEventMappings_CalendarAccounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "CalendarAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CalendarEventMappings_CalendarEvents_InternalEventId",
                    column: x => x.InternalEventId,
                    principalTable: "CalendarEvents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CalendarEventMappings_Calendars_CalendarId",
                    column: x => x.CalendarId,
                    principalTable: "Calendars",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CalendarSyncStates",
            columns: table => new
            {
                CalendarId = table.Column<Guid>(type: "TEXT", nullable: false),
                Cursor = table.Column<string>(type: "TEXT", nullable: true),
                LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalendarSyncStates", x => x.CalendarId);
                table.ForeignKey(
                    name: "FK_CalendarSyncStates_Calendars_CalendarId",
                    column: x => x.CalendarId,
                    principalTable: "Calendars",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            table: "Calendars",
                columns: CalendarSeedColumns,
            values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "local", "Local Calendar", "Local" });

        migrationBuilder.CreateIndex(
            name: "IX_CalendarEvents_CalendarId",
            table: "CalendarEvents",
            column: "CalendarId");

        migrationBuilder.CreateIndex(
            name: "IX_CalendarAccounts_Provider_ProviderAccountId",
            table: "CalendarAccounts",
                columns: AccountUniqueIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CalendarEventMappings_AccountId",
            table: "CalendarEventMappings",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_CalendarEventMappings_CalendarId",
            table: "CalendarEventMappings",
            column: "CalendarId");

        migrationBuilder.CreateIndex(
            name: "IX_CalendarEventMappings_Provider_AccountId_CalendarId_ExternalEventId",
            table: "CalendarEventMappings",
                columns: EventMappingUniqueIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Calendars_AccountId",
            table: "Calendars",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_Calendars_Provider_AccountId_ExternalId",
            table: "Calendars",
                columns: CalendarUniqueIndexColumns,
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_CalendarEvents_Calendars_CalendarId",
            table: "CalendarEvents",
            column: "CalendarId",
            principalTable: "Calendars",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CalendarEvents_Calendars_CalendarId",
            table: "CalendarEvents");

        migrationBuilder.DropTable(
            name: "CalendarEventMappings");

        migrationBuilder.DropTable(
            name: "CalendarSyncStates");

        migrationBuilder.DropTable(
            name: "Calendars");

        migrationBuilder.DropTable(
            name: "CalendarAccounts");

        migrationBuilder.DropIndex(
            name: "IX_CalendarEvents_CalendarId",
            table: "CalendarEvents");

        migrationBuilder.DropColumn(
            name: "CalendarId",
            table: "CalendarEvents");

        migrationBuilder.DropColumn(
            name: "Location",
            table: "CalendarEvents");
    }
}
