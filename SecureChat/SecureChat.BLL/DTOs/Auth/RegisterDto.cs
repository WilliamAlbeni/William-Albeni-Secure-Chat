using System.ComponentModel.DataAnnotations;

namespace SecureChat.BLL.DTOs.Auth
{
    public class RegisterDto
    {
        [Required, MaxLength(50)]
        public string Username { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        [Required]
        public string PublicKey { get; set; }
    }
}