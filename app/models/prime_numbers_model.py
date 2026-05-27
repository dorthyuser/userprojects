from dataclasses import dataclass


@dataclass(frozen=True)
class PrimeNumbersResult:
    start: int
    end: int
    primes: list[int]
