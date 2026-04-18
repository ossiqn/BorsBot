import os
import sys
from contextlib import asynccontextmanager
from fastapi import FastAPI, Depends, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from sqlalchemy.orm import Session
from loguru import logger
from dotenv import load_dotenv

from models import PazarItemi, TradeSignal, BotDurum, ManuelFiyat
from database import init_db, get_db, IslemGecmisi, PiyasaOrtalamalari, ManuelFiyatTablosu
from analyzer import analiz_et, toplam_kar_hesapla

load_dotenv()

logger.remove()
logger.add(
    sys.stdout,
    format="<green>{time:HH:mm:ss}</green> | <level>{level}</level> | {message}",
    level="INFO"
)
logger.add(
    "logs/borsa_{time:YYYY-MM-DD}.log",
    rotation="00:00",
    retention="7 days",
    level="DEBUG"
)


@asynccontextmanager
async def lifespan(app: FastAPI):
    os.makedirs("logs", exist_ok=True)
    init_db()
    logger.info("BorsaBot Zeka Motoru baslatildi.")
    yield
    logger.info("BorsaBot kapatildi.")


app = FastAPI(
    title="BorsaBot Zeka Motoru",
    version="2.0.0",
    lifespan=lifespan
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"]
)

islem_sayaci = {"toplam": 0, "al": 0, "sat": 0}


@app.post("/analiz_et", response_model=TradeSignal)
def analiz_endpoint(item: PazarItemi, db: Session = Depends(get_db)):
    try:
        sinyal = analiz_et(db, item.item_adi, item.fiyat, item.pazar_id, item.satici_id)
        islem_sayaci["toplam"] += 1
        if sinyal.aksiyon.value == "AL":
            islem_sayaci["al"] += 1
        elif sinyal.aksiyon.value == "SAT":
            islem_sayaci["sat"] += 1
        return sinyal
    except Exception as e:
        logger.error(f"Analiz hatasi: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/durum", response_model=BotDurum)
def durum_endpoint(db: Session = Depends(get_db)):
    from datetime import datetime
    return BotDurum(
        aktif=True,
        toplam_islem=islem_sayaci["toplam"],
        toplam_kar=toplam_kar_hesapla(db),
        son_guncelleme=datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")
    )


@app.get("/piyasa")
def piyasa_listesi(db: Session = Depends(get_db)):
    kayitlar = db.query(PiyasaOrtalamalari).all()
    return [
        {
            "item_adi": k.item_adi,
            "ortalama_fiyat": k.ortalama_fiyat,
            "medyan_fiyat": k.medyan_fiyat,
            "min_fiyat": k.min_fiyat,
            "max_fiyat": k.max_fiyat,
            "ornek_sayisi": k.ornek_sayisi,
            "guncelleme": k.guncelleme_tarihi.strftime("%Y-%m-%d %H:%M:%S") if k.guncelleme_tarihi else ""
        }
        for k in kayitlar
    ]


@app.post("/manuel_fiyat")
def manuel_fiyat_ekle(veri: ManuelFiyat, db: Session = Depends(get_db)):
    mevcut = db.query(ManuelFiyatTablosu).filter_by(item_adi=veri.item_adi).first()
    if mevcut:
        mevcut.ortalama_fiyat = veri.ortalama_fiyat
    else:
        db.add(ManuelFiyatTablosu(item_adi=veri.item_adi, ortalama_fiyat=veri.ortalama_fiyat))
    db.commit()
    return {"mesaj": f"{veri.item_adi} manuel fiyati guncellendi."}


@app.delete("/manuel_fiyat/{item_adi}")
def manuel_fiyat_sil(item_adi: str, db: Session = Depends(get_db)):
    kayit = db.query(ManuelFiyatTablosu).filter_by(item_adi=item_adi).first()
    if not kayit:
        raise HTTPException(status_code=404, detail="Item bulunamadi.")
    db.delete(kayit)
    db.commit()
    return {"mesaj": f"{item_adi} manuel fiyati silindi."}


@app.get("/saglik")
def saglik():
    return {"durum": "aktif", "versiyon": "2.0.0"}