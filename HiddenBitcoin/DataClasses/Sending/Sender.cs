using System;
using System.Collections.Generic;
using HiddenBitcoin.DataClasses.Interfaces;
using NBitcoin;

namespace HiddenBitcoin.DataClasses.Sending
{
    public abstract class Sender : IAssertNetwork
    {
        protected List<Transaction> CreatedTransactions = new List<Transaction>();

        // ReSharper disable once InconsistentNaming
        protected readonly NBitcoin.Network _Network;

        protected Sender(Network network)
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

        public abstract TransactionInfo CreateSendAllTransaction(List<string> fromPrivateKeys, string toAddress,
            FeeType feeType = FeeType.Fastest, string message = "");

        /// <summary>
        /// </summary>
        /// <param name="fromPrivateKeys"></param>
        /// <param name="to"></param>
        /// <param name="feeType"></param>
        /// <param name="changeAddress"></param>
        /// <param name="message"></param>
        /// <param name="sendAll">If true changeAddress and amounts of to does not matter, we send them all</param>
        /// <returns></returns>
        public abstract TransactionInfo CreateTransaction(List<string> fromPrivateKeys, List<AddressAmountPair> to,
            FeeType feeType = FeeType.Fastest, string changeAddress = "", string message = "", bool sendAll = false);

        public abstract void Send(string transactionId);
    }
}