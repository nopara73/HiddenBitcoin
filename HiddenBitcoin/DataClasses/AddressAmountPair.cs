namespace HiddenBitcoin.DataClasses
{
    public struct AddressAmountPair
    {
        public AddressAmountPair(string address, decimal amount)
        {
            Address = address;
            Amount = amount;    
        }
        public string Address { get; set; }
        public decimal Amount { get; set; }
    }
}