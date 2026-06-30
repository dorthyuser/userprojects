from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class PrimeNumberRecord:
    prime_number: int
