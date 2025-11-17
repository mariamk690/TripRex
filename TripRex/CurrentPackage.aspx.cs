using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using TripRexLibraries;
using Utilities;

namespace TripRex
{
    public partial class CurrentPackage : Page
    {
        StoredProcs sp = new StoredProcs();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                LoadPackage();
        }

        private void LoadPackage()
        {
            if (Session["UserID"] == null)
            {
                lblMessage.Text = "Please sign in to view your vacation package.";
                lblMessage.Visible = true;
                pnlPackage.Visible = false;
                return;
            }

            int userId = Convert.ToInt32(Session["UserID"]);
            int packageId = sp.PackageGetOrCreate(userId);

            DataSet ds = sp.PackageGet(packageId); 

            if (ds == null || ds.Tables.Count < 2 || ds.Tables[1].Rows.Count == 0)
            {
                lblMessage.Text = "Your package is currently empty.";
                lblMessage.Visible = true;
                pnlPackage.Visible = false;
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

            rptPackageItems.DataSource = displayTable;
            rptPackageItems.DataBind();

            lblTotal.Text = $"${total:F2}";
            pnlPackage.Visible = true;
            lblMessage.Visible = false;


            lblTotal.Text = $"${total:F2}";
            pnlPackage.Visible = true;
            lblMessage.Visible = false;
        }

        protected void rptPackageItems_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "RemoveItem")
            {
                try
                {
                    if (Session["UserID"] == null)
                    {
                        lblMessage.Text = "Please sign in first.";
                        lblMessage.Visible = true;
                        return;
                    }

                    int userId = Convert.ToInt32(Session["UserID"]);
                    int packageId = sp.PackageGetOrCreate(userId);

                    string[] parts = e.CommandArgument.ToString().Split('|');
                    int refId = Convert.ToInt32(parts[0]);
                    string serviceType = parts[1];

                    sp.PackageAddUpdateItem(packageId, serviceType, refId, 0, null, null);

                    LoadPackage();
                }
                catch (Exception ex)
                {
                    lblMessage.Text = "Error removing item: " + ex.Message;
                    lblMessage.Visible = true;
                }
            }
        }
    }
}
