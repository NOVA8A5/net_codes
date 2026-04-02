using Microsoft.EntityFrameworkCore;
using DroneAPI.Models;

namespace DroneAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Drone> Drones { get; set; }
    public DbSet<Operator> Operators { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<DroneData> DroneData { get; set; }
}