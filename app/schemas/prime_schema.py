from pydantic import BaseModel, ConfigDict, Field, field_validator


class PrimeResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    prime: int = Field(..., ge=100, le=200)

    @field_validator("prime")
    @classmethod
    def validate_prime(cls, value: int) -> int:
        if value < 100 or value > 200:
            raise ValueError("prime must be between 100 and 200")
        if value < 2:
            raise ValueError("prime must be a prime number")
        for divisor in range(2, int(value ** 0.5) + 1):
            if value % divisor == 0:
                raise ValueError("prime must be a prime number")
        return value
