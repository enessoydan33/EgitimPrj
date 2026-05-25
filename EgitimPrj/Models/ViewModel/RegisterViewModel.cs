using System.ComponentModel.DataAnnotations;
using EgitimPrj.Models.Validation;

namespace EgitimPrj.Models.ViewModel
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Ad zorunludur.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        public string SurName { get; set; } = string.Empty;

        private string _email = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [StandardEmail]
        public string Email
        {
            get => _email;
            set => _email = EmailInput.Normalize(value);
        }

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Veli adı")]
        public string? ParentName { get; set; }

        [Display(Name = "Veli soyadı")]
        public string? ParentSurname { get; set; }

        private string? _parentEmail;

        [Display(Name = "Veli e-posta")]
        [StandardEmail(ErrorMessage = "Veli için geçerli bir e-posta girin.")]
        public string? ParentEmail
        {
            get => _parentEmail;
            set => _parentEmail = string.IsNullOrWhiteSpace(value)
                ? null
                : EmailInput.Normalize(value);
        }

        [Display(Name = "Veli şifresi")]
        public string? ParentPassword { get; set; }

        [Display(Name = "Veli şifresi (tekrar)")]
        [Compare("ParentPassword", ErrorMessage = "Veli şifreleri uyuşmuyor.")]
        public string? ParentConfirmPassword { get; set; }

        [Required(ErrorMessage = "Kullanım şartlarını kabul etmelisiniz.")]
        public bool AcceptTerms { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasEmail = !string.IsNullOrWhiteSpace(ParentEmail);
            var hasPwd = !string.IsNullOrWhiteSpace(ParentPassword);
            if (hasEmail != hasPwd)
            {
                yield return new ValidationResult(
                    "Veli hesabı için e-posta ve şifreyi birlikte girin.",
                    new[] { nameof(ParentEmail), nameof(ParentPassword) });
            }

            if (hasPwd && (ParentPassword?.Length ?? 0) < 6)
            {
                yield return new ValidationResult(
                    "Veli şifresi en az 6 karakter olmalıdır.",
                    new[] { nameof(ParentPassword) });
            }
        }
    }
}
