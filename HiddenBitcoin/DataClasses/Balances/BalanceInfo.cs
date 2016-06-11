namespace HiddenBitcoin.DataClasses.Balances
{
    public abstract class BalanceInfo
    {
        protected BalanceInfo(decimal unconfirmed, decimal confirmed)
        {
            Unconfirmed = unconfirmed;
            Confirmed = confirmed;
        }

        public decimal Confirmed { get; }
        public decimal Unconfirmed { get; }
        public decimal Balance => Confirmed + Unconfirmed;
    }
}