namespace BorsaBot.Models
{
    public class MarketItem
    {
        public string ItemAdi { get; set; } = string.Empty;
        public int Fiyat { get; set; }
        public int PazarId { get; set; }
        public int SaticiId { get; set; }
        public int Miktar { get; set; } = 1;
        public DateTime TespitZamani { get; set; } = DateTime.UtcNow;
    }
}