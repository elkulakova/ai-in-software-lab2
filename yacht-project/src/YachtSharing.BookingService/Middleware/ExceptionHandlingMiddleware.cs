using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.Infrastructure.Exceptions;

namespace YachtSharing.BookingService.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            string errorCode = MapDomainExceptionToCode(ex);
            await WriteErrorAsync(context, ex.StatusCode, errorCode, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, "NOT_FOUND", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "BAD_REQUEST", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "INVALID_OPERATION", ex.Message);
        }
        catch (CatalogIntegrationException ex)
        {
            _logger.LogError(ex, "External Catalog service communication failed.");
            await WriteErrorAsync(
                context,
                StatusCodes.Status502BadGateway,
                "CATALOG_SERVICE_UNAVAILABLE",
                ex.Message);
        }
        catch (GimsIntegrationException ex)
        {
            _logger.LogError(ex, "External GIMS registry communication failed.");
            await WriteErrorAsync(
                context,
                StatusCodes.Status502BadGateway,
                "GIMS_SERVICE_UNAVAILABLE",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred in BookingService");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "SERVER_ERROR", "Internal server error");
        }
    }

    private static string MapDomainExceptionToCode(DomainException exception)
    {
        return exception switch
        {
            InvalidWorkingHoursException => "INVALID_WORKING_HOURS",
            ExpiredInsuranceException => "INSURANCE_EXPIRED",
            PaymentAmountMismatchException => "PAYMENT_RECONCILIATION_FAILED",
            GimsVerificationFailedException => "MCHS_VERIFICATION_FAILED",
            PaymentTimeoutException => "REQUEST_TIMEOUT",
            InvalidBookingDurationException => "INVALID_BOOKING_DURATION",
            InvalidBookingStatusTransitionException => "INVALID_STATUS_TRANSITION",
            ClientHasActiveBookingsException => "CANNOT_DELETE_CLIENT",
            YachtNotAvailableException => "YACHT_NOT_AVAILABLE",
            _ => "DOMAIN_ERROR",
        };
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string errorCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        string payload = JsonSerializer.Serialize(new
        {
            error = errorCode,
            details = message,
            timestamp = DateTime.UtcNow,
        });

        await context.Response.WriteAsync(payload);
    }
}