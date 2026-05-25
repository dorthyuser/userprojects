from dataclasses import dataclass


@dataclass(frozen=True)
class PrimeNumberRange:
    start: int
    end: int
