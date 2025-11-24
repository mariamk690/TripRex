using Microsoft.AspNetCore.Mvc;
using CoreTripRex.Models.AccountInfo;
using TripRexLibraries;
using System.Data;
using Utilities;

namespace CoreTripRex.Controllers
{
    public class AccountInfoController : Controller
    {
        private StoredProcs _sp;

        public AccountInfoController()
        {
            _sp = new StoredProcs();
        }

        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            AccountInfoViewModel vm = LoadProfile(userId.Value);
            return View(vm);
        }

        private AccountInfoViewModel LoadProfile(int userId)
        {
            AccountInfoViewModel vm = new AccountInfoViewModel();

            DataSet ds = _sp.GetProfile(userId);

            if (ds.Tables.Count > 0)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = ds.Tables[0].Rows[0];

                    if (dr["first_name"] == DBNull.Value)
                        vm.FirstName = "";
                    else
                        vm.FirstName = dr["first_name"].ToString();

                    if (dr["last_name"] == DBNull.Value)
                        vm.LastName = "";
                    else
                        vm.LastName = dr["last_name"].ToString();

                    if (dr["email"] == DBNull.Value)
                        vm.Email = "";
                    else
                        vm.Email = dr["email"].ToString();

                    if (dr["phone"] == DBNull.Value)
                        vm.Phone = "";
                    else
                        vm.Phone = dr["phone"].ToString();

                    if (dr["address"] == DBNull.Value)
                        vm.Address = "";
                    else
                        vm.Address = dr["address"].ToString();

                    if (dr["city"] == DBNull.Value)
                        vm.City = "";
                    else
                        vm.City = dr["city"].ToString();

                    if (dr["state"] == DBNull.Value)
                        vm.State = "";
                    else
                        vm.State = dr["state"].ToString();

                    if (dr["zip_code"] == DBNull.Value)
                        vm.Zip = "";
                    else
                        vm.Zip = dr["zip_code"].ToString();

                    if (dr["country"] == DBNull.Value)
                        vm.Country = "";
                    else
                        vm.Country = dr["country"].ToString();
                }
            }

            vm.UserId = userId;
            vm.PaymentMethods = LoadPayments(userId);
            vm.PastTrips = LoadTrips(userId);

            return vm;
        }

        private List<PaymentMethod> LoadPayments(int userId)
        {
            List<PaymentMethod> list = new List<PaymentMethod>();

            DataSet ds = _sp.ListPaymentMethods(userId);

            if (ds.Tables.Count > 0)
            {
                DataTable t = ds.Tables[0];
                int i = 0;
                while (i < t.Rows.Count)
                {
                    DataRow dr = t.Rows[i];

                    PaymentMethod p = new PaymentMethod();

                    if (dr["id"] == DBNull.Value)
                        p.Id = 0;
                    else
                        p.Id = Convert.ToInt32(dr["id"]);

                    if (dr["brand"] == DBNull.Value)
                        p.Brand = "";
                    else
                        p.Brand = dr["brand"].ToString();

                    if (dr["last4"] == DBNull.Value)
                        p.Last4 = "";
                    else
                        p.Last4 = dr["last4"].ToString();

                    if (dr["exp_month"] == DBNull.Value)
                        p.ExpMonth = 0;
                    else
                        p.ExpMonth = Convert.ToInt32(dr["exp_month"]);

                    if (dr["exp_year"] == DBNull.Value)
                        p.ExpYear = 0;
                    else
                        p.ExpYear = Convert.ToInt32(dr["exp_year"]);

                    p.IsDefault = Convert.ToBoolean(dr["is_default"]);

                    list.Add(p);
                    i++;
                }
            }

            return list;
        }

        private List<TripPackage> LoadTrips(int userId)
        {
            List<TripPackage> list = new List<TripPackage>();

            DataSet ds = _sp.PastPackages(userId);

            if (ds.Tables.Count > 0)
            {
                DataTable t = ds.Tables[0];
                int i = 0;
                while (i < t.Rows.Count)
                {
                    DataRow dr = t.Rows[i];

                    TripPackage tp = new TripPackage();

                    if (dr["title"] == DBNull.Value)
                        tp.Title = "";
                    else
                        tp.Title = dr["title"].ToString();

                    if (dr["start_date"] == DBNull.Value)
                        tp.StartDate = "";
                    else
                        tp.StartDate = dr["start_date"].ToString();

                    if (dr["end_date"] == DBNull.Value)
                        tp.EndDate = "";
                    else
                        tp.EndDate = dr["end_date"].ToString();

                    list.Add(tp);
                    i++;
                }
            }

            return list;
        }

        [HttpPost]
        public IActionResult Save(AccountInfoViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            int result = _sp.UpdateProfile(
                userId.Value,
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

            AccountInfoViewModel vm = LoadProfile(userId.Value);

            if (result > 0)
                vm.Message = "Profile updated successfully.";
            else
                vm.Message = "No changes were made.";

            return View("Index", vm);
        }

        [HttpPost]
        public IActionResult AddPayment(AccountInfoViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            string card = model.CardNumber;
            if (card == null)
                card = "";

            int len = card.Length;
            string last4 = card;
            if (len >= 4)
                last4 = card.Substring(len - 4, 4);

            string brand = DetectBrand(card);

            int expMonth = 0;
            int expYear = 0;

            string exp = model.Expiration;
            if (exp != null)
            {
                if (exp.Contains("/"))
                {
                    string[] parts = exp.Split("/");
                    int m;
                    int y;

                    if (int.TryParse(parts[0], out m))
                        expMonth = m;

                    if (int.TryParse(parts[1], out y))
                    {
                        if (y < 100)
                            y = y + 2000;

                        expYear = y;
                    }
                }
            }

            _sp.AddPaymentMethod(userId.Value, brand, last4, expMonth, expYear, false);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeletePayment(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            _sp.DeletePaymentMethod(userId.Value, id);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult SetDefault(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            _sp.SetDefaultPaymentMethod(userId.Value, id);

            return RedirectToAction("Index");
        }

        private string DetectBrand(string card)
        {
            if (card == null)
                return "Unknown";

            if (card.StartsWith("4"))
                return "Visa";

            if (card.StartsWith("5"))
                return "Mastercard";

            if (card.StartsWith("3"))
                return "Amex";

            if (card.StartsWith("6"))
                return "Discover";

            return "Card";
        }
    }
}
