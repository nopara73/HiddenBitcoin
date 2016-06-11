namespace HiddenBitcoin.DataClasses.Balances
{
    public class AddressBalanceInfo : BalanceInfo
    {
        public AddressBalanceInfo(string address, decimal unconfirmed, decimal confirmed) : base(unconfirmed, confirmed)
        {
            Address = address;
        }

        public string Address { get; }
    }
}