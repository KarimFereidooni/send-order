namespace SendOrder
{
    using Newtonsoft.Json;
    using System.IO;

    public class Data
    {
        private static Data instance;
        public static Data Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = ReadData();
                }

                return instance;
            }
        }

        public static Data ReadData()
        {
            return JsonConvert.DeserializeObject<Data>(File.ReadAllText("Data.json"));
        }

        public Proxy Proxy { get; set; }
        public string Url { get; set; }
        public Order Order { get; set; }
        public string Cookie { get; set; }
        public string StartsAt { get; set; }
        public string EndsAt { get; set; }
        public int SendCount { get; set; }
        public int SendInterval { get; set; }
    }

    public class Proxy
    {
        public bool Enabled { get; set; }
        public string Value { get; set; }
    }

    public class Order
    {
        public bool IsSymbolCautionAgreement { get; set; }
        public bool CautionAgreementSelected { get; set; }
        public bool IsSymbolSepahAgreement { get; set; }
        public bool SepahAgreementSelected { get; set; }
        public int OrderCount { get; set; }
        public int OrderPrice { get; set; }
        public int FinancialProviderId { get; set; }
        public string MinimumQuantity { get; set; }
        public int MaxShow { get; set; }
        public int OrderId { get; set; }
        public string Isin { get; set; }
        public int OrderSide { get; set; }
        public int OrderValidity { get; set; }
        public string OrderValiditydate { get; set; }
        public bool ShortSellIsEnabled { get; set; }
        public int ShortSellIncentivePercent { get; set; }
    }
}
