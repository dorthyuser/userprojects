from pydantic import BaseModel, Field, field_validator, model_validator


class PrimeNumbersQuery(BaseModel):
    start: int = Field(default=50, ge=2)
    end: int = Field(default=100, ge=2)

    @field_validator("start", "end")
    @classmethod
    def validate_bounds(cls, value: int) -> int:
        return value

    @model_validator(mode="after")
    def validate_range(self) -> "PrimeNumbersQuery":
        if self.start > self.end:
            raise ValueError("start must be less than or equal to end")
        return self


class PrimeNumbersResponse(BaseModel):
    primes: list[int]
