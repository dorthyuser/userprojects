from pydantic import BaseModel, ConfigDict, Field, model_validator


class SequenceRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    start: int = Field(..., description="Inclusive start of the range")
    end: int = Field(..., description="Inclusive end of the range")

    @model_validator(mode="after")
    def validate_range(self) -> "SequenceRequest":
        if self.start < -10 or self.end > 50:
            raise ValueError("Validation Error")
        if self.start > self.end:
            raise ValueError("Validation Error")
        return self


class SequenceResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    start: int
    end: int
    sequence: list[int]
    count: int
