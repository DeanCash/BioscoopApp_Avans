using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models.User
{
    public class UserModel
    {
        public int UserId { get; set; }           
        [Key]
        public string Username { get; set; } = default!;  
        public string PasswordHash { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
