from pydantic import BaseModel, Field


class AgeModel(BaseModel):
    years: int = Field(..., ge=0)
    months: int = Field(..., ge=0)
    weeks: int = Field(..., ge=0)
    days: int = Field(..., ge=0)
    hours: int = Field(..., ge=0)
    minutes: int = Field(..., ge=0)
    seconds: int = Field(..., ge=0)


class LifetimeStatsModel(BaseModel):
    totalYearsLived: float = Field(..., ge=0)
    totalMonthsLived: float = Field(..., ge=0)
    totalWeeksLived: float = Field(..., ge=0)
    totalDaysLived: float = Field(..., ge=0)
    totalHoursLived: float = Field(..., ge=0)
    totalMinutesLived: float = Field(..., ge=0)
    totalSecondsLived: float = Field(..., ge=0)


class LifetimeResponse(BaseModel):
    dateOfBirth: str
    currentDate: str
    age: AgeModel
    lifetimeStats: LifetimeStatsModel