using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdIsDeleteFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Households_InviteCode",
                table: "Households");

            migrationBuilder.CreateIndex(
                name: "IX_Households_InviteCode",
                table: "Households",
                column: "InviteCode",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Households_InviteCode",
                table: "Households");

            migrationBuilder.CreateIndex(
                name: "IX_Households_InviteCode",
                table: "Households",
                column: "InviteCode",
                unique: true);
        }
    }
}
