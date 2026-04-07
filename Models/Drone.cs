namespace DroneAPI.Models;

public class Drone
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public int Battery { get; set; }

    public string Status { get; set; } = "Idle";

    public List<DroneData>? DroneDatas { get; set; }
}