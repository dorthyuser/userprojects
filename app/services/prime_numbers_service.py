from app.models.prime_numbers_model import PrimeNumberRange


def generate_prime_numbers(start: int, end: int) -> list[int]:
    prime_range = PrimeNumberRange(start=start, end=end)
    primes: list[int] = []

    for number in range(prime_range.start, prime_range.end + 1):
        if number < 2:
            continue

        is_prime = True
        for divisor in range(2, int(number ** 0.5) + 1):
            if number % divisor == 0:
                is_prime = False
                break

        if is_prime:
            primes.append(number)

    return primes
