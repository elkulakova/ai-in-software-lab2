#!/usr/bin/env python3
"""
Проверка конвенций Yacht Sharing Pro (BookingService + общие).

Запуск из папки yacht-project:
  python3 check_conventions.py

Печатает «нарушений: N» и список. Exit 0 если N=0, иначе 1.

Конвенции (выведены из текущего кода, не «best practices»):
  1. Domain не импортирует Infrastructure / Controllers / Application / ASP.NET / EF
  2. Controllers не трогают DbContext / EF / Infrastructure.Persistence
  3. Controllers зависят от Application.Abstractions (не от Infrastructure.*)
  4. Каждый конкретный DomainException замаплен в ExceptionHandlingMiddleware
  5. Route контроллеров Booking: api/v1/... или health
  6. Публичные async-методы Controllers и Application/Services принимают CancellationToken
  7. В tests/ ассерты через FluentAssertions (.Should), не Assert.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
BOOKING = ROOT / "src" / "YachtSharing.BookingService"
TESTS = ROOT / "tests"

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
        yield p


def lines_of(path: Path) -> list[str]:
    return path.read_text(encoding="utf-8", errors="replace").splitlines()


# ---------------------------------------------------------------------------
# 1. Domain не зависит от внешних слоёв
# ---------------------------------------------------------------------------
FORBIDDEN_DOMAIN_USING = re.compile(
    r"^using\s+("
    r"YachtSharing\.BookingService\.(Infrastructure|Controllers|Application|Middleware|DTOs)"
    r"|Microsoft\.AspNetCore"
    r"|Microsoft\.EntityFrameworkCore"
    r")\b"
)


def check_domain_isolation() -> None:
    domain = BOOKING / "Domain"
    for path in iter_cs(domain):
        for i, line in enumerate(lines_of(path), 1):
            if FORBIDDEN_DOMAIN_USING.search(line.strip()):
                add(path, i, f"Domain не должен импортировать внешние слои: {line.strip()}")


# ---------------------------------------------------------------------------
# 2. Controllers не ходят в БД напрямую
# ---------------------------------------------------------------------------
DB_TOUCH = re.compile(
    r"\b(BookingDbContext|DbContext|EntityFrameworkCore|Infrastructure\.Persistence)\b"
)


def check_controllers_no_db() -> None:
    controllers = BOOKING / "Controllers"
    for path in iter_cs(controllers):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if DB_TOUCH.search(line):
                add(path, i, "Controller не должен обращаться к DbContext/EF/Persistence")


# ---------------------------------------------------------------------------
# 3. Controllers зависят от Application.Abstractions, не от Infrastructure
# ---------------------------------------------------------------------------
USING_INFRA = re.compile(r"^using\s+YachtSharing\.BookingService\.Infrastructure\b")
FIELD_OR_PARAM_INFRA = re.compile(
    r"\bYachtSharing\.BookingService\.Infrastructure\.[A-Za-z0-9_.]+"
)
# конкретные репозитории/контекст в полях контроллера
CONCRETE_DEP = re.compile(
    r"private\s+readonly\s+(BookingDbContext|IBookingRepository|IClientRepository|"
    r"IPaymentRepository|ICrewRepository|IUnitOfWork)\b"
)


def check_controllers_depend_on_abstractions() -> None:
    controllers = BOOKING / "Controllers"
    for path in iter_cs(controllers):
        text_lines = lines_of(path)
        for i, line in enumerate(text_lines, 1):
            stripped = line.strip()
            if stripped.startswith("//"):
                continue
            if USING_INFRA.search(stripped) or FIELD_OR_PARAM_INFRA.search(stripped):
                add(path, i, "Controller не должен зависеть от Infrastructure — только Application.Abstractions")
            if CONCRETE_DEP.search(stripped):
                add(
                    path,
                    i,
                    "Controller должен вызывать UseCase (IBookingService и т.п.), не репозиторий/UoW напрямую",
                )


# ---------------------------------------------------------------------------
# 4. DomainException → MapDomainExceptionToCode
# ---------------------------------------------------------------------------
CLASS_DOMAIN_EX = re.compile(
    r"class\s+(\w+)\s*:\s*DomainException\b"
)


def check_domain_exceptions_mapped() -> None:
    exceptions_dir = BOOKING / "Domain" / "Exceptions"
    middleware = BOOKING / "Middleware" / "ExceptionHandlingMiddleware.cs"
    if not middleware.exists():
        add(BOOKING, 1, "нет ExceptionHandlingMiddleware.cs — некуда мапить DomainException")
        return

    middleware_text = middleware.read_text(encoding="utf-8", errors="replace")

    for path in iter_cs(exceptions_dir):
        for i, line in enumerate(lines_of(path), 1):
            m = CLASS_DOMAIN_EX.search(line)
            if not m:
                continue
            name = m.group(1)
            if name == "DomainException":
                continue
            if name not in middleware_text:
                add(
                    path,
                    i,
                    f"{name} не замаплен в ExceptionHandlingMiddleware.MapDomainExceptionToCode",
                )


# ---------------------------------------------------------------------------
# 5. Route: api/v1/... или health
# ---------------------------------------------------------------------------
ROUTE_ATTR = re.compile(r"\[Route\(\s*\"([^\"]+)\"\s*\)\]")


def check_controller_routes() -> None:
    controllers = BOOKING / "Controllers"
    for path in iter_cs(controllers):
        for i, line in enumerate(lines_of(path), 1):
            m = ROUTE_ATTR.search(line)
            if not m:
                continue
            route = m.group(1)
            if route == "health" or route.startswith("api/v1/"):
                continue
            add(path, i, f"Route должен быть api/v1/... или health, сейчас: {route}")


# ---------------------------------------------------------------------------
# 6. CancellationToken у публичных async-методов
# ---------------------------------------------------------------------------
# упрощённо: строка с public async Task... и следующая/та же без CancellationToken
ASYNC_METHOD = re.compile(
    r"public\s+async\s+Task(?:<[^>]+>)?\s+(\w+)\s*\(([^)]*)\)"
)


def check_cancellation_token() -> None:
    targets = [
        BOOKING / "Controllers",
        BOOKING / "Application" / "Services",
    ]
    for base in targets:
        for path in iter_cs(base):
            content = path.read_text(encoding="utf-8", errors="replace")
            # склеиваем переносы внутри сигнатур грубо: ищем public async Task ... )
            for m in ASYNC_METHOD.finditer(content):
                name = m.group(1)
                params = m.group(2)
                # позиция для номера строки — начало матча
                line_no = content[: m.start()].count("\n") + 1
                if "CancellationToken" not in params:
                    add(
                        path,
                        line_no,
                        f"async-метод {name} должен принимать CancellationToken",
                    )


# ---------------------------------------------------------------------------
# 7. Тесты: FluentAssertions, не Assert.
# ---------------------------------------------------------------------------
ASSERT_RE = re.compile(r"\bAssert\.")


def check_tests_fluent() -> None:
    for path in iter_cs(TESTS):
        for i, line in enumerate(lines_of(path), 1):
            if line.strip().startswith("//"):
                continue
            if ASSERT_RE.search(line):
                add(path, i, "в тестах используй FluentAssertions (.Should()), не Assert.")


# ---------------------------------------------------------------------------
# бонус (простой): версии пакетов только в Directory.Packages.props
# ---------------------------------------------------------------------------
VERSION_IN_CSPROJ = re.compile(
    r"""<PackageReference\b[^>]*\bVersion\s*=""",
    re.IGNORECASE,
)


def check_central_package_versions() -> None:
    for path in ROOT.rglob("*.csproj"):
        if "bin" in path.parts or "obj" in path.parts:
            continue
        for i, line in enumerate(lines_of(path), 1):
            if VERSION_IN_CSPROJ.search(line):
                add(path, i, "версия пакета только в Directory.Packages.props, не в csproj")


def main() -> int:
    check_domain_isolation()
    check_controllers_no_db()
    check_controllers_depend_on_abstractions()
    check_domain_exceptions_mapped()
    check_controller_routes()
    check_cancellation_token()
    check_tests_fluent()
    check_central_package_versions()

    n = len(violations)
    print(f"нарушений: {n}")
    for v in violations:
        print(f"  - {v}")
    return 1 if n else 0


if __name__ == "__main__":
    sys.exit(main())
