using System;
using NBitcoin;
using QBitNinja.Client.Models;

namespace HiddenBitcoin.DataClasses.Histories
{
    public class AddressHistoryRecord
    {
        private readonly BalanceOperation _operation;
        public readonly string Address;

        public AddressHistoryRecord(string address, BalanceOperation operation)
        {
            Address = address;
            _operation = operation;
        }

        public decimal Amount => _operation.Amount.ToDecimal(MoneyUnit.BTC);
        public DateTimeOffset DateTime => _operation.FirstSeen;
        public bool Confirmed => _operation.Confirmations > 0;
        public string TransactionId => _operation.TransactionId.ToString();
    }
}