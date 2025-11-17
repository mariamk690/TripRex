<%@ Page Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeBehind="AccountInfo.aspx.cs"
    Inherits="TripRex.AccountInfo" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-5">
        <h1 class="h4 mb-4">Manage Your Information</h1>

        <asp:Label ID="lblMessage" runat="server" CssClass="text-success mb-3 d-block" Visible="false"></asp:Label>

        <div class="row g-4">
            <!-- Contact / Address -->
            <div class="col-lg-7">
                <div class="form-section">
                    <div class="row g-3">
                        <div class="col-md-6">
                            <label class="form-label">First Name</label>
                            <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Last Name</label>
                            <asp:TextBox ID="txtLastName" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Phone #</label>
                            <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" placeholder="(555) 123-4567" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Email</label>
                            <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" TextMode="Email" placeholder="you@example.com" />
                        </div>
                        <div class="col-12">
                            <label class="form-label">Address Line 1</label>
                            <asp:TextBox ID="txtAddress" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">City</label>
                            <asp:TextBox ID="txtCity" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">State</label>
                            <asp:TextBox ID="txtState" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">Zipcode</label>
                            <asp:TextBox ID="txtZip" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-md-2">
                            <label class="form-label">Country</label>
                            <asp:TextBox ID="txtCountry" runat="server" CssClass="form-control" />
                        </div>
                        <div class="col-12">
                            <asp:Button ID="btnSave" runat="server" CssClass="btn btn-primary" Text="Save Changes" OnClick="btnSave_Click" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- Payment Methods and Past Trips -->
            <div class="col-lg-5">
                <!-- Payment Methods -->
                <div class="card shadow-sm mb-4 border-0">
                    <div class="card-header bg-white d-flex justify-content-between align-items-center">
                        <h5 class="m-0">Saved Payment Methods</h5>
                        <button class="btn btn-sm btn-outline-primary"
                            type="button"
                            data-bs-toggle="collapse"
                            data-bs-target="#collapseAddCard"
                            aria-expanded="false"
                            aria-controls="collapseAddCard">
                            Add New
                        </button>
                    </div>

                    <div id="collapseAddCard" class="collapse border-top" data-bs-parent="#paymentMethods">
                        <div class="card-body">
                            <div class="row g-3">
                                <div class="col-12">
                                    <label class="form-label">Card #</label>
                                    <asp:TextBox ID="txtCardNum" runat="server" CssClass="form-control" />
                                </div>
                                <div class="col-6">
                                    <label class="form-label">Exp (MM/YY)</label>
                                    <asp:TextBox ID="txtExp" runat="server" CssClass="form-control" />
                                </div>
                                <div class="col-6">
                                    <label class="form-label">Security</label>
                                    <asp:TextBox ID="txtCVV" runat="server" CssClass="form-control" />
                                </div>
                                <div class="col-12">
                                    <label class="form-label">Zipcode</label>
                                    <asp:TextBox ID="txtZipCard" runat="server" CssClass="form-control" />
                                </div>
                                <div class="col-12">
                                    <asp:Button ID="btnAddPayment" runat="server" CssClass="btn btn-success w-100" Text="Add" OnClick="btnAddPayment_Click" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <asp:Repeater ID="rptPaymentMethods" runat="server" OnItemCommand="rptPaymentMethods_ItemCommand">
                        <ItemTemplate>
                            <div class="border-bottom d-flex justify-content-between align-items-center p-3">
                                <div>
                                    <strong class="me-1"><%# Eval("brand") %></strong>
                                    <span class="text-muted">•••• <%# Eval("last4") %></span>
                                    <span class="text-muted small ms-1">(Exp <%# string.Format("{0:D2}/{1}", Eval("exp_month"), Eval("exp_year")) %>)</span>
                                    <%# Convert.ToBoolean(Eval("is_default")) ? "<span class='badge bg-success ms-2'>Default</span>" : "" %>
                                </div>
                                <div class="d-flex gap-2">
                                    <asp:Button runat="server"
                                        CommandName="SetDefault"
                                        CommandArgument='<%# Eval("Id") %>'
                                        CssClass='<%# Convert.ToBoolean(Eval("is_default")) ? "btn btn-sm btn-outline-secondary disabled" : "btn btn-sm btn-outline-secondary" %>'
                                        Text="Set Default" />
                                    <asp:Button runat="server"
                                        CommandName="Delete"
                                        CommandArgument='<%# Eval("Id") %>'
                                        CssClass="btn btn-sm btn-outline-danger"
                                        Text="Delete" />
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>


                <!-- Past Trips -->
                <div class="accordion" id="pastTrips">
                    <asp:ListView ID="lvPastTrips" runat="server">
                        <ItemTemplate>
                            <div class="accordion-item">
                                <h2 class="accordion-header" id="heading<%# Container.DataItemIndex %>">
                                    <button class="accordion-button collapsed"
                                        type="button"
                                        data-bs-toggle="collapse"
                                        data-bs-target="#collapse<%# Container.DataItemIndex %>"
                                        aria-expanded="false"
                                        aria-controls="collapse<%# Container.DataItemIndex %>">
                                        Package #<%# Eval("id") %> – <%# Eval("status") %>
                                    </button>
                                </h2>
                                <div id="collapse<%# Container.DataItemIndex %>"
                                    class="accordion-collapse collapse"
                                    aria-labelledby="heading<%# Container.DataItemIndex %>"
                                    data-bs-parent="#pastTrips">
                                    <div class="accordion-body">
                                        <strong>Total:</strong> $<%# Eval("total_amount", "{0:F2}") %><br />
                                        <strong>Destination:</strong> <%# Eval("destination") %><br />
                                        <strong>Trip Dates:</strong>
                                        <%# string.Format("{0:MMM dd, yyyy}", Eval("trip_start")) %> –
                        <%# string.Format("{0:MMM dd, yyyy}", Eval("trip_end")) %><br />
                                        <strong>Length:</strong> <%# Eval("trip_length") %> days<br />
                                        <strong>Created:</strong> <%# string.Format("{0:MMM dd, yyyy}", Eval("created_utc")) %><br />
                                        <strong>Last Updated:</strong> <%# string.Format("{0:MMM dd, yyyy}", Eval("updated_utc")) %>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                        <EmptyDataTemplate>
                            <div class="text-muted small p-3">No past trips found.</div>
                        </EmptyDataTemplate>
                    </asp:ListView>
                </div>

            </div>
        </div>
    </div>
</asp:Content>
