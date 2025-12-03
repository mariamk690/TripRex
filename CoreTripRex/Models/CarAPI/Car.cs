namespace CoreTripRex.Models.CarAPI
{
    public class Car
    {
        public int CarID { get; set; }
        public int AgencyID { get; set; }
        public string CompanyName { get; set; }
        public string CarModel { get; set; }
        public string CarType { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public double DailyRate { get; set; }
        public bool IsAvailable { get; set; }
        public int Seats { get; set; }
        public string TransmissionType { get; set; }
        public string FuelType { get; set; }
        public string ImageURL { get; set; }
        public string Description { get; set; }
    }
}
