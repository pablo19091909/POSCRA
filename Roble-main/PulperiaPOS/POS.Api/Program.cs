using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using POS.Api.Application;
using POS.Api.Configuration;
using POS.Api.Domain;
using POS.Api.Health;
using POS.Api.Infrastructure.Data;
using POS.Api.Application.Clientes;
using POS.Api.Application.Productos;
using POS.Api.Infrastructure.Data.Clientes;
using POS.Api.Infrastructure.Data.Productos;
using POS.Api.Application.Ventas;
using POS.Api.Application.Caja;
using POS.Api.Infrastructure.Data.Ventas;
using POS.Api.Infrastructure.Data.Caja;
using POS.Api.Infrastructure.Logging;
using POS.Api.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese solo el token JWT temporal para pruebas locales."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection("Authentication"));
builder.Services.Configure<FeatureFlagsOptions>(builder.Configuration.GetSection("FeatureFlags"));
builder.Services.Configure<EnvironmentSafetyOptions>(builder.Configuration.GetSection("EnvironmentSafety"));
builder.Services.Configure<JwtOptions>(options =>
{
    builder.Configuration.GetSection("Jwt").Bind(options);
    var environmentSigningKey = Environment.GetEnvironmentVariable(ConfigurationKeys.JwtSigningKeyEnvironmentVariable);
    if (!string.IsNullOrWhiteSpace(environmentSigningKey))
    {
        options.SigningKey = environmentSigningKey;
    }
});

builder.Services.AddSingleton<IDatabaseConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IDatabaseHealthCheck, DatabaseHealthCheck>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IDatabaseEnvironmentSafetyService, DatabaseEnvironmentSafetyService>();
builder.Services.AddScoped<IIdempotenciaVentaService, IdempotenciaVentaService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<ICajaRepository, CajaRepository>();
builder.Services.AddScoped<ICajaIdempotencyService, CajaIdempotencyService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<ILegacyPasswordVerifier, LegacySha256PasswordVerifier>();
builder.Services.AddSingleton<IPermissionProvider, RolePermissionProvider>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

var authenticationEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");
var configuredJwtSigningKey = Environment.GetEnvironmentVariable(ConfigurationKeys.JwtSigningKeyEnvironmentVariable)
    ?? builder.Configuration["Jwt:SigningKey"];

if (authenticationEnabled && !JwtOptionsValidator.HasUsableSigningKey(configuredJwtSigningKey))
{
    throw new InvalidOperationException("JWT signing key is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            NameClaimType = PermissionClaimTypes.Username,
            RoleClaimType = "role",
            IssuerSigningKey = JwtOptionsValidator.HasUsableSigningKey(configuredJwtSigningKey)
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuredJwtSigningKey!))
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('0', 32))),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    var permissions = typeof(PermissionNames)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(field => field.FieldType == typeof(string))
        .Select(field => (string)field.GetValue(null)!)
        .ToArray();

    foreach (var permission in permissions)
    {
        options.AddPolicy(permission, policy => policy.RequirePermission(permission));
    }
});

builder.Services.AddRateLimiter(options =>
{
    var permitLimit = Math.Max(1, builder.Configuration.GetValue<int?>("RateLimiting:Login:PermitLimit") ?? 5);
    var windowSeconds = Math.Max(10, builder.Configuration.GetValue<int?>("RateLimiting:Login:WindowSeconds") ?? 60);

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("AuthLogin", limiterOptions =>
    {
        limiterOptions.PermitLimit = permitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(windowSeconds);
        limiterOptions.QueueLimit = 0;
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyNames.WpfLocalClient, policy =>
    {
        var origins = allowedOrigins.Length > 0
            ? allowedOrigins
            : ["https://localhost:65535"];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyNames.WpfLocalClient);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
