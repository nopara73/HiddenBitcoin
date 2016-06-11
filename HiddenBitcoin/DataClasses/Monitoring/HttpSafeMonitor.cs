using System;
using HiddenBitcoin.DataClasses.Balances;
using HiddenBitcoin.DataClasses.Histories;
using HiddenBitcoin.DataClasses.KeyManagement;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class HttpSafeMonitor : HttpMonitor
    {
        public HttpSafeMonitor(Safe safe) : base(safe.Network)
        {
        }

        public SafeBalanceInfo GetSafeBalanceInfo()
        {
            throw new NotImplementedException();
        }

        public SafeHistory GetSafeHistory()
        {
            throw new NotImplementedException();
        }
    }
}