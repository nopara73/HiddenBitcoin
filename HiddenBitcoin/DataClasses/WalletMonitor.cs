using System;
using HiddenBitcoin.Helpers;
using NBitcoin;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses
{
    public class WalletMonitor : Monitor
    {
        private readonly KeySetClient _keySet;
        private readonly NBitcoin.Network _network;
        private readonly QBitNinjaClient _qBitNinjaClient;

        public Network Network
        {
            get
            {
                if (_network == NBitcoin.Network.Main)
                    return Network.MainNet;
                if (_network == NBitcoin.Network.TestNet)
                    return Network.TestNet;
                throw new InvalidOperationException("WrongNetwork");
            }
        }

        private BitcoinExtPubKey SeedBitcoinExtPubKey { get; }

        public WalletMonitor(string seedPublicKey)
        {
            SeedBitcoinExtPubKey = new BitcoinExtPubKey(seedPublicKey);
            _network = SeedBitcoinExtPubKey.Network;
            _qBitNinjaClient = new QBitNinjaClient(_network);

            var walletName = KeyGenerator.GetUniqueKey(12);
            var wallet = _qBitNinjaClient.GetWalletClient(walletName);
            wallet.CreateIfNotExists().Wait();

            _keySet = wallet.GetKeySetClient("main");

            // ReSharper disable once CoVariantArrayConversion
            _keySet.CreateIfNotExists(new[] {SeedBitcoinExtPubKey.ExtPubKey}).Wait();

            //var foo = wallet.GetBalanceSummary().Result;
            //var balance = foo.UnConfirmed.Amount.ToDecimal(MoneyUnit.BTC);
            //Console.WriteLine(balance);

            //for (uint i = 0; i < 10; i++)
            //{
            //    Console.WriteLine(SeedBitcoinExtPubKey.ExtPubKey.Derive(i).GetWif(_network).ScriptPubKey.GetDestinationAddress(_network));
            //}

            //foreach (var addr in wallet.GetAddresses().Result)
            //{
            //    Console.WriteLine(addr.Address);
            //}
        }

        // Monitor
        //
        // get balance(address): address, balance, bool: confirmed
        // get transaction history(address): List<AddressTransaction>
        //
        // AddressTransactionHistory:
        //    bool: to the address or from the address,
        //    amount,
        //    addresses (the addresses the money come from OR went to), 
        //    timestamp, 
        //    bool: confirmed
        //    transaction id

        // WalletMonitor
        //
        // get balance (bool: confirmed)
        // get all addresses with balances (): address, balance, bool: confirmed
        // get transaction history (): List<WalletTransaction>
        //
        // WalletTransactionHistory
        //    bool: to the wallet or from the wallet,
        //    amount,
        //    from addresses, to addresses (the addresses the money come from AND went to),
        //    timestamp,
        //    bool: confirmed
        //    transaction id
        //
        // After MaxKeyNumber reached problem! you can only migrate
        //
        // sync happens by events, when anything changes, you should catch and handle it
    }
}