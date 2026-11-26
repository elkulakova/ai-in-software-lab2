using Catalog;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Repositories;
using YachtSharing.BookingService.Application.Abstractions.Services;
using YachtSharing.BookingService.Application.Abstractions.UseCases;
using YachtSharing.BookingService.Application.Mappings;
using YachtSharing.BookingService.Domain.Entities;
using YachtSharing.BookingService.Domain.Enums;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.Domain.ValueObjects;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.BookingService.Application.Services;

public class BookingManagementService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClientRepository _clientRepository;
    private readonly ICrewRepository _crewRepository;
    private readonly ICatalogIntegrationService _catalogService;
    private readonly IGimsIntegrationService _gimsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<BookingManagementService> _logger;

    public BookingManagementService(
        IUnitOfWork unitOfWork,
        IClientRepository clientRepository,
        ICrewRepository crewRepository,
        ICatalogIntegrationService catalogService,
        IGimsIntegrationService gimsService,
        IDateTimeProvider dateTimeProvider,
        ILogger<BookingManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _clientRepository = clientRepository;
        _crewRepository = crewRepository;
        _catalogService = catalogService;
        _gimsService = gimsService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Guid> RegisterClientAsync(
        RegisterClientDto dto,
        CancellationToken cancellationToken = default)
    {
        UserClient? existingClient = await _clientRepository.GetByNameAndEmailAsync(dto.Name, dto.Email, cancellationToken);

        if (existingClient != null)
        {
            _logger.LogInformation("Client with email {Email} already exists. Returning existing ID.", dto.Email);
            return existingClient.Id;
        }

        var newClient = new UserClient(dto.Name, dto.Email);

        _clientRepository.Add(newClient);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully registered new client with ID {ClientId}", newClient.Id);

        return newClient.Id;
    }

    public async Task DeleteClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        UserClient client = await _clientRepository.GetByIdAsync(clientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Client with ID {clientId} was not found.");

        bool hasActiveBookings = await _unitOfWork.BookingRepository
            .HasActiveBookingsForClientAsync(clientId, cancellationToken);

        if (hasActiveBookings)
        {
            throw new ClientHasActiveBookingsException(clientId);
        }

        _clientRepository.Remove(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Client {ClientId} and related booking data were deleted.", clientId);
    }

    public async Task<BookingResponseDto> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        Booking booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId, cancellationToken)
                          ?? throw new KeyNotFoundException($"Booking with ID {bookingId} was not found.");

        return booking.ToResponseDto();
    }

    public async Task<BookingResponseDto> CreateBookingAsync(
        BookingRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        UserClient client = await _clientRepository.GetByIdAsync(dto.ClientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Client with ID {dto.ClientId} was not found.");

        decimal pricePerHour = await _catalogService.GetYachtPriceIfAvailableAsync(
            dto.YachtId, dto.StartDate, dto.EndDate, cancellationToken);

        if (pricePerHour <= 0)
        {
            throw new YachtNotAvailableException(dto.YachtId, dto.StartDate, dto.EndDate);
        }

        var passport = new Passport(dto.Passport.Series, dto.Passport.Number);
        bool isGimsValid = await _gimsService.VerifySkipperLicenseAsync(
            passport.Series, passport.Number, cancellationToken);

        if (!isGimsValid)
        {
            throw new GimsVerificationFailedException(passport.Series, passport.Number);
        }

        double durationHours = (dto.EndDate - dto.StartDate).TotalHours;
        decimal finalCost = pricePerHour * (decimal)durationHours;

        var booking = new Booking(
            client.Id,
            dto.YachtId,
            dto.StartDate,
            dto.EndDate,
            passport,
            finalCost);

        booking.VerifyGims();

        var crewMembers = new List<CrewMember>();
        foreach (CrewMemberDto crewDto in dto.Crew)
        {
            var member = new CrewMember(booking.Id, crewDto.FullName, crewDto.InsuranceExpiry, CrewRole.CrewMember);
            if (!member.IsInsuranceValid(booking.EndDate))
            {
                throw new ExpiredInsuranceException(member.FullName, member.InsuranceExpiry);
            }

            crewMembers.Add(member);
        }

        _unitOfWork.BookingRepository.Add(booking);
        _crewRepository.AddRange(crewMembers);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var grpcRequest = new RegisterBookingRequest
        {
            YachtId = booking.YachtId,
            StartTime = booking.StartDate.ToString("o"),
            EndTime = booking.EndDate.ToString("o"),
        };

        await _catalogService.RegisterBookingAsync(grpcRequest);

        return booking.ToResponseDto();
    }

    public async Task<PaymentSessionResponseDto> PrepareCheckoutAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        Booking booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking with ID {bookingId} was not found.");

        DateTime now = _dateTimeProvider.UtcNow;
        DateTime expirationTime = booking.CreatedAt.AddHours(1);

        if (booking.Status is BookingStatus.Expired or BookingStatus.Cancelled)
        {
            throw new InvalidBookingStatusTransitionException(booking.Status, BookingStatus.PendingPayment);
        }

        if (now > expirationTime)
        {
            booking.Expire();
            _unitOfWork.BookingRepository.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new PaymentTimeoutException(bookingId);
        }

        var paymentUrl = new Uri($"https://yacht-pay.mock/session/{booking.Id}");
        string transactionId = $"tx_{Guid.NewGuid():N}";
        return booking.ToPaymentSessionDto(paymentUrl, transactionId, expirationTime);
    }

    public async Task ProcessPaymentWebhookAsync(
        PaymentWebhookDto dto,
        CancellationToken cancellationToken = default)
    {
        Payment? existingPayment = await _unitOfWork.PaymentRepository.GetByTransactionIdAsync(dto.TransactionId, cancellationToken);
        if (existingPayment != null)
        {
            _logger.LogInformation("Webhook ignored. Transaction {TransactionId} has already been processed.", dto.TransactionId);
            return;
        }

        Booking booking = await _unitOfWork.BookingRepository.GetByIdAsync(dto.BookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking with ID {dto.BookingId} associated with webhook was not found.");

        if (booking.FinalCost != dto.Amount)
        {
            throw new PaymentAmountMismatchException(booking.Id, booking.FinalCost, dto.Amount);
        }

        switch (dto.Status)
        {
            case PaymentStatus.Success:
            {
                booking.Confirm();

                var payment = new Payment(
                    booking.Id,
                    dto.Amount,
                    dto.TransactionId,
                    _dateTimeProvider.UtcNow);

                _unitOfWork.BookingRepository.Update(booking);
                _unitOfWork.PaymentRepository.Add(payment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment successful for booking {BookingId}. Transaction: {TransactionId}", booking.Id, dto.TransactionId);
                break;
            }

            case PaymentStatus.Failed:
                _logger.LogWarning(
                    "Payment failed for booking {BookingId}. Transaction {TransactionId}. The booking remains available for retry.", dto.BookingId, dto.TransactionId);
                break;
        }
    }

    public async Task CancelBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        Booking booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking with ID {bookingId} was not found.");

        DateTime now = _dateTimeProvider.UtcNow;
        booking.Cancel(now);

        _unitOfWork.BookingRepository.Update(booking);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}