using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using TripRexLibraries;
using Utilities;

namespace TripRex
{
    public partial class Checkout : Page
    {
        StoredProcs sp = new StoredProcs();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                LoadCheckout();
        }

        private void LoadCheckout()
        {
            if (Session["UserID"] == null)
            {
                lblMessage.Text = "Please sign in to continue to checkout.";
                lblMessage.Visible = true;
                return;
            }

            lblUser.Text = "Hi, " + (Session["UserName"] != null
                            ? Session["UserName"].ToString()
                            : "Traveler") + "!";

            int userId = Convert.ToInt32(Session["UserID"]);
            int packageId = Session["PackageID"] != null
                ? Convert.ToInt32(Session["PackageID"])
                : sp.PackageGetActive(userId);
            if (packageId == 0)
            {
                lblMessage.Text = "No active package found — add items before checkout.";
                lblMessage.Visible = true;
                return;
            }

            DataSet ds = sp.PackageGet(packageId);
            if (ds == null || ds.Tables.Count < 2 || ds.Tables[1].Rows.Count == 0)
            {
                lblMessage.Text = "Your package is empty — add items before checkout.";
                lblMessage.Visible = true;
                return;
            }

            DataTable items = ds.Tables[1];
            decimal total = 0;

            DateTime tripStart = Session["TripStart"] != null ? Convert.ToDateTime(Session["TripStart"]) : DateTime.MinValue;
            DateTime tripEnd = Session["TripEnd"] != null ? Convert.ToDateTime(Session["TripEnd"]) : DateTime.MinValue;
            int totalDays = (tripEnd - tripStart).Days;
            if (totalDays < 1) totalDays = 1;

            DataTable displayTable = items.Clone();
            displayTable.Columns.Add("computed_total", typeof(decimal));
            displayTable.Columns.Add("computed_dates", typeof(string));
            displayTable.Columns.Add("computed_qty_label", typeof(string));

            foreach (DataRow r in items.Rows)
            {
                DataRow newRow = displayTable.NewRow();
                newRow.ItemArray = r.ItemArray;

                string type = r["service_type"] != DBNull.Value ? r["service_type"].ToString() : "";
                decimal unitPrice = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0m;
                decimal lineTotal = r["line_total"] != DBNull.Value ? Convert.ToDecimal(r["line_total"]) : 0m;

                if (type == "Hotel" || type == "Car Rental")
                {
                    decimal computed = unitPrice * totalDays;
                    newRow["computed_total"] = computed;
                    newRow["computed_dates"] = $"{tripStart:MM/dd}–{tripEnd:MM/dd}";
                    newRow["computed_qty_label"] = $"x {totalDays} {(type == "Hotel" ? "nights" : "days")}";
                    total += computed;
                }
                else
                {
                    newRow["computed_total"] = lineTotal;
                    if (r["start_utc"] != DBNull.Value && r["end_utc"] != DBNull.Value)
                        newRow["computed_dates"] = $"{Convert.ToDateTime(r["start_utc"]):MM/dd}–{Convert.ToDateTime(r["end_utc"]):MM/dd}";
                    else
                        newRow["computed_dates"] = "—";
                    newRow["computed_qty_label"] = "x 1";
                    total += lineTotal;
                }

                displayTable.Rows.Add(newRow);
            }

            rptItems.DataSource = displayTable;
            rptItems.DataBind();

            lblTotal.Text = $"${total:F2}";
        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    lblMessage.Text = "Please sign in before completing checkout.";
                    lblMessage.Visible = true;
                    return;
                }

                int userId = Convert.ToInt32(Session["UserID"]);
                int packageId = Session["PackageID"] != null
                    ? Convert.ToInt32(Session["PackageID"])
                    : sp.PackageGetActive(userId);

                if (packageId == 0)
                {
                    lblMessage.Text = "No active package found to book.";
                    lblMessage.Visible = true;
                    return;
                }

                DataSet dsCheck = sp.PackageGet(packageId);
                if (dsCheck == null || dsCheck.Tables.Count < 2 || dsCheck.Tables[1].Rows.Count == 0)
                {
                    lblMessage.Text = "Your package is empty — please add vacation items before booking.";
                    lblMessage.Visible = true;
                    return;
                }

                int paymentMethodId = -1;

                if (rdoAddCard.Checked)
                {
                    string cardNum = txtCardNumber.Text.Replace(" ", "");
                    string exp = txtExp.Text.Trim();
                    string brand = cardNum.StartsWith("4") ? "Visa" :
                                   cardNum.StartsWith("5") ? "Mastercard" : "Card";
                    string last4 = cardNum.Length >= 4 ? cardNum.Substring(cardNum.Length - 4) : "0000";

                    int expMonth = 0, expYear = 0;
                    if (exp.Contains("/"))
                    {
                        string[] parts = exp.Split('/');
                        int.TryParse(parts[0], out expMonth);
                        int.TryParse(parts[1], out expYear);
                        if (expYear < 100) expYear += 2000;
                    }

                    sp.AddPaymentMethod(userId, brand, last4, expMonth, expYear, true);

                    DataSet ds = sp.ListPaymentMethods(userId);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        paymentMethodId = Convert.ToInt32(ds.Tables[0].Rows[0]["Id"]);
                    }
                }
                else
                {
                    DataSet ds = sp.ListPaymentMethods(userId);
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
                    throw new Exception("No valid payment method found or created.");

                int result = sp.CheckoutPackage(packageId, paymentMethodId, "USD", "WEB-" + DateTime.UtcNow.Ticks);

                lblMessage.Visible = true;
                if (result > 0)
                {
                    lblMessage.CssClass = "text-success d-block mb-3";
                    lblMessage.Text = "Your trip has been booked successfully! 🎉";

                    try
                    {
                        Email mail = new Email();

                        string userEmail = Session["UserEmail"]?.ToString() ?? "student@temple.edu";
                        string userName = Session["UserName"]?.ToString() ?? "Traveler";

                        DataSet dsSummary = sp.PackageGet(packageId);
                        DataTable items = (dsSummary != null && dsSummary.Tables.Count > 1)
                                            ? dsSummary.Tables[1]
                                            : null;

                        if (items == null || items.Rows.Count == 0)
                        {
                            lblMessage.Text = "Booking completed but no vacation details were found.";
                            lblMessage.CssClass = "text-warning d-block mb-3";
                            return;
                        }

                        string detailsHtml = "";
                        decimal grandTotal = 0;

                        DateTime tripStart = Session["TripStart"] != null ? Convert.ToDateTime(Session["TripStart"]) : DateTime.MinValue;
                        DateTime tripEnd = Session["TripEnd"] != null ? Convert.ToDateTime(Session["TripEnd"]) : DateTime.MinValue;
                        int totalDays = (tripEnd - tripStart).Days;
                        if (totalDays < 1) totalDays = 1;

                        detailsHtml = "<table style='border-collapse:collapse;width:100%;margin-top:10px' border='1' cellpadding='6'>" +
                                      "<tr style='background:#f2f2f2;text-align:left'>" +
                                      "<th>Item</th><th>Type</th><th>Dates</th><th>Qty</th><th>Cost</th>" +
                                      "</tr>";

                        foreach (DataRow r in items.Rows)
                        {
                            string name = r["display_name"]?.ToString() ?? "—";
                            string type = r["service_type"]?.ToString() ?? "—";
                            decimal unitPrice = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0m;
                            decimal lineTotal = r["line_total"] != DBNull.Value ? Convert.ToDecimal(r["line_total"]) : 0m;

                            decimal computedTotal = lineTotal;
                            string dates = "—";
                            string qtyLabel = "x1";

                            if (type == "Hotel" || type == "Car Rental")
                            {
                                computedTotal = unitPrice * totalDays;
                                dates = $"{tripStart:MM/dd}–{tripEnd:MM/dd}";
                                qtyLabel = $"x {totalDays} {(type == "Hotel" ? "nights" : "days")}";
                            }
                            else if (r["start_utc"] != DBNull.Value && r["end_utc"] != DBNull.Value)
                            {
                                dates = $"{Convert.ToDateTime(r["start_utc"]):MM/dd}–{Convert.ToDateTime(r["end_utc"]):MM/dd}";
                            }

                            grandTotal += computedTotal;

                            detailsHtml += $"<tr>" +
                                           $"<td>{name}</td>" +
                                           $"<td>{type}</td>" +
                                           $"<td>{dates}</td>" +
                                           $"<td>{qtyLabel}</td>" +
                                           $"<td>${computedTotal:F2}</td>" +
                                           $"</tr>";
                        }

                        detailsHtml += $"<tr style='font-weight:bold;background:#f9f9f9'>" +
                                       $"<td colspan='4' style='text-align:right'>Total</td>" +
                                       $"<td>${grandTotal:F2}</td>" +
                                       $"</tr></table>";

                        string subject = "TripRex Booking Confirmation";
                        string body = $@"
                            <p>Hi {userName},</p>
                            <p>Thanks for booking your trip with <strong>TripRex</strong>!</p>
                            <p>Here are your vacation details:</p>
                            {detailsHtml}
                            <p>We hope you have an amazing trip! 🌴</p>
                            <p>— The TripRex Team</p>
                        ";

                        mail.SendMail(userEmail, "tuo90411@temple.edu", subject, body);
                    }
                    catch (Exception mailEx)
                    {
                        lblMessage.Visible = true;
                        lblMessage.CssClass = "text-danger d-block mb-3";
                        lblMessage.Text = "Email send failed: " + Server.HtmlEncode(mailEx.ToString());
                    }


                    //   sp.PackageClear(packageId);
                    Session.Remove("PackageID");
                }
                else
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = "Checkout failed (code " + result + ").";
                }
            }
            catch (Exception ex)
            {
                lblMessage.Visible = true;
                lblMessage.CssClass = "text-danger d-block mb-3";
                lblMessage.Text = "Checkout error: " + Server.HtmlEncode(ex.Message);
                System.Diagnostics.Debug.WriteLine("Checkout error: " + ex.ToString());
            }
        }
    }
}
