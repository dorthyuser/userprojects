from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class PrimeNumberRange:
    start: int
    end: int
