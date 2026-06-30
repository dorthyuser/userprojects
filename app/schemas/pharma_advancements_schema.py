from pydantic import BaseModel, ConfigDict, Field, field_validator


class PharmaAdvancementItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

    name: str = Field(..., min_length=1, max_length=100)

    @field_validator("name")
    @classmethod
    def validate_name_length(cls, value: str) -> str:
        if len(value) > 100:
            raise ValueError("name must be at most 100 characters")
        return value


class PharmaAdvancementsResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    items: list[PharmaAdvancementItem]
