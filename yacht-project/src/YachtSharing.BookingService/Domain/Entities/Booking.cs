using System;
using YachtSharing.BookingService.Domain.Enums;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.Domain.ValueObjects;

namespace YachtSharing.BookingService.Domain.Entities;

public class Booking
{
    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    public int YachtId { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public Passport Passport { get; private set; }

    public bool IsGimsVerified { get; private set; }

    public decimal FinalCost { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public BookingStatus Status { get; private set; }

    public bool IsRefundEligible { get; private set; }

    public Booking(
        Guid clientId,
        int yachtId,
        DateTime startDate,
        DateTime endDate,
        Passport passport,
        decimal finalCost)
    {
        double duration = (endDate - startDate).TotalHours;
        if (duration < 1.0)
        {
            throw new InvalidBookingDurationException(duration);
        }

        if (startDate.Hour < 10 || endDate.Hour > 21 || endDate is { Hour: 21, Minute: > 0 })
        {
            throw new InvalidWorkingHoursException(startDate, endDate);
        }

        Id = Guid.NewGuid();
        ClientId = clientId;
        YachtId = yachtId;
        StartDate = startDate;
        EndDate = endDate;
        Passport = passport;
        IsGimsVerified = false;
        FinalCost = finalCost;
        CreatedAt = DateTime.UtcNow;
        Status = BookingStatus.PendingPayment;
        IsRefundEligible = false;
    }

    public void VerifyGims()
    {
        IsGimsVerified = true;
    }

    public void Confirm()
    {
        if (Status != BookingStatus.PendingPayment)
        {
            throw new InvalidBookingStatusTransitionException(Status, BookingStatus.Confirmed);
        }

        Status = BookingStatus.Confirmed;
    }

    public void Expire()
    {
        if (Status != BookingStatus.PendingPayment)
        {
            throw new InvalidBookingStatusTransitionException(Status, BookingStatus.Expired);
        }

        Status = BookingStatus.Expired;
    }

    public void Cancel(DateTime now)
    {
        if (Status != BookingStatus.Confirmed && Status != BookingStatus.PendingPayment)
        {
            throw new InvalidBookingStatusTransitionException(Status, BookingStatus.Cancelled);
        }

        if (Status == BookingStatus.Confirmed)
        {
            IsRefundEligible = (StartDate - now).TotalHours > 24;
        }

        Status = BookingStatus.Cancelled;
    }

    private Booking()
    {
        Passport = new Passport();
    }
}
