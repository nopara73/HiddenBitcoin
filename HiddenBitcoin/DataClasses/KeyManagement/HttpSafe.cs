using System;
using System.Collections.Generic;
using System.Linq;
using HiddenBitcoin.DataClasses.Monitoring;

namespace HiddenBitcoin.DataClasses.KeyManagement
{
    public class HttpSafe : LimitedSafe
    {
        internal HttpSafe(HttpSafeMonitor httpSafeMonitor)
            : base(httpSafeMonitor.BaseSafe, httpSafeMonitor.AddressCount)
        {
            HttpSafeMonitor = httpSafeMonitor;
        }

        public HttpSafeMonitor HttpSafeMonitor { get; }

        public List<string> UnusedAddresses
        {
            get
            {
                var unusedAddresses = Addresses.ToList();
                foreach (var addressHistoryRecord in HttpSafeMonitor.SafeHistory.Records)
                {
                    unusedAddresses.Remove(addressHistoryRecord.Address);
                }

                if (unusedAddresses.Count == 0)
                    throw new ArgumentException("Every address of HttpSafe has been used.");

                return unusedAddresses;
            }
        }

        public List<string> NotEmptyAddresses
            => HttpSafeMonitor.SafeBalanceInfo.AddressBalances.Where(
                addressBalanceInfo => addressBalanceInfo.Balance > 0)
                .Select(addressBalanceInfo => addressBalanceInfo.Address).ToList();
    }
}