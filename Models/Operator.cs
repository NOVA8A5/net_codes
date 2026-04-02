namespace DroneAPI.Models;

public class Operator
{
    public int Id { get; set; }
    public string Name { get; set; }

    public List<Assignment> Assignments { get; set; } = new();
}