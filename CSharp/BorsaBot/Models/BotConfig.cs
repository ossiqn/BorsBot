namespace BorsaBot.Models
{
    public class BotConfig
    {
        public string ApiUrl { get; set; } = "http://127.0.0.1:5000";
        public int TaramaHiziMs { get; set; } = 800;
        public int MaxEnvanter { get; set; } = 50;
        public bool OtomatikSatis { get; set; } = true;
        public bool AntiDetect { get; set; } = true;
        public int MinKarMarji { get; set; } = 500000;
        public string OyunProsesAdi { get; set; } = "metin2client";
        public int PaketGonderimGecikmesiMs { get; set; } = 120;
    }
}