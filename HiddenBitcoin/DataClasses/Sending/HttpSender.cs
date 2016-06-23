using System;
using System.Collections.Generic;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses.Sending
{
    public class HttpSender : Sender
    {
        protected readonly QBitNinjaClient Client;

        public HttpSender(Network network) : base(network)
        {
            Client = new QBitNinjaClient(_Network);
        }

        public override TransactionInfo SendAll(List<string> fromPrivateKeys, string toAddress,
            FeeType feeType = FeeType.Fastest, string message = "")
        {
            var addressAmountPair = new AddressAmountPair
            {
                Address = toAddress,
                Amount = 0 // doesn't matter, we send all
            };

            return Send(
                fromPrivateKeys,
                new List<AddressAmountPair> {addressAmountPair},
                feeType,
                message: message,
                sendAll: true
                );
        }

        public override TransactionInfo Send(List<string> fromPrivateKeys, List<AddressAmountPair> to,
            FeeType feeType = FeeType.Fastest, string changeAddress = "", string message = "", bool sendAll = false)
        {
            //// Set privatekeys
            //var fromKeys = new List<Key>();
            //var fromExtKeys = new List<ExtKey>();
            //foreach (var privatekey in fromPrivateKeys)
            //{
            //    try
            //    {
            //        var bitcoinSecret = new BitcoinSecret(privatekey);
            //        AssertNetwork(bitcoinSecret.Network);
            //        fromKeys.Add(bitcoinSecret.PrivateKey);
            //    }
            //    catch
            //    {
            //        var bitcoinExtKey = new BitcoinExtKey(privatekey);
            //        AssertNetwork(bitcoinExtKey.Network);
            //        fromExtKeys.Add(bitcoinExtKey.ExtKey);
            //    }
            //}

            //// Set changeScriptPubKey
            //if (sendAll)
            //    changeAddress = "";
            //Script changeScriptPubKey;
            //if (changeAddress != "")
            //{
            //    var changeBitcoinAddress = BitcoinAddress.Create(changeAddress);
            //    AssertNetwork(changeBitcoinAddress.Network);
            //    changeScriptPubKey = changeBitcoinAddress.ScriptPubKey;
            //}
            //else
            //{
            //    changeScriptPubKey = fromExtKeys.Count != 0
            //        ? fromExtKeys.First().ScriptPubKey
            //        : fromKeys.First().ScriptPubKey;
            //}

            //// Gather coins to spend
            //var client = new QBitNinjaClient(_Network);
            //var unspentCoins = new List<Coin>();
            //foreach (var extKey in fromExtKeys)
            //{
            //    var destination = extKey.ScriptPubKey.GetDestinationAddress(_Network);
            //    var balanceModel = client.GetBalance(destination, true).Result;
            //    foreach (var operation in balanceModel.Operations)
            //    {
            //        unspentCoins.Add(operation.);
            //    }
            //}
            //foreach (var key in fromKeys)
            //{
            //    var destination = key.ScriptPubKey.GetDestinationAddress(_Network);
            //    var balanceModel = client.GetBalance(destination, true).Result;
            //    unspentBalanceModels.Add(balanceModel);
            //}


            //foreach (var addressAmountPair in to)
            //    AssertNetwork(BitcoinAddress.Create(addressAmountPair.Address).Network);


            throw new NotImplementedException();
        }
    }
}