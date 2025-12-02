using CoreTripRex.Models;
using CoreTripRex.Models.AccountInfo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using TripRexLibraries;
using Utilities;

namespace CoreTripRex.Controllers
{
    public class AccountInfoController : Controller
    {
        private readonly StoredProcs _sp;
        private readonly UserManager<AppUser> _userManager;

        public AccountInfoController(UserManager<AppUser> userManager)
        {
            _sp = new StoredProcs();
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return RedirectToAction("Login", "Account");

            int userId = appUser.LegacyUserId;
            var vm = LoadProfile(userId);
            return View(vm);
        }

        private AccountInfoViewModel LoadProfile(int userId)
        {
            var vm = new AccountInfoViewModel();
            DataSet ds = _sp.GetProfile(userId);

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];

                vm.FirstName = dr["first_name"] == DBNull.Value ? "" : dr["first_name"].ToString();
                vm.LastName = dr["last_name"] == DBNull.Value ? "" : dr["last_name"].ToString();
                vm.Email = dr["email"] == DBNull.Value ? "" : dr["email"].ToString();
                vm.Phone = dr["phone"] == DBNull.Value ? "" : dr["phone"].ToString();
                vm.Address = dr["address"] == DBNull.Value ? "" : dr["address"].ToString();
                vm.City = dr["city"] == DBNull.Value ? "" : dr["city"].ToString();
                vm.State = dr["state"] == DBNull.Value ? "" : dr["state"].ToString();
                vm.Zip = dr["zip_code"] == DBNull.Value ? "" : dr["zip_code"].ToString();
                vm.Country = dr["country"] == DBNull.Value ? "" : dr["country"].ToString();
            }

            vm.UserId = userId;
            vm.PaymentMethods = LoadPayments(userId);
            vm.PastTrips = LoadTrips(userId);

            return vm;
        }

        private List<PaymentMethod> LoadPayments(int userId)
        {
            var list = new List<PaymentMethod>();
            DataSet ds = _sp.ListPaymentMethods(userId);

            if (ds.Tables.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    list.Add(new PaymentMethod
                    {
                        Id = dr["id"] == DBNull.Value ? 0 : Convert.ToInt32(dr["id"]),
                        Brand = dr["brand"] == DBNull.Value ? "" : dr["brand"].ToString(),
                        Last4 = dr["last4"] == DBNull.Value ? "" : dr["last4"].ToString(),
                        ExpMonth = dr["exp_month"] == DBNull.Value ? 0 : Convert.ToInt32(dr["exp_month"]),
                        ExpYear = dr["exp_year"] == DBNull.Value ? 0 : Convert.ToInt32(dr["exp_year"]),
                        IsDefault = Convert.ToBoolean(dr["is_default"])
                    });
                }
            }

            return list;
        }

        private List<TripPackage> LoadTrips(int userId)
        {
            var list = new List<TripPackage>();
            DataSet ds = _sp.PastPackages(userId);

            if (ds.Tables.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    list.Add(new TripPackage
                    {
                        Title = dr["title"] == DBNull.Value ? "" : dr["title"].ToString(),
                        StartDate = dr["start_date"] == DBNull.Value ? "" : dr["start_date"].ToString(),
                        EndDate = dr["end_date"] == DBNull.Value ? "" : dr["end_date"].ToString()
                    });
                }
            }

            return list;
        }

        [HttpPost]
        public async Task<IActionResult> Save(AccountInfoViewModel model)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return RedirectToAction("Login", "Account");

            int userId = appUser.LegacyUserId;

            int result = _sp.UpdateProfile(
                userId,
                model.FirstName,
                model.LastName,
                model.Email,
                model.Phone,
                model.Address,
                model.City,
                model.State,
                model.Zip,
                model.Country
            );

            var vm = LoadProfile(userId);
            vm.Message = result > 0 ? "Profile updated successfully." : "No changes were made.";

            return View("Index", vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddPayment(AccountInfoViewModel model)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return RedirectToAction("Login", "Account");

            int userId = appUser.LegacyUserId;

            string card = model.CardNumber ?? "";
            string last4 = card.Length >= 4 ? card[^4..] : card;

            string brand = DetectBrand(card);

            int expMonth = 0;
            int expYear = 0;

            if (!string.IsNullOrEmpty(model.Expiration) && model.Expiration.Contains("/"))
            {
                var parts = model.Expiration.Split("/");
                int.TryParse(parts[0], out expMonth);
                if (int.TryParse(parts[1], out expYear) && expYear < 100)
                    expYear += 2000;
            }

            _sp.AddPaymentMethod(userId, brand, last4, expMonth, expYear, false);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return RedirectToAction("Login", "Account");

            int userId = appUser.LegacyUserId;
            _sp.DeletePaymentMethod(userId, id);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SetDefault(int id)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return RedirectToAction("Login", "Account");

            int userId = appUser.LegacyUserId;
            _sp.SetDefaultPaymentMethod(userId, id);

            return RedirectToAction("Index");
        }

        private string DetectBrand(string card)
        {
            if (string.IsNullOrEmpty(card)) return "Unknown";
            if (card.StartsWith("4")) return "Visa";
            if (card.StartsWith("5")) return "Mastercard";
            if (card.StartsWith("3")) return "Amex";
            if (card.StartsWith("6")) return "Discover";
            return "Card";
        }
    }
}
