# REPORT: Context engineering — AGENTS.md и skill

## 1. Кодовая база

Репозиторий: `yacht-project` (Yacht Sharing Pro, .NET 9).

Чек-лист шага 0:

- [x] Разделение слоёв BookingService: Controllers → Application → Domain → Infrastructure  
- [x] Размер ~4k строк C#, не тривиальный  
- [x] Есть гоняемые тесты (`dotnet test` unit + integration)  
- [x] ≥5–7 неочевидных конвенций (Mappings, sealed record DTO, middleware, `IDateTimeProvider`, `IReadOnlyList`, …)  
- [x] Популярный стек (ASP.NET / EF / xUnit)

Чистая точка baseline: коммит `7517704` (тег `baseline-clean`).  
Точка с контекстом: `main` / `5f8c052` (AGENTS.md + skill + check-скрипт).

---

## 2. Задачи и промпты

### 2.1. Основная задача (baseline + прогон с AGENTS)

**Экспорт данных клиента** («скачать мои данные»).

Промпт (одинаковый на всех прогонах export; **без** упоминания конвенций / AGENTS / check):

```
Добавь экспорт данных клиента в YachtSharing.BookingService («скачать мои данные»).
  Требования:
  1. REST-эндпоинт, например GET /api/v1/clients/{clientId}/export (или эквивалент в том же стиле API проекта).
  2. Ответ — JSON с данными клиента: профиль + все его бронирования + члены экипажа этих броней + платежи по этим броням (всё, что есть в домене BookingService по этому client_id).
  3. Доступ только к данным указанного клиента: если клиента нет — 404; не подмешивай чужие бронирования/платежи.
  4. Если для клиента нет броней — всё равно верни валидный экспорт (профиль + пустые коллекции), не ошибку.
  5. Нужны unit- и/или integration-тесты на сценарии: успешный экспорт с данными; клиент не найден; клиент без броней. Сделай так, чтобы фича естественно вписалась в существующий проект. Не объясняй теорию — сразу внеси изменения в код.
```

Каждый прогон: отдельная ветка, **новый чат**, один и тот же текст промпта.

### 2.2. Задача generalization (вторая, скрытая относительно правил)

**Пагинация + фильтрация списка бронирований.**

Промпт (полностью, как использовался):

```
Добавь пагинацию и фильтрацию списка бронирований в YachtSharing.BookingService.
  Требования:
  1. REST-эндпоинт GET /api/v1/bookings с query-параметрами:
     - page (номер страницы, с 1),
     - limit (размер страницы),
     - status (опциональный фильтр по статусу бронирования: PendingPayment, Confirmed, Cancelled, Expired — как в домене).
  2. Ответ — JSON со списком бронирований на странице и метаданными пагинации (как минимум: total count и/или hasMore / totalPages — выбери один понятный вариант и придерживайся его).
  3. Валидация: некорректные page/limit (например ≤ 0) и неизвестный status — ошибка 400 через принятый в проекте механизм ошибок (не «тихий» дефолт без явной политики). Разумные значения по умолчанию для page/limit, если параметры не переданы, допустимы — зафиксируй их в коде.
  4. Фильтрация и limit/offset (или аналог) должны выполняться на уровне доступа к данным (repository), а не «выгрузить всё в память и порезать в контроллере».
  5. Unit- и/или integration-тесты: успешная первая страница; фильтр по status; невалидный status или page/limit.
  Сделай так, чтобы фича естественно вписалась в существующий проект. Не объясняй теорию — сразу внеси изменения в
  код.
```

Те же `AGENTS.md` + skill, что и для with-agents export. Новые ветки от `main` с контекстом, новые чаты.

---

## 3. Конвенции и автоматическая проверка

Файл: `yacht-project/check_conventions.py` (вариант 1 из шага 4 лабы — скрипт без LLM).

```bash
cd yacht-project
python3 check_conventions.py --base 7517704
# печатает: нарушений: N + список file:line
```

Режим `--base` считает только `.cs` из diff относительно чистой базы (включая untracked). На чистом дереве без agent-diff: **0**.

Проверяемые правила (согласованы с `yacht-project/AGENTS.md`):

1. Domain без Infrastructure / Controllers / Application / ASP.NET / EF  
2. Controllers без DbContext / EF / Persistence  
3. Controllers → Application.Abstractions, не Infrastructure  
4. `DomainException` замаплены в `ExceptionHandlingMiddleware`  
5. Route: `api/v1/...` или `health`  
6. Публичные async + `CancellationToken`  
7. Тесты: FluentAssertions, не `Assert.`  
8. Имена unit-тестов: `Method_Expected_When...` / `Method_Returns...`  
9. Application: `IDateTimeProvider`, не `DateTime.UtcNow/Now`  
10. Entity: `private set`  
11. Application не импортирует Infrastructure  
12. Нет `Console.Write*` в BookingService  
13. DTO = `public sealed record`  
14–15. `new *Dto(` только в `Application/Mappings`  
16. Controllers без `Domain.Entities`  
17. `Booking.Status =` только в Domain  
18. Репозитории: `IReadOnlyList<>`  
19. Controllers не ловят KeyNotFound/DomainException (middleware; в `--base`)

Сырые логи замеров: каталог `logs/` (`baseline-attempt-*.txt`, `with-agents-attempt-*.txt`, `generalization-attempt-*.txt`, `SUMMARY.txt`).

---

## 4. Baseline (без AGENTS.md / skill)

Ветки от `7517704`, задача — export (промпт §2.1).

| Прогон | Ветка | Коммит | Нарушений |
|--------|--------|--------|-----------|
| 1 | `baseline-attempt-1` | `b76e5c2` | **9** |
| 2 | `baseline-attempt-2` | `ca09181` | **9** |
| 3 | `baseline-attempt-3` | `b199567` | **5** |

Среднее: **(9 + 9 + 5) / 3 ≈ 7.7** нарушений.

> В сообщении коммита attempt-1 было «8 violations»; актуальный скрипт даёт **9**. В отчёте — числа повторного автоматического замера.

### 4.1. Типичные ошибки агента

**Все три прогона**

- Сборка export-DTO через `new …Dto(` **в** `BookingManagementService`, а не в `Application/Mappings` (правила 14–15) — основной объём нарушений.

**Прогон 1**

- `ClientsController` сам ловит `KeyNotFoundException` (правило 19).

**Прогон 2**

- `Application` импортирует `Infrastructure` (правило 11).

**Прогон 3**

- Только нарушения маппинга (5× `new …Dto(` в сервисе); без catch/Infrastructure в check.

### 4.2. Вывод

Агент стабильно делает фичу «рабочей», но **не угадывает неочевидный паттерн Mappings**. Грубые слои (DbContext в Controller) почти не ломает — поэтому check специально ужесточали под реальные паттерны репо.

---

## 5. AGENTS.md и skill

На `main`:

- `AGENTS.md` (указатель) + канонический **`yacht-project/AGENTS.md`** — формат [agents.md](https://agents.md/): overview, setup/test commands, code style, testing, PR.  
- Skill: **`yacht-project/skills/implement-api-endpoint/SKILL.md`** (+ `references/e2e-isolation.md` для progressive disclosure).  
  Frontmatter `name` / `description`; пошаговая реализация API под те же проверяемые правила.

Инструкции **конкретные и проверяемые** (куда класть DTO, запрет `new *Dto(` вне Mappings, middleware вместо catch, `IReadOnlyList`, имена тестов) — не «write good tests».

Черновики авторов локально: `AGENTS_liza.md` / `AGENTS_luda.md`, `SKILL_liza.md` / `SKILL_luda.md` (в `.gitignore`).

---

## 6. Прогон с контекстом (та же задача export)

База веток: `main` с AGENTS/skill. Тот же промпт §2.1. Новые чаты.

| Прогон | Ветка | Коммит | Нарушений |
|--------|--------|--------|-----------|
| 1 | `with-agents-attempt-1` | `9309509` | **0** |
| 2 | `with-agents-attempt-2` | `fd59eab` | **0** |
| 3 | `with-agents-attempt-3` | `f393959` | **0** |

| Метрика | Baseline (среднее) | С AGENTS/skill | Δ |
|---------|-------------------|----------------|---|
| нарушений | ≈7.7 (9 / 9 / 5) | **0 / 0 / 0** | устранение типичных нарушений маппинга / слоёв |

### Что сработало

- **14–15 (Mappings)** — главный выигрыш: с контекстом DTO собираются через `*MappingExtensions`, check не ругается.  
- **11 (Application ↛ Infrastructure)** — во with-agents не всплыло.  
- **19 (не catch в Controller)** — во with-agents не всплыло.  
- Skill с чеклистом «route → use-case → Mappings → repo → tests» совпал с тем, что агент реально ломал на baseline.

### Что было малополезно / не проявилось на export

- Правила про присвоение `Booking.Status` — задача export их почти не провоцирует.  
- `sealed record` / `IReadOnlyList` — на with-agents агент и так часто соблюдал; на baseline check их почти не ловил как основной источник N.  
- Раздел E2E в skill (`references/e2e-isolation.md`) на этих прогонах не использовался (задача — реализация API, не стенд).

---

## 7. Generalization (пагинация + фильтр)

Те же AGENTS.md + skill. Промпт — полностью §2.2. Ветки от `main` с контекстом.

| Прогон | Ветка | Коммит | Нарушений |
|--------|--------|--------|-----------|
| 1 | `generalization-attempt-1` | `d0fabc1` | **0** |
| 2 | `generalization-attempt-2` | `38cd237` | **0** |
| 3 | `generalization-attempt-3` | `cb1924c` | **0** |

### Вывод по генерализуемости

Правила **не привязаны только к export**: на другой задаче (query `page`/`limit`/`status`, список, repository limit/offset, валидация 400) check снова дал **0**.  
Значит инструкции сформулированы как общие принципы слоёв/DTO/Mappings/ошибок, а не как «как сделать export».

*(Если бы на generalization снова сыпались `new *Dto(` в Service — правила были бы слишком узкими или skill слишком export-специфичным; этого не произошло.)*

---

## 8. Итоги

| Этап | Нарушений (check `--base 7517704`) |
|------|-------------------------------------|
| Baseline export ×3 | 9, 9, 5 |
| With AGENTS export ×3 | 0, 0, 0 |
| Generalization pagination ×3 | 0, 0, 0 |

1. **Качество инструкций:** проверяемые пункты в AGENTS/skill совпадают со скриптом.  
2. **Измеримость:** автоматический check + ≥2 baseline + числа до/после.  
3. **Генерализуемость:** вторая задача (пагинация) с теми же правилами → 0 нарушений.  
4. **Анализ:** без контекста агент стабильно игнорировал Mappings; AGENTS/skill это устранили; на новой задаче эффект сохранился. Малополезными на данных прогонах оказались статус-мутации и E2E-справка в skill.

---

## Приложение A. Ветки эксперимента

| Ветка | Назначение |
|--------|------------|
| `baseline-attempt-1..3` | export без AGENTS |
| `with-agents-attempt-1..3` | export с AGENTS/skill |
| `generalization-attempt-1..3` | pagination+filter с AGENTS/skill |
| `main` | AGENTS, skill, check, REPORT |

## Приложение B. Воспроизведение замера

```bash
git checkout <ветка>
cd yacht-project   # или путь к check с main
python3 check_conventions.py --base 7517704
```

Актуальный скрипт: `yacht-project/check_conventions.py` на `main`.
