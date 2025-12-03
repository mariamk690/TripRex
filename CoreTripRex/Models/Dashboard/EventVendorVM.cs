namespace CoreTripRex.Models.Dashboard
{
    public class EventVendorVM
    {
        public string Organizer { get; set; }
        public string Venue { get; set; }
        public string Caption { get; set; }
        public string ImageUrl { get; set; }
        public List<EventOptionVM> Events { get; set; } = new();
    }
    public class EventOptionVM
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public decimal Price { get; set; }
    }
}
