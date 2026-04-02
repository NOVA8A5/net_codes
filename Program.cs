using DroneAPI.Data;
using DroneAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🔥 Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// 🔥 DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// 🔥 CORS
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// 🔥 Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false"));

// 🔥 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔥 JWT
var key = "THIS_IS_A_VERY_SECRET_KEY_1234567890";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddAuthorization();

// 🔥 Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

// ================= AUTH =================

// Register Admin
app.MapPost("/api/v1/register-admin", async (User user, AppDbContext db) =>
{
    user.Role = "Admin";
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("Admin created");
});

// Add Operator (Admin only)
app.MapPost("/api/v1/add-operator", async (User user, AppDbContext db) =>
{
    user.Role = "Operator";
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("Operator created");
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

// Login
app.MapPost("/api/v1/login", async (User u, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(x =>
        x.Username == u.Username && x.Password == u.Password);

    if (user == null) return Results.Unauthorized();

    var token = new JwtSecurityTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256)
    });

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});

// ================= DRONES =================

// Get Drones
app.MapGet("/api/v1/drones", async (AppDbContext db) =>
    await db.Drones.ToListAsync());

// Add Drone (Admin)
app.MapPost("/api/v1/drones", async (Drone d, AppDbContext db) =>
{
    db.Drones.Add(d);
    await db.SaveChangesAsync();
    return Results.Ok(d);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

// ================= OPERATORS =================

app.MapGet("/api/v1/operators", async (AppDbContext db) =>
    await db.Operators.ToListAsync())
.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

// ================= ASSIGN =================

app.MapPost("/api/v1/assign", async (Assignment a, AppDbContext db) =>
{
    var exists = await db.Assignments.AnyAsync(x => x.DroneId == a.DroneId);
    if (exists) return Results.BadRequest("Already assigned");

    db.Assignments.Add(a);
    await db.SaveChangesAsync();

    return Results.Ok("Assigned");
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

// ================= CONTROL =================

// Move (Operator)
app.MapPost("/api/v1/move/{id}", async (int id, string dir, AppDbContext db) =>
{
    var d = await db.Drones.FindAsync(id);
    if (d == null) return Results.NotFound();

    switch (dir)
    {
        case "up": d.Y += d.Speed; break;
        case "down": d.Y -= d.Speed; break;
        case "left": d.X -= d.Speed; break;
        case "right": d.X += d.Speed; break;
    }

    d.Battery -= 2;
    d.Status = "Moving";

    await db.SaveChangesAsync();
    return Results.Ok(d);

}).RequireAuthorization(new AuthorizeAttribute { Roles = "Operator" });

// Takeoff
app.MapPost("/api/v1/takeoff/{id}", async (int id, AppDbContext db) =>
{
    var d = await db.Drones.FindAsync(id);
    d.Status = "Flying";
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Operator" });

// Land
app.MapPost("/api/v1/land/{id}", async (int id, AppDbContext db) =>
{
    var d = await db.Drones.FindAsync(id);
    d.Status = "Landed";
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Operator" });

// ================= PHOTO =================

app.MapPost("/api/v1/photo/{id}", async (int id) =>
{
    var folder = "photos";
    Directory.CreateDirectory(folder);

    var file = $"drone_{id}_{DateTime.Now.Ticks}.txt";
    await File.WriteAllTextAsync($"photos/{file}", "Photo captured");

    return Results.Ok(file);

}).RequireAuthorization(new AuthorizeAttribute { Roles = "Operator" });

app.Run();