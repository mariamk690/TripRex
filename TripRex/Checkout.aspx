<%@ Page Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="Checkout.aspx.cs"
    Inherits="TripRex.Checkout" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5">
        <!-- Header -->
        <div class="d-flex justify-content-between align-items-end mb-3">
            <h1 class="h4 mb-0">Review &amp; Pay</h1>
            <asp:Label ID="lblUser" runat="server" CssClass="text-secondary small" />
        </div>

        <!-- Status / Error message -->
        <asp:Label ID="lblMessage" runat="server"
            CssClass="text-danger mb-3 d-block" Visible="false" />

        <div class="row g-4">
            <!-- Left: Package summary -->
            <div class="col-lg-8">
                <div class="card shadow-sm">
                    <div class="table-responsive">
                        <table class="table align-middle mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th style="width: 18%">Item</th>
                                    <th>Description</th>
                                    <th style="width: 20%">Dates</th>
                                    <th class="text-end" style="width: 12%">Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptItems" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td class="fw-medium"><%# Eval("service_type") %></td>
                                            <td>
                                                <div>
                                                    <div><strong><%# Eval("display_name") %></strong></div>
                                                    <div class="text-muted small"><%# Eval("details") %></div>
                                                    <div class="text-muted small">Ref ID: <%# Eval("ref_id") %></div>
                                                </div>
                                            </td>
                                            <td>
                                                <%# Eval("computed_dates") %>
                                                <div class="text-muted small"><%# Eval("computed_qty_label") %></div>
                                            </td>
                                            <td class="text-end">$<%# Eval("computed_total", "{0:F2}") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>

                            <tfoot class="table-light">
                                <tr>
                                    <th colspan="3" class="text-end">Total (incl. est. taxes &amp; fees)</th>
                                    <th class="text-end">
                                        <asp:Label ID="lblTotal" runat="server" />
                                    </th>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </div>
            </div>

            <!-- Right: Payment info -->
            <div class="col-lg-4">
                <div class="card p-3 p-md-4 shadow-sm">
                    <h2 class="h6 mb-3">Payment Method</h2>

                    <div class="form-check mb-2">
                        <asp:RadioButton ID="rdoCardOnFile" runat="server"
                            GroupName="pay" Checked="true"
                            CssClass="form-check-input" />
                        <label class="form-check-label" for="rdoCardOnFile">
                            Use card on file •••• 2525
                        </label>
                    </div>

                    <div class="form-check mb-3">
                        <asp:RadioButton ID="rdoAddCard" runat="server"
                            GroupName="pay"
                            CssClass="form-check-input" />
                        <label class="form-check-label" for="rdoAddCard">
                            Add a new credit card
                        </label>
                    </div>


                    <!-- Card entry fields -->
                    <div class="row g-2">
                        <div class="col-12">
                            <label class="form-label">Card #</label>
                            <asp:TextBox ID="txtCardNumber" runat="server"
                                CssClass="form-control"
                                placeholder="1234 5678 9012 3456" />
                        </div>
                        <div class="col-6">
                            <label class="form-label">Exp (MM/YY)</label>
                            <asp:TextBox ID="txtExp" runat="server"
                                CssClass="form-control"
                                placeholder="05/27" />
                        </div>
                        <div class="col-6">
                            <label class="form-label">Security</label>
                            <asp:TextBox ID="txtCvv" runat="server"
                                CssClass="form-control"
                                placeholder="123"
                                TextMode="Password" />
                        </div>
                        <div class="col-12">
                            <label class="form-label">Zipcode</label>
                            <asp:TextBox ID="txtZip" runat="server"
                                CssClass="form-control"
                                placeholder="19104" />
                        </div>
                    </div>

                    <asp:Button ID="btnConfirm" runat="server"
                        Text="Time to take a trip!"
                        CssClass="btn btn-success w-100 mt-3"
                        OnClick="btnConfirm_Click" />

                    <p class="small text-secondary mt-2 mb-0">
                        You’ll receive an email confirmation. Have fun!
                    </p>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
