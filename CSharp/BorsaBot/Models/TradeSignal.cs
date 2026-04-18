namespace BorsaBot.Models
{
    public enum Aksiyon { AL, SAT, BEKLE }

    public class TradeSignal
    {
        public Aksiyon Aksiyon { get; set; }
        public int PazarId { get; set; }
        public int KarMarji { get; set; }
        public int HedefSatisFiyati { get; set; }
        public double GuvenSkoru { get; set; }
        public string Sebep { get; set; } = string.Empty;
    }
}