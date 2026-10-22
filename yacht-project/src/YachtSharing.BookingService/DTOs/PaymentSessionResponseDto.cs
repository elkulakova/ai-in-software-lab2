using System;

namespace YachtSharing.BookingService.DTOs;

public sealed record PaymentSessionResponseDto(
    Guid BookingId,
    decimal Amount,
    Uri PaymentUrl,
    string TransactionId,
    DateTime ExpirationTime);