using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using TripRexLibraries;
using Utilities;

namespace TripRex
{
    public partial class AccountInfo : Page
    {
        StoredProcs sp = new StoredProcs();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                LoadProfile();
        }

        private void LoadProfile()
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/RegisterSignIn.aspx?mode=login");
                return;
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // Load user profile
            DataSet dsProfile = sp.GetProfile(userId);
            if (dsProfile.Tables.Count > 0 && dsProfile.Tables[0].Rows.Count > 0)
            {
                DataRow dr = dsProfile.Tables[0].Rows[0];
                txtFirstName.Text = dr["first_name"] != DBNull.Value ? dr["first_name"].ToString() : "";
                txtLastName.Text = dr["last_name"] != DBNull.Value ? dr["last_name"].ToString() : "";
                txtEmail.Text = dr["email"] != DBNull.Value ? dr["email"].ToString() : "";
                txtPhone.Text = dr["phone"] != DBNull.Value ? dr["phone"].ToString() : "";
                txtAddress.Text = dr["address"] != DBNull.Value ? dr["address"].ToString() : "";
                txtCity.Text = dr["city"] != DBNull.Value ? dr["city"].ToString() : "";
                txtState.Text = dr["state"] != DBNull.Value ? dr["state"].ToString() : "";
                txtZip.Text = dr["zip_code"] != DBNull.Value ? dr["zip_code"].ToString() : "";
                txtCountry.Text = dr["country"] != DBNull.Value ? dr["country"].ToString() : "";
            }

            // Load payment methods
            LoadPaymentMethods(userId);

            // Load past trips
            DataSet dsTrips = sp.PastPackages(userId);
            if (dsTrips.Tables.Count > 0 && dsTrips.Tables[0].Rows.Count > 0)
            {
                lvPastTrips.DataSource = dsTrips.Tables[0];
                lvPastTrips.DataBind();
            }
            else
            {
                lvPastTrips.DataSource = null;
                lvPastTrips.DataBind();
            }

        }

        private void LoadPaymentMethods(int userId)
        {
            DataSet dsPayments = sp.ListPaymentMethods(userId);
            if (dsPayments.Tables.Count > 0)
            {
                rptPaymentMethods.DataSource = dsPayments.Tables[0];
                rptPaymentMethods.DataBind();
            }
            else
            {
                rptPaymentMethods.DataSource = null;
                rptPaymentMethods.DataBind();
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            int result = sp.UpdateProfile(
                userId,
                txtFirstName.Text.Trim(),
                txtLastName.Text.Trim(),
                txtEmail.Text.Trim(),
                txtPhone.Text.Trim(),
                txtAddress.Text.Trim(),
                txtCity.Text.Trim(),
                txtState.Text.Trim(),
                txtZip.Text.Trim(),
                txtCountry.Text.Trim()
            );

            lblMessage.Text = result > 0 ? "Profile updated successfully." : "No changes were made.";
            lblMessage.Visible = true;
        }

        protected void btnAddPayment_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
                return;

            int userId = Convert.ToInt32(Session["UserID"]);

            string cardNumber = txtCardNum.Text.Trim();
            string last4 = "";

            if (!string.IsNullOrEmpty(cardNumber) && cardNumber.Length >= 4)
                last4 = cardNumber.Substring(cardNumber.Length - 4);
            else
                last4 = cardNumber;

            string brand = DetectCardBrand(cardNumber);

            int? expMonth = null;
            int? expYear = null;

            string expInput = txtExp.Text.Trim();
            if (!string.IsNullOrEmpty(expInput) && expInput.Contains("/"))
            {
                string[] parts = expInput.Split('/');
                int m, y;
                if (int.TryParse(parts[0], out m)) expMonth = m;
                if (int.TryParse(parts[1], out y))
                {
                    if (y < 100) y += 2000;
                    expYear = y;
                }
            }

            sp.AddPaymentMethod(userId, brand, last4, expMonth, expYear, false);
            LoadPaymentMethods(userId);
        }

        protected void rptPaymentMethods_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (Session["UserID"] == null)
                return;

            int userId = Convert.ToInt32(Session["UserID"]);
            int methodId = Convert.ToInt32(e.CommandArgument);

            if (e.CommandName == "Delete")
            {
                sp.DeletePaymentMethod(userId, methodId);
            }
            else if (e.CommandName == "SetDefault")
            {
                sp.SetDefaultPaymentMethod(userId, methodId);
            }

            LoadPaymentMethods(userId);
        }

        private string DetectCardBrand(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return "Unknown";

            if (cardNumber.StartsWith("4")) return "Visa";
            if (cardNumber.StartsWith("5")) return "Mastercard";
            if (cardNumber.StartsWith("3")) return "Amex";
            if (cardNumber.StartsWith("6")) return "Discover";

            return "Card";
        }
    }
}
