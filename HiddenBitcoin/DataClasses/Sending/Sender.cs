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

        public TransactionInfo CreateSpendAllTransaction(List<string> fromPrivateKeys, string toAddress,
            FeeType feeType = FeeType.Fastest, string message = "")
        {
            var addressAmountPair = new AddressAmountPair
            {
                Address = toAddress,
                Amount = 0 // doesn't matter, we send all
            };

            return CreateTransaction(
                fromPrivateKeys,
                new List<AddressAmountPair> { addressAmountPair },
                feeType,
                message: message,
                spendAll: true
                );
        }

        /// <summary>
        /// </summary>
        /// <param name="fromPrivateKeys"></param>
        /// <param name="to"></param>
        /// <param name="feeType"></param>
        /// <param name="changeAddress"></param>
        /// <param name="message"></param>
        /// <param name="spendAll">If true changeAddress and amounts of to does not matter, we send them all</param>
        /// <returns></returns>
        public abstract TransactionInfo CreateTransaction(List<string> fromPrivateKeys, List<AddressAmountPair> to,
            FeeType feeType = FeeType.Fastest, string changeAddress = "", string message = "", bool spendAll = false);

        public abstract void Send(string transactionId);
    }
}