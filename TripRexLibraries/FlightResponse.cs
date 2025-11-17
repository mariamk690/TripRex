using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace TripRexLibraries
{
    public class FlightResponse
    {
        private int flightID;
        private string airCarrierName;
        private string flightNumber;
        private string departCode;
        private string arriveCode;
        private string departureTime;
        private string arrivalTime;
        private string classCode;
        private decimal price;
        private string imageUrl;
        private string caption;
        public int FlightID
        {
            get { return flightID; }
            set { flightID = value; }
        }

        public string AirCarrierName
        {
            get { return airCarrierName; }
            set { airCarrierName = value; }
        }

        public string FlightNumber
        {
            get { return flightNumber; }
            set { flightNumber = value; }
        }

        public string DepartCode
        {
            get { return departCode; }
            set { departCode = value; }
        }

        public string ArriveCode
        {
            get { return arriveCode; }
            set { arriveCode = value; }
        }

        public string DepartureTime
        {
            get { return departureTime; }
            set { departureTime = value; }
        }

        public string ArrivalTime
        {
            get { return arrivalTime; }
            set { arrivalTime = value; }
        }

        public string ClassCode
        {
            get { return classCode; }
            set { classCode = value; }
        }

        public decimal Price
        {
            get { return price; }
            set { price = value; }
        }

        public string ImageUrl
        {
            get { return imageUrl; }
            set { imageUrl = value; }
        }
        public string Caption
        {
            get { return caption; }
            set { caption = value; }
        }
    }
}

