namespace DroneAPI.Models;



public class Assignment
{
    public int Id { get; set; }

    public int DroneId { get; set; }
    public Drone Drone { get; set; }   // ✅ now works

    public int OperatorId { get; set; }
    public User Operator { get; set; }
}