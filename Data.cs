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
        public Order Order { get; set; }
        public string Cookie { get; set; }
        public string StartsAt { get; set; }
        public string EndsAt { get; set; }
        public int SendCount { get; set; }
        public int SendInterval { get; set; }
        public int NonceInterval { get; set; }
    }

    public class Proxy
    {
        public bool Enabled { get; set; }
        public string Value { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerTitle { get; set; }
        public string OrderSide { get; set; }
        public int OrderSideId { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
        public int Value { get; set; }
        public string ValidityDate { get; set; }
        public string MinimumQuantity { get; set; }
        public string DisclosedQuantity { get; set; }
        public int ValidityType { get; set; }
        public int InstrumentId { get; set; }
        public string InstrumentIsin { get; set; }
        public string InstrumentName { get; set; }
        public int BankAccountId { get; set; }
        public int ExpectedRemainingQuantity { get; set; }
        public int TradedQuantity { get; set; }
        public string CategoryId { get; set; }
        public int RemainingQuantity { get; set; }
        public int OrderExecuterId { get; set; }
    }
}
