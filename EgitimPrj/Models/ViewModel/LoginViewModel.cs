using System.ComponentModel.DataAnnotations;
using EgitimPrj.Models.Validation;

namespace EgitimPrj.Models.ViewModel
{
    public class LoginViewModel
    {
        private string _email = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [StandardEmail]
        public string Email
        {
            get => _email;
            set => _email = EmailInput.Normalize(value);
        }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;

    }
}
