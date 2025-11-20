namespace CoreTripRex.Models.AccountInfoModels
{
    public class PaymentMethod
    {
        private int _id;
        private string _brand;
        private string _last4;
        private int _expMonth;
        private int _expYear;
        private bool _isDefault;

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Brand
        {
            get { return _brand; }
            set { _brand = value; }
        }

        public string Last4
        {
            get { return _last4; }
            set { _last4 = value; }
        }

        public int ExpMonth
        {
            get { return _expMonth; }
            set { _expMonth = value; }
        }

        public int ExpYear
        {
            get { return _expYear; }
            set { _expYear = value; }
        }

        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; }
        }
    }
}
