namespace CoreTripRex.Models.Dashboard
{
    public class HotelVendorVM
    {
        public int VendorID { get; set; }
        public string HotelName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Caption { get; set; }
        public string ImageUrl { get; set; }
        public decimal StartingPrice { get; set; }
        public List<RoomOptionVM> Rooms { get; set; } = new(); 
    }
    public class RoomOptionVM
    {
        public int ID { get; set; }
        public string RoomType { get; set; }
        public string MaxOccupancy { get; set; }
        public decimal PricePerNight { get; set; }
        public string ImageUrl { get; set; }
    }
}
