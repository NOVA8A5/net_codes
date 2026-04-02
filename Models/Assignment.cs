namespace DroneAPI.Models;

public class Assignment
{
    public int Id { get; set; }

    public int DroneId { get; set; }
    public int OperatorId { get; set; }

    public Drone Drone { get; set; }
    public Operator Operator { get; set; }
}