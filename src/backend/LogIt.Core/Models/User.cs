namespace LogIt.Core.Models;

public enum UserRole { Backend, Frontend, System }

public class User
{
    public int UserId { get; set; }
    public UserRole Role { get; set; }
}
