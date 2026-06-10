from __future__ import annotations

from datetime import datetime, timezone
from typing import Dict

from models.lifetime_models import LifetimeResponse


def parse_date_of_birth(date_of_birth: str) -> datetime:
    try:
        parsed = datetime.fromisoformat(date_of_birth.replace("Z", "+00:00"))
    except ValueError as exc:
        raise ValueError("dateOfBirth must be a valid ISO 8601 datetime string.") from exc

    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


def calculate_lifetime_stats(date_of_birth: datetime, current_date: datetime) -> Dict[str, object]:
    if current_date < date_of_birth:
        raise ValueError("dateOfBirth cannot be in the future.")

    delta = current_date - date_of_birth
    total_seconds = int(delta.total_seconds())
    total_minutes = total_seconds // 60
    total_hours = total_minutes // 60
    total_days = delta.days
    total_weeks = total_days // 7

    years = current_date.year - date_of_birth.year
    months = current_date.month - date_of_birth.month
    days = current_date.day - date_of_birth.day
    hours = current_date.hour - date_of_birth.hour
    minutes = current_date.minute - date_of_birth.minute
    seconds = current_date.second - date_of_birth.second

    if seconds < 0:
        seconds += 60
        minutes -= 1
    if minutes < 0:
        minutes += 60
        hours -= 1
    if hours < 0:
        hours += 24
        days -= 1
    if days < 0:
        previous_month = current_date.month - 1 or 12
        previous_year = current_date.year if current_date.month > 1 else current_date.year - 1
        days_in_previous_month = (datetime(previous_year, previous_month % 12 + 1, 1, tzinfo=timezone.utc) - datetime(previous_year, previous_month, 1, tzinfo=timezone.utc)).days
        days += days_in_previous_month
        months -= 1
    if months < 0:
        months += 12
        years -= 1

    response = LifetimeResponse(
        dateOfBirth=date_of_birth.isoformat(),
        currentDate=current_date.isoformat(),
        age={
            "years": years,
            "months": months,
            "weeks": total_weeks,
            "days": total_days,
            "hours": total_hours,
            "minutes": total_minutes,
            "seconds": total_seconds,
        },
        lifetimeStats={
            "totalYearsLived": total_seconds / (365.2425 * 24 * 3600),
            "totalMonthsLived": total_seconds / (30.436875 * 24 * 3600),
            "totalWeeksLived": total_weeks,
            "totalDaysLived": total_days,
            "totalHoursLived": total_hours,
            "totalMinutesLived": total_minutes,
            "totalSecondsLived": total_seconds,
        },
    )
    return response.model_dump()
