using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using Utilities;
using System.Collections.Generic;

namespace AirlineController.Controllers
{
    [Produces("application/json")]
    [Route("api/Flights")]
    [ApiController]
    public class FlightsController : Controller
    {
        DBConnect objDB = new DBConnect();

        // GET api/Flights/GetAirCarriers?departureCity=Philadelphia&departureState=Pennsylvania&arrivalCity=Las Vegas&arrivalState=Nevada
        [HttpGet("GetAirCarriers")]
        public List<AirCarrier> GetAirCarriers(string departureCity, string departureState, string arrivalCity, string arrivalState)
        {
            List<AirCarrier> carriers = new List<AirCarrier>();

            SqlCommand objCommand = new SqlCommand();
            objCommand.CommandType = CommandType.StoredProcedure;
            objCommand.CommandText = "usp_AirCarriers_Get";
            objCommand.Parameters.AddWithValue("@DepartureCity", departureCity);
            objCommand.Parameters.AddWithValue("@DepartureState", departureState);
            objCommand.Parameters.AddWithValue("@ArrivalCity", arrivalCity);
            objCommand.Parameters.AddWithValue("@ArrivalState", arrivalState);

            DataSet ds = objDB.GetDataSetUsingCmdObj(objCommand);

            int count = ds.Tables[0].Rows.Count;

            for (int i = 0; i < count; i++)
            {
                AirCarrier ac = new AirCarrier();
                ac.AirCarrierID = int.Parse(objDB.GetField("AirCarrierID", i).ToString());
                ac.AirCarrierName = objDB.GetField("AirCarrierName", i).ToString();
                ac.Phone = objDB.GetField("phone", i).ToString();
                ac.Email = objDB.GetField("email", i).ToString();
                ac.Description = objDB.GetField("description", i).ToString();

                carriers.Add(ac);
            }

            return carriers;
        }

        // GET api/Flights/GetFlights?airCarrierId=8&departureCity=Philadelphia&departureState=Pennsylvania&arrivalCity=Las Vegas&arrivalState=Nevada
        [HttpGet("GetFlights")]
        public IActionResult GetFlights(int airCarrierId, string departureCity, string departureState, string arrivalCity, string arrivalState)
        {
            try
            {
                List<Flight> flights = new List<Flight>();

                SqlCommand objCommand = new SqlCommand();
                objCommand.CommandType = CommandType.StoredProcedure;
                objCommand.CommandText = "usp_Flights_ByCarrier";
                objCommand.Parameters.AddWithValue("@AirCarrierID", airCarrierId);
                objCommand.Parameters.AddWithValue("@DepartureCity", departureCity);
                objCommand.Parameters.AddWithValue("@DepartureState", departureState);
                objCommand.Parameters.AddWithValue("@ArrivalCity", arrivalCity);
                objCommand.Parameters.AddWithValue("@ArrivalState", arrivalState);

                DataSet ds = objDB.GetDataSetUsingCmdObj(objCommand);
                int count = ds.Tables[0].Rows.Count;

                for (int i = 0; i < count; i++)
                {
                    Flight f = new Flight();
                    f.FlightID = int.Parse(objDB.GetField("FlightId", i).ToString());
                    f.AirCarrierName = objDB.GetField("AirCarrierName", i).ToString();
                    f.FlightNumber = objDB.GetField("flight_number", i).ToString();
                    f.DepartCode = objDB.GetField("DepartCode", i).ToString();
                    f.ArriveCode = objDB.GetField("ArriveCode", i).ToString();
                    f.DepartureTime = objDB.GetField("departure_time", i).ToString();
                    f.ArrivalTime = objDB.GetField("arrival_time", i).ToString();
                    f.ClassCode = objDB.GetField("class_code", i).ToString();
                    f.Price = decimal.Parse(objDB.GetField("price", i).ToString());
                    f.Caption = objDB.GetField("caption", i)?.ToString() ?? "Multiple routes and fare classes available";

                    flights.Add(f);
                }

                return Ok(flights);
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "errorlog.txt");

                System.IO.File.AppendAllText(logPath,
                    $"{DateTime.Now}: {ex.Message}\n{ex.StackTrace}\n\n");

                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }



        // POST api/Flights/FindFlights
        [HttpPost("FindFlights")]
        public List<Flight> FindFlights([FromBody] FlightRequirements req)
        {
            List<Flight> flights = new List<Flight>();

            SqlCommand objCommand = new SqlCommand();
            objCommand.CommandType = CommandType.StoredProcedure;
            objCommand.CommandText = "usp_Flights_Search";
            objCommand.Parameters.AddWithValue("@from_code", req.FromCode);
            objCommand.Parameters.AddWithValue("@to_code", req.ToCode);
            objCommand.Parameters.AddWithValue("@depart_date", req.DepartDate.Date);
            objCommand.Parameters.AddWithValue("@class_code", (object)req.ClassCode ?? DBNull.Value);
            objCommand.Parameters.AddWithValue("@minPrice", (object)req.MinPrice ?? DBNull.Value);
            objCommand.Parameters.AddWithValue("@maxPrice", (object)req.MaxPrice ?? DBNull.Value);

            DataSet ds = objDB.GetDataSetUsingCmdObj(objCommand);
            int count = ds.Tables[0].Rows.Count;

            for (int i = 0; i < count; i++)
            {
                Flight f = new Flight();
                f.FlightID = int.Parse(objDB.GetField("flight_id", i).ToString());
                f.AirCarrierName = objDB.GetField("vendor_name", i).ToString();
                f.FlightNumber = objDB.GetField("flight_number", i).ToString();
                f.DepartCode = objDB.GetField("depart_code", i).ToString();
                f.ArriveCode = objDB.GetField("arrive_code", i).ToString();
                f.DepartureTime = objDB.GetField("departure_time", i).ToString();
                f.ArrivalTime = objDB.GetField("arrival_time", i).ToString();
                f.ClassCode = objDB.GetField("class_code", i).ToString();
                f.Price = decimal.Parse(objDB.GetField("price", i).ToString());
                f.ImageUrl = objDB.GetField("image_url", i).ToString();
                f.Caption = objDB.GetField("caption", i)?.ToString() ?? "Multiple routes and fare classes available";


                flights.Add(f);
            }


            return flights;
        }

        // POST api/Flights/FilterFlightsByCarrier
        [HttpPost("FilterFlightsByCarrier")]
        public List<Flight> FilterFlightsByCarrier([FromBody] FilteredFlightRequest req)
        {
            List<Flight> flights = new List<Flight>();

            SqlCommand objCommand = new SqlCommand();
            objCommand.CommandType = CommandType.StoredProcedure;
            objCommand.CommandText = "usp_Flights_FilterByCarrier";
            objCommand.Parameters.AddWithValue("@AirCarrierID", req.AirCarrierID);
            objCommand.Parameters.AddWithValue("@from_code", req.Requirements.FromCode);
            objCommand.Parameters.AddWithValue("@to_code", req.Requirements.ToCode);
            objCommand.Parameters.AddWithValue("@depart_date", req.Requirements.DepartDate.Date);
            objCommand.Parameters.AddWithValue("@class_code", (object)req.Requirements.ClassCode ?? DBNull.Value);
            objCommand.Parameters.AddWithValue("@minPrice", (object)req.Requirements.MinPrice ?? DBNull.Value);
            objCommand.Parameters.AddWithValue("@maxPrice", (object)req.Requirements.MaxPrice ?? DBNull.Value);

            DataSet ds = objDB.GetDataSetUsingCmdObj(objCommand);
            int count = ds.Tables[0].Rows.Count;

            for (int i = 0; i < count; i++)
            {
                Flight f = new Flight();
                f.FlightID = int.Parse(objDB.GetField("FlightId", i).ToString());
                f.AirCarrierName = objDB.GetField("AirCarrierName", i).ToString();
                f.FlightNumber = objDB.GetField("flight_number", i).ToString();
                f.DepartCode = objDB.GetField("DepartCode", i).ToString();
                f.ArriveCode = objDB.GetField("ArriveCode", i).ToString();
                f.DepartureTime = objDB.GetField("departure_time", i).ToString();
                f.ArrivalTime = objDB.GetField("arrival_time", i).ToString();
                f.ClassCode = objDB.GetField("class_code", i).ToString();
                f.Price = decimal.Parse(objDB.GetField("price", i).ToString());

                flights.Add(f);
            }

            return flights;
        }

        [HttpPost("Reserve")]
        public int Reserve([FromBody] ReservationRequest request)
        {
            if (!IsAuthorized(request.TravelSiteID, request.TravelSiteAPIToken))
                return -1;

            SqlConnection objConn = new SqlConnection("server=127.0.0.1,5555;Database=fa25_3342_tuo90411;User id=tuo90411;Password=gah4fahK7e");
            SqlCommand objCommand = new SqlCommand("usp_Flight_Reserve", objConn);
            objCommand.CommandType = CommandType.StoredProcedure;

            objCommand.Parameters.AddWithValue("@AirCarrierID", request.AirCarrierID);
            objCommand.Parameters.AddWithValue("@FlightID", request.Flight.FlightID);
            objCommand.Parameters.AddWithValue("@CustomerName", request.Customer.FullName);
            objCommand.Parameters.AddWithValue("@CustomerEmail", request.Customer.Email);
            objCommand.Parameters.AddWithValue("@CustomerPhone", request.Customer.Phone);
            objCommand.Parameters.AddWithValue("@RoundTrip", request.Flight.RoundTrip);

            SqlParameter outputParam = new SqlParameter("@ReservationID", SqlDbType.Int);
            outputParam.Direction = ParameterDirection.Output;
            objCommand.Parameters.Add(outputParam);

            objConn.Open();
            objCommand.ExecuteNonQuery();
            int reservationId = Convert.ToInt32(outputParam.Value);
            objConn.Close();

            return reservationId;
        }
        private bool IsAuthorized(string travelSiteId, string apiToken)
        {
            return travelSiteId == "TripRex" && apiToken == "ABC123TOKEN";
        }
    }

    public class AirCarrier
    {
        public int AirCarrierID { get; set; }
        public string AirCarrierName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
    }

    public class Flight
    {
        public int FlightID { get; set; }
        public string AirCarrierName { get; set; }
        public string FlightNumber { get; set; }
        public string DepartCode { get; set; }
        public string ArriveCode { get; set; }
        public string DepartureTime { get; set; }
        public string ArrivalTime { get; set; }
        public string ClassCode { get; set; }
        public string Caption { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; internal set; }
    }

    public class FlightRequirements
    {
        public string FromCode { get; set; }
        public string ToCode { get; set; }
        public DateTime DepartDate { get; set; }
        public string? ClassCode { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public class FilteredFlightRequest
    {
        public int AirCarrierID { get; set; }
        public FlightRequirements Requirements { get; set; }
    }

    public class ReservationRequest
    {
        public int AirCarrierID { get; set; }
        public FlightInfo Flight { get; set; }
        public CustomerInfo Customer { get; set; }
        public string TravelSiteID { get; set; }
        public string TravelSiteAPIToken { get; set; }
    }

    public class FlightInfo
    {
        public int FlightID { get; set; }
        public bool RoundTrip { get; set; }
    }

    public class CustomerInfo
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
