using DroneAPI.Models;

namespace DroneAPI.Models;

public class DroneData
{
    public int Id { get; set; }

    public int DroneId { get; set; }
    public Drone Drone { get; set; }   // ✅ FIX

    public int X { get; set; }
    public int Y { get; set; }
    public int Battery { get; set; }
}