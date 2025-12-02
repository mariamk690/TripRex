using CoreTripRex.Models.CurrentPackage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using TripRexLibraries;
using Utilities;

namespace CoreTripRex.Controllers
{
    [Authorize]
    public class CurrentPackageController : Controller
    {
        private readonly StoredProcs _sp = new StoredProcs();

        [HttpGet]
        public IActionResult Index()
        {
            var model = BuildCurrentPackageViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int refId, string serviceType)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Message"] = "Please sign in first.";
                    return RedirectToAction("Index");
                }

                int packageId = _sp.PackageGetOrCreate(userId.Value);

                // Same as WebForms: set qty = 0 to remove
                _sp.PackageAddUpdateItem(packageId, serviceType, refId, 0, null, null);

                TempData["Message"] = null;
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error removing item: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // --- logic ported from LoadPackage() ---
        private CurrentPackageVM BuildCurrentPackageViewModel()
        {
            var model = new CurrentPackageVM();

            if (TempData.ContainsKey("Message"))
            {
                model.Message = TempData["Message"] as string;
            }

            // Equivalent of Session["UserID"] check
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                if (string.IsNullOrEmpty(model.Message))
                    model.Message = "Please sign in to view your vacation package.";

                model.HasPackage = false;
                return model;
            }

            int packageId = _sp.PackageGetOrCreate(userId.Value);
            DataSet ds = _sp.PackageGet(packageId);

            if (ds == null || ds.Tables.Count < 2 || ds.Tables[1].Rows.Count == 0)
            {
                if (string.IsNullOrEmpty(model.Message))
                    model.Message = "Your package is currently empty.";
                model.HasPackage = false;
                return model;
            }

            DataTable items = ds.Tables[1];

            string tripStartStr = HttpContext.Session.GetString("TripStart");
            string tripEndStr = HttpContext.Session.GetString("TripEnd");

            DateTime tripStart = !string.IsNullOrEmpty(tripStartStr)
                ? Convert.ToDateTime(tripStartStr)
                : DateTime.MinValue;

            DateTime tripEnd = !string.IsNullOrEmpty(tripEndStr)
                ? Convert.ToDateTime(tripEndStr)
                : DateTime.MinValue;

            int totalDays = (tripEnd - tripStart).Days;
            if (totalDays < 1) totalDays = 1;

            decimal total = 0;

            foreach (DataRow r in items.Rows)
            {
                var item = new CurrentPackageItemVM
                {
                    ServiceType = r["service_type"] != DBNull.Value ? r["service_type"].ToString() : "",
                    DisplayName = r["display_name"] != DBNull.Value ? r["display_name"].ToString() : "",
                    Details = r["details"] != DBNull.Value ? r["details"].ToString() : "",
                    RefId = r["ref_id"] != DBNull.Value ? Convert.ToInt32(r["ref_id"]) : 0
                };

                string type = item.ServiceType;
                decimal unitPrice = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0m;
                decimal lineTotal = r["line_total"] != DBNull.Value ? Convert.ToDecimal(r["line_total"]) : 0m;

                if ((type == "Hotel" || type == "Car Rental") && tripStart != DateTime.MinValue && tripEnd != DateTime.MinValue)
                {
                    decimal computed = unitPrice * totalDays;
                    item.ComputedTotal = computed;
                    item.ComputedDates = $"{tripStart:MM/dd}–{tripEnd:MM/dd}";
                    item.ComputedQtyLabel = $"x {totalDays} {(type == "Hotel" ? "nights" : "days")}";
                    total += computed;
                }
                else
                {
                    item.ComputedTotal = lineTotal;

                    if (r["start_utc"] != DBNull.Value && r["end_utc"] != DBNull.Value)
                    {
                        DateTime startUtc = Convert.ToDateTime(r["start_utc"]);
                        DateTime endUtc = Convert.ToDateTime(r["end_utc"]);
                        item.ComputedDates = $"{startUtc:MM/dd}–{endUtc:MM/dd}";
                    }
                    else
                    {
                        item.ComputedDates = "—";
                    }

                    item.ComputedQtyLabel = "x 1";
                    total += lineTotal;
                }

                model.Items.Add(item);
            }

            model.Total = total;
            model.HasPackage = true;
            return model;
        }
    }
}
