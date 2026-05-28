from typing import Any

from pydantic import AnyUrl, BaseModel, ConfigDict, Field, field_validator


class WebsiteSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    url: AnyUrl
    title: str | None = None
    metaTitle: str | None = None
    metaDescription: str | None = None


class BusinessSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    name: str | None = None
    description: str | None = None
    industry: str | None = None
    founded: str | None = None
    employees: str | None = None


class ContactSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    emails: list[str] = Field(default_factory=list)
    phones: list[str] = Field(default_factory=list)
    addresses: list[str] = Field(default_factory=list)


class SocialMediaSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    facebook: str | None = None
    instagram: str | None = None
    linkedin: str | None = None
    twitter: str | None = None
    youtube: str | None = None


class ExtractRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    url: AnyUrl


class ExtractResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    website: WebsiteSchema
    business: BusinessSchema
    contact: ContactSchema
    socialMedia: SocialMediaSchema
    navigation: list[str]
    services: list[Any]
    products: list[Any]
    team: list[Any]
    testimonials: list[Any]
    faqs: list[Any]
    blogs: list[Any]
    pricing: list[Any]
    forms: list[Any]
    images: list[Any]
    videos: list[Any]
    technologies: list[Any]
    pages: list[Any]
