from __future__ import annotations

from app.models.prime_number_model import PrimeNumberRange


def generate_prime_numbers(start: int, end: int) -> list[int]:
    prime_range = PrimeNumberRange(start=start, end=end)
    return [number for number in range(prime_range.start, prime_range.end + 1) if _is_prime(number)]


def _is_prime(number: int) -> bool:
    if number < 2:
        return False
    if number == 2:
        return True
    if number % 2 == 0:
        return False
    limit = int(number ** 0.5) + 1
    for factor in range(3, limit, 2):
        if number % factor == 0:
            return False
    return True
