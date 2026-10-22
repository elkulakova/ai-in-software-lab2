using System;
using System.ComponentModel.DataAnnotations;
using YachtSharing.BookingService.Domain.Enums;

namespace YachtSharing.BookingService.DTOs;

public sealed record PaymentWebhookDto(
    [Required] Guid BookingId,
    [Required] string TransactionId,
    [Required] PaymentStatus Status,
    [Required] decimal Amount);