using System;
using HiddenBitcoin.DataClasses.Balances;
using HiddenBitcoin.DataClasses.Histories;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public abstract class Monitor
    {
        // ReSharper disable once InconsistentNaming
        protected readonly NBitcoin.Network _Network;

        protected Monitor(Network network)
        {
            _Network = Convert.ToNBitcoinNetwork(network);
        }

        public Network Network => Convert.ToHiddenBitcoinNetwork(_Network);

        public abstract AddressBalanceInfo GetAddressBalanceInfo(string address);
        public abstract TransactionInfo GetTransactionInfo(string transactionId);
        public abstract AddressHistory GetAddressHistory(string address);

        protected void AssertNetwork(Network network)
        {
            if (network != Network)
                throw new Exception("Wrong network");
        }

        protected void AssertNetwork(NBitcoin.Network network)
        {
            if (network != _Network)
                throw new Exception("Wrong network");
        }
    }
}