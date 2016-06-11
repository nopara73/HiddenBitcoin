using System.Collections.Generic;
using System.Linq;
using HiddenBitcoin.DataClasses.KeyManagement;

namespace HiddenBitcoin.DataClasses.Balances
{
    public class SafeBalanceInfo : BalanceInfo
    {
        public List<AddressBalanceInfo> AddressBalances;

        public SafeBalanceInfo(Safe safe, List<AddressBalanceInfo> addressBalances) :
            base(addressBalances.Sum(x => x.Unconfirmed), addressBalances.Sum(x => x.Confirmed))
        {
            Safe = safe;
            AddressBalances = addressBalances;
        }

        public Safe Safe { get; }
        public int MonitoredAddressCount => AddressBalances.Count;
    }
}