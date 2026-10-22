using System.ComponentModel.DataAnnotations;

namespace YachtSharing.BookingService.DTOs;

public sealed record RegisterClientDto(
    [Required] string Name,
    [Required] [EmailAddress] string Email);