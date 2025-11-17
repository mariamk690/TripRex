<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Dashboard.aspx.cs" Inherits="TripRex.Dashboard" %>

<asp:Content ID="cHead" ContentPlaceHolderID="HeadContent" runat="server" />

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

    <section class="py-5">
        <div class="container">
            <div class="row">
                <!-- Left: Main content -->
                <div class="col-lg-9">

                    <!-- Header -->
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <h1 class="h3 m-0">Plan your vacation</h1>
                        <asp:Button ID="btnResume" runat="server"
                            CssClass="btn btn-outline-primary d-none d-md-inline"
                            Text="Resume saved package" PostBackUrl="~/CurrentPackage.aspx" />
                    </div>

                    <asp:Label ID="lblStatus" runat="server" CssClass="text-muted small d-block mb-3"></asp:Label>

                    <!-- Search Card -->
                    <div class="card p-4 mb-4 shadow-sm">
                        <div class="row g-3">
                            <div class="col-md-3">
                                <label class="form-label">Current Location</label>
                                <asp:TextBox ID="txtOrigin" runat="server" CssClass="form-control" placeholder="Philadelphia"></asp:TextBox>
                            </div>
                            <div class="col-md-3">
                                <label class="form-label">Destination</label>
                                <asp:TextBox ID="txtDestination" runat="server" CssClass="form-control" placeholder="Las Vegas"></asp:TextBox>
                            </div>
                            <div class="col-md-3">
                                <label class="form-label">Start Date</label>
                                <asp:TextBox ID="txtStartDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                            </div>
                            <div class="col-md-3">
                                <label class="form-label">End Date</label>
                                <asp:TextBox ID="txtEndDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                            </div>
                        </div>

                        <hr class="my-4" />

                        <div class="row align-items-center">
                            <div class="col-md-8">
                                <label class="form-label d-block mb-2">Include in Package</label>
                                <div class="d-flex flex-wrap gap-4">
                                    <div class="form-check">
                                        <asp:CheckBox ID="chkFlights" runat="server" CssClass="form-check-input" Checked="true" />
                                        <label class="form-check-label" for="<%= chkFlights.ClientID %>">Flights</label>
                                    </div>
                                    <div class="form-check">
                                        <asp:CheckBox ID="chkHotels" runat="server" CssClass="form-check-input" Checked="true" />
                                        <label class="form-check-label" for="<%= chkHotels.ClientID %>">Hotels</label>
                                    </div>
                                    <div class="form-check">
                                        <asp:CheckBox ID="chkCars" runat="server" CssClass="form-check-input" />
                                        <label class="form-check-label" for="<%= chkCars.ClientID %>">Cars</label>
                                    </div>
                                    <div class="form-check">
                                        <asp:CheckBox ID="chkEvents" runat="server" CssClass="form-check-input" />
                                        <label class="form-check-label" for="<%= chkEvents.ClientID %>">Events</label>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4 text-md-end mt-3 mt-md-0">
                                <asp:Button ID="btnSearch" runat="server"
                                    CssClass="btn btn-primary px-4"
                                    Text="Find Options" OnClick="btnSearch_Click" />
                            </div>
                        </div>

                        <asp:Label ID="lblError" runat="server" CssClass="text-danger small mt-3 d-block" Visible="false"></asp:Label>
                    </div>

                    <asp:Panel ID="pnlFlights" runat="server" Visible="false" CssClass="mb-5">
                        <h2 class="h5 mb-3">Roundtrip Flights</h2>

                        <div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4">
                            <asp:Repeater ID="rptFlightVendors" runat="server">
                                <ItemTemplate>
                                    <div class="col">
                                        <div class="card shadow-sm h-100">
                                            <!-- Airline image -->
                                            <asp:Image runat="server"
                                                ImageUrl='<%# Eval("image_url") ?? ResolvePlaceholder("airline") %>'
                                                AlternateText='<%# Eval("vendor") %>'
                                                CssClass="card-img-top"
                                                Style="height: 140px; object-fit: contain; background-color: #f8f9fa;" />

                                            <div class="card-body d-flex flex-column justify-content-between">
                                                <div>
                                                    <h5 class="card-title mb-1"><%# Eval("vendor") %></h5>
                                                    <p class="card-text text-muted small mb-0">
                                                        <%# Eval("caption") ?? "Multiple routes and fare classes available" %>
                                                    </p>

                                                </div>

                                                <div class="mt-auto d-flex justify-content-between align-items-center">
                                                    <button class="btn btn-outline-primary btn-sm" type="button"
                                                        data-bs-toggle="collapse"
                                                        data-bs-target="#f_<%# Container.ItemIndex %>">
                                                        View Flights
                                                    </button>
                                                    <span class="fw-semibold text-primary small">Economy & Business</span>
                                                </div>
                                            </div>

                                            <div id="f_<%# Container.ItemIndex %>" class="collapse border-top">
                                                <div class="card-body p-0">
                                                    <asp:Repeater ID="rptFlightOptions" runat="server" OnItemCommand="ItemCommand_AddToPackage">
                                                        <HeaderTemplate>
                                                            <ul class="list-group list-group-flush">
                                                        </HeaderTemplate>

                                                        <ItemTemplate>
                                                            <li class="list-group-item d-flex justify-content-between align-items-center">
                                                                <div>
                                                                    <div class="fw-medium">
                                                                        Flight <%# Eval("flight_number") %>
                                                                    </div>
                                                                    <div class="text-muted small">
                                                                        <%# Eval("depart_code") %> → <%# Eval("arrive_code") %>
                                                                    </div>
                                                                    <div class="text-muted small">
                                                                        <%# Eval("departure_time", "{0:g}") %> → <%# Eval("arrival_time", "{0:g}") %>
                                                                    </div>
                                                                    <div class="text-muted small">
                                                                        Class: <%# Eval("class_code") %>
                                                                    </div>
                                                                </div>

                                                                <div class="text-end">
                                                                    <div class="fw-semibold text-primary mb-1">
                                                                        $<%# Eval("price", "{0:F2}") %>
                                                                    </div>
                                                                    <asp:Button ID="btnAddFlight" runat="server"
                                                                        CssClass="btn btn-sm btn-success"
                                                                        Text="Add to Package"
                                                                        CommandName="AddFlight"
                                                                        CommandArgument='<%# Eval("flight_id") %>' />
                                                                </div>
                                                            </li>
                                                        </ItemTemplate>

                                                        <FooterTemplate></ul></FooterTemplate>
                                                    </asp:Repeater>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>

                    <!-- Hotels -->
                    <asp:Panel ID="pnlHotels" runat="server" Visible="false" CssClass="mb-5">
                        <h2 class="h5 mb-3">Hotels</h2>
                        <div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4">
                            <asp:Repeater ID="rptHotelVendors" runat="server">
                                <ItemTemplate>
                                    <div class="col">
                                        <div class="card shadow-sm h-100">
                                            <asp:Image runat="server"
                                                ImageUrl='<%# GetPrimaryVendorImage(Convert.ToInt32(Eval("vendor_id"))) %>'
                                                AlternateText='<%# Eval("hotel_name") %>'
                                                CssClass="card-img-top"
                                                Style="height: 160px; object-fit: cover;" />

                                            <div class="card-body d-flex flex-column justify-content-between">
                                                <div>
                                                    <h5 class="card-title mb-1"><%# Eval("hotel_name") %></h5>
                                                    <p class="card-text text-muted small mb-1">
                                                        <%# Eval("phone") %><br />
                                                        <%# Eval("email") %>
                                                    </p>
                                                    <p class="text-muted small mb-0">
                                                        <%# Eval("caption") ?? "Comfort and style near your destination" %>
                                                    </p>

                                                </div>

                                                <div class="mt-auto d-flex justify-content-between align-items-center">
                                                    <button class="btn btn-outline-primary btn-sm" type="button"
                                                        data-bs-toggle="collapse"
                                                        data-bs-target='<%# "#h_" + Container.ItemIndex + "_" + Eval("vendor_id") %>'
                                                        aria-controls='<%# "h_" + Container.ItemIndex + "_" + Eval("vendor_id") %>'
                                                        aria-expanded="false">
                                                        View Rooms
                                                    </button>
                                                    <span class="fw-semibold text-primary">$<%# Eval("starting_price", "{0:F2}") %>/night
                                                    </span>
                                                </div>
                                            </div>

                                            <div id='<%# "h_" + Container.ItemIndex + "_" + Eval("vendor_id") %>'
                                                class="collapse border-top">
                                                <div class="card-body p-0">
                                                    <div id='<%# "carousel_" + Container.ItemIndex %>'
                                                        class="carousel slide" data-bs-ride="false">
                                                        <div class="carousel-inner">
                                                            <asp:Repeater ID="rptRoomOptions" runat="server" OnItemCommand="ItemCommand_AddToPackage">
                                                                <ItemTemplate>
                                                                    <div class='carousel-item <%# Container.ItemIndex == 0 ? "active" : "" %>'>
                                                                        <div class="position-relative">
                                                                            <asp:Image ID="imgRoom" runat="server"
                                                                                CssClass="card-img-top room-img"
                                                                                ImageUrl='<%# Eval("image_url") ?? "~/images/no-image.png" %>'
                                                                                AlternateText='<%# Eval("room_type") %>' />
                                                                            <div class="carousel-caption d-none d-md-block bg-dark bg-opacity-50 rounded px-2 py-1">
                                                                                <h6 class="text-white mb-0"><%# Eval("room_type") %></h6>
                                                                            </div>
                                                                        </div>
                                                                        <div class="card-body text-center">
                                                                            <p class="text-muted small mb-1">
                                                                                Max Guests: <%# Eval("max_occupancy") %>
                                                                            </p>
                                                                            <p class="fw-semibold text-primary mb-2">
                                                                                $<%# Eval("base_price", "{0:F2}") %>/night
                                                                            </p>
                                                                            <asp:Button ID="btnAddRoom" runat="server"
                                                                                CssClass="btn btn-sm btn-success w-100"
                                                                                Text="Add to Package"
                                                                                CommandName="AddRoom"
                                                                                CommandArgument='<%# Eval("id") %>' />
                                                                        </div>
                                                                    </div>
                                                                </ItemTemplate>
                                                            </asp:Repeater>
                                                        </div>

                                                        <!-- Carousel controls -->
                                                        <button class="carousel-control-prev" type="button"
                                                            data-bs-target='<%# "#carousel_" + Container.ItemIndex %>' data-bs-slide="prev">
                                                            <span class="carousel-control-prev-icon" aria-hidden="true"></span>
                                                            <span class="visually-hidden">Previous</span>
                                                        </button>
                                                        <button class="carousel-control-next" type="button"
                                                            data-bs-target='<%# "#carousel_" + Container.ItemIndex %>' data-bs-slide="next">
                                                            <span class="carousel-control-next-icon" aria-hidden="true"></span>
                                                            <span class="visually-hidden">Next</span>
                                                        </button>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>

                    <!-- Cars -->
                    <asp:Panel ID="pnlCars" runat="server" Visible="false" CssClass="mb-5">
                        <h2 class="h5 mb-3">Car Rentals</h2>
                        <div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4">
                            <asp:Repeater ID="rptCarVendors" runat="server">
                                <ItemTemplate>
                                    <div class="col">
                                        <div class="card shadow-sm h-100">
                                            <asp:Image runat="server"
                                                ImageUrl='<%# GetPrimaryVendorImage(Convert.ToInt32(Eval("vendor_id"))) %>'
                                                AlternateText='<%# Eval("agency") %>'
                                                CssClass="card-img-top"
                                                Style="height: 160px; object-fit: cover;" />

                                            <div class="card-body d-flex flex-column justify-content-between">
                                                <div>
                                                    <h5 class="card-title mb-1"><%# Eval("agency") %></h5>
                                                    <p class="text-muted small mb-0">
                                                        <%# Eval("caption") ?? "Economy to SUV options available — reliable local service." %>
                                                    </p>

                                                </div>

                                                <div class="mt-auto d-flex justify-content-between align-items-center">
                                                    <button class="btn btn-outline-primary btn-sm" type="button"
                                                        data-bs-toggle="collapse"
                                                        data-bs-target="#car_<%# Container.ItemIndex %>">
                                                        View Cars
                                                    </button>
                                                    <span class="fw-semibold text-primary">from $<%# Eval("starting_price", "{0:F2}") %>/day
                                                    </span>
                                                </div>
                                            </div>

                                            <!-- Collapsible car list -->
                                            <div id="car_<%# Container.ItemIndex %>" class="collapse border-top">
                                                <div class="card-body p-0">
                                                    <div id="carousel_car_<%# Container.ItemIndex %>" class="carousel slide" data-bs-ride="false">
                                                        <div class="carousel-inner">
                                                            <asp:Repeater ID="rptCarOptions" runat="server" OnItemCommand="ItemCommand_AddToPackage">
                                                                <ItemTemplate>
                                                                    <div class='carousel-item <%# Container.ItemIndex == 0 ? "active" : "" %>'>
                                                                        <div class="position-relative">
                                                                            <asp:Image ID="imgCar" runat="server"
                                                                                CssClass="card-img-top"
                                                                                ImageUrl='<%# Eval("image_url") ?? "~/images/placeholders/car.png" %>'
                                                                                AlternateText='<%# Eval("make") + " " + Eval("model") %>' />
                                                                            <div class="carousel-caption d-none d-md-block bg-dark bg-opacity-50 rounded px-2 py-1">
                                                                                <h6 class="text-white mb-0">
                                                                                    <%# Eval("make") %> <%# Eval("model") %>
                                                                                </h6>
                                                                            </div>
                                                                        </div>
                                                                        <div class="card-body text-center">
                                                                            <p class="text-muted small mb-1">
                                                                                Class: <%# Eval("car_class") %> • Seats: <%# Eval("seats") %>
                                                                            </p>
                                                                            <p class="fw-semibold text-primary mb-2">
                                                                                $<%# Eval("daily_rate", "{0:F2}") %>/day
                                                                            </p>
                                                                            <asp:Button ID="btnAddCar" runat="server"
                                                                                CssClass="btn btn-sm btn-success w-100"
                                                                                Text="Add to Package"
                                                                                CommandName="AddCar"
                                                                                CommandArgument='<%# Eval("id") %>' />
                                                                        </div>
                                                                    </div>
                                                                </ItemTemplate>
                                                            </asp:Repeater>
                                                        </div>

                                                        <button class="carousel-control-prev" type="button"
                                                            data-bs-target="#carousel_car_<%# Container.ItemIndex %>" data-bs-slide="prev">
                                                            <span class="carousel-control-prev-icon" aria-hidden="true"></span>
                                                            <span class="visually-hidden">Previous</span>
                                                        </button>
                                                        <button class="carousel-control-next" type="button"
                                                            data-bs-target="#carousel_car_<%# Container.ItemIndex %>" data-bs-slide="next">
                                                            <span class="carousel-control-next-icon" aria-hidden="true"></span>
                                                            <span class="visually-hidden">Next</span>
                                                        </button>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>

                    <!-- Events -->
                    <asp:Panel ID="pnlEvents" runat="server" Visible="false" CssClass="mb-5">
                        <h2 class="h5 mb-3">Events</h2>
                        <div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4">
                            <asp:Repeater ID="rptEventVendors" runat="server">
                                <ItemTemplate>
                                    <div class="col">
                                        <div class="card shadow-sm h-100">
                                            <asp:Image runat="server"
                                                ImageUrl='<%# Eval("image_url") ?? ResolvePlaceholder("event") %>'
                                                AlternateText='<%# Eval("organizer") %>'
                                                CssClass="card-img-top"
                                                Style="height: 160px; object-fit: cover;" />
                                            <div class="card-body d-flex flex-column justify-content-between">
                                                <div>
                                                    <h5 class="card-title mb-1"><%# Eval("organizer") %></h5>
                                                    <p class="card-text text-muted small mb-1">
                                                        Venue: <%# Eval("venue") %>
                                                    </p>
                                                    <p class="text-muted small mb-0">
                                                        <%# Eval("caption") ?? "Click below to view upcoming shows" %>
                                                    </p>
                                                </div>
                                                <div class="mt-auto d-flex justify-content-between align-items-center">
                                                    <button class="btn btn-outline-primary btn-sm" type="button"
                                                        data-bs-toggle="collapse"
                                                        data-bs-target="#ev_<%# Container.ItemIndex %>">
                                                        View Events
                                                    </button>
                                                </div>
                                            </div>

                                            <div id="ev_<%# Container.ItemIndex %>" class="collapse border-top">
                                                <div class="card-body p-0">
                                                    <asp:Repeater ID="rptEventOptions" runat="server" OnItemCommand="ItemCommand_AddToPackage">
                                                        <HeaderTemplate>
                                                            <ul class="list-group list-group-flush">
                                                        </HeaderTemplate>
                                                        <ItemTemplate>
                                                            <li class="list-group-item d-flex justify-content-between align-items-center">
                                                                <div>
                                                                    <div class="fw-medium"><%# Eval("name") %></div>
                                                                    <div class="text-muted small mb-1">
                                                                        <%# Eval("start_time", "{0:ddd, MMM d • h:mm tt}") %>
                                                                    </div>
                                                                </div>
                                                                <div class="text-end">
                                                                    <div class="fw-semibold mb-2">
                                                                        <%# Convert.ToDecimal(Eval("price")) == 0 ? "Free" :
                                                        "$" + Convert.ToDecimal(Eval("price")).ToString("F2") %>
                                                                    </div>
                                                                    <asp:Button ID="btnAddEvent" runat="server"
                                                                        CssClass="btn btn-sm btn-success"
                                                                        Text="Add to Package"
                                                                        CommandName="AddEvent"
                                                                        CommandArgument='<%# Eval("id") %>' />
                                                                </div>
                                                            </li>
                                                        </ItemTemplate>
                                                        <FooterTemplate></ul></FooterTemplate>
                                                    </asp:Repeater>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>



                </div>
                <!-- end left col -->

                <!-- Right: Travel Cart sidebar -->
                <div class="col-lg-3">
                    <asp:Panel ID="pnlCart" runat="server" CssClass="card p-3 shadow-sm sticky-top" Style="top: 90px;" Visible="false">
                        <h5 class="mb-3">Your Package</h5>

                        <asp:Repeater ID="rptCartItems" runat="server">
                            <ItemTemplate>
                                <div class="border-bottom py-2">
                                    <div class="d-flex justify-content-between">
                                        <div>
                                            <strong><%# Eval("service_type") %></strong><br />
                                            <span class="fw-semibold"><%# Eval("display_name") %></span><br />
                                            <span class="text-muted small"><%# Eval("details") %></span><br />
                                            <span class="text-muted small">Ref ID: <%# Eval("ref_id") %></span>
                                        </div>
                                        <div class="text-end">
                                            <div class="fw-semibold text-primary">$<%# Eval("unit_price", "{0:F2}") %></div>
                                            <div class="text-muted small">
                                                x <%# Eval("qty") %> = $<%# Eval("computed_total", "{0:F2}") %>
                                            </div>

                                        </div>
                                    </div>
                                </div>
                            </ItemTemplate>

                        </asp:Repeater>

                        <div class="d-flex justify-content-between align-items-center mt-3">
                            <asp:Label ID="lblCartTotal" runat="server" CssClass="fw-bold"></asp:Label>
                            <asp:Button ID="btnViewEntirePkg" runat="server" CssClass="btn btn-primary btn-sm" Text="View all Details" PostBackUrl="~/CurrentPackage.aspx" />
                            <asp:Button ID="btnCheckout" runat="server" CssClass="btn btn-primary btn-sm" Text="Checkout" PostBackUrl="~/Checkout.aspx" />
                        </div>
                    </asp:Panel>
                </div>
                <!-- end right col -->

            </div>
            <!-- end row -->
        </div>
        <!-- end container -->
    </section>

</asp:Content>
