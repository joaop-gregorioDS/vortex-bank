using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bankcore.Transactions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "boletos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Line = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Beneficiary = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    External = table.Column<bool>(type: "boolean", nullable: false),
                    PayeeCustomerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boletos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "card_invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    ClosedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Paid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "card_purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Merchant = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Pan = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Holder = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Expiry = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Cvv = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ReceivableLedgerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "deposit_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Agency = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Number = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposit_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency", x => new { x.UserId, x.Key });
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Normal = table.Column<int>(type: "integer", nullable: false),
                    ProjectedBalance = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    GuardNonNegative = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_accounts", x => x.Id);
                    table.CheckConstraint("ck_ledger_nonnegative", "NOT \"GuardNonNegative\" OR \"ProjectedBalance\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "pix_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepositAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pix_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_jobs",
                columns: table => new
                {
                    JobType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_jobs", x => new { x.JobType, x.BusinessDate, x.AccountId });
                });

            migrationBuilder.CreateTable(
                name: "simulator_state",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulator_state", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ted_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromDepositId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToDepositId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    ScheduledFor = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FeeJournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    SettlementJournalId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ted_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "postings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_postings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_postings_journal_entries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "journal_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_boletos_Line",
                table: "boletos",
                column: "Line",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_Cpf",
                table: "customers",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deposit_accounts_Number",
                table: "deposit_accounts",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pix_keys_Value",
                table: "pix_keys",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_postings_JournalEntryId",
                table: "postings",
                column: "JournalEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "boletos");

            migrationBuilder.DropTable(
                name: "card_invoices");

            migrationBuilder.DropTable(
                name: "card_purchases");

            migrationBuilder.DropTable(
                name: "cards");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "deposit_accounts");

            migrationBuilder.DropTable(
                name: "idempotency");

            migrationBuilder.DropTable(
                name: "ledger_accounts");

            migrationBuilder.DropTable(
                name: "pix_keys");

            migrationBuilder.DropTable(
                name: "postings");

            migrationBuilder.DropTable(
                name: "processed_jobs");

            migrationBuilder.DropTable(
                name: "simulator_state");

            migrationBuilder.DropTable(
                name: "ted_orders");

            migrationBuilder.DropTable(
                name: "journal_entries");
        }
    }
}
