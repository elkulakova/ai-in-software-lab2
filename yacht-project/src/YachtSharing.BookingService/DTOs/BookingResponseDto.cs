using System;
using YachtSharing.BookingService.Domain.Enums;

namespace YachtSharing.BookingService.DTOs;

public sealed record BookingResponseDto(
    Guid BookingId,
    BookingStatus Status,
    decimal FinalCost,
    DateTime ExpiresAt,
    bool? IsRefundEligible);