namespace CoreTripRex.Models.CarAPI
{
    public class Reservation
    {
        public int AgencyID { get; set; }
        public int CarID { get; set; }
        public int CustomerID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string TravelSiteID { get; set; }
        public string TravelSiteAPIToken { get; set; }
    }

}
