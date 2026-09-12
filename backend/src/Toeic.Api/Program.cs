using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Toeic.Api.Middleware;
using Toeic.Application.Common.Interfaces;
using Toeic.Infrastructure;
using Toeic.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration validation ----------------------------------------------
// Nguyên tắc #8 (prompt gốc): không hard-code secret. Jwt:Key phải đến từ user-secrets
// (dev) hoặc biến môi trường / secret manager thật (production) — appsettings.json chỉ có
// placeholder rỗng. Fail fast ngay khi khởi động nếu thiếu, thay vì lỗi khó hiểu lúc login.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key chưa được cấu hình hoặc quá ngắn (cần tối thiểu 256-bit / 32 byte UTF-8). " +
        "Dev: dotnet user-secrets set \"Jwt:Key\" \"<random-secret-toi-thieu-32-ky-tu>\" " +
        "--project src/Toeic.Api. Production: đặt qua biến môi trường Jwt__Key hoặc secret manager.");
}

// --- Services -------------------------------------------------------------

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TOEIC Scoring Support System API",
        Version = "v1"
    });

    // Cho phép nhập "Bearer {token}" trực tiếp trên Swagger UI để test các endpoint [Authorize].
    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Description = "Nhập access token (không cần gõ chữ 'Bearer ')."
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
          Array.Empty<string>() }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

// --- Authentication / Authorization ----------------------------------------

var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

//// THÊM DÒNG NÀY ĐỂ TẮT AUTO-MAPPING:
//JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // THÊM DÒNG NÀY ĐỂ TẮT MAPPING TRONG .NET 8
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
    });

// Policy theo role (ADMIN/RATER/CANDIDATE) — dùng [Authorize(Policy = "...")] hoặc trực tiếp
// [Authorize(Roles = "ADMIN")] (đã dùng Roles trực tiếp trong AuthController cho đơn giản).
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("ADMIN"))
    .AddPolicy("RaterOnly", p => p.RequireRole("RATER"))
    .AddPolicy("CandidateOnly", p => p.RequireRole("CANDIDATE"))
    .AddPolicy("StaffOnly", p => p.RequireRole("ADMIN", "RATER"));

// CORS cho Frontend Next.js dev server — sẽ siết lại theo Environment ở Phase sau.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();


if (app.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}



// --- Middleware pipeline ----------------------------------------------------

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check tối thiểu để demo Phase 1: xác nhận API + DB connection sống.
app.MapGet("/api/v1/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new { status = "ok", database = canConnect ? "connected" : "unreachable" });
}).AllowAnonymous();

// Seed tài khoản ADMIN đầu tiên nếu SeedAdmin:* được cấu hình và chưa có ADMIN nào (xem DbSeeder).
// PHASE8-ISSUE-18: cùng scope, seed thêm tài khoản RATER cho evaluation-service nếu
// SeedEvalService:* được cấu hình (dùng lại đúng db/hasher/seedLogger đã resolve ở trên).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    await DbSeeder.SeedAdminAsync(db, hasher, app.Configuration, seedLogger);
    await DbSeeder.SeedEvalServiceAsync(db, hasher, app.Configuration, seedLogger);
}

app.Run();
