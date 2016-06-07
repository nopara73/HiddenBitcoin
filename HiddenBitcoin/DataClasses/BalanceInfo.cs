namespace HiddenBitcoin.DataClasses
{
    public struct BalanceInfo
    {
        public decimal Confirmed;
        public decimal Unconfirmed;

        public BalanceInfo(decimal unconfirmed, decimal confirmed)
        {
            Unconfirmed = unconfirmed;
            Confirmed = confirmed;
        }
    }
}