using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EgitimPrj.Models.Validation;

/// <summary>
/// E-posta için pratik doğrulama: baş/son boşluk, NBSP ve Türkçe karakterlere göre net mesaj.
/// [EmailAddress] bazı geçerli kullanım biçimlerinde ve Unicode içinde gereksiz reddeder.
/// </summary>
public sealed class StandardEmailAttribute : ValidationAttribute
{
    private static readonly Regex AsciiEmail = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public StandardEmailAttribute()
        : base("Geçerli bir e-posta girin.") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string raw)
            return ValidationResult.Success;

        var s = EmailInput.Normalize(raw);
        if (string.IsNullOrEmpty(s))
            return ValidationResult.Success;

        foreach (var c in s)
        {
            if (c > 127)
            {
                return new ValidationResult(
                    "E-posta adresinde Türkçe harf (ı, ğ, ş, ö, ü, ç) veya özel karakter kullanılamaz. " +
                    "Genelde ı yerine i, ö yerine o yazılır; adresi İngilizce klavye ile kontrol edin.");
            }
        }

        if (!AsciiEmail.IsMatch(s))
            return new ValidationResult(ErrorMessage ?? "Geçerli bir e-posta girin.");

        return ValidationResult.Success;
    }
}

public static class EmailInput
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Trim().Replace('\u00A0', ' ').Trim();
    }
}
