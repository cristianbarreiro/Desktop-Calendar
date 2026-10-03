using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarWidget.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261005000000_AddPendingCalendarOperations")]
public sealed class AddPendingCalendarOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PendingCalendarOperations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CalendarId = table.Column<Guid>(type: "TEXT", nullable: false),
                InternalEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PendingCalendarOperations", operation => operation.Id);
                table.ForeignKey("FK_PendingCalendarOperations_Calendars_CalendarId", operation => operation.CalendarId,
                    "Calendars", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PendingCalendarOperations_CalendarId_InternalEventId_Type",
            table: "PendingCalendarOperations",
            columns: ["CalendarId", "InternalEventId", "Type"],
            unique: true);

        migrationBuilder.AddColumn<string>(name: "LastSyncedLocalVersion", table: "CalendarEventMappings",
            type: "TEXT", maxLength: 64, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PendingCalendarOperations");
        migrationBuilder.DropColumn(name: "LastSyncedLocalVersion", table: "CalendarEventMappings");
    }
}
