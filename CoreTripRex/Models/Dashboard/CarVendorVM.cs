namespace CoreTripRex.Models.Dashboard
{
    public class CarVendorVM
    {
        public int VendorID { get; set; }
        public string Agency { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public decimal StartingPrice { get; set; }
        public List<CarOptionVM> Cars { get; set; } = new();
    }
    public class CarOptionVM
    {
        public int ID { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string CarClass { get; set; }
        public int Seats { get; set; }
        public decimal DailyRate { get; set; }
        public string ImageUrl { get; set; }
    }
}
