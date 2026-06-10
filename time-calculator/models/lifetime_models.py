from __future__ import annotations

from typing import Dict

from pydantic import BaseModel, Field


class LifetimeResponse(BaseModel):
    dateOfBirth: str = Field(...)
    currentDate: str = Field(...)
    age: Dict[str, int] = Field(...)
    lifetimeStats: Dict[str, float | int] = Field(...)
