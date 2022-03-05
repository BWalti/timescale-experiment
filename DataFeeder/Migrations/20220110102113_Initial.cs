using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataFeeder.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "temperatures",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    avgtemperature = table.Column<double>(type: "double precision", nullable: true),
                    uncertainty = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temperatures", x => new { x.date, x.country });
                });

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");
            migrationBuilder.Sql("select create_hypertable('temperatures', 'date')");
            migrationBuilder.Sql("SELECT set_chunk_time_interval('temperatures', INTERVAL '500 days');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "temperatures");
        }
    }
}
