using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieGenreAndImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Tariffs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "TariffId",
                table: "Orders",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Movies",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Movies",
                type: "longtext",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TariffId",
                table: "Orders",
                column: "TariffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Tariffs_TariffId",
                table: "Orders",
                column: "TariffId",
                principalTable: "Tariffs",
                principalColumn: "TariffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Tariffs_TariffId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TariffId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "TariffId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Movies");
        }
    }
}
