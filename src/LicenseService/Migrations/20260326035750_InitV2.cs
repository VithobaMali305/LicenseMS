using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LicenseService.Migrations
{
    /// <inheritdoc />
    public partial class InitV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Uw9zmzjNUxdYotR6hdc8qu4RI8MQXktf4UWOOXmCZ7j.vpaF3/oG2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$18QP67IfHno7cPsuVnvlEOedKDE1a7Y1U8Sq7ezjVT5rtHbKqMQCi");
        }
    }
}
