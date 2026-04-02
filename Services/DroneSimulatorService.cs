using DroneAPI.Data;
using DroneAPI.Models;
using Microsoft.EntityFrameworkCore;

public class DroneSimulatorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DroneSimulatorService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rand = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var drones = await db.Drones.ToListAsync();

            foreach (var d in drones)
            {
                d.X += rand.Next(-1, 2);
                d.Y += rand.Next(-1, 2);
                d.Battery -= 1;

                db.DroneData.Add(new DroneData
                {
                    DroneId = d.Id,
                    X = d.X,
                    Y = d.Y,
                    Battery = d.Battery
                });
            }

            await db.SaveChangesAsync();

            //await Task.Delay(3000, stoppingToken); // every 3 sec

        }
    }
}