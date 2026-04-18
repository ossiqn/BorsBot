Markdown

# BorsBot

Oyun içi pazarları gerçek zamanlı analiz eden, Python destekli akıllı ticaret botu.

## Nasıl Çalışır

C# katmanı oyunun belleğini okur ve pazar verilerini toplar. Python katmanı bu verileri analiz ederek al/sat kararı üretir. İki katman yerel API üzerinden haberleşir.
Oyun Belleği → C# (Veri Toplama) → Python (Analiz) → C# (İşlem)

text


## Özellikler

- Gerçek zamanlı pazar tarama
- Geçmiş fiyat verisiyle karşılaştırmalı analiz
- Otomatik al/sat kararı
- Anti-detect gecikme sistemi
- Manuel fiyat tanımlama desteği
- WPF arayüz

## Kurulum

**Gereksinimler**
- .NET 8.0
- Python 3.10+

**Python motoru**
```bash
cd Python
pip install -r requirements.txt
python kur.py
C# arayüzü

Bash

cd CSharp/BorsaBot
dotnet run
Yapı
text

BorsBot/
├── CSharp/
│   └── BorsaBot/
│       ├── Core/          # Bot motoru, bellek okuma, paket yönetimi
│       ├── Models/        # Veri modelleri
│       └── ViewModels/    # Arayüz mantığı
└── Python/
    ├── main.py            # FastAPI sunucusu
    ├── analyzer.py        # Fiyat analizi
    ├── database.py        # SQLite veritabanı
    └── models.py          # Pydantic modeller
Konfigürasyon
Oyuna göre düzenlenmesi gereken değerler:

Dosya	Değer	Açıklama
MemoryReader.cs	Adresler.*	Cheat Engine ile tespit edilir
PacketManager.cs	XorSifrele	Wireshark ile tespit edilir
BotConfig.cs	OyunProsesAdi	Oyunun .exe adı
Teknolojiler
C# / WPF / .NET 8
Python / FastAPI / SQLAlchemy / Pandas
SQLite
Lisans
MIT OSSIQN
