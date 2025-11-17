using System;
using System.Web.UI;

namespace TripRex
{
    public partial class Site : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ToggleUserControls();
            }
        }

        private void ToggleUserControls()
        {
            // check if the user is logged in
            if (Session["UserID"] != null)
            {
                pnlGuest.Visible = false;
                pnlUser.Visible = true;

                // Prefer first name if you stored it
                string displayName = "User";

                if (Session["FullName"] != null)
                    displayName = Session["FullName"].ToString().Split(' ')[0];
                else if (Session["UserEmail"] != null)
                    displayName = Session["UserEmail"].ToString();

                lblUserName.Text = $"Hi, {displayName}";
            }
            else
            {
                pnlGuest.Visible = true;
                pnlUser.Visible = false;
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Response.Redirect("~/Dashboard.aspx");
        }
    }
}
