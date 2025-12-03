namespace CoreTripRex.Models.Dashboard
{
    public class CartVM
    {
        public List<CartItemVM> Items { get; set; } = new();
        public decimal Total { get; set; }
    }
    public class CartItemVM
    {
        public string ServiceType { get; set; }
        public string DisplayName { get; set; }
        public string Details { get; set; }
        public string RefId { get; set; }
        public decimal ItemPrice { get; set; }
        public int Quantity { get; set; }
        public decimal ComputedPrice { get; set; }
    }
}
