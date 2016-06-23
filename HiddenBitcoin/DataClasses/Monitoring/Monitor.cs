using System;
using HiddenBitcoin.DataClasses.Balances;
using HiddenBitcoin.DataClasses.Histories;
using HiddenBitcoin.DataClasses.Interfaces;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public abstract class Monitor : IAssertNetwork
    {
        // ReSharper disable once InconsistentNaming
        protected readonly NBitcoin.Network _Network;

        protected Monitor(Network network)
        {
            _Network = Convert.ToNBitcoinNetwork(network);
        }

        public Network Network => Convert.ToHiddenBitcoinNetwork(_Network);

        public void AssertNetwork(Network network)
        {
            if (network != Network)
                throw new Exception("Wrong network");
        }

        public void AssertNetwork(NBitcoin.Network network)
        {
            if (network != _Network)
                throw new Exception("Wrong network");
        }

        public abstract AddressBalanceInfo GetAddressBalanceInfo(string address);
        public abstract TransactionInfo GetTransactionInfo(string transactionId);
        public abstract AddressHistory GetAddressHistory(string address);
    }
}