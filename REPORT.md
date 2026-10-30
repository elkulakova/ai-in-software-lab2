# REPORT: Context engineering — AGENTS.md и skill

## 1. Кодовая база

Репозиторий: `yacht-project` (Yacht Sharing Pro, .NET 9).

Почему подходит под шаг 0:

- есть слои BookingService: Controllers → Application → Domain → Infrastructure;
- ~4k строк C#, тесты (unit + integration), docker-compose;
- неочевидные конвенции (маппинг только в `Application/Mappings`, `sealed record` DTO, `IDateTimeProvider`, ошибки через middleware и т.д.).

Чистая точка для baseline: коммит `7517704` (тег `baseline-clean`).

## 2. Задача для прогонов

**Экспорт данных клиента** («скачать мои данные»):

- `GET /api/v1/clients/{clientId}/export`;
- JSON: профиль + бронирования + экипаж + платежи;
- 404 если клиента нет; пустые коллекции если броней нет;
- unit/integration-тесты на три сценария.

Промпт на всех baseline-прогонах одинаковый; **без** упоминания конвенций / AGENTS / check-скрипта. Каждый прогон — новая ветка от `7517704` и **новый чат** с агентом.

промпт:

```
Добавь экспорт данных клиента в YachtSharing.BookingService («скачать мои данные»).
  Требования:
  1. REST-эндпоинт, например GET /api/v1/clients/{clientId}/export (или эквивалент в том же стиле API проекта).
  2. Ответ — JSON с данными клиента: профиль + все его бронирования + члены экипажа этих броней + платежи по этим броням (всё, что есть в домене BookingService по этому client_id).
  3. Доступ только к данным указанного клиента: если клиента нет — 404; не подмешивай чужие бронирования/платежи.
  4. Если для клиента нет броней — всё равно верни валидный экспорт (профиль + пустые коллекции), не ошибку.
  5. Нужны unit- и/или integration-тесты на сценарии: успешный экспорт с данными; клиент не найден; клиент без броней. Сделай так, чтобы фича естественно вписалась в существующий проект. Не объясняй теорию — сразу внеси изменения в код.
```

## 3. Конвенции и автоматическая проверка

Файл: `yacht-project/check_conventions.py`.

Замер прогона агента (только файлы из diff):

```bash
python3 yacht-project/check_conventions.py yacht-project --base 7517704
```

Краткий список правил (проверяемые):

1. Domain не тянет Infrastructure / Controllers / Application / ASP.NET / EF  
2. Controllers без DbContext / EF / Persistence  
3. Controllers зависят от Application.Abstractions, не от Infrastructure  
4. `DomainException`-наследники замаплены в `ExceptionHandlingMiddleware`  
5. Route: `api/v1/...` или `health`  
6. Публичные async в Controllers / Application.Services — с `CancellationToken`  
7. В тестах FluentAssertions, не `Assert.`  
8. Имена unit-тестов: `Method_Expected_When...` / `Method_Returns...`  
9. Application без `DateTime.UtcNow/Now` — только `IDateTimeProvider`  
10. Entity-свойства с `private set`  
11. Application не импортирует Infrastructure  
12. Нет `Console.Write*` в BookingService  
13. DTO — `public sealed record`  
14–15. `new *Dto(` только в `Application/Mappings`, не в Controllers / Services  
16. Controllers не импортируют `Domain.Entities`  
17. `Booking.Status = ...` только в Domain (через domain-методы)  
18. Коллекции в репозиториях — `IReadOnlyList<>`  
19. Controllers не ловят `KeyNotFoundException` / `DomainException` (это middleware; проверка в режиме `--base`)

На чистом `7517704` и на `main` без agent-diff: **нарушений: 0**.

## 4. Baseline (без AGENTS.md / skill)

Сырые логи: `logs/baseline-attempt-1.txt`, `logs/baseline-attempt-2.txt`, сводка `logs/SUMMARY.txt`.

| Прогон | Ветка | Коммит | Нарушений (check `--base 7517704`) |
|--------|--------|--------|-------------------------------------|
| 1 | `baseline-attempt-1` | `b76e5c2` | **9** |
| 2 | `baseline-attempt-2` | `ca09181` | **9** |
| 3 | `baseline-attempt-3` | — | *(пока чистая база; прогон у сокомандницы)* |

> В сообщении коммита attempt-1 было «8 violations»; повторный прогон актуальным скриптом с `main` даёт **9**. В отчёте опираемся на повторный автоматический замер.

### 4.1. Что ломал агент (типичное)

**Оба прогона**

- Сборка export-DTO через `new …Dto(` **прямо в** `BookingManagementService`, а не через `Application/Mappings` (*MappingExtensions). Это основной объём нарушений (правила 14–15).

**Прогон 1**

- Дополнительно: `ClientsController` ловит `KeyNotFoundException` сам (правило 19), хотя в проекте есть `ExceptionHandlingMiddleware`.

**Прогон 2**

- Дополнительно: `Application` импортирует `Infrastructure` (правило 11).
- Контроллер в этом прогоне, судя по check, без catch-нарушения; зато слой Application «протекает» вниз.

### 4.2. Вывод по baseline

Агент стабильно решает задачу функционально, но **не соблюдает неочевидный паттерн маппинга** проекта. Слои Controllers/DbContext в целом не ломает «в лоб» — без ужесточённых правил (DTO/Mappings/middleware) baseline давал бы ~0, и эффект AGENTS.md было бы нечем мерить.

Среднее baseline: **(9 + 9) / 2 = 9** нарушений за прогон.

## 5. AGENTS.md и skill

**Статус: ещё не сделано.**

Планируется:

- корневой `AGENTS.md` с теми же проверяемыми правилами, что в check-скрипте;
- skill (например `skills/implement-api-endpoint/SKILL.md`) с пошаговым чеклистом для новой API-фичи: route → use-case → mapping extensions → репозитории/`IReadOnlyList` → ошибки через domain/middleware → тесты с именованием.

*(Заполнить после написания файлов.)*

## 6. Прогон с контекстом (та же задача export)

**Статус: ещё не сделано.**

План:

1. Ветка от `7517704` / `main` **с** `AGENTS.md` + skill.  
2. Новый чат, тот же промпт export.  
3. `python3 check_conventions.py yacht-project --base 7517704` → `N_after`.  
4. Сравнение с baseline (~9).

| Метрика | Baseline (среднее) | С AGENTS/skill | Δ |
|---------|-------------------|----------------|---|
| нарушений | 9 | *TBD* | *TBD* |

Лог: `logs/with-context-attempt-1.txt` *(создать после прогона)*.

### Какие правила ожидаемо сработают

- 14–15 (Mappings) — главный источник baseline-ошибок;  
- 11 (Application ↛ Infrastructure);  
- 19 (не catch в Controller).

### Какие могут оказаться «бесполезными» на этой задаче

- правила про `Booking.Status =` / soft-delete-подобные — export их почти не провоцирует;  
- `sealed record` / `IReadOnlyList` — агент иногда и так соблюдает.

*(Уточнить по факту после прогона.)*

## 7. Generalization (вторая задача)

**Статус: ещё не сделано.**

Кандидат: soft delete бронирования **или** пагинация/фильтрация списка — та же архитектура, другой сценарий; те же AGENTS.md + skill.

Зафиксировать: промпт, коммит, `нарушений: N`, сравнение с ожиданиями.

Лог: `logs/generalization.txt` *(создать после прогона)*.

## 8. Итоги (черновик)

- Без контекста агент **стабильно** нарушает конвенцию маппинга (~9 нарушений / прогон).  
- Check-скрипт даёт воспроизводимые числа до/после.  
- Дальше: написать AGENTS.md + skill → прогон export с контекстом → generalization → дописать разделы 5–7 и финальный разбор «что сработало / что нет / почему».

## Приложение: как воспроизвести замеры

```bash
# baseline-attempt-1
git checkout baseline-attempt-1
python3 /path/to/main/yacht-project/check_conventions.py yacht-project --base 7517704

# baseline-attempt-2
git checkout baseline-attempt-2
python3 /path/to/main/yacht-project/check_conventions.py yacht-project --base 7517704
```

Актуальная копия скрипта — на ветке `main` (`yacht-project/check_conventions.py`).
