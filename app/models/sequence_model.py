from dataclasses import dataclass


@dataclass(slots=True)
class SequenceResult:
    start: int
    end: int
    sequence: list[int]
    count: int
