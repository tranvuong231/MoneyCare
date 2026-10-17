using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyCare.Migrations
{
    /// <inheritdoc />
    public partial class ThemDongBoGiaoDich : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Nguon",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianTao",
                table: "Transactions",
                type: "datetime2",
                nullable: false,
                // Với các giao dịch đã có sẵn trong DB (chưa từng có cột này), lấy tạm NgayGiaoDich
                // làm ThoiGianTao ban đầu (giờ 00:00:00) - vẫn đúng thứ tự tương đối theo ngày, chỉ
                // các giao dịch tạo MỚI sau migration này mới có giờ:phút:giây chính xác.
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<int>(
                name: "TrangThaiDongBo",
                table: "Transactions",
                type: "int",
                nullable: true);

            // Backfill: các giao dịch đã tồn tại trước migration này chưa có giờ:phút:giây chính xác,
            // tạm lấy NgayGiaoDich (00:00:00) làm ThoiGianTao để không bị NULL/sai lệch quá xa thực tế.
            migrationBuilder.Sql("UPDATE Transactions SET ThoiGianTao = NgayGiaoDich;");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BillId",
                table: "Transactions",
                column: "BillId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Bills_BillId",
                table: "Transactions",
                column: "BillId",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Bills_BillId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BillId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BillId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Nguon",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ThoiGianTao",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TrangThaiDongBo",
                table: "Transactions");
        }
    }
}
