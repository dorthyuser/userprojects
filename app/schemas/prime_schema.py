from pydantic import BaseModel, ConfigDict, Field, field_validator


class PrimeNumberResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    prime_number: int = Field(..., ge=100, le=200)

    @field_validator("prime_number")
    @classmethod
    def validate_prime_number(cls, value: int) -> int:
        if value < 100 or value > 200:
            raise ValueError("prime_number must be between 100 and 200")
        if value not in {101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199}:
            raise ValueError("prime_number must be prime")
        return value
