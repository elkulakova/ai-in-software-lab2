using System;

namespace YachtSharing.BookingService.Domain.ValueObjects;

public record Passport
{
    public string Series { get; init; }

    public string Number { get; init; }

    public Passport(string series = "0000", string number = "000000")
    {
        if (string.IsNullOrWhiteSpace(series) || string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Neither passport number nor series can be empty.");
        }

        if (series.Length != 4 || number.Length != 6)
        {
            throw new ArgumentException("Either series or number length is wrong.");
        }

        Series = series;
        Number = number;
    }
}