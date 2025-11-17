using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using TripRexLibraries;
using Utilities;

namespace TripRex
{
    public partial class Dashboard : Page
    {
        StoredProcs sp = new StoredProcs();
        private static readonly Random _rand = new Random();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                LoadDashboard();
        }

        private void LoadDashboard()
        {
            if (Session["UserID"] != null)
            {
                int userId = Convert.ToInt32(Session["UserID"]);
                btnResume.Visible = true;
                lblStatus.Text = "Welcome back!";
                LoadCart(userId);

                try
                {
                    SqlCommand objCommand = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandText = "usp_Get_Saved_Search"
                    };
                    objCommand.Parameters.AddWithValue("@UserID", userId);

                    DBConnect objDB = new DBConnect();
                    DataSet ds = objDB.GetDataSetUsingCmdObj(objCommand);

                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        if (ds.Tables[0].Rows[0]["last_search"] != System.DBNull.Value)
                        {
                            byte[] byteArray = (byte[])ds.Tables[0].Rows[0]["last_search"];
                            BinaryFormatter deSerializer = new BinaryFormatter();
                            MemoryStream memStream = new MemoryStream(byteArray);
                            SavedSearch objSavedSearch = (SavedSearch)deSerializer.Deserialize(memStream);

                            txtOrigin.Text = objSavedSearch.Origin;
                            txtDestination.Text = objSavedSearch.Destination;
                            txtStartDate.Text = objSavedSearch.DepartDate.ToString("yyyy-MM-dd");
                            txtEndDate.Text = objSavedSearch.ReturnDate.ToString("yyyy-MM-dd");
                            Session["TripStart"] = objSavedSearch.DepartDate;
                            Session["TripEnd"] = objSavedSearch.ReturnDate;

                            LoadCart(userId);
                            lblStatus.Text += "<br/>Your previous search has been restored!";
                        }
                        else
                        {
                            lblStatus.Text += "<br/>A saved search was never stored for this account.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text += "<br/>Unable to load saved search: " + ex.Message;
                }
            }
            else
            {
                btnResume.Visible = false;
                lblStatus.Text = "Browsing as guest — sign in to build a package and check out.";
                pnlCart.Visible = false;
            }
        }

        private void LoadCart(int userId)
        {
            int packageId = sp.PackageGetOrCreate(userId);
            DataSet ds = sp.PackageGet(packageId);

            if (ds != null && ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
            {
                DataTable items = ds.Tables[1];
                decimal total = 0;

                DateTime tripStart = Convert.ToDateTime(Session["TripStart"]);
                DateTime tripEnd = Convert.ToDateTime(Session["TripEnd"]);
                int totalDays = (tripEnd - tripStart).Days;
                if (totalDays < 1) totalDays = 1;

                foreach (DataRow r in items.Rows)
                {
                    string type = r["service_type"].ToString();

                    if (type == "Hotel" || type == "Car Rental")
                    {
                        decimal unitPrice = Convert.ToDecimal(r["unit_price"]);
                        decimal lineTotal = unitPrice * totalDays;
                        r["line_total"] = lineTotal;
                        r["start_utc"] = tripStart;
                        r["end_utc"] = tripEnd;
                    }

                    if (r["line_total"] != DBNull.Value)
                        total += Convert.ToDecimal(r["line_total"]);
                }

                DataTable displayTable = items.Clone();
                displayTable.Columns.Add("computed_total", typeof(decimal));

                foreach (DataRow r in items.Rows)
                {
                    DataRow newRow = displayTable.NewRow();
                    newRow.ItemArray = r.ItemArray;
                    string type = r["service_type"].ToString();
                    decimal unitPrice = Convert.ToDecimal(r["unit_price"]);

                    if (type == "Hotel" || type == "Car Rental")
                    {
                        newRow["computed_total"] = unitPrice * totalDays;
                        newRow["qty"] = totalDays;
                    }
                    else
                    {
                        newRow["computed_total"] = Convert.ToDecimal(r["line_total"]);
                    }

                    displayTable.Rows.Add(newRow);
                }

                rptCartItems.DataSource = displayTable;
                rptCartItems.DataBind();
                lblCartTotal.Text = $"Total: ${total:F2}";
                pnlCart.Visible = true;
            }
            else
            {
                pnlCart.Visible = false;
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            try
            {
                string origin = txtOrigin.Text.Trim();
                string destination = txtDestination.Text.Trim();

                if (!DateTime.TryParse(txtStartDate.Text, out DateTime startDate) ||
                    !DateTime.TryParse(txtEndDate.Text, out DateTime endDate))
                    throw new Exception("Please enter valid start and end dates.");

                if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
                    throw new Exception("Please fill in both location fields.");

                Session["TripStart"] = startDate;
                Session["TripEnd"] = endDate;

                int originCityId = ResolveCity(origin);
                int destCityId = ResolveCity(destination);
                string fromCode = GetAirportCodeFromCity(originCityId);
                string toCode = GetAirportCodeFromCity(destCityId);

                // === Save search ===
                if (Session["UserID"] != null)
                {
                    try
                    {
                        int userId = Convert.ToInt32(Session["UserID"]);
                        SavedSearch objSearch = new SavedSearch
                        {
                            Origin = origin,
                            Destination = destination,
                            DepartDate = startDate,
                            ReturnDate = endDate
                        };

                        BinaryFormatter serializer = new BinaryFormatter();
                        MemoryStream memStream = new MemoryStream();
                        serializer.Serialize(memStream, objSearch);
                        byte[] byteArray = memStream.ToArray();

                        SqlCommand objCommand = new SqlCommand
                        {
                            CommandType = CommandType.StoredProcedure,
                            CommandText = "usp_Store_Saved_Search"
                        };
                        objCommand.Parameters.AddWithValue("@UserID", userId);
                        objCommand.Parameters.AddWithValue("@SavedSearch", byteArray);

                        DBConnect objDB = new DBConnect();
                        int retVal = objDB.DoUpdate(objCommand);

                        lblStatus.Text = retVal > 0
                            ? "Search criteria saved successfully."
                            : "A problem occurred saving the search.";
                    }
                    catch (Exception ex)
                    {
                        lblError.Text = "Serialization error: " + ex.Message;
                        lblError.Visible = true;
                    }
                }

                // === Flights ===
                if (chkFlights.Checked)
                {
                    try
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        string apiUrl = "https://cis-iis2.temple.edu/Fall2025/CIS3342_tuo90411/WebAPI/api/Flights/FindFlights";

                        // string apiUrl = "https://localhost:44338/api/Flights/FindFlights";
                        string jsonOutbound = $@"{{
                            ""FromCode"": ""{fromCode}"",
                            ""ToCode"": ""{toCode}"",
                            ""DepartDate"": ""{startDate:yyyy-MM-ddTHH:mm:ss}"",
                            ""ClassCode"": null,
                            ""MinPrice"": null,
                            ""MaxPrice"": null
                        }}";

                        using (HttpClient client = new HttpClient())
                        {
                            var content = new StringContent(jsonOutbound, System.Text.Encoding.UTF8, "application/json");
                            var response = client.PostAsync(apiUrl, content).Result;

                            if (!response.IsSuccessStatusCode)
                                throw new Exception($"API call failed: {response.StatusCode}");

                            string json = response.Content.ReadAsStringAsync().Result;
                            var flights = js.Deserialize<List<FlightResponse>>(json);

                            if (flights == null || flights.Count == 0)
                                throw new Exception("No flights returned from API.");

                            DataTable dt = new DataTable();
                            dt.Columns.AddRange(new[]
                            {
                                new DataColumn("flight_id", typeof(int)),
                                new DataColumn("vendor_name", typeof(string)),
                                new DataColumn("flight_number", typeof(string)),
                                new DataColumn("depart_code", typeof(string)),
                                new DataColumn("arrive_code", typeof(string)),
                                new DataColumn("departure_time", typeof(DateTime)),
                                new DataColumn("arrival_time", typeof(DateTime)),
                                new DataColumn("class_code", typeof(string)),
                                new DataColumn("price", typeof(decimal)),
                                new DataColumn("image_url", typeof(string)),
                                new DataColumn("caption", typeof(string))
                            });

                            foreach (var f in flights)
                            {
                                dt.Rows.Add(f.FlightID, f.AirCarrierName, f.FlightNumber, f.DepartCode,
                                    f.ArriveCode, f.DepartureTime, f.ArrivalTime, f.ClassCode, f.Price, f.ImageUrl, f.Caption);
                            }

                            DataSet ds = new DataSet();
                            ds.Tables.Add(dt);
                            BindFlights(ds, pnlFlights, rptFlightVendors);
                        }
                    }
                    catch (Exception ex)
                    {
                        lblError.Text = "Flight API error: " + ex.Message;
                        lblError.Visible = true;
                        pnlFlights.Visible = false;
                    }
                }
                else
                {
                    pnlFlights.Visible = false;
                }

                // === Hotels ===
                if (chkHotels.Checked)
                {
                    DataSet dsHotels = sp.HotelSearchbyCity(destCityId);
                    BindHotels(dsHotels, pnlHotels, rptHotelVendors);
                }
                else pnlHotels.Visible = false;

                // === Cars ===
                if (chkCars.Checked)
                {
                    DataSet dsCars = sp.CarsSearch(destCityId);
                    BindCars(dsCars, pnlCars, rptCarVendors);
                }
                else pnlCars.Visible = false;

                // === Events ===
                if (chkEvents.Checked)
                {
                    DataSet dsEvents = sp.EventSearch(destCityId, startDate, endDate);
                    BindEvents(dsEvents, pnlEvents, rptEventVendors);
                }
                else pnlEvents.Visible = false;
            }
            catch (Exception ex)
            {
                lblError.Text = "Error: " + ex.Message;
                lblError.Visible = true;
            }
        }

        // === Lookup helpers ===
        private int ResolveCity(string name)
        {
            DataSet ds = sp.CitiesList();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                if (row["name"].ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt32(row["id"]);
            }

            throw new Exception("City not found: " + name);
        }

        private string GetAirportCodeFromCity(int cityId)
        {
            DataSet ds = sp.AirportList(cityId);

            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                throw new Exception("No airport found for selected city.");

            return ds.Tables[0].Rows[0]["code"].ToString();
        }

        // === Flight binding ===
        private void BindFlights(DataSet ds, Panel panel, Repeater rpt)
        {
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            {
                panel.Visible = false;
                return;
            }

            DataTable dt = ds.Tables[0];
            DataTable vendors = new DataTable();
            vendors.Columns.Add("vendor");
            vendors.Columns.Add("image_url");
            vendors.Columns.Add("caption");

            foreach (DataRow row in dt.Rows)
            {
                string vendorName = Convert.ToString(row["vendor_name"]);
                bool exists = false;
                foreach (DataRow v in vendors.Rows)
                {
                    if (v["vendor"].ToString().Equals(vendorName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }


                if (!exists)
                {
                    DataRow vrow = vendors.NewRow();
                    vrow["vendor"] = vendorName;
                    string img = dt.Columns.Contains("image_url") && row["image_url"] != DBNull.Value
                        ? Convert.ToString(row["image_url"])
                        : ResolvePlaceholder("airline");
                    vrow["image_url"] = img;
                    if (dt.Columns.Contains("caption"))
                        vrow["caption"] = row["caption"]?.ToString();
                    else if (dt.Columns.Contains("description"))
                        vrow["caption"] = row["description"]?.ToString();
                    else
                        vrow["caption"] = "Multiple routes and fare classes available";
                    vendors.Rows.Add(vrow);
                }
            }

            rpt.ItemDataBound += (s, e) =>
            {
                if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
                {
                    string vendor = DataBinder.Eval(e.Item.DataItem, "vendor").ToString();
                    var child = (Repeater)e.Item.FindControl("rptFlightOptions");
                    var view = new DataView(dt)
                    {
                        RowFilter = $"vendor_name = '{vendor.Replace("'", "''")}'"
                    };
                    child.DataSource = view;
                    child.DataBind();
                }
            };

            rpt.DataSource = vendors;
            rpt.DataBind();
            panel.Visible = true;
        }

        // === Hotel binding ===
        private void BindHotels(DataSet ds, Panel panel, Repeater rpt)
        {
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            {
                panel.Visible = false;
                return;
            }

            DataTable dtHotels = ds.Tables[0];

            if (!dtHotels.Columns.Contains("image_url"))
                dtHotels.Columns.Add("image_url");

            foreach (DataRow h in dtHotels.Rows)
            {
                h["image_url"] = GetRandomImage("~/Images/Rooms/", ResolvePlaceholder("airline"));
            }

            rpt.ItemDataBound += new RepeaterItemEventHandler(rptHotelVendors_ItemDataBound);
            rpt.DataSource = dtHotels;
            rpt.DataBind();
            panel.Visible = true;
        }


        protected void rptHotelVendors_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                object vendorObj = DataBinder.Eval(e.Item.DataItem, "vendor_id");
                if (vendorObj == null) return;

                int vendorId = Convert.ToInt32(vendorObj);
                Repeater childRpt = (Repeater)e.Item.FindControl("rptRoomOptions");
                if (childRpt == null) return;

                DataSet dsRooms = sp.HotelRooms(vendorId);
                if (dsRooms == null || dsRooms.Tables.Count == 0 || dsRooms.Tables[0].Rows.Count == 0) return;

                DataTable normalized = NormalizeRoomTypes(dsRooms.Tables[0]);
                AddRoomImages(vendorId, normalized);
                childRpt.DataSource = normalized;
                childRpt.DataBind();
            }
        }
        private string GetRandomImage(string folderPath, string fallback = "/Images/placeholder.jpg")
        {
            try
            {
                string physicalPath = Server.MapPath(folderPath);
                string[] files = Directory.GetFiles(physicalPath, "*.jpg");

                if (files.Length == 0)
                    return fallback;

                string randomFile = Path.GetFileName(files[_rand.Next(files.Length)]);
                return $"{folderPath}{randomFile}";
            }
            catch
            {
                return fallback;
            }
        }

        // === Cars binding ===
        private void BindCars(DataSet ds, Panel panel, Repeater rpt)
        {
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            {
                panel.Visible = false;
                return;
            }

            DataTable dtCars = ds.Tables[0];
            DataTable vendors = new DataTable();
            vendors.Columns.Add("agency");
            vendors.Columns.Add("vendor_id", typeof(int));
            vendors.Columns.Add("starting_price", typeof(decimal));
            vendors.Columns.Add("image_url");
            vendors.Columns.Add("caption");

            foreach (DataRow row in dtCars.Rows)
            {
                string agency = row["agency"].ToString();

                bool exists = false;
                foreach (DataRow v in vendors.Rows)
                {
                    if (v["agency"].ToString().Equals(agency, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    DataRow n = vendors.NewRow();
                    n["agency"] = agency;
                    n["vendor_id"] = row["vendor_id"];
                    n["starting_price"] = Convert.ToDecimal(row["daily_rate"]);
                    n["image_url"] = GetRandomImage("~/Images/Cars/", ResolvePlaceholder("car"));
                    if (dtCars.Columns.Contains("caption"))
                        n["caption"] = row["caption"].ToString();
                    else if (dtCars.Columns.Contains("description"))
                        n["caption"] = row["description"].ToString();
                    else
                        n["caption"] = "Economy to SUV options available – reliable local service.";
                    vendors.Rows.Add(n);
                }
            }

            rpt.ItemDataBound += (s, e) =>
            {
                if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
                {
                    string agency = DataBinder.Eval(e.Item.DataItem, "agency").ToString();
                    Repeater child = (Repeater)e.Item.FindControl("rptCarOptions");

                    DataView view = new DataView(dtCars)
                    {
                        RowFilter = $"agency = '{agency.Replace("'", "''")}'"
                    };

                    DataTable carOptions = view.ToTable();

                    foreach (DataRow car in carOptions.Rows)
                    {
                        car["image_url"] = GetRandomImage("~/Images/Cars/", ResolvePlaceholder("car"));
                    }

                    child.DataSource = carOptions;
                    child.DataBind();
                }
            };

            rpt.DataSource = vendors;
            rpt.DataBind();
            panel.Visible = true;
        }


        // === Events binding ===
        private void BindEvents(DataSet ds, Panel panel, Repeater rpt)
        {
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            {
                panel.Visible = false;
                return;
            }

            DataTable dtEvents = ds.Tables[0];
            DataTable groups = new DataTable();
            groups.Columns.Add("organizer");
            groups.Columns.Add("venue");
            groups.Columns.Add("image_url");
            groups.Columns.Add("caption");

            foreach (DataRow row in dtEvents.Rows)
            {
                string organizer = row.Table.Columns.Contains("organizer")
                    ? row["organizer"].ToString()
                    : "Local Events";

                string venue = row.Table.Columns.Contains("venue")
                    ? row["venue"].ToString()
                    : "Various";

                string image = row.Table.Columns.Contains("image_url")
                    ? row["image_url"].ToString()
                    : ResolvePlaceholder("event");

                bool exists = false;
                foreach (DataRow g in groups.Rows)
                {
                    if (g["organizer"].ToString().Equals(organizer, StringComparison.OrdinalIgnoreCase) &&
                        g["venue"].ToString().Equals(venue, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    DataRow g = groups.NewRow();
                    g["organizer"] = organizer;
                    g["venue"] = venue;
                    g["image_url"] = image;
                    if (dtEvents.Columns.Contains("caption"))
                        g["caption"] = row["caption"].ToString();
                    else if (dtEvents.Columns.Contains("description"))
                        g["caption"] = row["description"].ToString();
                    else
                        g["caption"] = "Exciting events happening during your stay.";
                    groups.Rows.Add(g);
                }
            }

            rpt.ItemDataBound += (s, e) =>
            {
                if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
                {
                    string organizer = DataBinder.Eval(e.Item.DataItem, "organizer").ToString();
                    string venue = DataBinder.Eval(e.Item.DataItem, "venue").ToString();
                    Repeater child = (Repeater)e.Item.FindControl("rptEventOptions");

                    DataView view = new DataView(dtEvents)
                    {
                        RowFilter = $"organizer = '{organizer.Replace("'", "''")}' AND venue = '{venue.Replace("'", "''")}'"
                    };
                    child.DataSource = view;
                    child.DataBind();
                }
            };

            rpt.DataSource = groups;
            rpt.DataBind();
            panel.Visible = true;
        }

        // === Common Helpers ===
        private string FindFirst(DataTable src, string[] candidates)
        {
            foreach (string c in candidates)
                if (src.Columns.Contains(c))
                    return c;

            return null;
        }

        private void AddRoomImages(int vendorId, DataTable rooms)
        {
            try
            {
                foreach (DataRow r in rooms.Rows)
                {
                    r["image_url"] = GetRandomImage("~/Images/Rooms/", ResolvePlaceholder("airline"));
                }
            }
            catch
            {
                foreach (DataRow r in rooms.Rows)
                {
                    r["image_url"] = ResolvePlaceholder("airline");
                }
            }
        }


        private DataTable NormalizeRoomTypes(DataTable src)
        {
            DataTable t = new DataTable();
            t.Columns.AddRange(new[]
            {
                new DataColumn("id", typeof(int)),
                new DataColumn("room_type", typeof(string)),
                new DataColumn("max_occupancy", typeof(int)),
                new DataColumn("base_price", typeof(decimal)),
                new DataColumn("image_url", typeof(string))
            });

            string colId = FindFirst(src, new[] { "id", "room_id", "type_id", "room_type_id" });
            string colType = FindFirst(src, new[] { "room_type", "type_name", "name" });
            string colOcc = FindFirst(src, new[] { "max_occupancy", "capacity", "max_guests" });
            string colRate = FindFirst(src, new[] { "base_price", "price", "rate" });

            foreach (DataRow r in src.Rows)
            {
                if (colId == null || r[colId] == DBNull.Value)
                    continue;

                DataRow n = t.NewRow();
                n["id"] = Convert.ToInt32(r[colId]);
                n["room_type"] = (colType != null) ? Convert.ToString(r[colType]) : "Room";
                n["max_occupancy"] = (colOcc != null && r[colOcc] != DBNull.Value)
                    ? Convert.ToInt32(r[colOcc])
                    : 2;
                n["base_price"] = (colRate != null && r[colRate] != DBNull.Value)
                    ? Convert.ToDecimal(r[colRate])
                    : 0m;
                n["image_url"] = "";
                t.Rows.Add(n);
            }

            return t;
        }

        // === Image Helpers ===
        public string ResolvePlaceholder(string kind)
        {
            switch ((kind ?? "").ToLowerInvariant())
            {
                case "airline":
                    return "~/images/placeholders/airline.png";
                case "car":
                    return "~/images/placeholders/car.png";
                case "event":
                    return "~/images/placeholders/event.png";
                default:
                    return "~/images/no-image.png";
            }
        }

        public string GetPrimaryVendorImage(int vendorId)
        {
            try
            {
                var ds = sp.GetVendorDetails(vendorId);

                if (ds != null && ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    var imgs = ds.Tables[1];

                    foreach (DataRow r in imgs.Rows)
                        if (imgs.Columns.Contains("first_pic") && r["first_pic"] != DBNull.Value && Convert.ToBoolean(r["first_pic"]))
                            return Convert.ToString(r["image_url"]);

                    return Convert.ToString(imgs.Rows[0]["image_url"]);
                }
            }
            catch { }

            return ResolvePlaceholder("vendor");
        }

        public string GetAirlineLogo(string airlineName)
        {
            if (string.IsNullOrWhiteSpace(airlineName))
                return ResolvePlaceholder("airline");

            string k = airlineName.ToLowerInvariant();

            if (k.Contains("american")) return "~/images/airlines/aa.png";
            if (k.Contains("delta")) return "~/images/airlines/dl.png";
            if (k.Contains("united")) return "~/images/airlines/ua.png";

            return ResolvePlaceholder("airline");
        }

        public string GetCarClassImage(string carClassOrMake)
        {
            if (string.IsNullOrWhiteSpace(carClassOrMake))
                return ResolvePlaceholder("car");

            string k = carClassOrMake.ToLowerInvariant();

            if (k.Contains("suv")) return "~/images/cars/suv.png";
            if (k.Contains("compact")) return "~/images/cars/compact.png";
            if (k.Contains("midsize")) return "~/images/cars/midsize.png";

            return ResolvePlaceholder("car");
        }

        // === Add to package ===
        protected void ItemCommand_AddToPackage(object source, RepeaterCommandEventArgs e)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    lblError.Text = "Please sign in to add items to your vacation package.";
                    lblError.Visible = true;
                    return;
                }

                int userId = Convert.ToInt32(Session["UserID"]);

                int packageId;
                if (Session["PackageID"] != null)
                {
                    packageId = Convert.ToInt32(Session["PackageID"]);
                }
                else
                {
                    packageId = sp.PackageGetOrCreate(userId);
                    Session["PackageID"] = packageId;
                }

                string cmd = e.CommandName;
                int refId = Convert.ToInt32(e.CommandArgument);
                string type = null;

                switch (cmd)
                {
                    case "AddFlight":
                        type = "Flight";
                        break;
                    case "AddRoom":
                        type = "Hotel";
                        break;
                    case "AddCar":
                        type = "Car Rental";
                        break;
                    case "AddEvent":
                        type = "Event";
                        break;
                }

                if (string.IsNullOrEmpty(type))
                    return;

                DateTime? startUtc = null;
                DateTime? endUtc = null;
                int qty = 1;

                if (type == "Hotel" || type == "Car Rental")
                {
                    if (Session["TripStart"] != null)
                        startUtc = Convert.ToDateTime(Session["TripStart"]);
                    if (Session["TripEnd"] != null)
                        endUtc = Convert.ToDateTime(Session["TripEnd"]);
                }

                int result = sp.PackageAddUpdateItem(packageId, type, refId, qty, startUtc, endUtc);

                if (result >= 0 || result == -1)
                {
                    lblStatus.Text = $"{type} added successfully.";
                    lblStatus.CssClass = "text-success small d-block mb-3";
                    lblError.Visible = false;
                    LoadCart(userId);
                }
                else
                {
                    lblError.Text = $"Failed to add {type}.";
                    lblError.Visible = true;
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Error: " + ex.Message;
                lblError.Visible = true;
            }
        }

    }
}
