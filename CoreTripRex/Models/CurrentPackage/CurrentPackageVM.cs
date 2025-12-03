namespace CoreTripRex.Models.CurrentPackage
{
    public class CurrentPackageItemVM
    {
        public int RefId { get; set; }
        public string ServiceType { get; set; }
        public string DisplayName { get; set; }
        public string Details { get; set; }
        public string ComputedDates { get; set; }
        public string ComputedQtyLabel { get; set; }
        public decimal ComputedTotal { get; set; }
    }

    public class CurrentPackageVM
    {
        public string Message { get; set; }
        public bool HasPackage { get; set; }
        public decimal Total { get; set; }
        public List<CurrentPackageItemVM> Items { get; set; } = new();
    }
}

