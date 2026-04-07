using DroneAPI.Data;
using DroneAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================= CONFIG =================
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtExpire = builder.Configuration["Jwt:ExpireMinutes"];

if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
    throw new Exception("JWT Key must be at least 32 characters long");

// ================= LOGGING =================
Log.Logger = new LoggerConfiguration()
.WriteTo.Console()
.CreateLogger();

builder.Host.UseSerilog();

// ================= DATABASE =================
builder.Services.AddDbContext<AppDbContext>(opt =>
opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ================= CORS =================
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ================= SWAGGER =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ================= JWT =================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ================= AUTH =================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", p => p.RequireRole("Admin"));
    options.AddPolicy("Operator", p => p.RequireRole("Operator"));
});

var app = builder.Build();

// ================= DB INIT =================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ================= MIDDLEWARE =================
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();

// ================= TEST =================
app.MapGet("/", () => "API Running...");

// ================= AUTH =================

// Register Admin
app.MapPost("/api/v1/register-admin", async (User user, AppDbContext db) =>
{
    user.Role = "Admin";
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("Admin created");
});

// Add Operator
app.MapPost("/api/v1/add-operator", async (User user, AppDbContext db) =>
{
    user.Role = "Operator";
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("Operator created");
}).RequireAuthorization("Admin");

// Get Operators
app.MapGet("/api/v1/operators", async (AppDbContext db) =>
{
    return await db.Users.Where(x => x.Role == "Operator").ToListAsync();
}).RequireAuthorization("Admin");

// Login
app.MapPost("/api/v1/login", async (User u, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(x =>
    x.Username == u.Username && x.Password == u.Password);


if (user == null) return Results.Unauthorized();

    int expireMinutes = string.IsNullOrEmpty(jwtExpire) ? 60 : Convert.ToInt32(jwtExpire);

    var token = new JwtSecurityTokenHandler().CreateToken(
        new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256)
        });

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token)
    });


});

// ================= DRONES =================

// Get drones (FIXED)
app.MapGet("/api/v1/drones", async (HttpContext ctx, AppDbContext db) =>
{
    var username = ctx.User.Identity?.Name;


var user = await db.Users.FirstOrDefaultAsync(x => x.Username == username);
    if (user == null) return Results.Forbid();

    if (user.Role == "Admin")
    {
        return Results.Ok(await db.Drones
            .Select(d => new
            {
                d.Id,
                name = d.Name ?? ("Drone " + d.Id),
                d.X,
                d.Y,
                d.Battery,
                d.Status
            }).ToListAsync());
    }

    var ids = await db.Assignments
        .Where(a => a.OperatorId == user.Id)
        .Select(a => a.DroneId)
        .ToListAsync();

    return Results.Ok(await db.Drones
        .Where(d => ids.Contains(d.Id))
        .Select(d => new
        {
            d.Id,
            name = d.Name ?? ("Drone " + d.Id),
            d.X,
            d.Y,
            d.Battery,
            d.Status
        }).ToListAsync());


}).RequireAuthorization();

// Add drone
app.MapPost("/api/v1/drones", async (Drone d, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(d.Name)) d.Name = "Drone";
    if (string.IsNullOrWhiteSpace(d.Status)) d.Status = "Idle";
    if (d.Battery <= 0) d.Battery = 100;


db.Drones.Add(d);
    await db.SaveChangesAsync();
    return Results.Ok(d);


}).RequireAuthorization("Admin");

// ================= ASSIGN =================
app.MapPost("/api/v1/assign", async (Assignment a, AppDbContext db) =>
{
    if (!await db.Drones.AnyAsync(x => x.Id == a.DroneId))
        return Results.BadRequest("Drone not found");


if (!await db.Users.AnyAsync(x => x.Id == a.OperatorId))
        return Results.BadRequest("Operator not found");

    if (await db.Assignments.AnyAsync(x => x.DroneId == a.DroneId))
        return Results.BadRequest("Already assigned");

    db.Assignments.Add(a);
    await db.SaveChangesAsync();
    return Results.Ok("Assigned");


}).RequireAuthorization("Admin");

// ================= CONTROL =================

// Move to position
app.MapPost("/api/v1/move-to-position", async (MoveDroneRequest req, HttpContext ctx, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(x => x.Username == ctx.User.Identity.Name);
    if (user == null) return Results.Forbid();


var allowed = await db.Assignments.AnyAsync(a => a.OperatorId == user.Id && a.DroneId == req.DroneId);
    if (!allowed) return Results.Forbid();

    var drone = await db.Drones.FindAsync(req.DroneId);
    if (drone == null) return Results.NotFound();

    drone.X = req.X;
    drone.Y = req.Y;
    drone.Battery -= 2;

    db.DroneDatas.Add(new DroneData
    {
        DroneId = drone.Id,
        X = drone.X,
        Y = drone.Y,
        Battery = drone.Battery
    });

    await db.SaveChangesAsync();
    return Results.Ok(drone);


}).RequireAuthorization("Operator");

// Move +1
app.MapPost("/api/v1/move/{id}", async (int id, HttpContext ctx, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(x => x.Username == ctx.User.Identity.Name);
    if (user == null) return Results.Forbid();


var ok = await db.Assignments.AnyAsync(a => a.OperatorId == user.Id && a.DroneId == id);
    if (!ok) return Results.Forbid();

    var d = await db.Drones.FindAsync(id);
    if (d == null) return Results.NotFound();

    d.X += 1;
    d.Battery -= 2;

    await db.SaveChangesAsync();
    return Results.Ok(d);


}).RequireAuthorization("Operator");

// Takeoff
app.MapPost("/api/v1/takeoff/{id}", async (int id, HttpContext ctx, AppDbContext db) =>
{
    var d = await db.Drones.FindAsync(id);
    if (d == null) return Results.NotFound();


d.Status = "Flying";
    await db.SaveChangesAsync();

    return Results.Ok();


}).RequireAuthorization("Operator");

// Land
app.MapPost("/api/v1/land/{id}", async (int id, AppDbContext db) =>
{
    var d = await db.Drones.FindAsync(id);
    if (d == null) return Results.NotFound();


d.Status = "Landed";
    await db.SaveChangesAsync();

    return Results.Ok();


}).RequireAuthorization("Operator");

// ================= PHOTO =================
app.MapPost("/api/v1/photo/{id}", async (int id) =>
{
    Directory.CreateDirectory("photos");


var file = $"drone_{id}_{DateTime.Now.Ticks}.txt";
    await File.WriteAllTextAsync($"photos/{file}", "Photo captured");

    return Results.Ok(file);


}).RequireAuthorization("Operator");

app.Run();
