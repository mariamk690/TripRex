using CoreTripRex.Models.Checkout;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using CoreTripRex.Models;
using TripRexLibraries;
using Utilities;

namespace TripRex.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly StoredProcs _sp;

        public CheckoutController()
        {
            _sp = new StoredProcs();
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = LoadCheckoutViewModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(CheckoutViewModel form)
        {
            var model = LoadCheckoutViewModel();
            if (!model.HasMessage && model.Items.Count == 0)
            {
                model.HasMessage = true;
                model.MessageCssClass = "text-danger d-block mb-3";
                model.Message = "Your package is empty.";
                return View(model);
            }

            try
            {
                string userIdRaw = HttpContext.Session.GetString("UserID");
                if (string.IsNullOrEmpty(userIdRaw))
                {
                    model.HasMessage = true;
                    model.MessageCssClass = "text-danger d-block mb-3";
                    model.Message = "Please sign in before completing checkout.";
                    return View(model);
                }

                int userId = Convert.ToInt32(userIdRaw);

                int packageId = 0;
                string packageIdRaw = HttpContext.Session.GetString("PackageID");
                if (!string.IsNullOrEmpty(packageIdRaw))
                    packageId = Convert.ToInt32(packageIdRaw);
                if (packageId == 0)
                    packageId = _sp.PackageGetActive(userId);

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
                    string cardNum = form.CardNumber;
                    if (cardNum == null)
                        cardNum = string.Empty;
                    cardNum = cardNum.Replace(" ", "");

                    string exp = form.Exp;
                    if (exp == null)
                        exp = string.Empty;
                    exp = exp.Trim();

                    string brand = "Card";
                    if (cardNum.StartsWith("4"))
                        brand = "Visa";
                    else if (cardNum.StartsWith("5"))
                        brand = "Mastercard";

                    string last4 = "0000";
                    if (cardNum.Length >= 4)
                        last4 = cardNum.Substring(cardNum.Length - 4);

                    int expMonth = 0;
                    int expYear = 0;
                    if (exp.Contains("/"))
                    {
                        string[] parts = exp.Split('/');
                        if (parts.Length >= 2)
                        {
                            int.TryParse(parts[0], out expMonth);
                            int.TryParse(parts[1], out expYear);
                            if (expYear < 100)
                                expYear += 2000;
                        }
                    }

                    _sp.AddPaymentMethod(userId, brand, last4, expMonth, expYear, true);

                    DataSet ds = _sp.ListPaymentMethods(userId);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        paymentMethodId = Convert.ToInt32(ds.Tables[0].Rows[0]["Id"]);
                    }
                }
                else
                {
                    DataSet ds = _sp.ListPaymentMethods(userId);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow[] defaults = ds.Tables[0].Select("is_default = 1");
                        if (defaults.Length > 0)
                            paymentMethodId = Convert.ToInt32(defaults[0]["Id"]);
                        else
                            paymentMethodId = Convert.ToInt32(ds.Tables[0].Rows[0]["Id"]);
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

                        string userEmail = HttpContext.Session.GetString("UserEmail");
                        if (string.IsNullOrEmpty(userEmail))
                            userEmail = "student@temple.edu";

                        string userName = HttpContext.Session.GetString("UserName");
                        if (string.IsNullOrEmpty(userName))
                            userName = "Traveler";

                        DataSet dsSummary = _sp.PackageGet(packageId);
                        DataTable items = null;
                        if (dsSummary != null && dsSummary.Tables.Count > 1)
                            items = dsSummary.Tables[1];

                        if (items == null || items.Rows.Count == 0)
                        {
                            model.MessageCssClass = "text-warning d-block mb-3";
                            model.Message = "Booking completed but no vacation details were found.";
                            return View(model);
                        }

                        string tripStartRaw = HttpContext.Session.GetString("TripStart");
                        string tripEndRaw = HttpContext.Session.GetString("TripEnd");

                        DateTime tripStart;
                        DateTime tripEnd;

                        if (!DateTime.TryParse(tripStartRaw, out tripStart))
                            tripStart = DateTime.MinValue;
                        if (!DateTime.TryParse(tripEndRaw, out tripEnd))
                            tripEnd = DateTime.MinValue;

                        int totalDays = (tripEnd - tripStart).Days;
                        if (totalDays < 1)
                            totalDays = 1;

                        string detailsHtml = "<table style='border-collapse:collapse;width:100%;margin-top:10px' border='1' cellpadding='6'>" +
                                             "<tr style='background:#f2f2f2;text-align:left'>" +
                                             "<th>Item</th><th>Type</th><th>Dates</th><th>Qty</th><th>Cost</th>" +
                                             "</tr>";

                        decimal grandTotal = 0;

                        foreach (DataRow r in items.Rows)
                        {
                            string name = "—";
                            if (r["display_name"] != DBNull.Value)
                                name = r["display_name"].ToString();

                            string type = "—";
                            if (r["service_type"] != DBNull.Value)
                                type = r["service_type"].ToString();

                            decimal unitPrice = 0;
                            if (r["unit_price"] != DBNull.Value)
                                unitPrice = Convert.ToDecimal(r["unit_price"]);

                            decimal lineTotal = 0;
                            if (r["line_total"] != DBNull.Value)
                                lineTotal = Convert.ToDecimal(r["line_total"]);

                            decimal computedTotal = lineTotal;
                            string dates = "—";
                            string qtyLabel = "x1";

                            if (type == "Hotel" || type == "Car Rental")
                            {
                                computedTotal = unitPrice * totalDays;
                                dates = tripStart.ToString("MM/dd") + "–" + tripEnd.ToString("MM/dd");
                                if (type == "Hotel")
                                    qtyLabel = "x " + totalDays + " nights";
                                else
                                    qtyLabel = "x " + totalDays + " days";
                            }
                            else if (r["start_utc"] != DBNull.Value && r["end_utc"] != DBNull.Value)
                            {
                                DateTime startUtc = Convert.ToDateTime(r["start_utc"]);
                                DateTime endUtc = Convert.ToDateTime(r["end_utc"]);
                                dates = startUtc.ToString("MM/dd") + "–" + endUtc.ToString("MM/dd");
                            }

                            grandTotal += computedTotal;

                            detailsHtml += "<tr>" +
                                           "<td>" + name + "</td>" +
                                           "<td>" + type + "</td>" +
                                           "<td>" + dates + "</td>" +
                                           "<td>" + qtyLabel + "</td>" +
                                           "<td>$" + computedTotal.ToString("F2") + "</td>" +
                                           "</tr>";
                        }

                        detailsHtml += "<tr style='font-weight:bold;background:#f9f9f9'>" +
                                       "<td colspan='4' style='text-align:right'>Total</td>" +
                                       "<td>$" + grandTotal.ToString("F2") + "</td>" +
                                       "</tr></table>";

                        string subject = "TripRex Booking Confirmation";
                        string body = "<p>Hi " + userName + ",</p>" +
                                      "<p>Thanks for booking your trip with <strong>TripRex</strong>!</p>" +
                                      "<p>Here are your vacation details:</p>" +
                                      detailsHtml +
                                      "<p>We hope you have an amazing trip! 🌴</p>" +
                                      "<p>— The TripRex Team</p>";

                        mail.SendMail(userEmail, "tuo90411@temple.edu", subject, body);
                    }
                    catch (Exception)
                    {
                        model.MessageCssClass = "text-danger d-block mb-3";
                        model.Message = "Email send failed, but your booking was completed.";
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

        private CheckoutViewModel LoadCheckoutViewModel()
        {
            var model = new CheckoutViewModel();

            string userIdRaw = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdRaw))
            {
                model.HasMessage = true;
                model.MessageCssClass = "text-warning d-block mb-3";
                model.Message = "Please sign in to continue to checkout.";
                return model;
            }

            string userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                userName = "Traveler";
            model.UserGreeting = "Hi, " + userName + "!";

            int userId = Convert.ToInt32(userIdRaw);

            int packageId = 0;
            string packageIdRaw = HttpContext.Session.GetString("PackageID");
            if (!string.IsNullOrEmpty(packageIdRaw))
                packageId = Convert.ToInt32(packageIdRaw);
            if (packageId == 0)
                packageId = _sp.PackageGetActive(userId);

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

            DateTime tripStart;
            DateTime tripEnd;

            if (!DateTime.TryParse(tripStartRaw, out tripStart))
                tripStart = DateTime.MinValue;
            if (!DateTime.TryParse(tripEndRaw, out tripEnd))
                tripEnd = DateTime.MinValue;

            int totalDays = (tripEnd - tripStart).Days;
            if (totalDays < 1)
                totalDays = 1;

            decimal total = 0;
            var list = new List<CheckoutItemViewModel>();

            foreach (DataRow r in items.Rows)
            {
                string type = string.Empty;
                if (r["service_type"] != DBNull.Value)
                    type = r["service_type"].ToString();

                string name = "—";
                if (r["display_name"] != DBNull.Value)
                    name = r["display_name"].ToString();

                decimal unitPrice = 0;
                if (r["unit_price"] != DBNull.Value)
                    unitPrice = Convert.ToDecimal(r["unit_price"]);

                decimal lineTotal = 0;
                if (r["line_total"] != DBNull.Value)
                    lineTotal = Convert.ToDecimal(r["line_total"]);

                decimal computedTotal = 0;
                string dates = "—";
                string qtyLabel = "x 1";

                if (type == "Hotel" || type == "Car Rental")
                {
                    computedTotal = unitPrice * totalDays;
                    dates = tripStart.ToString("MM/dd") + "–" + tripEnd.ToString("MM/dd");
                    if (type == "Hotel")
                        qtyLabel = "x " + totalDays + " nights";
                    else
                        qtyLabel = "x " + totalDays + " days";
                    total += computedTotal;
                }
                else
                {
                    computedTotal = lineTotal;
                    if (r["start_utc"] != DBNull.Value && r["end_utc"] != DBNull.Value)
                    {
                        DateTime startUtc = Convert.ToDateTime(r["start_utc"]);
                        DateTime endUtc = Convert.ToDateTime(r["end_utc"]);
                        dates = startUtc.ToString("MM/dd") + "–" + endUtc.ToString("MM/dd");
                    }
                    qtyLabel = "x 1";
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

            model.Items = list;
            model.Total = total;

            return model;
        }
    }
}
