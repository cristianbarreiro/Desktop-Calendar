using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarWidget.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261004000000_AddCalendarConnectionAndSyncStatus")]
public sealed class AddCalendarConnectionAndSyncStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsConnected",
            table: "CalendarAccounts",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "LastError",
            table: "CalendarSyncStates",
            type: "TEXT",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsConnected", table: "CalendarAccounts");
        migrationBuilder.DropColumn(name: "LastError", table: "CalendarSyncStates");
    }
}
