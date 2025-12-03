using CoreTripRex.Models;
using CoreTripRex.Models.Dashboard;
using CoreTripRex.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using TripRexLibraries;
using Utilities;

namespace CoreTripRex.Controllers.DashboardControllers
{
    public class DashboardController : Controller
    {
        private readonly StoredProcs sp = new StoredProcs();
        private readonly UserManager<AppUser> _userManager;
        private static readonly Random rand = new Random();
        private readonly CarApiService _carApi;
        public DashboardController(UserManager<AppUser> userManager, CarApiService carApi)
        {
            _userManager = userManager;
            _carApi = carApi;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                IncludeFlights = true,
                IncludeHotels = true,
                IncludeCars = true,
                IncludeEvents = true
            };

            var identityUser = await _userManager.GetUserAsync(User);
            int? userId = identityUser?.LegacyUserId > 0 ? identityUser.LegacyUserId : (int?)null;

            if (userId.HasValue)
            {
                model.ShowResumeButton = true;
                model.StatusMessage = "Welcome back! You can resume your previous session.";
                TryLoadSavedSearch(userId.Value, model);
                model.Cart = LoadCartVm(userId.Value, model);
            }
            else
            {
                model.ShowResumeButton = false;
                model.StatusMessage = "Browsing as guest. Sign in to build your trip and save your progress.";
                model.ShowCart = false;
            }

            if (TempData.ContainsKey("DashboardStatus")) model.StatusMessage = TempData["DashboardStatus"] as string ?? model.StatusMessage;
            if (TempData.ContainsKey("DashboardError")) model.ErrorMessage = TempData["DashboardError"] as string ?? model.ErrorMessage;

            return View(model);
        }
        [HttpPost]
        public IActionResult AddFlightWithSeats(int flightId, int quantity)
        {
            try
            {
                var identityUser = _userManager.GetUserAsync(User).Result;
                int? userId = identityUser?.LegacyUserId > 0 ? identityUser.LegacyUserId : (int?)null;

                if (!userId.HasValue)
                {
                    TempData["DashboardError"] = "Please sign in to add flights to your cart.";
                    return RedirectToAction("Index");
                }

                int packageId;
                string packageIdStr = HttpContext.Session.GetString("PackageID");
                if (!string.IsNullOrEmpty(packageIdStr) && int.TryParse(packageIdStr, out int pid))
                    packageId = pid;
                else
                {
                    packageId = sp.PackageGetOrCreate(userId.Value);
                    HttpContext.Session.SetString("PackageID", packageId.ToString());
                }

                int result = sp.PackageAddUpdateItem(
                    packageId,
                    "Flight",
                    flightId,
                    quantity,
                    null,
                    null
                );

                if (result >= 0 || result == -1)
                    TempData["DashboardStatus"] = $"{quantity} seat(s) added to your cart.";
                else
                    TempData["DashboardError"] = "Failed to add seats to cart.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["DashboardError"] = "Seat error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Search(DashboardViewModel model)
        {
            model.ErrorMessage = null;
            model.StatusMessage ??= string.Empty;

            try
            {
                if (!model.StartDate.HasValue || !model.EndDate.HasValue)
                    throw new Exception("Enter valid start and end date.");
                if (string.IsNullOrWhiteSpace(model.Origin) || string.IsNullOrWhiteSpace(model.Destination))
                    throw new Exception("Fill in both location fields.");

                model.Origin = model.Origin.Trim();
                model.Destination = model.Destination.Trim();

                DateTime startDate = model.StartDate.Value;
                DateTime endDate = model.EndDate.Value;

                HttpContext.Session.SetString("TripStart", startDate.ToString("o"));
                HttpContext.Session.SetString("TripEnd", endDate.ToString("o"));

                int originCityId = ResolveCity(model.Origin);
                int destCityId = ResolveCity(model.Destination);
                string fromCode = GetAirportCodeFromCity(originCityId);
                string toCode = GetAirportCodeFromCity(destCityId);

                var identityUser = await _userManager.GetUserAsync(User);
                int? userId = identityUser?.LegacyUserId > 0 ? identityUser.LegacyUserId : (int?)null;

                if (model.IncludeFlights)
                {
                    try
                    {
                        model.FlightVendors = await LoadFlightsVm(fromCode, toCode, startDate);
                        model.ShowFlights = model.FlightVendors.Count > 0;
                    }
                    catch (Exception ex)
                    {
                        model.ErrorMessage = "Flight API error: " + ex.Message;
                        model.ShowFlights = false;
                    }
                }
                else model.ShowFlights = false;

                if (model.IncludeHotels)
                {
                    try
                    {
                        var dsHotels = sp.HotelSearchbyCity(destCityId);
                        model.HotelVendors = LoadHotelsVm(dsHotels);
                        model.ShowHotels = model.HotelVendors.Count > 0;
                    }
                    catch (Exception ex)
                    {
                        model.ErrorMessage = "Hotel error: " + ex.Message;
                        model.ShowHotels = false;
                    }
                }
                else model.ShowHotels = false;

                if (model.IncludeCars)
                {
                    try
                    {
                        string city = model.Destination;
                        string state = ResolveState(city);

                        var agencies = await _carApi.GetAgencies(city, state);

                        var carVendors = new List<CarVendorVM>();

                        foreach (var agency in agencies)
                        {
                            var cars = await _carApi.GetCars(agency.AgencyID, city, state);

                            var vendor = new CarVendorVM
                            {
                                VendorID = agency.AgencyID,
                                Agency = agency.AgencyName,
                                Caption = "Available rental vehicles",
                                StartingPrice = cars.Any() ? (decimal)cars.Min(c => c.DailyRate) : 0m,
                                ImageUrl = Url.Content("~/images/placeholders/car.png")
                            };

                            foreach (var car in cars)
                            {
                                vendor.Cars.Add(new CarOptionVM
                                {
                                    ID = car.CarID,
                                    Make = car.CompanyName,
                                    Model = car.CarModel,
                                    CarClass = car.CarType,
                                    Seats = car.Seats,
                                    DailyRate = (decimal)car.DailyRate,
                                    ImageUrl = string.IsNullOrWhiteSpace(car.ImageURL)
                                        ? "~/images/placeholders/car.png"
                                        : car.ImageURL
                                });
                            }

                            carVendors.Add(vendor);
                        }

                        model.CarVendors = carVendors;
                        model.ShowCars = carVendors.Any();
                    }
                    catch (Exception ex)
                    {
                        model.ErrorMessage = "Car API error: " + ex.Message;
                        model.ShowCars = false;
                    }
                }
                else model.ShowCars = false;

                if (model.IncludeEvents)
                {
                    try
                    {
                        var dsEvents = sp.EventSearch(destCityId, startDate, endDate);
                        model.EventVendors = LoadEventsVm(dsEvents);
                        model.ShowEvents = model.EventVendors.Count > 0;
                    }
                    catch (Exception ex)
                    {
                        model.ErrorMessage = "Event error: " + ex.Message;
                        model.ShowEvents = false;
                    }
                }
                else model.ShowEvents = false;

                if (userId.HasValue)
                {
                    TrySaveSearch(userId.Value, model);
                    model.Cart = LoadCartVm(userId.Value, model);
                    model.ShowCart = model.Cart != null && model.Cart.Items.Count > 0;
                }
                else model.ShowCart = false;

                return View("Index", model);
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "Error: " + ex.Message;
                return View("Index", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCar(int carId) => AddToPackage("Car Rental", carId);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddFlight(int flightId) => AddToPackage("Flight", flightId);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddRoom(int roomId) => AddToPackage("Hotel", roomId);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEvent(int eventId) => AddToPackage("Event", eventId);

        private IActionResult AddToPackage(string type, int refId)
        {
            try
            {
                var identityUser = _userManager.GetUserAsync(User).Result;
                int? userId = identityUser?.LegacyUserId > 0 ? identityUser.LegacyUserId : (int?)null;

                if (!userId.HasValue)
                {
                    TempData["DashboardError"] = "Please sign in to add items to your cart.";
                    return RedirectToAction("Index");
                }

                int packageId;
                string packageIdStr = HttpContext.Session.GetString("PackageID");
                if (!string.IsNullOrEmpty(packageIdStr) && int.TryParse(packageIdStr, out int pid))
                    packageId = pid;
                else
                {
                    packageId = sp.PackageGetOrCreate(userId.Value);
                    HttpContext.Session.SetString("PackageID", packageId.ToString());
                }

                DateTime? startUtc = null;
                DateTime? endUtc = null;

                if (type == "Hotel" || type == "Car Rental")
                {
                    var tripStartStr = HttpContext.Session.GetString("TripStart");
                    var tripEndStr = HttpContext.Session.GetString("TripEnd");
                    if (DateTime.TryParse(tripStartStr, out var ts)) startUtc = ts;
                    if (DateTime.TryParse(tripEndStr, out var te)) endUtc = te;
                }

                int result = sp.PackageAddUpdateItem(packageId, type, refId, 1, startUtc, endUtc);

                if (result >= 0 || result == -1)
                {
                    TempData["DashboardStatus"] = $"{type} added successfully.";
                    TempData["DashboardError"] = null;
                }
                else TempData["DashboardError"] = $"Failed to add {type}.";
            }
            catch (Exception ex)
            {
                TempData["DashboardError"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        private void TryLoadSavedSearch(int userId, DashboardViewModel model)
        {
            try
            {
                SqlCommand cmd = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandText = "usp_Get_Saved_Search"
                };
                cmd.Parameters.AddWithValue("@UserID", userId);

                DBConnect objDB = new DBConnect();
                DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    if (row["last_search"] != DBNull.Value)
                    {
                        byte[] byteArray = (byte[])row["last_search"];

                        string json = System.Text.Encoding.UTF8.GetString(byteArray);
                        var saved = JsonSerializer.Deserialize<SavedSearch>(json);

                        if (saved != null)
                        {
                            model.Origin = saved.Origin;
                            model.Destination = saved.Destination;
                            model.StartDate = saved.DepartDate;
                            model.EndDate = saved.ReturnDate;

                            HttpContext.Session.SetString("TripStart", saved.DepartDate.ToString("o"));
                            HttpContext.Session.SetString("TripEnd", saved.ReturnDate.ToString("o"));

                            model.StatusMessage += "<br/>Your previous search has been restored!";
                        }
                    }
                    else
                    {
                        model.StatusMessage += "<br/>A saved search was never stored for this account.";
                    }
                }
            }
            catch (Exception ex)
            {
                model.StatusMessage += "<br/>Unable to load saved search: " + ex.Message;
            }
        }


        private void TrySaveSearch(int userId, DashboardViewModel model)
        {
            try
            {
                SavedSearch objSearch = new SavedSearch
                {
                    Origin = model.Origin,
                    Destination = model.Destination,
                    DepartDate = model.StartDate ?? DateTime.MinValue,
                    ReturnDate = model.EndDate ?? DateTime.MinValue
                };

                string json = JsonSerializer.Serialize(objSearch);
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);

                SqlCommand cmd = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandText = "usp_Store_Saved_Search"
                };
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@SavedSearch", byteArray);

                DBConnect objDB = new DBConnect();
                int retVal = objDB.DoUpdateUsingCmdObj(cmd);

                model.StatusMessage = retVal > 0
                    ? "Search criteria saved successfully."
                    : "A problem occurred saving the search.";
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "Serialization error: " + ex.Message;
            }
        }


        private int ResolveCity(string name)
        {
            DataSet ds = sp.CitiesList();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                if (row["name"].ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt32(row["id"]);
            }
            throw new Exception("City not found: " + name);
        }

        private string GetAirportCodeFromCity(int cityId)
        {
            DataSet ds = sp.AirportList(cityId);
            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                throw new Exception("No airport found for selected city.");
            return ds.Tables[0].Rows[0]["code"].ToString();
        }

        private async Task<List<FlightVendorVM>> LoadFlightsVm(string fromCode, string toCode, DateTime departDate)
        {
            const string apiUrl = "https://cis-iis2.temple.edu/Fall2025/CIS3342_tuo90411/WebAPI/api/Flights/FindFlights";
            var body = new
            {
                FromCode = fromCode,
                ToCode = toCode,
                DepartDate = departDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                ClassCode = (string)null,
                MinPrice = (decimal?)null,
                MaxPrice = (decimal?)null
            };

            using var client = new HttpClient();
            string jsonOutbound = JsonSerializer.Serialize(body);
            var content = new StringContent(jsonOutbound, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);

            string rawResponse = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception("FLIGHT API CALL FAILED\n\n" + rawResponse);

            string json = await response.Content.ReadAsStringAsync();
            var flights = JsonSerializer.Deserialize<List<FlightResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var vendors = new List<FlightVendorVM>();
            foreach (var group in flights.GroupBy(f => f.AirCarrierName))
            {
                var first = group.First();
                var vendorVm = new FlightVendorVM
                {
                    Vendor = group.Key,
                    ImageUrl = string.IsNullOrWhiteSpace(first.ImageUrl) ? "~/images/placeholders/airline.png" : first.ImageUrl,
                    Caption = string.IsNullOrWhiteSpace(first.Caption) ? "Multiple routes and fare classes available" : first.Caption
                };

                foreach (var f in group)
                {
                    vendorVm.Options.Add(new FlightOptionVM
                    {
                        FlightID = f.FlightID,
                        FlightNumber = f.FlightNumber,
                        DepartCode = f.DepartCode,
                        ArriveCode = f.ArriveCode,
                        DepartTime = Convert.ToDateTime(f.DepartureTime),
                        ArriveTime = Convert.ToDateTime(f.ArrivalTime),
                        ClassCode = f.ClassCode,
                        Price = f.Price
                    });
                }

                vendors.Add(vendorVm);
            }

            return vendors;
        }

        private List<HotelVendorVM> LoadHotelsVm(DataSet ds)
        {
            var result = new List<HotelVendorVM>();
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return result;

            DataTable dtHotels = ds.Tables[0];
            foreach (DataRow h in dtHotels.Rows)
            {
                var vendor = new HotelVendorVM
                {
                    VendorID = h.Table.Columns.Contains("vendor_id") ? Convert.ToInt32(h["vendor_id"]) : 0,
                    HotelName = h.Table.Columns.Contains("hotel_name") ? h["hotel_name"].ToString() : string.Empty,
                    Phone = h.Table.Columns.Contains("phone") ? h["phone"].ToString() : string.Empty,
                    Email = h.Table.Columns.Contains("email") ? h["email"].ToString() : string.Empty,
                    Caption = h.Table.Columns.Contains("caption") && h["caption"] != DBNull.Value ? h["caption"].ToString() : "Comfort and style near your destination",
                    StartingPrice = h.Table.Columns.Contains("starting_price") && h["starting_price"] != DBNull.Value ? Convert.ToDecimal(h["starting_price"]) : 0m,
                    ImageUrl = Url.Content("~/images/placeholders/hotel.png")
                };

                if (vendor.VendorID != 0)
                {
                    DataSet dsRooms = sp.HotelRooms(vendor.VendorID);
                    if (dsRooms != null && dsRooms.Tables.Count > 0 && dsRooms.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow r in dsRooms.Tables[0].Rows)
                        {
                            vendor.Rooms.Add(new RoomOptionVM
                            {
                                ID = r.Table.Columns.Contains("room_type_id")
                                ? Convert.ToInt32(r["room_type_id"])
                                : 0,

                                RoomType = r.Table.Columns.Contains("room_type")
                                ? r["room_type"].ToString()
                                : "Room",

                                MaxOccupancy = r.Table.Columns.Contains("max_occupancy")
                                ? r["max_occupancy"].ToString()
                                : "2",

                                PricePerNight = r.Table.Columns.Contains("price") && r["price"] != DBNull.Value
                                ? Convert.ToDecimal(r["price"])
                                : 0m,

                                ImageUrl = Url.Content("~/images/placeholders/hotel.png")
                            });

                        }
                    }
                }

                result.Add(vendor);
            }

            return result;
        }

        private List<CarVendorVM> LoadCarsVm(DataSet ds)
        {
            var result = new List<CarVendorVM>();
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return result;

            DataTable dtCars = ds.Tables[0];
            var vendorsByAgency = new Dictionary<string, CarVendorVM>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in dtCars.Rows)
            {
                string agency = row.Table.Columns.Contains("agency") ? row["agency"].ToString() : "Car Rental";

                if (!vendorsByAgency.TryGetValue(agency, out var vendor))
                {
                    vendor = new CarVendorVM
                    {
                        Agency = agency,
                        VendorID = row.Table.Columns.Contains("vendor_id") ? Convert.ToInt32(row["vendor_id"]) : 0,
                        StartingPrice = row.Table.Columns.Contains("daily_rate") && row["daily_rate"] != DBNull.Value ? Convert.ToDecimal(row["daily_rate"]) : 0m,
                        Caption = "Economy to SUV options available.",
                        ImageUrl = Url.Content("~/images/placeholders/car.png")
                    };

                    vendorsByAgency[agency] = vendor;
                    result.Add(vendor);
                }

                vendor.Cars.Add(new CarOptionVM
                {
                    ID = row.Table.Columns.Contains("id") ? Convert.ToInt32(row["id"]) : 0,
                    Make = row.Table.Columns.Contains("make") ? row["make"].ToString() : string.Empty,
                    Model = row.Table.Columns.Contains("model") ? row["model"].ToString() : string.Empty,
                    CarClass = row.Table.Columns.Contains("car_class") ? row["car_class"].ToString() : string.Empty,
                    Seats = row.Table.Columns.Contains("seats") && row["seats"] != DBNull.Value ? Convert.ToInt32(row["seats"]) : 0,
                    DailyRate = row.Table.Columns.Contains("daily_rate") && row["daily_rate"] != DBNull.Value ? Convert.ToDecimal(row["daily_rate"]) : 0m,
                    ImageUrl = Url.Content("~/images/placeholders/car.png")
                });
            }

            return result;
        }

        private List<EventVendorVM> LoadEventsVm(DataSet ds)
        {
            var result = new List<EventVendorVM>();
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return result;

            DataTable dtEvents = ds.Tables[0];
            var groups = new Dictionary<(string Organizer, string Venue), EventVendorVM>();

            foreach (DataRow row in dtEvents.Rows)
            {
                string organizer = dtEvents.Columns.Contains("organizer") && row["organizer"] != DBNull.Value ? row["organizer"].ToString() : "Local Events";
                string venue = dtEvents.Columns.Contains("venue") && row["venue"] != DBNull.Value ? row["venue"].ToString() : "Various";

                var key = (organizer, venue);
                if (!groups.TryGetValue(key, out var vendor))
                {
                    vendor = new EventVendorVM
                    {
                        Organizer = organizer,
                        Venue = venue,
                        ImageUrl = Url.Content("~/images/placeholders/event.png"),
                        Caption = "Exciting events happening during your stay."
                    };

                    groups[key] = vendor;
                    result.Add(vendor);
                }

                vendor.Events.Add(new EventOptionVM
                {
                    ID = dtEvents.Columns.Contains("id") && row["id"] != DBNull.Value ? Convert.ToInt32(row["id"]) : 0,
                    Name = dtEvents.Columns.Contains("name") ? row["name"].ToString() : string.Empty,
                    StartTime = dtEvents.Columns.Contains("start_time") && row["start_time"] != DBNull.Value ? Convert.ToDateTime(row["start_time"]) : DateTime.MinValue,
                    Price = dtEvents.Columns.Contains("price") && row["price"] != DBNull.Value ? Convert.ToDecimal(row["price"]) : 0m
                });
            }

            return result;
        }

        private CartVM LoadCartVm(int userId, DashboardViewModel model)
        {
            var cart = new CartVM();

            int packageId = sp.PackageGetOrCreate(userId);
            DataSet ds = sp.PackageGet(packageId);

            if (ds == null || ds.Tables.Count <= 1 || ds.Tables[1].Rows.Count == 0)
            {
                model.ShowCart = false;
                return cart;
            }

            DataTable items = ds.Tables[1];
            decimal total = 0m;

            DateTime tripStart;
            DateTime tripEnd;

            var tripStartStr = HttpContext.Session.GetString("TripStart");
            var tripEndStr = HttpContext.Session.GetString("TripEnd");

            if (!DateTime.TryParse(tripStartStr, out tripStart))
                tripStart = DateTime.UtcNow;

            if (!DateTime.TryParse(tripEndStr, out tripEnd))
                tripEnd = tripStart.AddDays(1);

            int totalDays = (tripEnd - tripStart).Days;
            if (totalDays < 1) totalDays = 1;

            foreach (DataRow r in items.Rows)
            {
                string type = r["service_type"].ToString();

                decimal unitPrice = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0m;

                decimal lineTotal;
                if (type == "Hotel" || type == "Car Rental")
                    lineTotal = unitPrice * totalDays;
                else
                    lineTotal = r["line_total"] != DBNull.Value ? Convert.ToDecimal(r["line_total"]) : unitPrice;

                total += lineTotal;

                cart.Items.Add(new CartItemVM
                {
                    ServiceType = type,
                    DisplayName = r.Table.Columns.Contains("display_name") ? r["display_name"].ToString() : string.Empty,
                    Details = r.Table.Columns.Contains("details") ? r["details"].ToString() : string.Empty,
                    RefId = r.Table.Columns.Contains("ref_id") ? r["ref_id"].ToString() : string.Empty,
                    ItemPrice = unitPrice,
                    Quantity = (type == "Hotel" || type == "Car Rental") ? totalDays : (r.Table.Columns.Contains("qty") && r["qty"] != DBNull.Value ? Convert.ToInt32(r["qty"]) : 1),
                    ComputedPrice = lineTotal
                });
            }

            cart.Total = total;
            model.ShowCart = cart.Items.Count > 0;

            return cart;
        }
        private string ResolveState(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return "NA";

            city = city.Trim().ToLower();

            return city switch
            {
                "philadelphia" => "PA",
                "las vegas" => "NV",
                "orlando" => "FL",
                _ => "NA"
            };
        }

    }
}
