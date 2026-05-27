from pydantic import BaseModel, Field, model_validator


class PrimeNumbersResponse(BaseModel):
    start: int = Field(ge=0)
    end: int = Field(ge=0)
    primes: list[int]

    @model_validator(mode="after")
    def validate_range(self) -> "PrimeNumbersResponse":
        if self.start > self.end:
            raise ValueError("start must be less than or equal to end")
        return self
