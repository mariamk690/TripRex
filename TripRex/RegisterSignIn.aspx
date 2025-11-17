<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RegisterSignIn.aspx.cs" Inherits="TripRex.RegisterSignIn" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5">
        <div class="row g-4">

            <!-- Login -->
            <div class="col-lg-6" id="loginPanel" runat="server">
                <div class="card p-4">
                    <h2 class="h5 mb-3">Ready to Explore? Log in</h2>
                    <div class="mb-3">
                        <label class="form-label">Email</label>
                        <asp:TextBox ID="txtLoginEmail" runat="server" CssClass="form-control" />
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Password</label>
                        <asp:TextBox ID="txtLoginPassword" runat="server" CssClass="form-control" TextMode="Password" />
                    </div>
                    <asp:Button ID="btnLogin" runat="server" CssClass="btn btn-primary w-100" Text="Login" OnClick="btnLogin_Click" />
                    <p class="small text-secondary mt-3">
                        Don’t have an account?
          <asp:LinkButton ID="lnkShowRegister" runat="server" OnClick="lnkShowRegister_Click">Sign up here</asp:LinkButton>
                    </p>
                    <asp:Label ID="lblLoginError" runat="server" CssClass="text-danger small mt-2 d-block" />
                </div>
            </div>

            <!-- Register -->
            <div class="col-lg-6" id="registerPanel" runat="server" visible="false">
                <div class="card p-4">
                    <h2 class="h5 mb-3">Start your journey — Sign up</h2>
                    <div class="row g-3">
                        <div class="col-12">
                            <label class="form-label">Full Name</label>
                            <asp:TextBox ID="txtFullName" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-12">
                            <label class="form-label">Email</label>
                            <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" TextMode="Email" />
                        </div>
                        <div class="col-12">
                            <label class="form-label">Phone</label>
                            <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Password</label>
                            <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" TextMode="Password" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Repeat Password</label>
                            <asp:TextBox ID="txtRepeatPassword" runat="server" CssClass="form-control" TextMode="Password" />
                        </div>
                        <div class="col-12">
                            <asp:Button ID="btnRegister" runat="server" CssClass="btn btn-success w-100" Text="Sign Up" OnClick="btnRegister_Click" />
                        </div>
                        <div class="col-12">
                            <asp:Label ID="lblRegisterError" runat="server" CssClass="text-danger small" />
                        </div>
                        <div class="col-12 mt-2">
                            <asp:LinkButton ID="lnkShowLogin" runat="server" OnClick="lnkShowLogin_Click">Back to Login</asp:LinkButton>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-lg-6 d-flex flex-column align-items-center justify-content-center text-center">
                <img src="<%: ResolveUrl("~/Images/Tiny.png") %>"
                    alt="TripRex Logo"
                    class="img-fluid mb-3"
                    style="max-width: 320px; height: auto; filter: drop-shadow(0 4px 10px rgba(0,0,0,0.2));" />

                <p class="fw-semibold text-success" style="font-size: 1.1rem;">
                    Have a Roarsome Trip! — Tiny the T-Rex
                </p>
            </div>

        </div>
    </div>
</asp:Content>
