namespace CoreTripRex.Models.AccountInfoModels
{
    public class AccountInfoViewModel
    {
        private int _userId;
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _phone;
        private string _address;
        private string _city;
        private string _state;
        private string _zip;
        private string _country;

        private List<PaymentMethod> _paymentMethods;
        private List<TripPackage> _pastTrips;

        private string _cardNumber;
        private string _expiration;
        private string _message;

        public int UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        public string FirstName
        {
            get { return _firstName; }
            set { _firstName = value; }
        }

        public string LastName
        {
            get { return _lastName; }
            set { _lastName = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string Phone
        {
            get { return _phone; }
            set { _phone = value; }
        }

        public string Address
        {
            get { return _address; }
            set { _address = value; }
        }

        public string City
        {
            get { return _city; }
            set { _city = value; }
        }

        public string State
        {
            get { return _state; }
            set { _state = value; }
        }

        public string Zip
        {
            get { return _zip; }
            set { _zip = value; }
        }

        public string Country
        {
            get { return _country; }
            set { _country = value; }
        }

        public List<PaymentMethod> PaymentMethods
        {
            get { return _paymentMethods; }
            set { _paymentMethods = value; }
        }

        public List<TripPackage> PastTrips
        {
            get { return _pastTrips; }
            set { _pastTrips = value; }
        }

        public string CardNumber
        {
            get { return _cardNumber; }
            set { _cardNumber = value; }
        }

        public string Expiration
        {
            get { return _expiration; }
            set { _expiration = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }
    }
}
