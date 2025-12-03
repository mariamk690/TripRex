using CoreTripRex.Models;
using CoreTripRex.Models.Checkout;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TripRexLibraries;
using Utilities;

namespace CoreTripRex.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly StoredProcs _sp;
        private readonly UserManager<AppUser> _userManager;

        public CheckoutController(UserManager<AppUser> userManager)
        {
            _sp = new StoredProcs();
            _userManager = userManager;
        }

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
                        Email mail = new Email();

                        string userEmail = user.Email ?? "student@temple.edu";
                        string userName = user.FullName ?? "Traveler";

                        DataSet dsSummary = _sp.PackageGet(packageId);
                        DataTable items = dsSummary?.Tables.Count > 1 ? dsSummary.Tables[1] : null;

                        if (items == null || items.Rows.Count == 0)
                        {
                            model.MessageCssClass = "text-warning d-block mb-3";
                            model.Message = "Booking completed but no vacation details were found.";
                            return View(model);
                        }

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

                        mail.SendMail(userEmail, "tuo90411@temple.edu", subject, body);
                    }
                    catch { }

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

                List<PieSlice> slices = new List<PieSlice>();
                int i = 0;
                while (i < count)
                {
                    PieSlice slice = new PieSlice();
                    slice.Value = values[i];
                    slice.Label = labels[i];
                    slices.Add(slice);
                    i = i + 1;
                }

                ScottPlot.Plottables.Pie pie = plt.Add.Pie(slices);
                pie.SliceLabelDistance = 0.5;
                pie.ExplodeFraction = 0.05;

                plt.Axes.Frameless();
                plt.HideGrid();

                byte[] bytes = plt.GetImageBytes(600, 400, ScottPlot.ImageFormat.Png);
                string base64 = Convert.ToBase64String(bytes);
                model.ChartImageUrl = "data:image/png;base64," + base64;
            }

            model.Items = list;
            model.Total = total;

            return model;

        }
    }
}
