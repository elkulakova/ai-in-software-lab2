#!/usr/bin/env python3
"""
Проверка конвенций Yacht Sharing Pro (BookingService).

Запуск:
  python3 check_conventions.py
  python3 check_conventions.py /path/to/yacht-project
  python3 check_conventions.py --base 7517704          # только diff агента
  python3 check_conventions.py yacht-project --base baseline-clean

Печатает «нарушений: N». Exit 0 если N=0.

Конвенции (из реального кода; неочевидные специально, чтобы агент ломал):
  1. Domain не импортирует Infrastructure/Controllers/Application/ASP.NET/EF
  2. Controllers не трогают DbContext / EF / Persistence
  3. Controllers зависят от Application.Abstractions, не от Infrastructure
  4. DomainException-наследники замаплены в ExceptionHandlingMiddleware
  5. Route контроллеров: api/v1/... или health
  6. Публичные async в Controllers и Application/Services — с CancellationToken
  7. tests: FluentAssertions (.Should), не Assert.
  8. UnitTests: Method_Expected_When... или Method_Returns...
  9. Application: не DateTime.UtcNow/Now — только IDateTimeProvider
 10. Domain/Entities: свойства с private set, не { get; set; }
 11. Application не импортирует Infrastructure
 12. BookingService: нет Console.Write/WriteLine
 13. DTOs: публичные типы — sealed record (не class)
 14. Controllers не делают new *Dto( — маппинг/сборка DTO не в контроллере
 15. Application/Services не делают new *Dto( — только через Application/Mappings
 16. Controllers не импортируют Domain.Entities (только DTOs + UseCases [+ Exceptions])
 17. Смена Booking.Status = ... только в Domain/Entities (через Confirm/Expire/Cancel)
 18. В Abstractions/Repositories коллекции возвращают IReadOnlyList<>, не List<>/IEnumerable<>
 19. Controllers не ловят KeyNotFoundException/DomainException (есть middleware)
     — особенно видно в режиме --base
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
BOOKING = ROOT / "src" / "YachtSharing.BookingService"
TESTS = ROOT / "tests"
CHANGED: set[Path] | None = None
violations: list[str] = []


def add(path: Path, line_no: int, msg: str) -> None:
    try:
        rel = path.relative_to(ROOT)
    except ValueError:
        rel = path
    violations.append(f"{rel}:{line_no}: {msg}")


def iter_cs(base: Path):
    if not base.exists():
        return
    for p in base.rglob("*.cs"):
        if "bin" in p.parts or "obj" in p.parts:
            continue
        if CHANGED is not None and p.resolve() not in CHANGED:
            continue
        yield p


def lines_of(path: Path) -> list[str]:
    return path.read_text(encoding="utf-8", errors="replace").splitlines()


def load_changed_since(base_ref: str) -> set[Path]:
    probe = ROOT
    while probe != probe.parent and not (probe / ".git").exists():
        probe = probe.parent
    if not (probe / ".git").exists():
        raise SystemExit("не найден git-репозиторий для --base")

    rel = ROOT.relative_to(probe) if ROOT.is_relative_to(probe) else ROOT
    result = subprocess.run(
        ["git", "diff", "--name-only", "--diff-filter=ACMR", base_ref, "--", str(rel)],
        cwd=probe,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        raise SystemExit(f"git diff failed: {result.stderr.strip()}")

    files: set[Path] = set()
    for line in result.stdout.splitlines():
        line = line.strip()
        if not line.endswith(".cs"):
            continue
        path = (probe / line).resolve()
        if path.exists():
            files.add(path)

    # Untracked files (ещё не в коммите) тоже часть прогона агента
    untracked = subprocess.run(
        ["git", "ls-files", "--others", "--exclude-standard", "--", str(rel)],
        cwd=probe,
        capture_output=True,
        text=True,
        check=False,
    )
    if untracked.returncode == 0:
        for line in untracked.stdout.splitlines():
            line = line.strip()
            if not line.endswith(".cs"):
                continue
            path = (probe / line).resolve()
            if path.exists():
                files.add(path)

    return files


# --- 1 ---
FORBIDDEN_DOMAIN_USING = re.compile(
    r"^using\s+("
    r"YachtSharing\.BookingService\.(Infrastructure|Controllers|Application|Middleware|DTOs)"
    r"|Microsoft\.AspNetCore"
    r"|Microsoft\.EntityFrameworkCore"
    r")\b"
)


def check_domain_isolation() -> None:
    for path in iter_cs(BOOKING / "Domain"):
        for i, line in enumerate(lines_of(path), 1):
            if FORBIDDEN_DOMAIN_USING.search(line.strip()):
                add(path, i, f"Domain не должен импортировать внешние слои: {line.strip()}")


# --- 2 ---
DB_TOUCH = re.compile(
    r"\b(BookingDbContext|DbContext|EntityFrameworkCore|Infrastructure\.Persistence)\b"
)


def check_controllers_no_db() -> None:
    for path in iter_cs(BOOKING / "Controllers"):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if DB_TOUCH.search(line):
                add(path, i, "Controller не должен обращаться к DbContext/EF/Persistence")


# --- 3 ---
USING_INFRA = re.compile(r"^using\s+YachtSharing\.BookingService\.Infrastructure\b")
FIELD_OR_PARAM_INFRA = re.compile(
    r"\bYachtSharing\.BookingService\.Infrastructure\.[A-Za-z0-9_.]+"
)
CONCRETE_DEP = re.compile(
    r"private\s+readonly\s+(BookingDbContext|IBookingRepository|IClientRepository|"
    r"IPaymentRepository|ICrewRepository|IUnitOfWork)\b"
)


def check_controllers_depend_on_abstractions() -> None:
    for path in iter_cs(BOOKING / "Controllers"):
        for i, line in enumerate(lines_of(path), 1):
            stripped = line.strip()
            if stripped.startswith("//"):
                continue
            if USING_INFRA.search(stripped) or FIELD_OR_PARAM_INFRA.search(stripped):
                add(path, i, "Controller — только Application.Abstractions, не Infrastructure")
            if CONCRETE_DEP.search(stripped):
                add(path, i, "Controller вызывает UseCase (IBookingService), не репозиторий/UoW")


# --- 4 ---
CLASS_DOMAIN_EX = re.compile(r"class\s+(\w+)\s*:\s*DomainException\b")


def check_domain_exceptions_mapped() -> None:
    exceptions_dir = BOOKING / "Domain" / "Exceptions"
    middleware = BOOKING / "Middleware" / "ExceptionHandlingMiddleware.cs"
    if CHANGED is not None:
        if not any("Domain/Exceptions" in str(p) or "Domain\\Exceptions" in str(p) for p in CHANGED):
            return
    if not middleware.exists():
        add(BOOKING, 1, "нет ExceptionHandlingMiddleware.cs")
        return
    middleware_text = middleware.read_text(encoding="utf-8", errors="replace")
    for path in iter_cs(exceptions_dir):
        for i, line in enumerate(lines_of(path), 1):
            m = CLASS_DOMAIN_EX.search(line)
            if not m or m.group(1) == "DomainException":
                continue
            if m.group(1) not in middleware_text:
                add(path, i, f"{m.group(1)} не замаплен в MapDomainExceptionToCode")


# --- 5 ---
ROUTE_ATTR = re.compile(r"\[Route\(\s*\"([^\"]+)\"\s*\)\]")


def check_controller_routes() -> None:
    for path in iter_cs(BOOKING / "Controllers"):
        for i, line in enumerate(lines_of(path), 1):
            m = ROUTE_ATTR.search(line)
            if not m:
                continue
            route = m.group(1)
            if route == "health" or route.startswith("api/v1/"):
                continue
            add(path, i, f"Route должен быть api/v1/... или health, сейчас: {route}")


# --- 6 ---
ASYNC_METHOD = re.compile(
    r"public\s+async\s+Task(?:<[^>]+>)?\s+(\w+)\s*\(([^)]*)\)"
)


def check_cancellation_token() -> None:
    for base in (BOOKING / "Controllers", BOOKING / "Application" / "Services"):
        for path in iter_cs(base):
            content = path.read_text(encoding="utf-8", errors="replace")
            for m in ASYNC_METHOD.finditer(content):
                if "CancellationToken" not in m.group(2):
                    line_no = content[: m.start()].count("\n") + 1
                    add(path, line_no, f"async-метод {m.group(1)} должен принимать CancellationToken")


# --- 7 ---
ASSERT_RE = re.compile(r"\bAssert\.")


def check_tests_fluent() -> None:
    for path in iter_cs(TESTS):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if ASSERT_RE.search(line):
                add(path, i, "в тестах FluentAssertions (.Should()), не Assert.")


# --- packages ---
VERSION_IN_CSPROJ = re.compile(r"""<PackageReference\b[^>]*\bVersion\s*=""", re.I)


def check_central_package_versions() -> None:
    for path in ROOT.rglob("*.csproj"):
        if "bin" in path.parts or "obj" in path.parts:
            continue
        if CHANGED is not None and path.resolve() not in CHANGED:
            continue
        for i, line in enumerate(lines_of(path), 1):
            if VERSION_IN_CSPROJ.search(line):
                add(path, i, "версия пакета только в Directory.Packages.props")


# --- 8 ---
TEST_METHOD = re.compile(
    r"public\s+(?:async\s+)?(?:Task(?:<[^>]+>)?|void)\s+(\w+)\s*\("
)
VALID_UNIT_TEST_NAME = re.compile(
    r"^.+_.+_When.+$|^.+_Returns.+$|^.+_Returns.+_For.+$"
)


def check_unit_test_naming() -> None:
    for path in iter_cs(TESTS / "YachtSharing.UnitTests"):
        lines = lines_of(path)
        for i, line in enumerate(lines, 1):
            if not TEST_METHOD.search(line):
                continue
            window = "\n".join(lines[max(0, i - 4) : i])
            if "[Fact]" not in window and "[Theory]" not in window:
                continue
            name = TEST_METHOD.search(line).group(1)  # type: ignore[union-attr]
            if not VALID_UNIT_TEST_NAME.match(name):
                add(
                    path,
                    i,
                    f"имя unit-теста «{name}» — Method_Expected_When... или Method_Returns...",
                )


# --- 9 ---
DATETIME_NOW = re.compile(r"\bDateTime\.(UtcNow|Now)\b")


def check_no_datetime_now_in_application() -> None:
    for path in iter_cs(BOOKING / "Application"):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if DATETIME_NOW.search(line):
                add(path, i, "в Application — IDateTimeProvider, не DateTime.UtcNow/Now")


# --- 10 ---
PUBLIC_SETTER = re.compile(r"\{\s*get;\s*set;\s*\}")


def check_entity_private_setters() -> None:
    for path in iter_cs(BOOKING / "Domain" / "Entities"):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if PUBLIC_SETTER.search(line):
                add(path, i, "свойства сущностей — только private set")


# --- 11 ---
APP_USES_INFRA = re.compile(r"^using\s+YachtSharing\.BookingService\.Infrastructure\b")


def check_application_no_infrastructure() -> None:
    for path in iter_cs(BOOKING / "Application"):
        for i, line in enumerate(lines_of(path), 1):
            if APP_USES_INFRA.search(line.strip()):
                add(path, i, "Application не импортирует Infrastructure")


# --- 12 ---
CONSOLE_RE = re.compile(r"\bConsole\.(WriteLine|Write)\s*\(")


def check_no_console_in_booking() -> None:
    for path in iter_cs(BOOKING):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if CONSOLE_RE.search(line):
                add(path, i, "Console запрещён — ILogger")


# --- 13. sealed record DTOs ---
TYPE_DECL = re.compile(r"^public\s+(sealed\s+)?(record|class)\s+(\w+)")


def check_dtos_sealed_records() -> None:
    for path in iter_cs(BOOKING / "DTOs"):
        for i, line in enumerate(lines_of(path), 1):
            m = TYPE_DECL.match(line.strip())
            if not m:
                continue
            sealed, kind, name = m.group(1), m.group(2), m.group(3)
            if kind != "record" or not sealed:
                add(
                    path,
                    i,
                    f"DTO «{name}» должен быть «public sealed record», сейчас: {line.strip()}",
                )


# --- 14–15. new *Dto( только в Mappings ---
NEW_DTO = re.compile(r"\bnew\s+([A-Z]\w*Dto)\s*\(")


def check_no_dto_construction_outside_mappings() -> None:
    mappings = (BOOKING / "Application" / "Mappings").resolve()
    for folder, label in (
        (BOOKING / "Controllers", "Controller"),
        (BOOKING / "Application" / "Services", "Application/Services"),
    ):
        for path in iter_cs(folder):
            if path.resolve().is_relative_to(mappings):
                continue
            for i, line in enumerate(lines_of(path), 1):
                if line.strip().startswith("//"):
                    continue
                m = NEW_DTO.search(line)
                if m:
                    add(
                        path,
                        i,
                        f"{label}: не собирай {m.group(1)} через new — "
                        f"вынеси в Application/Mappings (*MappingExtensions)",
                    )


# --- 16. Controllers без Domain.Entities ---
ENTITIES_USING = re.compile(
    r"^using\s+YachtSharing\.BookingService\.Domain\.Entities\b"
)


def check_controllers_no_entities() -> None:
    for path in iter_cs(BOOKING / "Controllers"):
        for i, line in enumerate(lines_of(path), 1):
            if ENTITIES_USING.search(line.strip()):
                add(
                    path,
                    i,
                    "Controller не импортирует Domain.Entities — только DTOs и UseCases",
                )


# --- 17. Status = BookingStatus только в Domain/Entities ---
STATUS_ASSIGN = re.compile(r"\.Status\s*=\s*BookingStatus\b")


def check_status_via_domain_methods() -> None:
    entities = (BOOKING / "Domain" / "Entities").resolve()
    for path in iter_cs(BOOKING):
        if path.resolve().is_relative_to(entities):
            continue
        if "Migrations" in path.parts:
            continue
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if STATUS_ASSIGN.search(line):
                add(
                    path,
                    i,
                    "не присваивай Booking.Status снаружи Domain — "
                    "Confirm()/Expire()/Cancel() и т.п.",
                )


# --- 18. IReadOnlyList в репозиториях ---
BAD_REPO_RETURN = re.compile(
    r"Task\s*<\s*(List|IList|ICollection|IEnumerable)\s*<"
)


def check_repo_ireadonlylist() -> None:
    for path in iter_cs(BOOKING / "Application" / "Abstractions" / "Repositories"):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if BAD_REPO_RETURN.search(line):
                add(
                    path,
                    i,
                    "коллекции в репозитории — Task<IReadOnlyList<T>>, не List/IEnumerable",
                )


# --- 19. Controllers не catch KeyNotFound/DomainException ---
CATCH_HANDLED = re.compile(
    r"catch\s*\(\s*(KeyNotFoundException|DomainException|"
    r"InvalidBookingStatusTransitionException)\b"
)


def check_controllers_no_exception_catch() -> None:
    # В легаси Bookings/Payments уже есть try/catch; на полном дереве это шум.
    # Ловим только в режиме --base (файлы, которые трогал агент).
    if CHANGED is None:
        return
    for path in iter_cs(BOOKING / "Controllers"):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if CATCH_HANDLED.search(line):
                add(
                    path,
                    i,
                    "Controller не ловит KeyNotFoundException/DomainException — "
                    "это ExceptionHandlingMiddleware",
                )


def configure_root(root: Path) -> None:
    global ROOT, BOOKING, TESTS
    ROOT = root.resolve()
    BOOKING = ROOT / "src" / "YachtSharing.BookingService"
    TESTS = ROOT / "tests"


def parse_args(argv: list[str]) -> tuple[Path | None, str | None]:
    root: Path | None = None
    base: str | None = None
    i = 0
    while i < len(argv):
        if argv[i] == "--base":
            if i + 1 >= len(argv):
                raise SystemExit("--base нужен commit/tag")
            base = argv[i + 1]
            i += 2
            continue
        if argv[i].startswith("-"):
            raise SystemExit(f"неизвестный аргумент: {argv[i]}")
        root = Path(argv[i])
        i += 1
    return root, base


def main() -> int:
    global CHANGED
    root_arg, base_ref = parse_args(sys.argv[1:])
    if root_arg is not None:
        configure_root(root_arg)

    if base_ref is not None:
        CHANGED = load_changed_since(base_ref)
        print(f"(режим --base {base_ref}: .cs в diff: {len(CHANGED)})")
        if not CHANGED:
            print("нарушений: 0")
            return 0

    check_domain_isolation()
    check_controllers_no_db()
    check_controllers_depend_on_abstractions()
    check_domain_exceptions_mapped()
    check_controller_routes()
    check_cancellation_token()
    check_tests_fluent()
    check_central_package_versions()
    check_unit_test_naming()
    check_no_datetime_now_in_application()
    check_entity_private_setters()
    check_application_no_infrastructure()
    check_no_console_in_booking()
    check_dtos_sealed_records()
    check_no_dto_construction_outside_mappings()
    check_controllers_no_entities()
    check_status_via_domain_methods()
    check_repo_ireadonlylist()
    check_controllers_no_exception_catch()

    n = len(violations)
    print(f"нарушений: {n}")
    for v in violations:
        print(f"  - {v}")
    return 1 if n else 0


if __name__ == "__main__":
    sys.exit(main())
