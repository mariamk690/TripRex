<%@ Page Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="CurrentPackage.aspx.cs"
    Inherits="TripRex.CurrentPackage" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5">
        <h1 class="h4 mb-4">Your Current Package</h1>

        <asp:Label ID="lblMessage" runat="server"
            CssClass="text-danger mb-3 d-block" Visible="false" />

        <asp:Panel ID="pnlPackage" runat="server" Visible="false">
            <div class="row g-4">
                <div class="col-lg-8">
                    <asp:Repeater ID="rptPackageItems" runat="server" OnItemCommand="rptPackageItems_ItemCommand">
                        <ItemTemplate>
                            <div class="card p-3 mb-3">
                                <div class="fw-semibold"><%# Eval("service_type") %></div>
                                <div class="text-muted small"><%# Eval("display_name") %></div>
                                <div class="text-secondary small mb-1"><%# Eval("details") %></div>
                                <div class="text-secondary small"><%# Eval("computed_dates") %> <span class="text-muted"><%# Eval("computed_qty_label") %></span></div>
                                <div class="fw-bold">$<%# Eval("computed_total", "{0:F2}") %></div>


                                <asp:Button ID="btnRemove" runat="server"
                                    CommandName="RemoveItem"
                                    CommandArgument='<%# Eval("ref_id") + "|" + Eval("service_type") %>'
                                    CssClass="btn btn-sm btn-outline-danger mt-2"
                                    Text="Remove" />
                            </div>
                        </ItemTemplate>

                    </asp:Repeater>
                </div>

                <div class="col-lg-4">
                    <div class="card p-4 sticky-top" style="top: 88px;">
                        <div class="d-flex justify-content-between">
                            <span class="fw-semibold">Package Total</span>
                            <asp:Label ID="lblTotal" runat="server" CssClass="fw-bold" />
                        </div>
                        <div class="small text-secondary mb-3">Taxes & fees calculated at checkout.</div>
                        <a class="btn btn-primary w-100" href="<%: ResolveUrl("~/Checkout.aspx") %>">Book Now</a>
                        <div class="text-center mt-4">
                            <img src="<%: ResolveUrl("~/Images/Tiny.png") %>"
                                alt="TripRex Mascot"
                                class="img-fluid mb-2"
                                style="max-width: 220px; height: auto; filter: drop-shadow(0 4px 10px rgba(0,0,0,0.2));" />
                            <p class="fw-semibold text-success" style="font-size: 1.05rem;">
                                Get ready for a dinomite trip! — Tiny the T-Rex
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>

    </div>
</asp:Content>
