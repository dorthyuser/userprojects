from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class PrimeNumber:
    value: int
