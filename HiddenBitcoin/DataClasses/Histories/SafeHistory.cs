using System.Collections.Generic;
using System.Linq;
using HiddenBitcoin.DataClasses.KeyManagement;

namespace HiddenBitcoin.DataClasses.Histories
{
    public class SafeHistory : History
    {
        public SafeHistory(Safe safe, IEnumerable<AddressHistory> addressHistories)
            : base(addressHistories.SelectMany(addressHistory => addressHistory.Records).ToList())
        {
            Safe = safe;
        }

        public Safe Safe { get; }
    }
}