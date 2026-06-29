from pydantic import BaseModel, ConfigDict, Field


class PrimeResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    prime: int = Field(..., ge=100, le=200)
