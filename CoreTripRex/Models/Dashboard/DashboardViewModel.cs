using System;
using System.Collections.Generic;
namespace CoreTripRex.Models.Dashboard
{
    public class DashboardViewModel
    {
        // search fields
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IncludeFlights { get; set; } = true;
        public bool IncludeHotels { get; set; } = true;
        public bool IncludeCars { get; set; }
        public bool IncludeEvents { get; set; }

        public string StatusMessage { get; set; }
        public string ErrorMessage { get; set; }
        public bool ShowResumeButton { get; set; }
        public bool ShowFlights { get; set; }
        public bool ShowHotels { get; set; }
        public bool ShowCars { get; set; }
        public bool ShowEvents { get; set; }
        public bool ShowCart { get; set; }
        public List<FlightVendorVM> FlightVendors { get; set; } = new();
        public List<HotelVendorVM> HotelVendors { get; set; } = new();
        public List<CarVendorVM> CarVendors { get; set; } = new();
        public List<EventVendorVM> EventVendors { get; set; } = new();
        public CartVM Cart { get; set; }

    }
}