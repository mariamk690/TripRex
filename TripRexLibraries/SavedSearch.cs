using System;

namespace TripRexLibraries
{
    [Serializable]
    public class SavedSearch
    {
        private string origin;
        private string destination;
        private DateTime departDate;
        private DateTime returnDate;

        public string Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        public string Destination
        {
            get { return destination; }
            set { destination = value; }
        }

        public DateTime DepartDate
        {
            get { return departDate; }
            set { departDate = value; }
        }

        public DateTime ReturnDate
        {
            get { return returnDate; }
            set { returnDate = value; }
        }
    }
}

