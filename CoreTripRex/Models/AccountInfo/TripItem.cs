namespace CoreTripRex.Models.AccountInfo
{
    public class TripItem
    {
        private string _type;
        private string _name;
        private string _startDate;
        private string _endDate;

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string StartDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }

        public string EndDate
        {
            get { return _endDate; }
            set { _endDate = value; }
        }
    }
}
