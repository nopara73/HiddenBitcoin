namespace HiddenBitcoin.DataClasses
{
    public struct BalanceInfo
    {
        public string Address { get; }
        public decimal Confirmed { get; }
        public decimal Unconfirmed { get; }
        public decimal Balance => Confirmed + Unconfirmed;

        public BalanceInfo(string address, decimal unconfirmed, decimal confirmed)
        {
            Unconfirmed = unconfirmed;
            Confirmed = confirmed;
            Address = address;
        }
    }
}