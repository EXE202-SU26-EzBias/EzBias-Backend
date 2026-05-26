using EzBias.API.BackgroundServices;
using EzBias.API.Hubs;
using EzBias.API.Integrations;
using EzBias.Application.Features.Auctions;
using EzBias.Application.Features.Auth;
using EzBias.Application.Features.Auth.Services;
using EzBias.Application.Features.Cart;
using EzBias.Application.Features.Disputes;
using EzBias.Application.Features.Admin;
using EzBias.Application.Features.Notifications;
using EzBias.Application.Features.Orders;
using EzBias.Application.Features.Payments;
using EzBias.Application.Features.Payouts;
using EzBias.Application.Features.Products;
using EzBias.Application.Features.Ratings;
using EzBias.Application.Features.Users;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Auth;
using EzBias.Infrastructure.Persistence;
using EzBias.Infrastructure.Persistence.SeedData;using EzBias.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});
builder.Services.Configure<SePayOptions>(builder.Configuration.GetSection(SePayOptions.SectionName));
builder.Services.Configure<BrevoOptions>(builder.Configuration.GetSection(BrevoOptions.SectionName));
builder.Services.Configure<CommissionOptions>(builder.Configuration.GetSection(CommissionOptions.SectionName));
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection(CloudinaryOptions.SectionName));
builder.Services.AddHttpClient("SePay", client =>
{
    var baseUrl = builder.Configuration["SePay:BaseUrl"] ?? "https://my.sepay.vn";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHttpClient("Brevo", client =>
{
    client.BaseAddress = new Uri("https://api.brevo.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IImageUploader, CloudinaryImageUploader>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EzBias API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Input your JWT access token in this format: Bearer {your token here}",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddDbContext<EzBiasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthEmailSender, BrevoAuthEmailSender>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOtpVerificationRepository, OtpVerificationRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IFandomRepository, FandomRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IEscrowRepository, EscrowRepository>();
builder.Services.AddScoped<IPayoutRepository, PayoutRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ICommissionRepository, CommissionRepository>();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddScoped<IBidRepository, BidRepository>();
builder.Services.AddScoped<IUnitOfWork, NotificationDispatchingUnitOfWork>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddScoped<IAuthApplicationService, AuthApplicationService>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminApplicationService, AdminApplicationService>();
builder.Services.AddScoped<IUserProfileApplicationService, UserProfileApplicationService>();
builder.Services.AddScoped<IProductManagementApplicationService, ProductManagementApplicationService>();
builder.Services.AddScoped<ICatalogQueryService, CatalogQueryService>();
builder.Services.AddScoped<ICartApplicationService, CartApplicationService>();
builder.Services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();
builder.Services.AddScoped<ICommissionRateProvider, ConfiguredCommissionRateProvider>();
builder.Services.AddScoped<ISePayClient, SePayClient>();
builder.Services.AddScoped<ISePayWebhookVerifier, SePayWebhookVerifier>();
builder.Services.AddScoped<IOrderApplicationService, OrderApplicationService>();
builder.Services.AddScoped<IPayoutApplicationService, PayoutApplicationService>();
builder.Services.AddScoped<IRatingApplicationService, RatingApplicationService>();
builder.Services.AddScoped<INotificationApplicationService, NotificationApplicationService>();
builder.Services.AddSingleton<INotificationFactory, NotificationFactory>();
builder.Services.AddScoped<ISellerAuctionApplicationService, SellerAuctionApplicationService>();
builder.Services.AddScoped<IAuctionBiddingApplicationService, AuctionBiddingApplicationService>();
builder.Services.AddScoped<IAuctionPostFlowQueryService, AuctionPostFlowQueryService>();
builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();
builder.Services.AddScoped<IDisputeApplicationService, DisputeApplicationService>();
builder.Services.AddHostedService<AuctionCloseScheduler>();
builder.Services.AddHostedService<DeliveredOrderFinalizeScheduler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        // Allow SignalR to receive JWT via query string (?access_token=...)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

const string FrontendCorsPolicy = "FrontendLocal";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://ez-bias-frontend.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EzBiasDbContext>();
    db.Database.Migrate();
    await ProductSeedData.SeedAsync(db);

    var sellers = ProductSeedData.GetSeedSellers(db);
    await AuctionSeedData.SeedAsync(db, sellers);
}

var enableSwagger = app.Environment.IsDevelopment() ||
                    string.Equals(builder.Configuration["Swagger:Enabled"], "true", StringComparison.OrdinalIgnoreCase);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseResponseCompression();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<AuctionHub>("/hubs/auction");

app.Run();
