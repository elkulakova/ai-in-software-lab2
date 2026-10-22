using System;
using YachtSharing.BookingService.Domain.Entities;
using YachtSharing.BookingService.Domain.Enums;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.BookingService.Application.Mappings;

public static class BookingMappingExtensions
{
    public static BookingResponseDto ToResponseDto(this Booking booking)
    {
        DateTime expiresAt = booking.CreatedAt.AddHours(1);
        bool? isRefundEligible = booking.Status == BookingStatus.Cancelled
            ? booking.IsRefundEligible
            : null;

        return new BookingResponseDto(
            booking.Id,
            booking.Status,
            booking.FinalCost,
            expiresAt,
            isRefundEligible);
    }

    public static PaymentSessionResponseDto ToPaymentSessionDto(this Booking booking, Uri paymentUrl, string transactionId, DateTime expiresAt)
    {
        return new PaymentSessionResponseDto(
            booking.Id,
            booking.FinalCost,
            paymentUrl,
            transactionId,
            expiresAt);
    }
}