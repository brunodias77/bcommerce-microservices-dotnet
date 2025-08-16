using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientService.Infra.Migrations
{
    /// <inheritdoc />
    public partial class FixUserRoleDefaultValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "clients",
                type: "text",
                nullable: false,
                defaultValue: "user",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "customer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "clients",
                type: "text",
                nullable: false,
                defaultValue: "customer",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "user");
        }
    }
}
