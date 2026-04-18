from pydantic import BaseModel, Field
from typing import Optional
from enum import Enum

class Aksiyon(str, Enum):
    AL = "AL"
    SAT = "SAT"
    BEKLE = "BEKLE"

class PazarItemi(BaseModel):
    item_adi: str = Field(..., min_length=1, max_length=128)
    fiyat: int = Field(..., gt=0)
    pazar_id: int = Field(..., gt=0)
    satici_id: Optional[int] = Field(default=None)
    miktar: Optional[int] = Field(default=1, gt=0)

class TradeSignal(BaseModel):
    aksiyon: Aksiyon
    pazar_id: int
    kar_marji: int = 0
    hedef_satis_fiyati: int = 0
    guven_skoru: float = 0.0
    sebep: str = ""

class BotDurum(BaseModel):
    aktif: bool
    toplam_islem: int
    toplam_kar: int
    son_guncelleme: str

class ManuelFiyat(BaseModel):
    item_adi: str = Field(..., min_length=1)
    ortalama_fiyat: int = Field(..., gt=0)