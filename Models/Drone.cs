namespace DroneAPI.Models;

public class Drone
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }

    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;

    public int Battery { get; set; } = 100;
    public int Speed { get; set; } = 1;

    public string Status { get; set; } = "Idle";

    public List<Assignment> Assignments { get; set; } = new();
}