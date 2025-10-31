using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;
using pm_api.Data;
using pm_api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- JWT settings bind (from appsettings or env) ----
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwtSettings);

// prefer env var JWT_SECRET for local/dev secrecy
var secretFromEnv = Environment.GetEnvironmentVariable("JWT_SECRET");
if (!string.IsNullOrWhiteSpace(secretFromEnv))
{
    jwtSettings.Secret = secretFromEnv;
}

if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
{
    Console.WriteLine("WARNING: JWT secret is empty. Set JWT_SECRET env var for token signing.");
}

// register JwtSettings so services can read it via IOptions
builder.Services.AddSingleton(Options.Create(jwtSettings));

// ---- core services ----
builder.Services.AddScoped<IJwtService, JwtService>();

// Scheduler DI (make sure Services/SchedulerService.cs + ISchedulerService.cs exist)
builder.Services.AddScoped<ISchedulerService, SchedulerService>();

// EF / Sqlite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=pm.db"));

// Authentication (JWT Bearer)
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret ?? string.Empty));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // local dev with self-signed certs
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// Controllers, swagger, and a tiny CORS policy for dev UI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevLocal", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
              .SetIsOriginAllowed(origin => true); // wide open for dev — lock this down in prod
    });
});

var app = builder.Build();

// Run EF migrations on startup (you already used this pattern)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DevLocal");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
