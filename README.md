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
MIT

text


---

# GitHub Repository Ayarları

**Repo Description (About kısmı):**
Oyun içi pazar botları için C# + Python hibrit altyapı. Bellek okuma, fiyat analizi ve otomatik işlem.

text


**Topics:**
csharp, python, fastapi, wpf, game-bot, memory-reading, trading-bot, dotnet

text


---

# Twitter/X Paylaşımı
Oyun içi ticaret botları için açık kaynak bir altyapı yayınladım.

C# oyunun belleğini okur, Python fiyatları analiz eder, ikisi yerel API üzerinden konuşur.

→ Bellek okuma (ReadProcessMemory)
→ Geçmiş veri + istatistiksel analiz
→ Otomatik al/sat motoru
→ WPF arayüz

github.com/ossiqn/BorsBot

#csharp #python #opensource

text


---

# Forum Paylaşımı (Türkçe)

**Başlık:**
[Açık Kaynak] BorsBot - C# + Python Hibrit Oyun İçi Ticaret Botu

text


**İçerik:**
Merhaba,

Oyun içi pazar botları için geliştirdiğim açık kaynak altyapıyı paylaşıyorum.

Sistem iki katmandan oluşuyor:

C# katmanı oyunun belleğini okuyarak pazar verilerini topluyor.
Python katmanı bu verileri geçmiş fiyatlarla karşılaştırarak al/sat kararı üretiyor.
İki katman yerel bir REST API üzerinden haberleşiyor.

Kullanılan teknolojiler:

C# / WPF / .NET 8 (arayüz ve bellek okuma)
Python / FastAPI / Pandas (analiz motoru)
SQLite (fiyat geçmişi)
Oyuna göre düzenlenmesi gereken iki değer var:
Bellek adresleri Cheat Engine ile, paket şifreleme anahtarı Wireshark ile tespit ediliyor.
Bunların nasıl yapılacağı README içinde açıklandı.

Kaynak kod:
github.com/ossiqn/BorsBot
