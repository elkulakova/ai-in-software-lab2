using System;

namespace YachtSharing.BookingService.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }

    public Guid BookingId { get; private set; }

    public decimal Amount { get; private set; }

    public string TransactionId { get; private set; }

    public DateTime PaymentDate { get; private set; }

    public Payment(Guid bookingId, decimal amount, string transactionId, DateTime paymentDate)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Paid amount must be positive.", nameof(amount));
        }

        if (paymentDate <= DateTime.MinValue || paymentDate > DateTime.MaxValue)
        {
            throw new ArgumentException("Payment date must be real.", nameof(paymentDate));
        }

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("Transaction ID cannot be empty.", nameof(transactionId));
        }

        Id = Guid.NewGuid();
        BookingId = bookingId;
        Amount = amount;
        TransactionId = transactionId;
        PaymentDate = paymentDate;
    }
}