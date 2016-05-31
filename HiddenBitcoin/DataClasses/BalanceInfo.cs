namespace HiddenBitcoin.DataClasses
{
    public struct BalanceInfo
    {
        public string Address;
        public decimal Confirmed;
        public decimal Unconfirmed;

        public BalanceInfo(string address, decimal unconfirmed, decimal confirmed)
        {
            Address = address;
            Unconfirmed = unconfirmed;
            Confirmed = confirmed;
        }
    }
}