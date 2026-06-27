from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator


class SequenceRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    start: int = Field(..., description="Inclusive start bound")
    end: int = Field(..., description="Inclusive end bound")

    @model_validator(mode="after")
    def validate_bounds(self) -> "SequenceRequest":
        if self.start < -10 or self.end > 50:
            raise ValueError("Validation Error")
        if self.start > self.end:
            raise ValueError("Validation Error")
        return self

    @field_validator("start", "end")
    @classmethod
    def validate_integers(cls, value: int) -> int:
        if not isinstance(value, int):
            raise ValueError("Validation Error")
        return value


class SequenceResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    start: int
    end: int
    values: list[int]
