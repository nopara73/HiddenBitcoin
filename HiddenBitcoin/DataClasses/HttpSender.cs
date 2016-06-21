using System;
using System.Collections.Generic;
using System.Linq;
using HiddenBitcoin.DataClasses.Monitoring;
using NBitcoin;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses
{
    public class HttpSender
    {
        // ReSharper disable once InconsistentNaming
        protected readonly NBitcoin.Network _Network;
        protected readonly QBitNinjaClient Client;

        public HttpSender(Network network)
        {
            _Network = Convert.ToNBitcoinNetwork(network);
            Client = new QBitNinjaClient(_Network);
        }

        public Network Network => Convert.ToHiddenBitcoinNetwork(_Network);

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

        public TransactionInfo Send(HttpSafe from, List<AddressAmountPair> to, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = from.NotEmptyAddresses.Select(from.GetPrivateKey).ToList();

            return Send(
                notEmptyPrivateKeys,
                to,
                feeType,
                from.UnusedAddresses.First(),
                message);
        }

        public TransactionInfo SendAll(HttpSafe from, string toAddress, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = from.NotEmptyAddresses.Select(from.GetPrivateKey).ToList();

            var addressAmountPair = new AddressAmountPair
            {
                Address = toAddress,
                Amount = 0 // doesn't matter, we send all
            };

            return Send(
                notEmptyPrivateKeys,
                new List<AddressAmountPair> {addressAmountPair},
                feeType,
                message: message,
                sendAll: true
                );
        }

        public TransactionInfo Send(List<string> fromPrivateKeys, List<AddressAmountPair> to,
            FeeType feeType = FeeType.Fastest, string changeAddress = "", string message = "", bool sendAll = false)
        {
            AssertNetwork(BitcoinAddress.Create(changeAddress).Network);
            foreach (var privatekey in fromPrivateKeys)
            {
                try
                {
                    var secret = new BitcoinSecret(privatekey);
                    AssertNetwork(secret.Network);
                }
                catch
                {
                    var secret = new BitcoinExtKey(privatekey);
                    AssertNetwork(secret.Network);
                }
            }
            foreach (var addressAmountPair in to)
            {
                var address = BitcoinAddress.Create(addressAmountPair.Address);
                AssertNetwork(address.Network);
            }

            throw new NotImplementedException();
        }
    }
}