using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyCare.Migrations
{
    /// <inheritdoc />
    public partial class ThemZaloPayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZaloPayOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppTransId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ZpTransId = table.Column<long>(type: "bigint", nullable: true),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MucDich = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    DaDongBoGiaoDich = table.Column<bool>(type: "bit", nullable: false),
                    PremiumOrderId = table.Column<int>(type: "int", nullable: true),
                    FinancialLinkId = table.Column<int>(type: "int", nullable: true),
                    QrCodeRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZaloPayOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZaloPayOrders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ZaloPayOrders_FinancialLinks_FinancialLinkId",
                        column: x => x.FinancialLinkId,
                        principalTable: "FinancialLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ZaloPayOrders_PremiumOrders_PremiumOrderId",
                        column: x => x.PremiumOrderId,
                        principalTable: "PremiumOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZaloPayOrders_AppTransId",
                table: "ZaloPayOrders",
                column: "AppTransId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZaloPayOrders_FinancialLinkId",
                table: "ZaloPayOrders",
                column: "FinancialLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloPayOrders_PremiumOrderId",
                table: "ZaloPayOrders",
                column: "PremiumOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloPayOrders_UserId",
                table: "ZaloPayOrders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZaloPayOrders");
        }
    }
}
