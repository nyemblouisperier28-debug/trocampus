namespace Trocampus.Models
{
    public class HomeSummary
    {
        public int Points { get; set; }
        public int MessagesUnread { get; set; }
        public int OffersReceived { get; set; }
        public int ExchangesCompleted { get; set; }
    }

    public class ListingDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Owner { get; set; }
        public string Location { get; set; }
        public string Type { get; set; } // Vente ou Échange
        public decimal? Price { get; set; }
        public int? Points { get; set; }
        public DateTime Date { get; set; }
    }
}