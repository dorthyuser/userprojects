import json
import logging

from app.models.pharma_advancements_model import PharmaAdvancement
from app.schemas.pharma_advancements_schema import PharmaAdvancementsResponse, PharmaAdvancementItem

logger = logging.getLogger(__name__)


def get_pharma_advancements() -> PharmaAdvancementsResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "read", "resource": "pharma_advancements"}))
    advancements = [
        PharmaAdvancement(name="mRNA vaccine platforms"),
        PharmaAdvancement(name="CRISPR-based gene therapies"),
        PharmaAdvancement(name="GLP-1 receptor agonists"),
        PharmaAdvancement(name="ADC targeted oncology drugs"),
        PharmaAdvancement(name="AI-assisted drug discovery"),
    ]
    items = [PharmaAdvancementItem(name=item.name) for item in advancements]
    return PharmaAdvancementsResponse(items=items)
