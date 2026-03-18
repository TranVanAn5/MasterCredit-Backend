using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MasterCredit.Migrations
{
    /// <inheritdoc />
    public partial class AddBillPaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillProviders_BillCategories",
                        column: x => x.CategoryId,
                        principalTable: "BillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BillAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CardId = table.Column<int>(type: "int", nullable: false),
                    ProviderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillPayments_BillProviders",
                        column: x => x.ProviderId,
                        principalTable: "BillProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillPayments_Cards",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillPayments_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BillCategories",
                columns: new[] { "Id", "CategoryCode", "CategoryName", "CreatedAt", "DisplayOrder", "IconUrl", "IsActive" },
                values: new object[,]
                {
                    { 1, "ELECTRIC", "Tiền điện", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "⚡", true },
                    { 2, "WATER", "Tiền nước", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "💧", true },
                    { 3, "INTERNET", "Internet/Wifi", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "🌐", true },
                    { 4, "TUITION", "Học phí", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "🎓", true },
                    { 5, "MOBILE", "Điện thoại", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, "📱", true },
                    { 6, "TV", "Truyền hình", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, "📺", true }
                });

            migrationBuilder.InsertData(
                table: "BillProviders",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "DisplayOrder", "IsActive", "LogoUrl", "ProviderCode", "ProviderName", "ServiceFee" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "/images/providers/evn-hanoi.png", "EVN_HN", "EVN Hà Nội", 2000m },
                    { 2, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "/images/providers/evn-hcm.png", "EVN_HCM", "EVN Hồ Chí Minh", 2000m },
                    { 3, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "/images/providers/pc-danang.png", "PC_DN", "PC Đà Nẵng", 2000m },
                    { 4, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "/images/providers/water-hanoi.png", "WATER_HN", "Công ty nước Hà Nội", 1500m },
                    { 5, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "/images/providers/sawaco.png", "SAWACO", "Sawaco (TP.HCM)", 1500m },
                    { 6, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "/images/providers/fpt.png", "FPT", "FPT Telecom", 2500m },
                    { 7, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "/images/providers/viettel.png", "VIETTEL", "Viettel", 2500m },
                    { 8, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "/images/providers/vnpt.png", "VNPT", "VNPT", 2500m },
                    { 9, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "/images/providers/hust.png", "HUST", "Đại học Bách Khoa Hà Nội", 0m },
                    { 10, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "/images/providers/vnuhcm.png", "VNUHCM", "Đại học Quốc gia TP.HCM", 0m },
                    { 11, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "/images/providers/viettel-mobile.png", "VIETTEL_MOBILE", "Viettel Mobile", 1000m },
                    { 12, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "/images/providers/vinaphone.png", "VINAPHONE", "Vinaphone", 1000m },
                    { 13, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "/images/providers/mobifone.png", "MOBIFONE", "Mobifone", 1000m },
                    { 14, 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "/images/providers/vtvcab.png", "VTVCAB", "VTVcab", 2000m },
                    { 15, 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "/images/providers/kplus.png", "KPLUS", "K+ (VTVcab)", 2000m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillCategories_CategoryCode",
                table: "BillCategories",
                column: "CategoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillCategories_DisplayOrder",
                table: "BillCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_CardId",
                table: "BillPayments",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_ProviderId",
                table: "BillPayments",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_ReferenceNumber",
                table: "BillPayments",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_TransactionDate",
                table: "BillPayments",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_User_Status",
                table: "BillPayments",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_User_TransactionDate",
                table: "BillPayments",
                columns: new[] { "UserId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_UserId",
                table: "BillPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillProviders_Category_DisplayOrder",
                table: "BillProviders",
                columns: new[] { "CategoryId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BillProviders_CategoryId",
                table: "BillProviders",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BillProviders_ProviderCode",
                table: "BillProviders",
                column: "ProviderCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillPayments");

            migrationBuilder.DropTable(
                name: "BillProviders");

            migrationBuilder.DropTable(
                name: "BillCategories");
        }
    }
}
