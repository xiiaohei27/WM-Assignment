using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Main.Migrations
{
    /// <inheritdoc />
    public partial class AddRedemptionToFoodOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "FoodOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRedeemed",
                table: "FoodOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RedeemedAt",
                table: "FoodOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedeemedBy",
                table: "FoodOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedemptionCode",
                table: "FoodOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "FoodOrders");

            migrationBuilder.DropColumn(
                name: "IsRedeemed",
                table: "FoodOrders");

            migrationBuilder.DropColumn(
                name: "RedeemedAt",
                table: "FoodOrders");

            migrationBuilder.DropColumn(
                name: "RedeemedBy",
                table: "FoodOrders");

            migrationBuilder.DropColumn(
                name: "RedemptionCode",
                table: "FoodOrders");
        }
    }
}
