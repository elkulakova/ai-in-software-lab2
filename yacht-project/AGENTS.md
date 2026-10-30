# AGENTS.md

Руководство для coding-агентов по репозиторию lab1 / проекту **Yacht Sharing Pro**  
(формат: [agents.md](https://agents.md/) — отдельный README для агентов).

Ближайший `AGENTS.md` к редактируемому файлу имеет приоритет; явный промпт пользователя перекрывает всё.

Для пошаговой реализации API-эндпоинта подгружай skill:  
`skills/implement-api-endpoint/SKILL.md`  
(путь относительно `yacht-project/`).

---

## Project overview

- Решение: `yacht-project/YachtSharingPro.sln`, **.NET 9**.
- Сервисы в `yacht-project/src/`:
  - `YachtSharing.BookingService` — клиенты, бронирования, платежи, expiration worker;
  - `YachtSharing.CatalogService` — каталог яхт / доступность (gRPC, `Protos/catalog.proto`);
  - `YachtSharing.MockGims` — мок проверки лицензий (REST).
- Booking → Catalog только **gRPC**; Booking → MockGims — **HTTP**.
- Конвенции BookingService проверяются скриптом `yacht-project/check_conventions.py` (должен совпадать с правилами ниже).

---

## Setup commands

Все команды — из `yacht-project/`, если не указано иное.

```bash
# зависимости / сборка
dotnet restore YachtSharingPro.sln
dotnet build YachtSharingPro.sln

# локальный стенд (раздельные PostgreSQL для Booking и Catalog)
docker compose up --build

# unit / integration tests
dotnet test tests/YachtSharing.UnitTests/YachtSharing.UnitTests.csproj
dotnet test tests/YachtSharing.IntegrationTests/YachtSharing.IntegrationTests.csproj

# проверка конвенций (полный прогон дерева)
python3 check_conventions.py

# проверка только изменений агента относительно чистой базы
python3 check_conventions.py --base 7517704
# ожидаемый итог: нарушений: 0
```

Из корня monorepo `lab1/`:

```bash
python3 yacht-project/check_conventions.py yacht-project --base 7517704
```

---

## Dev environment tips

- `Directory.Build.props`: nullable **on**, implicit usings **off** — пиши `using` явно.
- Версии NuGet только в `Directory.Packages.props`; в `.csproj` у `PackageReference` **не** указывай `Version=`.
- Слои BookingService:
  - `Controllers` → HTTP + use-cases + DTOs;
  - `Application` → сценарии, Abstractions, Mappings (без Infrastructure / EF);
  - `Domain` → Entities, ValueObjects, Exceptions (без ASP.NET / EF / Infrastructure / DTOs);
  - `Infrastructure` → EF, репозитории, внешние клиенты, DI (`InfrastructureExtensions`).
- `BookingServiceE2eFactory` подменяет БД на EF InMemory и интеграции заглушками — сохраняй эту изоляцию в integration-тестах.
- Catalog слушает HTTP/1 и HTTP/2 (gRPC); внутренний вызов Booking → Catalog идёт на gRPC-порт из конфига (`IntegrationServices:CatalogGrpcUrl`).

---

## Code style

Проверяемые конвенции (агент обязан соблюдать буквально):

### Layers

- `Domain/` не импортирует Application, Infrastructure, Controllers, Middleware, DTOs, `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`.
- `Application/` не импортирует `YachtSharing.BookingService.Infrastructure`.
- Controllers не трогают `DbContext` / EF / `Infrastructure.Persistence` и не инжектят репозитории / `IUnitOfWork` / `BookingDbContext`.
- Controllers не импортируют `Domain.Entities` — только DTOs и `Application.Abstractions` (допустимы `Domain.Exceptions` только если неизбежно; новые эндпоинты предпочтительно без catch).

### API & async

- Маршруты контроллеров: `api/v1/...` (исключение: `health`).
- Публичные `async` в Controllers и `Application/Services` принимают `CancellationToken` и прокидывают его дальше.

### Domain & time

- Свойства сущностей: `{ get; private set; }`.
- `Booking.Status = …` только внутри `Domain/Entities` через методы (`Confirm`, `Expire`, `Cancel`, …).
- В Application время только через `IDateTimeProvider` — не `DateTime.Now` / `DateTime.UtcNow`.
- В BookingService логируй через `ILogger`, не `Console.Write` / `WriteLine`.

### DTOs & mapping

- Публичные DTO: только `public sealed record`.
- `new SomeDto(` **запрещён** в Controllers и `Application/Services` — сборка/маппинг только в `Application/Mappings/*MappingExtensions.cs` (как `ToResponseDto`).

### Repositories & errors

- В `Application/Abstractions/Repositories` коллекции: `Task<IReadOnlyList<T>>`, не `List` / `IList` / `ICollection` / `IEnumerable`.
- Новый `DomainException` → ветка в `ExceptionHandlingMiddleware.MapDomainExceptionToCode`.
- Controllers **не** ловят `KeyNotFoundException` / `DomainException` / `InvalidBookingStatusTransitionException` — это middleware.  
  «Не найдено» в Application → `KeyNotFoundException`; бизнес-правило → `DomainException`.  
  (В check-скрипте правило catch активно в режиме `--base`.)

### Negative

- Не копируй try/catch из старых методов `BookingsController` для **новых** действий.
- Не клади бизнес-логику и сборку response DTO в Controller.

---

## Testing instructions

- Стек: xUnit, **FluentAssertions** (`.Should()`, без `Assert.*`), Moq.
- Имена unit-тестов: `Method_Expected_When...` или `Method_Returns...` (допустим `_For...`).  
  Пример: `ProcessPaymentWebhookAsync_DoesNothing_WhenTransactionAlreadyProcessed`.
- Добавляй/обновляй тесты на изменённый код (happy-path + «не найдено» + краевой случай из ТЗ).
- Перед завершением задачи по возможности запусти релевантный `dotnet test` и `check_conventions.py`; при падении — исправь.
- Если check/тесты не запускались — явно напиши почему.

---

## PR instructions

- Сообщения коммитов: `add|feat|fix (область): что изменилось`.  
  Пример: `fix (BookingService): исправлена обработка просроченной оплаты`.
- В описании PR: изменение поведения, затронутые сервисы/контракты, результаты тестов и `check_conventions.py`.
