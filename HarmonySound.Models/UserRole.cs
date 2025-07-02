namespace HarmonySound.Models
{
    public class UserRole
    {
        public virtual User? User { get; set; }
        public virtual Role? Role { get; set; }
    }
}
