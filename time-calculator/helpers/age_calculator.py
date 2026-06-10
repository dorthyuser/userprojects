from datetime import datetime, timezone
from typing import Any, Dict


def calculate_lifetime_stats(date_of_birth: datetime, current_date: datetime) -> Dict[str, Any]:
    if date_of_birth.tzinfo is None:
        date_of_birth = date_of_birth.replace(tzinfo=timezone.utc)
    if current_date.tzinfo is None:
        current_date = current_date.replace(tzinfo=timezone.utc)

    if date_of_birth > current_date:
        raise ValueError("dateOfBirth cannot be in the future.")

    delta = current_date - date_of_birth
    total_seconds = int(delta.total_seconds())
    total_minutes = total_seconds // 60
    total_hours = total_minutes // 60
    total_days = delta.days
    total_weeks = total_days // 7
    total_months = total_days // 30
    total_years = total_days // 365

    age_years = total_years
    age_months = total_months % 12
    age_weeks = total_weeks
    age_days = total_days
    age_hours = total_hours
    age_minutes = total_minutes
    age_seconds = total_seconds

    return {
        "age": {
            "years": age_years,
            "months": age_months,
            "weeks": age_weeks,
            "days": age_days,
            "hours": age_hours,
            "minutes": age_minutes,
            "seconds": age_seconds,
        },
        "lifetimeStats": {
            "totalYearsLived": round(delta.total_seconds() / (365.2425 * 24 * 3600), 6),
            "totalMonthsLived": round(delta.total_seconds() / (30.436875 * 24 * 3600), 6),
            "totalWeeksLived": round(delta.total_seconds() / (7 * 24 * 3600), 6),
            "totalDaysLived": round(delta.total_seconds() / (24 * 3600), 6),
            "totalHoursLived": round(delta.total_seconds() / 3600, 6),
            "totalMinutesLived": round(delta.total_seconds() / 60, 6),
            "totalSecondsLived": round(delta.total_seconds(), 6),
        },
    }