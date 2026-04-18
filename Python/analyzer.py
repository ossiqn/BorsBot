import numpy as np
import pandas as pd
from sqlalchemy.orm import Session
from datetime import datetime, timedelta
from loguru import logger
from database import FiyatGecmisi, PiyasaOrtalamalari, IslemGecmisi, ManuelFiyatTablosu
from models import TradeSignal, Aksiyon

UCUZ_ESIK = 0.78
PAHALI_ESIK = 1.15
SATIS_MARJI = 1.07
MIN_ORNEK_SAYISI = 5
GECMIS_GUN = 7
GUVEN_ESIK = 0.60


def fiyat_kaydet(db: Session, item_adi: str, fiyat: int, pazar_id: int, satici_id: int = None):
    kayit = FiyatGecmisi(
        item_adi=item_adi,
        fiyat=fiyat,
        pazar_id=pazar_id,
        satici_id=satici_id
    )
    db.add(kayit)
    db.commit()


def istatistik_hesapla(db: Session, item_adi: str) -> dict | None:
    sinir = datetime.utcnow() - timedelta(days=GECMIS_GUN)

    manuel = db.query(ManuelFiyatTablosu).filter_by(item_adi=item_adi).first()
    if manuel:
        return {
            "ortalama": float(manuel.ortalama_fiyat),
            "medyan": float(manuel.ortalama_fiyat),
            "std": 0.0,
            "min": manuel.ortalama_fiyat,
            "max": manuel.ortalama_fiyat,
            "sayi": 1,
            "guven": 1.0
        }

    kayitlar = db.query(FiyatGecmisi.fiyat).filter(
        FiyatGecmisi.item_adi == item_adi,
        FiyatGecmisi.tarih >= sinir
    ).all()

    if not kayitlar or len(kayitlar) < MIN_ORNEK_SAYISI:
        return None

    df = pd.DataFrame(kayitlar, columns=["fiyat"])
    fiyatlar = df["fiyat"]

    q1 = fiyatlar.quantile(0.25)
    q3 = fiyatlar.quantile(0.75)
    iqr = q3 - q1
    temiz = fiyatlar[(fiyatlar >= q1 - 1.5 * iqr) & (fiyatlar <= q3 + 1.5 * iqr)]

    if len(temiz) < MIN_ORNEK_SAYISI:
        temiz = fiyatlar

    sayi = len(temiz)
    guven = min(1.0, sayi / 50.0)

    return {
        "ortalama": float(temiz.mean()),
        "medyan": float(temiz.median()),
        "std": float(temiz.std()) if sayi > 1 else 0.0,
        "min": int(temiz.min()),
        "max": int(temiz.max()),
        "sayi": sayi,
        "guven": round(guven, 4)
    }


def ortalama_guncelle(db: Session, item_adi: str, istat: dict):
    mevcut = db.query(PiyasaOrtalamalari).filter_by(item_adi=item_adi).first()
    if mevcut:
        mevcut.ortalama_fiyat = istat["ortalama"]
        mevcut.medyan_fiyat = istat["medyan"]
        mevcut.std_sapma = istat["std"]
        mevcut.min_fiyat = istat["min"]
        mevcut.max_fiyat = istat["max"]
        mevcut.ornek_sayisi = istat["sayi"]
        mevcut.guncelleme_tarihi = datetime.utcnow()
    else:
        yeni = PiyasaOrtalamalari(
            item_adi=item_adi,
            ortalama_fiyat=istat["ortalama"],
            medyan_fiyat=istat["medyan"],
            std_sapma=istat["std"],
            min_fiyat=istat["min"],
            max_fiyat=istat["max"],
            ornek_sayisi=istat["sayi"]
        )
        db.add(yeni)
    db.commit()


def islem_kaydet(db: Session, item_adi: str, alis_fiyati: int):
    kayit = IslemGecmisi(item_adi=item_adi, alis_fiyati=alis_fiyati)
    db.add(kayit)
    db.commit()
    return kayit.id


def toplam_kar_hesapla(db: Session) -> int:
    kayitlar = db.query(IslemGecmisi).filter_by(tamamlandi=True).all()
    return sum(k.kar for k in kayitlar)


def analiz_et(db: Session, item_adi: str, fiyat: int, pazar_id: int, satici_id: int = None) -> TradeSignal:
    fiyat_kaydet(db, item_adi, fiyat, pazar_id, satici_id)

    istat = istatistik_hesapla(db, item_adi)

    if istat is None:
        return TradeSignal(
            aksiyon=Aksiyon.BEKLE,
            pazar_id=pazar_id,
            sebep="Yeterli veri yok"
        )

    ortalama_guncelle(db, item_adi, istat)

    ortalama = istat["ortalama"]
    guven = istat["guven"]

    if guven < GUVEN_ESIK:
        return TradeSignal(
            aksiyon=Aksiyon.BEKLE,
            pazar_id=pazar_id,
            guven_skoru=guven,
            sebep=f"Guven skoru dusuk: {guven:.2f}"
        )

    hedef_satis = int(ortalama * SATIS_MARJI)

    if fiyat <= int(ortalama * UCUZ_ESIK):
        kar = hedef_satis - fiyat
        logger.info(f"[AL] {item_adi} | Fiyat:{fiyat} | Ort:{ortalama:.0f} | Kar:{kar}")
        return TradeSignal(
            aksiyon=Aksiyon.AL,
            pazar_id=pazar_id,
            kar_marji=kar,
            hedef_satis_fiyati=hedef_satis,
            guven_skoru=guven,
            sebep=f"Piyasa ortalamasinin %{int((1 - fiyat/ortalama)*100)} altinda"
        )

    if fiyat >= int(ortalama * PAHALI_ESIK):
        return TradeSignal(
            aksiyon=Aksiyon.SAT,
            pazar_id=pazar_id,
            hedef_satis_fiyati=hedef_satis,
            guven_skoru=guven,
            sebep=f"Piyasa ortalamasinin %{int((fiyat/ortalama - 1)*100)} ustunde"
        )

    return TradeSignal(
        aksiyon=Aksiyon.BEKLE,
        pazar_id=pazar_id,
        guven_skoru=guven,
        sebep="Normal fiyat araliginda"
    )