using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuoiKyDocNet.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEmailVerifiedToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "AspNetUsers");
        }
    }
}
