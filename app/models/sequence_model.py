from dataclasses import dataclass


@dataclass(slots=True)
class SequenceResult:
    start: int
    end: int
    values: list[int]
