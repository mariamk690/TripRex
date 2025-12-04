using CoreTripRex.Models;
using CoreTripRex.Models.Checkout;
using CoreTripRex.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TripRexLibraries;
using Utilities;



namespace CoreTripRex.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly StoredProcs _sp;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;


        public CheckoutController(UserManager<AppUser> userManager, IEmailService emailService, IConfiguration configuration)
        {
            _sp = new StoredProcs();
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }
        private static readonly Dictionary<string, string> CityCoordinates =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Philadelphia", "39.9526,-75.1652" },
                { "Las Vegas", "36.1699,-115.1398" },
                { "Orlando", "28.5383,-81.3792" }
            };



        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await LoadCheckoutViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(CheckoutViewModel form)
        {
            var model = await LoadCheckoutViewModel();
            model.AddCard = form.AddCard;
            model.CardNumber = form.CardNumber;
            model.Exp = form.Exp;

            if (!model.HasMessage && model.Items.Count == 0)
            {
                model.HasMessage = true;
                model.MessageCssClass = "text-danger d-block mb-3";
                model.Message = "Your package is empty.";
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.LegacyUserId <= 0)
                {
                    model.HasMessage = true;
                    model.MessageCssClass = "text-danger d-block mb-3";
                    model.Message = "Please sign in before completing checkout.";
                    return View(model);
                }

                int userId = user.LegacyUserId;

                int packageId = HttpContext.Session.GetInt32("PackageID") ?? _sp.PackageGetActive(userId);
                if (packageId == 0)
                {
                    model.HasMessage = true;
                    model.MessageCssClass = "text-danger d-block mb-3";
                    model.Message = "No active package found to book.";
                    return View(model);
                }

                DataSet dsCheck = _sp.PackageGet(packageId);
                if (dsCheck == null || dsCheck.Tables.Count < 2 || dsCheck.Tables[1].Rows.Count == 0)
                {
                    model.HasMessage = true;
                    model.MessageCssClass = "text-danger d-block mb-3";
                    model.Message = "Your package is empty — please add vacation items before booking.";
                    return View(model);
                }

                int paymentMethodId = -1;

                if (form.AddCard)
                {
                    string cardNum = form.CardNumber?.Replace(" ", "") ?? "";
                    string exp = form.Exp?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(cardNum) || cardNum.Length < 4 || string.IsNullOrWhiteSpace(exp))
                    {
                        model.HasMessage = true;
                        model.MessageCssClass = "text-danger d-block mb-3";
                        model.Message = "Please enter a valid card number and expiration.";
                        return View(model);
                    }

                    string brand = cardNum.StartsWith("4") ? "Visa" :
                                   cardNum.StartsWith("5") ? "Mastercard" :
                                   "Card";

                    string last4 = cardNum.Length >= 4 ? cardNum[^4..] : "0000";

                    int expMonth = 0;
                    int expYear = 0;

                    if (exp.Contains("/"))
                    {
                        var parts = exp.Split('/');
                        if (parts.Length >= 2)
                        {
                            int.TryParse(parts[0], out expMonth);
                            int.TryParse(parts[1], out expYear);
                            if (expYear < 100) expYear += 2000;
                        }
                    }

                    _sp.AddPaymentMethod(userId, brand, last4, expMonth, expYear, true);

                    DataSet ds = _sp.ListPaymentMethods(userId);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        paymentMethodId = Convert.ToInt32(ds.Tables[0].Rows[0]["Id"]);
                }
                else
                {
                    DataSet ds = _sp.ListPaymentMethods(userId);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow[] defaults = ds.Tables[0].Select("is_default = 1");
                        paymentMethodId = Convert.ToInt32((defaults.Length > 0 ? defaults[0]["Id"] : ds.Tables[0].Rows[0]["Id"]));
                    }
                }

                if (paymentMethodId <= 0)
                {
                    model.HasMessage = true;
                    model.MessageCssClass = "text-danger d-block mb-3";
                    model.Message = "No valid payment method found or created.";
                    return View(model);
                }

                int result = _sp.CheckoutPackage(packageId, paymentMethodId, "USD", "WEB-" + DateTime.UtcNow.Ticks);

                if (result > 0)
                {
                    model.IsSuccess = true;
                    model.HasMessage = true;
                    model.MessageCssClass = "text-success d-block mb-3";
                    model.Message = "Your trip has been booked successfully! 🎉";

                    try
                    {
                        string userEmail = user.Email ?? "student@temple.edu";
                        string userName = user.FullName ?? "Traveler";

                        DataSet dsSummary = _sp.PackageGet(packageId);
                        DataTable items = dsSummary?.Tables.Count > 1 ? dsSummary.Tables[1] : null;

                        if (items != null && items.Rows.Count > 0)
                        {
                            string tripStartRaw = HttpContext.Session.GetString("TripStart");
                            string tripEndRaw = HttpContext.Session.GetString("TripEnd");

                            DateTime.TryParse(tripStartRaw, out DateTime tripStart);
                            DateTime.TryParse(tripEndRaw, out DateTime tripEnd);

                            int totalDays = (tripEnd - tripStart).Days;
                            if (totalDays < 1) totalDays = 1;

                            string detailsHtml = "<table style='border-collapse:collapse;width:100%;margin-top:10px' border='1' cellpadding='6'>" +
                                                 "<tr style='background:#f2f2f2;text-align:left'>" +
                                                 "<th>Item</th><th>Type</th><th>Dates</th><th>Qty</th><th>Cost</th>" +
                                                 "</tr>";

                            decimal grandTotal = 0;

                            foreach (DataRow r in items.Rows)
                            {
                                string name = r["display_name"]?.ToString() ?? "—";
                                string type = r["service_type"]?.ToString() ?? "—";

                                decimal unitPrice = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0;
                                decimal lineTotal = r["line_total"] != DBNull.Value ? Convert.ToDecimal(r["line_total"]) : 0;

                                decimal computedTotal = lineTotal;
                                string dates = "—";
                                string qtyLabel = "x1";

                                if (type == "Hotel" || type == "Car Rental")
                                {
                                    computedTotal = unitPrice * totalDays;
                                    dates = $"{tripStart:MM/dd}–{tripEnd:MM/dd}";
                                    qtyLabel = type == "Hotel" ? $"x {totalDays} nights" : $"x {totalDays} days";
                                }
                                else if (r["start_utc"] != DBNull.Value && r["end_utc"] != DBNull.Value)
                                {
                                    DateTime startUtc = Convert.ToDateTime(r["start_utc"]);
                                    DateTime endUtc = Convert.ToDateTime(r["end_utc"]);
                                    dates = $"{startUtc:MM/dd}–{endUtc:MM/dd}";
                                }

                                grandTotal += computedTotal;

                                detailsHtml += "<tr>" +
                                               $"<td>{name}</td><td>{type}</td><td>{dates}</td><td>{qtyLabel}</td><td>${computedTotal:F2}</td>" +
                                               "</tr>";
                            }

                            detailsHtml += "<tr style='font-weight:bold;background:#f9f9f9'>" +
                                           "<td colspan='4' style='text-align:right'>Total</td>" +
                                           $"<td>${grandTotal:F2}</td></tr></table>";

                            string subject = "TripRex Booking Confirmation";
                            string body = "<p>Hi " + userName + ",</p>" +
                                          "<p>Thanks for booking your trip with <strong>TripRex</strong>!</p>" +
                                          "<p>Here are your vacation details:</p>" +
                                          detailsHtml +
                                          "<p>We hope you have an amazing trip! 🌴</p>" +
                                          "<p>— The TripRex Team</p>";

                            await _emailService.SendEmailAsync(userEmail, subject, body);
                        }
                    }
                    catch
                    {
                    }

                    HttpContext.Session.Remove("PackageID");
                    model.Items.Clear();
                    model.Total = 0;
                }
                else
                {
                    model.HasMessage = true;
                    model.MessageCssClass = "text-danger d-block mb-3";
                    model.Message = "Checkout failed (code " + result + ").";
                }
            }
            catch (Exception ex)
            {
                model.HasMessage = true;
                model.MessageCssClass = "text-danger d-block mb-3";
                model.Message = "Checkout error: " + ex.Message;
            }

            return View(model);
        }

        private async Task<CheckoutViewModel> LoadCheckoutViewModel()
        {

            var model = new CheckoutViewModel();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.LegacyUserId <= 0)
            {
                model.HasMessage = true;
                model.MessageCssClass = "text-warning d-block mb-3";
                model.Message = "Please sign in to continue to checkout.";
                return model;
            }

            int userId = user.LegacyUserId;

            model.UserGreeting = "Hi, " + (user.FullName ?? "Traveler") + "!";

            int packageId = HttpContext.Session.GetInt32("PackageID") ?? _sp.PackageGetActive(userId);
            if (packageId == 0)
            {
                model.HasMessage = true;
                model.MessageCssClass = "text-warning d-block mb-3";
                model.Message = "No active package found — add items before checkout.";
                return model;
            }

            DataSet ds = _sp.PackageGet(packageId);
            if (ds == null || ds.Tables.Count < 2 || ds.Tables[1].Rows.Count == 0)
            {
                model.HasMessage = true;
                model.MessageCssClass = "text-warning d-block mb-3";
                model.Message = "Your package is empty — add items before checkout.";
                return model;
            }

            DataTable items = ds.Tables[1];

            string tripStartRaw = HttpContext.Session.GetString("TripStart");
            string tripEndRaw = HttpContext.Session.GetString("TripEnd");

            DateTime.TryParse(tripStartRaw, out DateTime tripStart);
            DateTime.TryParse(tripEndRaw, out DateTime tripEnd);

            int totalDays = (tripEnd - tripStart).Days;
            if (totalDays < 1) totalDays = 1;

            decimal total = 0;
            var list = new List<CheckoutItemViewModel>();

            foreach (DataRow r in items.Rows)
            {
                string type = r["service_type"]?.ToString() ?? "";
                string name = r["display_name"]?.ToString() ?? "—";

                decimal unitPrice = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0;
                decimal lineTotal = r["line_total"] != DBNull.Value ? Convert.ToDecimal(r["line_total"]) : 0;

                decimal computedTotal = 0;
                string dates = "—";
                string qtyLabel = "x 1";

                if (type == "Hotel" || type == "Car Rental")
                {
                    computedTotal = unitPrice * totalDays;
                    dates = $"{tripStart:MM/dd}–{tripEnd:MM/dd}";
                    qtyLabel = type == "Hotel"
                        ? $"x {totalDays} nights"
                        : $"x {totalDays} days";
                    total += computedTotal;
                }
                else
                {
                    computedTotal = lineTotal;

                    if (r["start_utc"] != DBNull.Value && r["end_utc"] != DBNull.Value)
                    {
                        DateTime startUtc = Convert.ToDateTime(r["start_utc"]);
                        DateTime endUtc = Convert.ToDateTime(r["end_utc"]);
                        dates = $"{startUtc:MM/dd}–{endUtc:MM/dd}";
                    }

                    total += lineTotal;
                }

                list.Add(new CheckoutItemViewModel
                {
                    DisplayName = name,
                    ServiceType = type,
                    ComputedTotal = computedTotal,
                    ComputedDates = dates,
                    ComputedQtyLabel = qtyLabel
                });
            }

            Dictionary<string, decimal> totalsByType = new Dictionary<string, decimal>();

            foreach (CheckoutItemViewModel item in list)
            {
                string typeKey = item.ServiceType;
                if (typeKey == null)
                {
                    typeKey = string.Empty;
                }

                if (!totalsByType.ContainsKey(typeKey))
                {
                    totalsByType[typeKey] = 0m;
                }

                totalsByType[typeKey] = totalsByType[typeKey] + item.ComputedTotal;
            }

            if (totalsByType.Count > 0)
            {
                int count = totalsByType.Count;
                string[] labels = new string[count];
                double[] values = new double[count];

                int index = 0;
                foreach (KeyValuePair<string, decimal> kvp in totalsByType)
                {
                    labels[index] = kvp.Key;
                    values[index] = Convert.ToDouble(kvp.Value);
                    index = index + 1;
                }

                Plot plt = new Plot();

                ScottPlot.Plottables.Pie pie = plt.Add.Pie(values);
                pie.ExplodeFraction = 0.05;
                pie.SliceLabelDistance = 0.5;

                double totalValue = 0;
                int j = 0;
                while (j < count)
                {
                    totalValue = totalValue + values[j];
                    j = j + 1;
                }

                int k = 0;
                while (k < count)
                {
                    double percent = 0;
                    if (totalValue > 0)
                    {
                        percent = values[k] / totalValue * 100.0;
                    }

                    pie.Slices[k].Label = labels[k];
                    pie.Slices[k].LabelFontSize = 14;
                    pie.Slices[k].LabelBold = true;
                    pie.Slices[k].LabelFontColor = Colors.Black.WithAlpha(0.7f);
                    pie.Slices[k].LegendText =
                        labels[k] + " $" + values[k].ToString("0") + " (" + percent.ToString("0") + "%)";

                    k = k + 1;
                }

                plt.ShowLegend();
                plt.Axes.Frameless();
                plt.HideGrid();

                byte[] bytes = plt.GetImageBytes(600, 400, ScottPlot.ImageFormat.Png);
                string base64 = Convert.ToBase64String(bytes);
                model.ChartImageUrl = "data:image/png;base64," + base64;


            }

            //google maps api
            string mapKey = _configuration["GoogleMaps:ApiKey"];
            if (!string.IsNullOrWhiteSpace(mapKey))
            {
                string baseUrl = "https://maps.googleapis.com/maps/api/staticmap";

                string origin = "39.9526,-75.1652";
                string destination = "25.7617,-80.1918";

                List<string> markerSegments = new List<string>();
                markerSegments.Add("markers=color:blue|label:H|25.7906,-80.1300");
                markerSegments.Add("markers=color:red|label:E|25.7743,-80.1937");

                string pathSegment = "path=color:0x0000ff|weight:4|" + origin + "|" + destination;
                string markersJoined = string.Join("&", markerSegments.ToArray());

                string query = "size=600x350&" + pathSegment + "&" + markersJoined + "&key=" + mapKey;

                model.MapImageUrl = baseUrl + "?" + query;
            }
            else
            {
                model.MapImageUrl = null;
            }


            model.Items = list;
            model.Total = total;
            string mapUrl = BuildMapFromSavedSearch(userId, items);
            model.MapImageUrl = mapUrl;





            DataSet dsCards = _sp.ListPaymentMethods(userId);
            if (dsCards != null && dsCards.Tables.Count > 0 && dsCards.Tables[0].Rows.Count > 0)
            {
                var table = dsCards.Tables[0];
                DataRow row = table.Select("is_default = 1").FirstOrDefault() ?? table.Rows[0];

                string brand = row["brand"]?.ToString() ?? "Card";
                string last4 = row["last4"]?.ToString() ?? "0000";
                int expMonth = row["exp_month"] != DBNull.Value ? Convert.ToInt32(row["exp_month"]) : 0;
                int expYear = row["exp_year"] != DBNull.Value ? Convert.ToInt32(row["exp_year"]) : 0;

                model.HasSavedCard = true;
                model.SavedCardLabel = $"{brand} ending in {last4} (exp {expMonth:D2}/{expYear % 100:D2})";
                model.AddCard = false;
            }
            else
            {
                model.HasSavedCard = false;
                model.SavedCardLabel = null;
                model.AddCard = true;
            }

            return model;

        }
        private (string Origin, string Destination)? GetSavedCities(int userId)
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
                    DataRow row = ds.Tables[0].Rows[0];

                    if (row["last_search"] != DBNull.Value)
                    {
                        byte[] byteArray = (byte[])row["last_search"];
                        string json = Encoding.UTF8.GetString(byteArray);
                        SavedSearch saved = JsonSerializer.Deserialize<SavedSearch>(json);

                        if (saved != null)
                        {
                            return (saved.Origin, saved.Destination);
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private string BuildMapFromSavedSearch(int userId, DataTable items)
        {
            string mapKey = _configuration["GoogleMaps:ApiKey"];
            if (string.IsNullOrWhiteSpace(mapKey))
            {
                return null;
            }

            var cities = GetSavedCities(userId);
            if (cities == null)
            {
                return null;
            }

            string originCity = cities.Value.Origin;
            string destinationCity = cities.Value.Destination;

            if (string.IsNullOrWhiteSpace(originCity) || string.IsNullOrWhiteSpace(destinationCity))
            {
                return null;
            }

            if (!CityCoordinates.ContainsKey(originCity))
            {
                return null;
            }

            if (!CityCoordinates.ContainsKey(destinationCity))
            {
                return null;
            }

            string originCoord = CityCoordinates[originCity];
            string destinationCoord = CityCoordinates[destinationCity];

            string centerCoord = destinationCoord;

            string[] oParts = originCoord.Split(',');
            string[] dParts = destinationCoord.Split(',');

            double oLat;
            double oLng;
            double dLat;
            double dLng;

            if (oParts.Length == 2 && dParts.Length == 2 &&
                double.TryParse(oParts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out oLat) &&
                double.TryParse(oParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out oLng) &&
                double.TryParse(dParts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out dLat) &&
                double.TryParse(dParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out dLng))
            {
                double centerLat = (oLat + dLat) / 2.0;
                double centerLng = (oLng + dLng) / 2.0;

                string centerLatText = centerLat.ToString("0.######", CultureInfo.InvariantCulture);
                string centerLngText = centerLng.ToString("0.######", CultureInfo.InvariantCulture);

                centerCoord = centerLatText + "," + centerLngText;
            }

            List<string> markerSegments = new List<string>();
            markerSegments.Add("markers=color:green|label:C|" + originCoord);
            markerSegments.Add("markers=color:red|label:D|" + destinationCoord);

            Random rand = new Random();

            if (items != null)
            {
                foreach (DataRow r in items.Rows)
                {
                    if (r["service_type"] == DBNull.Value)
                    {
                        continue;
                    }

                    string type = r["service_type"].ToString();

                    if (type == "Hotel")
                    {
                        string hotelCoord = OffsetCoordinate(destinationCoord, rand, 2.0);
                        markerSegments.Add("markers=color:blue|label:H|" + hotelCoord);
                    }
                    else if (type == "Event")
                    {
                        string eventCoord = OffsetCoordinate(destinationCoord, rand, 1.8);
                        markerSegments.Add("markers=color:orange|label:E|" + eventCoord);
                    }
                }
            }

            string pathSegment = "path=color:0x0000ff|weight:4|" + originCoord + "|" + destinationCoord;
            string markersJoined = string.Join("&", markerSegments.ToArray());

            string baseUrl = "https://maps.googleapis.com/maps/api/staticmap";

            string query = "size=1200x450"
                + "&center=" + centerCoord
                + "&zoom=4"
                + "&" + pathSegment
                + "&" + markersJoined
                + "&key=" + mapKey;

            string url = baseUrl + "?" + query;
            return url;
        }


        private string OffsetCoordinate(string baseCoord, Random rand, double maxOffsetDegrees)
        {
            if (string.IsNullOrWhiteSpace(baseCoord))
            {
                return baseCoord;
            }

            string[] parts = baseCoord.Split(',');
            if (parts.Length != 2)
            {
                return baseCoord;
            }

            double lat;
            double lng;

            if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lat))
            {
                return baseCoord;
            }

            if (!double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lng))
            {
                return baseCoord;
            }

            double latOffset = (rand.NextDouble() - 0.5) * maxOffsetDegrees;
            double lngOffset = (rand.NextDouble() - 0.5) * maxOffsetDegrees;

            double newLat = lat + latOffset;
            double newLng = lng + lngOffset;

            string latText = newLat.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
            string lngText = newLng.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);

            return latText + "," + lngText;
        }




    }
}
