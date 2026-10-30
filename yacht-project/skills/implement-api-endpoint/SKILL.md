---
name: implement-api-endpoint
description: >-
  Implement a REST endpoint or use-case in YachtSharing.BookingService following
  project layers, sealed-record DTOs, Application/Mappings, middleware errors,
  IDateTimeProvider, and FluentAssertions tests. Use when adding or changing
  Booking API features (export, soft delete, pagination, bookings, clients,
  payments). For optional multi-service E2E without touching the working repo,
  read references/e2e-isolation.md.
---

# Implement API endpoint (BookingService)

Follow steps **in order**. Align with root `AGENTS.md` and `yacht-project/check_conventions.py`.

Paths below are under `yacht-project/src/YachtSharing.BookingService/` unless noted.

## 1. API contract

1. Add/extend a controller action under `[Route("api/v1/...")]`.
2. Signature: `async Task<IActionResult> Name(..., CancellationToken cancellationToken)`.
3. Controller only: bind input → call `IBookingService` (or other Abstractions use-case) → `Ok` / `CreatedAtAction` / `NoContent`.
4. Do **not** `catch` `KeyNotFoundException` or `DomainException` — `ExceptionHandlingMiddleware` handles them.
5. Do **not** import `Domain.Entities` or write `new XxxDto(` in the controller.

## 2. DTOs

1. Put types in `DTOs/`.
2. Only `public sealed record Name(...);`.
3. Request DTOs: follow `BookingRequestDto` (`[Required]` where needed).

## 3. Application use-case

1. Add method on `IBookingService` + `BookingManagementService` (or a dedicated Abstractions use-case).
2. Always take `CancellationToken cancellationToken = default`.
3. Time via `_dateTimeProvider.UtcNow` only — never `DateTime.UtcNow` / `Now`.
4. Missing entity → `KeyNotFoundException`; business rule → `DomainException` (map new types in step 6).
5. Persist via `_unitOfWork` / repository abstractions — **no** `using …Infrastructure`.
6. Do **not** build response DTOs with `new …Dto(` in the service.

## 4. Mapping (required)

1. Edit/create `Application/Mappings/*MappingExtensions.cs`.
2. Put all `new …Dto(` and entity→DTO mapping **here** (e.g. `ToResponseDto`).
3. From the service call `entity.ToSomethingDto(...)`.

## 5. Repositories

1. Declare methods first on `Application/Abstractions/Repositories/I*.cs`.
2. Collection returns: `Task<IReadOnlyList<T>>` only.
3. Implement under `Infrastructure/Persistence/Repositories/`.

## 6. Domain (when state/rules change)

1. Mutate status via entity methods (`Confirm`, `Cancel`, …) — never `entity.Status = …` from Application.
2. Properties: `{ get; private set; }`.
3. New `DomainException`: file under `Domain/Exceptions/` + branch in `Middleware/ExceptionHandlingMiddleware.MapDomainExceptionToCode`.

## 7. Tests

1. Unit (`tests/YachtSharing.UnitTests`): Moq + FluentAssertions; name `Method_Expected_WhenCondition` or `Method_Returns…`.
2. Cover at least: success; not found; one edge case from the task.
3. Integration if required by the task — same assertion style, no `Assert.`.

## 8. Before finishing

```bash
# from yacht-project/ or with path from lab1 root
python3 check_conventions.py --base 7517704
dotnet test tests/YachtSharing.UnitTests/YachtSharing.UnitTests.csproj
```

Fix until conventions report `нарушений: 0` (when check was run).

## Optional: isolated E2E

If the user asks to verify a multi-service booking flow **without** modifying the working tree, load:

`references/e2e-isolation.md`
