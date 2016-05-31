using System;
using System.Collections.Generic;
using System.Linq;
using QBitNinja.Client.Models;

namespace HiddenBitcoin.DataClasses
{
    public class AddressHistory
    {
        public AddressHistory(string address, IEnumerable<BalanceOperation> operations)
        {
            Address = address;

            Records = new List<AddressHistoryRecord>();
            foreach (var operation in operations)
            {
                Records.Add(new AddressHistoryRecord(operation));
            }
        }

        public string Address { get; }
        public List<AddressHistoryRecord> Records { get; }
        public decimal TotalReceived => Records.Sum(x => x.Amount > 0 ? Math.Abs(x.Amount) : 0);
        public decimal TotalSpent => Records.Sum(x => x.Amount < 0 ? Math.Abs(x.Amount) : 0);
    }
}