using System;
using System.Data;
using System.Data.SqlClient;


namespace Utilities
{
    public class StoredProcs
    {
        DBConnect db = new DBConnect();

        //Account procedures
        public int AccountLogin(string email, byte[] passwordHash)
        {
            SqlCommand sql = new SqlCommand("usp_Account_Login");
            sql.CommandType = CommandType.StoredProcedure;

            sql.Parameters.AddWithValue("@email", email);
            sql.Parameters.AddWithValue("@password_hash", passwordHash);

            SqlParameter output = new SqlParameter("@user_id", SqlDbType.Int);
            output.Direction = ParameterDirection.Output; 
            sql.Parameters.Add(output);

            db.DoUpdate(sql);

            if (output.Value != DBNull.Value)
                return Convert.ToInt32(output.Value);
            else
                return 0;
        }


        public int AccountRegister(string role, string firstName, string lastName, string email, byte[] passwordHash, string phone)
        {
            SqlCommand sql = new SqlCommand("usp_Account_Register");
            sql.CommandType = CommandType.StoredProcedure;

            sql.Parameters.AddWithValue("@role", role);
            sql.Parameters.AddWithValue("@first_name", firstName);
            sql.Parameters.AddWithValue("@last_name", lastName);
            sql.Parameters.AddWithValue("@email", email);
            sql.Parameters.AddWithValue("@password_hash", passwordHash);
            sql.Parameters.AddWithValue("@phone", phone);

            SqlParameter output = new SqlParameter("@user_id", SqlDbType.Int);
            output.Direction = ParameterDirection.Output; 
            sql.Parameters.Add(output);

            db.DoUpdate(sql);

            if (output.Value != DBNull.Value)
                return Convert.ToInt32(output.Value);
            else
                return 0;
        }


        // lookup stored procedures
        public DataSet AirportList(int? cityId)
        {
            SqlCommand sql = new SqlCommand("usp_Airports_List");
            sql.CommandType = CommandType.StoredProcedure;
            if (cityId.HasValue)
                sql.Parameters.AddWithValue("@city_id", cityId.Value);
            else
                sql.Parameters.AddWithValue("@city_id", DBNull.Value);

            DataSet ds = db.GetDataSet(sql);
            return ds;
        }

        public DataSet CitiesList()
        {
            SqlCommand sql = new SqlCommand("usp_Cities_List");
            sql.CommandType = CommandType.StoredProcedure;
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }

        //car procedures
        public DataSet CarsSearch(int cityId)
        {
            SqlCommand sql = new SqlCommand("usp_Cars_Search");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@city_id", cityId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }
        public int ApiRentalCarInsert(int apiCarId, string vendorName, string model,
                              string carClass, int seats, decimal dailyRate,
                              string imageUrl)
        {
            SqlCommand cmd = new SqlCommand
            {
                CommandType = CommandType.StoredProcedure,
                CommandText = "usp_ApiRentalCar_Insert"
            };

            cmd.Parameters.AddWithValue("@api_car_id", apiCarId);
            cmd.Parameters.AddWithValue("@vendor_name", vendorName ?? string.Empty);
            cmd.Parameters.AddWithValue("@model", model ?? string.Empty);
            cmd.Parameters.AddWithValue("@car_class", carClass ?? string.Empty);
            cmd.Parameters.AddWithValue("@seats", seats);
            cmd.Parameters.AddWithValue("@daily_rate", dailyRate);
            cmd.Parameters.AddWithValue("@image_url", (object)imageUrl ?? DBNull.Value);

            var outParam = new SqlParameter("@new_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            DBConnect db = new DBConnect();
            db.DoUpdateUsingCmdObj(cmd);

            return (int)outParam.Value;
        }


        //event procedures
        public DataSet EventDetails(int eventId)
        {
            SqlCommand sql = new SqlCommand("usp_Event_GetDetails");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@event_id", eventId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }
        public int ApiEventInsert(int apiEventId, string name, string venue,
                          decimal price, DateTime start, DateTime end)
        {
            DBConnect db = new DBConnect();
            SqlCommand cmd = new SqlCommand
            {
                CommandType = CommandType.StoredProcedure,
                CommandText = "usp_ApiEvent_Insert"
            };

            cmd.Parameters.AddWithValue("@api_event_id", apiEventId);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@venue", venue);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@start_time", start);
            cmd.Parameters.AddWithValue("@end_time", end);

            DataSet ds = db.GetDataSetUsingCmdObj(cmd);

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return Convert.ToInt32(ds.Tables[0].Rows[0][0]);
            }

            throw new Exception("ApiEventInsert failed: No return value.");
        }

        public DataSet EventSearch(int cityId, DateTime startUtc, DateTime endUtc)
        {
            SqlCommand sql = new SqlCommand("usp_Events_Search");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@city_id", cityId);
            sql.Parameters.AddWithValue("@start_utc", startUtc);
            sql.Parameters.AddWithValue("@end_utc", endUtc);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }

        //flight procedures
        public DataSet FlightDetails(int flightId)
        {
            SqlCommand sql = new SqlCommand("usp_Flight_GetDetails");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@flight_id", flightId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }
        public DataSet FlightSearch(string fromCode, string toCode, DateTime departDate, string classCode, decimal? minPrice, decimal? maxPrice)
        {
            SqlCommand sql = new SqlCommand("usp_Flights_Search");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@from_code", fromCode);
            sql.Parameters.AddWithValue("@to_code", toCode);
            sql.Parameters.AddWithValue("@depart_date", departDate);
            sql.Parameters.AddWithValue("@class_code", classCode);
            if (minPrice.HasValue)
                sql.Parameters.AddWithValue("@minPrice", minPrice.Value);
            else
                sql.Parameters.AddWithValue("@minPrice", DBNull.Value);
            if (maxPrice.HasValue)
                sql.Parameters.AddWithValue("@maxPrice", maxPrice.Value);
            else
                sql.Parameters.AddWithValue("@maxPrice", DBNull.Value);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }

        //hotel procedures
        public DataSet HotelRooms(int vendorId)
        {
            SqlCommand sql = new SqlCommand("usp_Hotel_GetRoomTypes");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@vendor_id", vendorId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }
        public DataSet HotelSearchbyCity(int cityId)
        {
            SqlCommand sql = new SqlCommand("usp_Hotels_SearchByCity");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@city_id", cityId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }

        //package procedures
        public int PackageAddUpdateItem(int packageId, string serviceType, int refId, int qty, DateTime? startUtc, DateTime? endUtc)
        {
            SqlCommand sql = new SqlCommand("usp_Package_AddOrUpdateItem");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@package_id", packageId);
            sql.Parameters.AddWithValue("@service_type", serviceType);
            sql.Parameters.AddWithValue("@ref_id", refId);
            sql.Parameters.AddWithValue("@qty", qty);
            sql.Parameters.AddWithValue("@start_utc", (object)startUtc ?? DBNull.Value);
            sql.Parameters.AddWithValue("@end_utc", (object)endUtc ?? DBNull.Value);
            return db.DoUpdate(sql);
        }

        public int PackageClear(int packageId)
        {
            SqlCommand sql = new SqlCommand("usp_Package_Clear");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@package_id", packageId);
            return db.DoUpdateUsingCmdObj(sql);
        }

        public DataSet PackageGet(int packageId)
        {
            SqlCommand sql = new SqlCommand("usp_Package_Get");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@package_id", packageId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }
        public int PackageGetOrCreate(int userId)
        {
            SqlCommand sql = new SqlCommand("usp_Package_GetOrCreate");
            sql.CommandType = CommandType.StoredProcedure;

            sql.Parameters.AddWithValue("@user_id", userId);

            SqlParameter output = new SqlParameter("@package_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            sql.Parameters.Add(output);

            db.DoUpdate(sql);

            return Convert.ToInt32(output.Value);
        }


        public int PackageGetActive(int userId)
        {
            SqlCommand sql = new SqlCommand("usp_Package_GetActive");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@user_id", userId);

            DataSet ds = db.GetDataSet(sql);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return Convert.ToInt32(ds.Tables[0].Rows[0]["id"]);
            }

            return 0;
        }


        public DataSet PastPackages(int userId)
        {
            SqlCommand sql = new SqlCommand("usp_PastPackages_List");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@user_id", userId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }
        public int CheckoutPackage(int packageId, int paymentMethodId, string currency, string processorRef)
        {
            SqlCommand sql = new SqlCommand("usp_Checkout_BookPackage");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@package_id", packageId);
            sql.Parameters.AddWithValue("@payment_method_id", paymentMethodId);
            sql.Parameters.AddWithValue("@currency", currency);
            sql.Parameters.AddWithValue("@processor_ref", (object)processorRef ?? DBNull.Value);

            SqlParameter retParam = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
            retParam.Direction = ParameterDirection.ReturnValue;
            sql.Parameters.Add(retParam);

            db.DoUpdate(sql);

            return (retParam.Value == DBNull.Value) ? -1 : Convert.ToInt32(retParam.Value);
        }



        //payment method procedures
        public int AddPaymentMethod(int userId, string brand, string last4, int? expMonth, int? expYear, bool setDefault)
        {
            SqlCommand sql = new SqlCommand("usp_PaymentMethods_Add");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@user_id", userId);
            sql.Parameters.AddWithValue("@brand", (object)brand ?? DBNull.Value);
            sql.Parameters.AddWithValue("@last4", last4);
            sql.Parameters.AddWithValue("@exp_month", (object)expMonth ?? DBNull.Value);
            sql.Parameters.AddWithValue("@exp_year", (object)expYear ?? DBNull.Value);
            sql.Parameters.AddWithValue("@set_default", setDefault ? 1 : 0);

            SqlParameter output = new SqlParameter("@new_id", SqlDbType.Int);
            output.Direction = ParameterDirection.Output;
            sql.Parameters.Add(output);

            return db.DoUpdate(sql);
        }

        public int DeletePaymentMethod(int userId, int methodId)
        {
            SqlCommand sql = new SqlCommand("usp_PaymentMethods_Delete");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@user_id", userId);
            sql.Parameters.AddWithValue("@method_id", methodId);

            return db.DoUpdate(sql);
        }

        public DataSet ListPaymentMethods(int userId)
        {
            SqlCommand sql = new SqlCommand("usp_PaymentMethods_List");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@user_id", userId);
            DataSet ds = db.GetDataSet(sql);

            return ds;
        }

        public int SetDefaultPaymentMethod(int userId, int methodId)
        {
            SqlCommand sql = new SqlCommand("usp_PaymentMethods_SetDefault");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@user_id", userId);
            sql.Parameters.AddWithValue("@method_id", methodId);
            return db.DoUpdate(sql);
        }

        //profile procedures
        public DataSet GetProfile(int userId)
        {
            SqlCommand sql = new SqlCommand("usp_Profile_Get");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@user_id", userId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }

        public int UpdateProfile(int userId, string firstName, string lastName, string email, string phone, string address, string city, string state, string zipCode, string country)
        {
            SqlCommand sql = new SqlCommand("usp_Profile_Update");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@user_id", userId);
            sql.Parameters.AddWithValue("@first_name", (object)firstName ?? DBNull.Value);
            sql.Parameters.AddWithValue("@last_name", (object)lastName ?? DBNull.Value);
            sql.Parameters.AddWithValue("@email", (object)email ?? DBNull.Value);
            sql.Parameters.AddWithValue("@phone", (object)phone ?? DBNull.Value);
            sql.Parameters.AddWithValue("@address", (object)address ?? DBNull.Value);
            sql.Parameters.AddWithValue("@city", (object)city ?? DBNull.Value);
            sql.Parameters.AddWithValue("@state", (object)state ?? DBNull.Value);
            sql.Parameters.AddWithValue("@zip_code", (object)zipCode ?? DBNull.Value);
            sql.Parameters.AddWithValue("@country", (object)country ?? DBNull.Value);
            return db.DoUpdate(sql);
        }

        //vendor procedure
        public DataSet GetVendorDetails(int vendorId)
        {
            SqlCommand sql = new SqlCommand("usp_Vendor_GetDetails");
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("@vendor_id", vendorId);
            DataSet ds = db.GetDataSet(sql);
            return ds;
        }
    }
}