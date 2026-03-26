using System.ComponentModel.DataAnnotations;

namespace EgitimPrj.Models.ViewModel
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad zorunludur.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        public string SurName { get; set; } = string.Empty;

        [Required, EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanım şartlarını kabul etmelisiniz.")]
        public bool AcceptTerms { get; set; }
    }
}
