namespace CoreTripRex.Models.Dashboard
{
    public class FlightVendorVM
    {
        public string Vendor { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public List<FlightOptionVM> Options { get; set; } = new();
    }
    public class FlightOptionVM
    {
        public int FlightID { get; set; }
        public string FlightNumber { get; set; }
        public string DepartCode { get; set; }
        public string ArriveCode { get; set; }
        public DateTime DepartTime { get; set; }
        public DateTime ArriveTime { get; set; }
        public string ClassCode { get; set; }
        public decimal Price { get; set; }
    }
}
