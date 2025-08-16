using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientService.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keycloak_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(155)", maxLength: 155, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "date", nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    newsletter_opt_in = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ativo"),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "customer"),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    account_locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    email_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.client_id);
                });

            migrationBuilder.CreateTable(
                name: "addresses",
                columns: table => new
                {
                    address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    postal_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    street = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    street_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    complement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    neighborhood = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "BR"),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_addresses", x => x.address_id);
                    table.ForeignKey(
                        name: "fk_addresses_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consents",
                columns: table => new
                {
                    consent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    terms_version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_granted = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consents", x => x.consent_id);
                    table.ForeignKey(
                        name: "fk_consents_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saved_cards",
                columns: table => new
                {
                    saved_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nickname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_four_digits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    brand = table.Column<string>(type: "text", nullable: false),
                    gateway_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expiry_date = table.Column<DateTime>(type: "date", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_cards", x => x.saved_card_id);
                    table.ForeignKey(
                        name: "fk_saved_cards_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_addresses_client_id",
                table: "addresses",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "uq_addresses_default_per_client_type",
                table: "addresses",
                columns: new[] { "client_id", "type" },
                unique: true,
                filter: "is_default = TRUE AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_clients_active_email",
                table: "clients",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_clients_role",
                table: "clients",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "idx_clients_status",
                table: "clients",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_clients_cpf",
                table: "clients",
                column: "cpf",
                unique: true,
                filter: "cpf IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_clients_keycloak_user_id",
                table: "clients",
                column: "keycloak_user_id",
                unique: true,
                filter: "keycloak_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_consents_client_id",
                table: "consents",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "uq_client_consent_type",
                table: "consents",
                columns: new[] { "client_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_saved_cards_default_per_client",
                table: "saved_cards",
                column: "client_id",
                unique: true,
                filter: "is_default = TRUE AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_saved_cards_gateway_token",
                table: "saved_cards",
                column: "gateway_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses");

            migrationBuilder.DropTable(
                name: "consents");

            migrationBuilder.DropTable(
                name: "saved_cards");

            migrationBuilder.DropTable(
                name: "clients");
        }
    }
}
