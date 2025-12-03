namespace CoreTripRex.Models.CarAPI
{
    public class CarSelection
    {
        public int CarId { get; set; }
        public string Agency { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string CarClass { get; set; } = string.Empty;
        public int Seats { get; set; }
        public decimal DailyRate { get; set; }
        public int Quantity { get; set; }
    }
}
