namespace CoreTripRex.Models.AccountInfo
{
    public class TripPackage
    {
        private int _id;
        private string _title;
        private string _startDate;
        private string _endDate;
        private List<TripItem> _items;

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

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

        public List<TripItem> Items
        {
            get { return _items; }
            set { _items = value; }
        }
    }
}
