namespace DroneAPI.Models;

public class DroneData
{
    public int Id { get; set; }
    public int DroneId { get; set; }

    public int X { get; set; }
    public int Y { get; set; }
    public int Battery { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
}