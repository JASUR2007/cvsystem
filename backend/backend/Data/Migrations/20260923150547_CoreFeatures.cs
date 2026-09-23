using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace backend.Data.Migrations
{
    public partial class CoreFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Positions",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "ShortDescription" });

            migrationBuilder.AddColumn<bool>(
                name: "BooleanValue",
                table: "PositionAccessRules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NumberValue",
                table: "PositionAccessRules",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OptionId",
                table: "PositionAccessRules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "AspNetUsers",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_SearchVector",
                table: "Positions",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SearchVector",
                table: "AspNetUsers",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Positions_SearchVector",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SearchVector",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "BooleanValue",
                table: "PositionAccessRules");

            migrationBuilder.DropColumn(
                name: "NumberValue",
                table: "PositionAccessRules");

            migrationBuilder.DropColumn(
                name: "OptionId",
                table: "PositionAccessRules");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "AspNetUsers");
        }
    }
}
