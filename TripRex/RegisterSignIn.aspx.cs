using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using TripRexLibraries;
using Utilities;

namespace TripRex
{
    public partial class RegisterSignIn : Page
    {
        StoredProcs sp = new StoredProcs();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string mode = Request.QueryString["mode"];

                if (!string.IsNullOrEmpty(mode) && mode.ToLower() == "register")
                    ShowRegister();
                else
                    ShowLogin();
            }
        }

        private void ShowLogin()
        {
            loginPanel.Visible = true;
            registerPanel.Visible = false;
            lblLoginError.Text = "";
            lblRegisterError.Text = "";
        }

        private void ShowRegister()
        {
            loginPanel.Visible = false;
            registerPanel.Visible = true;
            lblLoginError.Text = "";
            lblRegisterError.Text = "";
        }

        protected void lnkShowRegister_Click(object sender, EventArgs e)
        {
            ShowRegister();
        }

        protected void lnkShowLogin_Click(object sender, EventArgs e)
        {
            ShowLogin();
        }

        // --- LOGIN ---
        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string email = txtLoginEmail.Text.Trim();
            string password = txtLoginPassword.Text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                lblLoginError.Text = "Please enter your email and password.";
                return;
            }

            byte[] passwordHash = ComputeHash(password);

            int userId = sp.AccountLogin(email, passwordHash);

            if (userId > 0)
            {
                Session["UserID"] = userId;
                Session["UserEmail"] = email;
                Response.Redirect("~/Dashboard.aspx");
            }
            else
            {
                lblLoginError.Text = "Invalid email or password.";
            }
        }


        // --- REGISTER ---
        protected void btnRegister_Click(object sender, EventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string firstName = "";
            string lastName = "";

            if (!string.IsNullOrEmpty(fullName))
            {
                string[] parts = fullName.Split(' ');
                firstName = parts[0];
                if (parts.Length > 1)
                    lastName = parts[1];
            }

            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string pass1 = txtPassword.Text.Trim();
            string pass2 = txtRepeatPassword.Text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass1) || string.IsNullOrEmpty(pass2))
            {
                lblRegisterError.Text = "All required fields must be filled.";
                return;
            }

            if (pass1 != pass2)
            {
                lblRegisterError.Text = "Passwords do not match.";
                return;
            }

            byte[] passwordHash = ComputeHash(pass1);

            try
            {
                int userId = sp.AccountRegister("Registered", firstName, lastName, email, passwordHash, phone);

                if (userId > 0)
                {
                    Session["UserID"] = userId;
                    Session["UserEmail"] = email;
                    Response.Redirect("~/Dashboard.aspx");
                }
                else
                {
                    lblRegisterError.Text = "Registration failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Email already registered"))
                    lblRegisterError.Text = "That email is already registered.";
                else
                    lblRegisterError.Text = "An unexpected database error occurred.";
            }
        }

        private byte[] ComputeHash(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }
    }
}
