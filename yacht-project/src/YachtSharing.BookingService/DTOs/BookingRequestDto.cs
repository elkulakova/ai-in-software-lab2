using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YachtSharing.BookingService.DTOs;

public sealed record BookingRequestDto(
    [Required] Guid ClientId,
    [Required] int YachtId,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    [Required] PassportDto Passport,
    [Required] IReadOnlyCollection<CrewMemberDto> Crew);