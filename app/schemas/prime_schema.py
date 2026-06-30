from pydantic import BaseModel, ConfigDict, Field, field_validator


class PrimeRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    requested_range_start: int = Field(default=100, ge=100, le=200)
    requested_range_end: int = Field(default=200, ge=100, le=200)

    @field_validator("requested_range_end")
    @classmethod
    def validate_range_order(cls, value: int, info: object) -> int:
        data = getattr(info, "data", {})
        start = data.get("requested_range_start")
        if start is not None and value < start:
            raise ValueError("requested_range_end must be greater than or equal to requested_range_start")
        return value


class PrimeResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    prime: int
