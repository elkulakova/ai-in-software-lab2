using System;
using YachtSharing.BookingService.Domain.Enums;

namespace YachtSharing.BookingService.Domain.Entities;

public class CrewMember
{
    public Guid Id { get; private set; }

    public Guid BookingId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public DateTime InsuranceExpiry { get; private set; }

    public CrewRole Role { get; private set; }

    public CrewMember(Guid bookingId, string fullName, DateTime insuranceExpiry, CrewRole role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Crew member name cannot be empty.", nameof(fullName));
        }

        Id = Guid.NewGuid();
        BookingId = bookingId;
        FullName = fullName;
        InsuranceExpiry = insuranceExpiry;
        Role = role;
    }

    public bool IsInsuranceValid(DateTime rentalEndDate)
    {
        return InsuranceExpiry >= rentalEndDate;
    }

    private CrewMember()
    {
    }
}