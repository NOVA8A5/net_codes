using Microsoft.EntityFrameworkCore;
using DroneAPI.Models;

namespace DroneAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Drone> Drones { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<DroneData> DroneDatas { get; set; }   // NEW MODEL

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DroneData>()
                .HasOne(d => d.Drone)
                .WithMany(d => d.DroneDatas)
                .HasForeignKey(d => d.DroneId);
        }
    }
}