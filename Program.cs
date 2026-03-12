using AspNetCoreRateLimit;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestauranteAPI.Data;
using RestauranteAPI.Hubs;
using RestauranteAPI.Middleware;
using RestauranteAPI.Models;
using RestauranteAPI.Services;
using RestauranteAPI.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// ──────────────────────────────────────────
// Rate Limiting — proteção básica contra abuso (ajustar limites em produção)
// ────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// ──────────────────────────────────────────
// Banco de Dados
// ──────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ──────────────────────────────────────────
// Identity
// ──────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false; // relaxado para UX
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ──────────────────────────────────────────
// JWT
// ──────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key não configurada.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // Necessário para SignalR — token via query string
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

// ──────────────────────────────────────────
// SignalR
// ──────────────────────────────────────────
builder.Services.AddSignalR();

// ──────────────────────────────────────────
// CORS — ajustar origin em produção
// ──────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEnd", policy =>
    {
        policy
            .WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // necessário para SignalR
    });
});

// ──────────────────────────────────────────
// Services (DI)
// ──────────────────────────────────────────
builder.Services.AddScoped<IComandaService, ComandaService>();
builder.Services.AddScoped<IItemService, ItemService>();

// ──────────────────────────────────────────
// Controllers + Swagger
// ──────────────────────────────────────────
builder.Services.AddControllers();

// ──────────────────────────────────────────
// FluentValidation
// ──────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Restaurante API", Version = "v1" });

    // Swagger com JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe: Bearer {seu token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

// ──────────────────────────────────────────
// Build
// ──────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseCors("FrontEnd");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CozinhaHub>("/hubs/cozinha");

// Seed de roles e admin padrão em dev
if (app.Environment.IsDevelopment())
    await SeedAsync(app);

app.Run();

// ──────────────────────────────────────────
// Seed
// ──────────────────────────────────────────
static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = ["Admin", "Garcom", "Cozinha", "Caixa"];

    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    // Admin padrão para desenvolvimento
    var adminEmail = "admin@restaurante.com";
    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            NomeCompleto = "Administrador"
        };
        await userManager.CreateAsync(admin, "Admin@1234");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}