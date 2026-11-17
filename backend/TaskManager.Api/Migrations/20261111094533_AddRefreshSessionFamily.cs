using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshSessionFamily : Migration
    {
        /// <inheritdoc />

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Añadimos FamilyId permitiendo NULL temporalmente.
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "RefreshSessions",
                type: "uuid",
                nullable: true);

            // 2. Asignamos una familia diferente a cada sesión existente.
            migrationBuilder.Sql("""
        UPDATE "RefreshSessions"
        SET "FamilyId" = "Id"
        WHERE "FamilyId" IS NULL;
        """);

            // 3. Hacemos FamilyId obligatorio.
            migrationBuilder.AlterColumn<Guid>(
                name: "FamilyId",
                table: "RefreshSessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 4. Creamos el índice para buscar sesiones de una familia.
            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_FamilyId",
                table: "RefreshSessions",
                column: "FamilyId");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshSessions_FamilyId",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "RefreshSessions");
        }
    }
}
