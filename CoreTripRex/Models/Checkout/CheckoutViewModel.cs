using ScottPlot;
using System.Collections.Generic;
using System;

namespace CoreTripRex.Models.Checkout
{
    public class CheckoutItemViewModel
    {
        private string _displayName;
        private string _serviceType;
        private decimal _computedTotal;
        private string _computedDates;
        private string _computedQtyLabel;


        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        public string ServiceType
        {
            get { return _serviceType; }
            set { _serviceType = value; }
        }

        public decimal ComputedTotal
        {
            get { return _computedTotal; }
            set { _computedTotal = value; }
        }

        public string ComputedDates
        {
            get { return _computedDates; }
            set { _computedDates = value; }
        }

        public string ComputedQtyLabel
        {
            get { return _computedQtyLabel; }
            set { _computedQtyLabel = value; }
        }
    }

    public class CheckoutViewModel
    {
        private string _userGreeting;
        private string _message;
        private string _messageCssClass;
        private bool _hasMessage;

        private List<CheckoutItemViewModel> _items;
        private decimal _total;
        private string _chartImageUrl;
        private bool _addCard;
        private string _cardNumber;
        private string _exp;

        private bool _isSuccess;
        public bool HasSavedCard { get; set; }
        public string? SavedCardLabel { get; set; }

        public string UserGreeting
        {
            get { return _userGreeting; }
            set { _userGreeting = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public string MessageCssClass
        {
            get { return _messageCssClass; }
            set { _messageCssClass = value; }
        }

        public bool HasMessage
        {
            get { return _hasMessage; }
            set { _hasMessage = value; }
        }

        public List<CheckoutItemViewModel> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public decimal Total
        {
            get { return _total; }
            set { _total = value; }
        }
        public string ChartImageUrl
        {
            get { return _chartImageUrl; }
            set { _chartImageUrl = value; }
        }

        public bool AddCard
        {
            get { return _addCard; }
            set { _addCard = value; }
        }

        public string CardNumber
        {
            get { return _cardNumber; }
            set { _cardNumber = value; }
        }

        public string Exp
        {
            get { return _exp; }
            set { _exp = value; }
        }

        public bool IsSuccess
        {
            get { return _isSuccess; }
            set { _isSuccess = value; }
        }

        public CheckoutViewModel()
        {
            _items = new List<CheckoutItemViewModel>();
        }
    }
}
