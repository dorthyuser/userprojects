from dataclasses import dataclass, field
from typing import Any


@dataclass(slots=True)
class ExtractResult:
    website: dict[str, Any]
    business: dict[str, Any]
    contact: dict[str, Any]
    socialMedia: dict[str, Any]
    navigation: list[str] = field(default_factory=list)
    services: list[Any] = field(default_factory=list)
    products: list[Any] = field(default_factory=list)
    team: list[Any] = field(default_factory=list)
    testimonials: list[Any] = field(default_factory=list)
    faqs: list[Any] = field(default_factory=list)
    blogs: list[Any] = field(default_factory=list)
    pricing: list[Any] = field(default_factory=list)
    forms: list[Any] = field(default_factory=list)
    images: list[Any] = field(default_factory=list)
    videos: list[Any] = field(default_factory=list)
    technologies: list[Any] = field(default_factory=list)
    pages: list[Any] = field(default_factory=list)
