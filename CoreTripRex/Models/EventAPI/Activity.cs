namespace CoreTripRex.Models.EventAPI
{
    public class Activity
    {
        public int EventID { get; set; }
        public string Title { get; set; }
        public string VenueName { get; set; }
        public string StartTime { get; set; }
        public decimal Price { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }
}
