using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EzBias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fandoms",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fandoms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    duration_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    duration_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    features = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    username = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "User"),
                    phone = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    address = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    city = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    zip = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    avatar_url = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    avatar_bg = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    bank_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    bank_account_number = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    bank_account_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    phone_verified_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    avg_seller_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    total_ratings = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    meta = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    read_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "otp_verifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    purpose = table.Column<string>(type: "text", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_verifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_otp_verifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "VND"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    reference = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    transfer_content = table.Column<string>(type: "text", nullable: true),
                    provider_txn_id = table.Column<string>(type: "text", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_payments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    fandom_id = table.Column<string>(type: "text", nullable: false),
                    artist = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    type = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    condition = table.Column<string>(type: "text", nullable: false, defaultValue: "Good"),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    stock = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    primary_image_url = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    is_auction = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    view_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_boosted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    boost_ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_products_fandoms_fandom_id",
                        column: x => x.fandom_id,
                        principalTable: "fandoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    device_info = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seller_follows",
                columns: table => new
                {
                    follower_id = table.Column<long>(type: "bigint", nullable: false),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller_follows", x => new { x.follower_id, x.seller_id });
                    table.ForeignKey(
                        name: "FK_seller_follows_users_follower_id",
                        column: x => x.follower_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_seller_follows_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    plan_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_subscription_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auctions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    floor_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reserve_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    current_bid = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    is_urgent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    has_proof_image = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    extension_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 300),
                    trigger_before_end = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    extension_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Live"),
                    winner_id = table.Column<long>(type: "bigint", nullable: true),
                    final_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    winner_payment_deadline = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auctions", x => x.id);
                    table.ForeignKey(
                        name: "FK_auctions_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auctions_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auctions_users_winner_id",
                        column: x => x.winner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cart_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "photocard_details",
                columns: table => new
                {
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    member_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    album_series = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    version = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    is_pob = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photocard_details", x => x.product_id);
                    table.ForeignKey(
                        name: "FK_photocard_details_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlists",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlists", x => new { x.user_id, x.product_id });
                    table.ForeignKey(
                        name: "FK_wishlists_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_wishlists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_boosts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    subscription_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_boosts", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_boosts_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_boosts_user_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "user_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_boosts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    auction_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_winning = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    username_snap = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    avatar_snap = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    avatar_bg_snap = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bids", x => x.id);
                    table.ForeignKey(
                        name: "FK_bids_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bids_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false, defaultValue: "Cart"),
                    auction_id = table.Column<long>(type: "bigint", nullable: true),
                    shipping_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    address_snap = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    carrier = table.Column<string>(type: "text", nullable: true),
                    tracking_number = table.Column<string>(type: "text", nullable: true),
                    shipped_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_orders_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_orders_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    initiator_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Open"),
                    admin_note = table.Column<string>(type: "text", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disputes", x => x.id);
                    table.ForeignKey(
                        name: "FK_disputes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_disputes_users_initiator_id",
                        column: x => x.initiator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: true),
                    product_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    product_image = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false, computedColumnSql: "quantity * unit_price", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "payment_orders",
                columns: table => new
                {
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_orders", x => new { x.payment_id, x.order_id });
                    table.ForeignKey(
                        name: "FK_payment_orders_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_orders_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payouts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    bank_transfer_ref = table.Column<string>(type: "text", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payouts", x => x.id);
                    table.ForeignKey(
                        name: "FK_payouts_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payouts_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    buyer_id = table.Column<long>(type: "bigint", nullable: false),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    product_rating = table.Column<short>(type: "smallint", nullable: false),
                    seller_rating = table.Column<short>(type: "smallint", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ratings", x => x.id);
                    table.CheckConstraint("ck_ratings_product_rating", "product_rating >= 1 AND product_rating <= 5");
                    table.CheckConstraint("ck_ratings_seller_rating", "seller_rating >= 1 AND seller_rating <= 5");
                    table.ForeignKey(
                        name: "FK_ratings_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ratings_users_buyer_id",
                        column: x => x.buyer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ratings_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    dispute_id = table.Column<long>(type: "bigint", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    provider_ref = table.Column<string>(type: "text", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.id);
                    table.ForeignKey(
                        name: "FK_refunds_disputes_dispute_id",
                        column: x => x.dispute_id,
                        principalTable: "disputes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_refunds_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_refunds_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "escrow_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    seller_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: true),
                    payout_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_escrow_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_escrow_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_escrow_transactions_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_escrow_transactions_payouts_payout_id",
                        column: x => x.payout_id,
                        principalTable: "payouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_escrow_transactions_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_auctions_ends_at",
                table: "auctions",
                column: "ends_at");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_seller_id",
                table: "auctions",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_status",
                table: "auctions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_winner_id",
                table: "auctions",
                column: "winner_id");

            migrationBuilder.CreateIndex(
                name: "uq_one_live_auction_per_product",
                table: "auctions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "idx_bids_auction_amount",
                table: "bids",
                columns: new[] { "auction_id", "amount" });

            migrationBuilder.CreateIndex(
                name: "IX_bids_auction_id",
                table: "bids",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "IX_bids_user_id",
                table: "bids",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_product_id",
                table: "cart_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_user_id_product_id",
                table: "cart_items",
                columns: new[] { "user_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_contact_messages_unread",
                table: "contact_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_disputes_open",
                table: "disputes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_initiator_id",
                table: "disputes",
                column: "initiator_id");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_order_id",
                table: "disputes",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_escrow_transactions_order_id",
                table: "escrow_transactions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_escrow_transactions_payment_id",
                table: "escrow_transactions",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_escrow_transactions_payout_id",
                table: "escrow_transactions",
                column: "payout_id");

            migrationBuilder.CreateIndex(
                name: "IX_escrow_transactions_seller_id",
                table: "escrow_transactions",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_unread",
                table: "notifications",
                columns: new[] { "user_id", "is_read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_auction_id",
                table: "orders",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_seller_status",
                table: "orders",
                columns: new[] { "seller_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_orders_user_status",
                table: "orders",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_seller_id_created_at",
                table: "orders",
                columns: new[] { "seller_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_user_id_created_at",
                table: "orders",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_otp_active",
                table: "otp_verifications",
                columns: new[] { "user_id", "purpose" });

            migrationBuilder.CreateIndex(
                name: "idx_otp_cleanup",
                table: "otp_verifications",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_payment_orders_order_id",
                table: "payment_orders",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_payments_provider_txn_id",
                table: "payments",
                column: "provider_txn_id");

            migrationBuilder.CreateIndex(
                name: "idx_payments_status_pending",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_payments_transfer_content",
                table: "payments",
                column: "transfer_content");

            migrationBuilder.CreateIndex(
                name: "IX_payments_user_id",
                table: "payments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_payouts_pending",
                table: "payouts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_payouts_order_id",
                table: "payouts",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payouts_seller_id",
                table: "payouts",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_boosts_active_product",
                table: "product_boosts",
                columns: new[] { "product_id", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "idx_boosts_expiry_scan",
                table: "product_boosts",
                column: "ends_at");

            migrationBuilder.CreateIndex(
                name: "IX_product_boosts_subscription_id",
                table: "product_boosts",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_boosts_user_id",
                table: "product_boosts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id_sort_order",
                table: "product_images",
                columns: new[] { "product_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "idx_products_boosted",
                table: "products",
                columns: new[] { "is_boosted", "boost_ends_at" });

            migrationBuilder.CreateIndex(
                name: "idx_products_browse",
                table: "products",
                columns: new[] { "fandom_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_products_fandom_id",
                table: "products",
                column: "fandom_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_seller_id",
                table: "products",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_type",
                table: "products",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_ratings_buyer_id",
                table: "ratings",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_ratings_order_id",
                table: "ratings",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_seller_id",
                table: "ratings",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_hash_active",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_dispute_id",
                table: "refunds",
                column: "dispute_id");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_order_id",
                table: "refunds",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_payment_id",
                table: "refunds",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_status",
                table: "refunds",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_seller_follows_seller_id",
                table: "seller_follows",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_subs_active",
                table: "user_subscriptions",
                columns: new[] { "user_id", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_payment_id",
                table: "user_subscriptions",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_plan_id",
                table: "user_subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wishlists_product_id",
                table: "wishlists",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bids");

            migrationBuilder.DropTable(
                name: "cart_items");

            migrationBuilder.DropTable(
                name: "contact_messages");

            migrationBuilder.DropTable(
                name: "escrow_transactions");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "otp_verifications");

            migrationBuilder.DropTable(
                name: "payment_orders");

            migrationBuilder.DropTable(
                name: "photocard_details");

            migrationBuilder.DropTable(
                name: "product_boosts");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "ratings");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "seller_follows");

            migrationBuilder.DropTable(
                name: "wishlists");

            migrationBuilder.DropTable(
                name: "payouts");

            migrationBuilder.DropTable(
                name: "user_subscriptions");

            migrationBuilder.DropTable(
                name: "disputes");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "auctions");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "fandoms");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
