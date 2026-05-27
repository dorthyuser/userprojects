from __future__ import annotations

from pydantic import BaseModel, Field, field_validator, model_validator


class PrimeNumbersResponse(BaseModel):
    start: int = Field(..., ge=2)
    end: int = Field(..., ge=2)
    primes: list[int] = Field(default_factory=list)

    @field_validator("primes")
    @classmethod
    def validate_primes(cls, value: list[int]) -> list[int]:
        if not value:
            return value
        if any(number < 2 for number in value):
            raise ValueError("prime numbers must be greater than or equal to 2")
        return value

    @model_validator(mode="after")
    def validate_range(self) -> "PrimeNumbersResponse":
        if self.end < self.start:
            raise ValueError("end must be greater than or equal to start")
        return self
