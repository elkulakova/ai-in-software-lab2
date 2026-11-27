using System;

namespace YachtSharing.BookingService.Domain.Entities;

public class UserClient
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public UserClient(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Client name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Client email cannot be empty.", nameof(email));
        }

        Id = Guid.NewGuid();
        Name = name;
        Email = email;
    }

    private UserClient()
    {
    }
}