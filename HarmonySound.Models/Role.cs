using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class Role
    {
        [Required]
        [MaxLength(20)]
        public string RoleName { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
