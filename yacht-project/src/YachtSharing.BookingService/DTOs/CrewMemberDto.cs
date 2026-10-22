using System;
using System.ComponentModel.DataAnnotations;

namespace YachtSharing.BookingService.DTOs;

public sealed record CrewMemberDto(
    [Required] string FullName,
    [Required] DateTime InsuranceExpiry);