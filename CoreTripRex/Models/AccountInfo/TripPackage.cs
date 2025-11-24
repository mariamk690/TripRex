namespace CoreTripRex.Models.AccountInfo
{
    public class TripPackage
    {
        private string _title;
        private string _startDate;
        private string _endDate;

        public string Title
        {
            get { return _title; }
            set { _title = value; }
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
