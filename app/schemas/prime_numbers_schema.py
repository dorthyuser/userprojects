from pydantic import BaseModel, ConfigDict, Field, model_validator


class PrimeNumbersResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    start: int = Field(ge=2)
    end: int = Field(ge=2)
    primes: list[int]

    @model_validator(mode="after")
    def validate_range(self) -> "PrimeNumbersResponse":
        if self.start > self.end:
            raise ValueError("start must be less than or equal to end")
        return self
