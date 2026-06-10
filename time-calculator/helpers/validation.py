from datetime import datetime, timezone
from typing import Any, Dict


def parse_date_of_birth(value: str) -> datetime:
    normalized = value.strip().replace("Z", "+00:00")
    try:
        parsed = datetime.fromisoformat(normalized)
    except ValueError as exc:
        raise ValueError("dateOfBirth must be a valid ISO 8601 datetime string.") from exc

    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


def build_error_response(message: str, code: str) -> Dict[str, Any]:
    return {"error": {"code": code, "message": message}}