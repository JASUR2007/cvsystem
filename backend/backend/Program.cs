using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is required.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is required.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddScoped<TokenService>();
var authentication = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddCookie("External", options =>
    {
        options.Cookie.Name = "talenthub_external";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddJwtBearer(options =>
    {
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
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var idValue = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var versionValue = context.Principal?.FindFirst("auth_version")?.Value;
                if (!Guid.TryParse(idValue, out var id) || !int.TryParse(versionValue, out var version))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await db.Users.AsNoTracking()
                    .Where(item => item.Id == id)
                    .Select(item => new { item.IsBlocked, item.AuthVersion })
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                if (user is null || user.IsBlocked || user.AuthVersion != version)
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });
if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]))
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = "External";
    });
}
if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:GitHub:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:GitHub:ClientSecret"]))
{
    authentication.AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
        options.SignInScheme = "External";
        options.Scope.Add("user:email");
    });
}
builder.Services.AddAuthorization();
var s3Endpoint = builder.Configuration["S3:Endpoint"];
var s3AccessKey = builder.Configuration["S3:AccessKey"];
var s3SecretKey = builder.Configuration["S3:SecretKey"];
if (!string.IsNullOrWhiteSpace(s3Endpoint) && !string.IsNullOrWhiteSpace(s3AccessKey) && !string.IsNullOrWhiteSpace(s3SecretKey))
{
    builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(
        new BasicAWSCredentials(s3AccessKey, s3SecretKey),
        new AmazonS3Config { ServiceURL = s3Endpoint, ForcePathStyle = true }));
}
var frontendOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
if (frontendOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
        policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod()));
}

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DbSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

if (frontendOrigins.Length > 0)
{
    app.UseCors("frontend");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();
