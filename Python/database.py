from sqlalchemy import create_engine, Column, Integer, String, Float, DateTime, Boolean, Index
from sqlalchemy.orm import declarative_base, sessionmaker
from datetime import datetime
from loguru import logger

DATABASE_URL = "sqlite:///borsa.db"

engine = create_engine(
    DATABASE_URL,
    connect_args={"check_same_thread": False},
    pool_size=10,
    max_overflow=20
)

SessionLocal = sessionmaker(bind=engine, autocommit=False, autoflush=False)
Base = declarative_base()


class FiyatGecmisi(Base):
    __tablename__ = "fiyat_gecmisi"
    __table_args__ = (
        Index("idx_item_tarih", "item_adi", "tarih"),
    )
    id = Column(Integer, primary_key=True, index=True)
    item_adi = Column(String(128), index=True, nullable=False)
    fiyat = Column(Integer, nullable=False)
    pazar_id = Column(Integer)
    satici_id = Column(Integer, nullable=True)
    tarih = Column(DateTime, default=datetime.utcnow, index=True)


class PiyasaOrtalamalari(Base):
    __tablename__ = "piyasa_ortalamalari"
    id = Column(Integer, primary_key=True, index=True)
    item_adi = Column(String(128), unique=True, index=True, nullable=False)
    ortalama_fiyat = Column(Float, nullable=False)
    medyan_fiyat = Column(Float, default=0)
    std_sapma = Column(Float, default=0)
    min_fiyat = Column(Integer, default=0)
    max_fiyat = Column(Integer, default=0)
    ornek_sayisi = Column(Integer, default=0)
    guncelleme_tarihi = Column(DateTime, default=datetime.utcnow)


class IslemGecmisi(Base):
    __tablename__ = "islem_gecmisi"
    id = Column(Integer, primary_key=True, index=True)
    item_adi = Column(String(128), nullable=False)
    alis_fiyati = Column(Integer, nullable=False)
    satis_fiyati = Column(Integer, default=0)
    kar = Column(Integer, default=0)
    tamamlandi = Column(Boolean, default=False)
    tarih = Column(DateTime, default=datetime.utcnow)


class ManuelFiyatTablosu(Base):
    __tablename__ = "manuel_fiyatlar"
    id = Column(Integer, primary_key=True, index=True)
    item_adi = Column(String(128), unique=True, index=True, nullable=False)
    ortalama_fiyat = Column(Integer, nullable=False)
    tarih = Column(DateTime, default=datetime.utcnow)


def init_db():
    Base.metadata.create_all(bind=engine)
    logger.info("Veritabani baslatildi.")


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()