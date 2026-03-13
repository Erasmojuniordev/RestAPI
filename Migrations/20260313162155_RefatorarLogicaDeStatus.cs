using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestauranteAPI.Migrations
{
    /// <inheritdoc />
    public partial class RefatorarLogicaDeStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Comandas SET Status = 'Fechada' WHERE Status = 'Entregue'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Comandas SET Status = 'Entregue' WHERE Status = 'Fechada'");
        }
    }
}
