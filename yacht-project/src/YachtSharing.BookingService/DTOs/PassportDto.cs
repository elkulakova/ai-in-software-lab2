using System.ComponentModel.DataAnnotations;

namespace YachtSharing.BookingService.DTOs;

public sealed record PassportDto(
    [Required]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Passport series must be exactly 4 digits.")]
    string Series,

    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Passport number must be exactly 6 digits.")]
    string Number);