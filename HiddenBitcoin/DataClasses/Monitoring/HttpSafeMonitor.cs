using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HiddenBitcoin.DataClasses.Balances;
using HiddenBitcoin.DataClasses.Histories;
using HiddenBitcoin.DataClasses.KeyManagement;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class HttpSafeMonitor : HttpMonitor
    {
        private readonly WalletClient _qBitNinjaWalletClient;

        public int AddressCount { get; }

        public Safe Safe { get; }

        private string QBitNinjaWalletName
        {
            get
            {
                // Let's generate the walletname from seedpublickey
                var bitcoinExtPubKey = new BitcoinExtPubKey(Safe.SeedPublicKey);
                // Let's get the pubkey, so the chaincode is lost
                var pubKey = bitcoinExtPubKey.ExtPubKey.PubKey;
                // Let's get the address, you can't directly access it from the safe
                // Also nobody would ever use this address for anything
                var address = pubKey.GetAddress(_Network).ToWif();
                // Let's just simply add the addresscount so in case we have the same safe, but different
                // sizes it should be in an other wallet
                return address + AddressCount;
            }
        }


        public HttpSafeMonitor(Safe safe, int addressCount) : base(safe.Network)
        {
            AssertNetwork(safe.Network);
            Safe = safe;
            AddressCount = addressCount;

            
            _qBitNinjaWalletClient = _client.GetWalletClient(QBitNinjaWalletName);
            _qBitNinjaWalletClient.CreateIfNotExists().Wait();

            InitializeQBitNinjaWallet();
        }

        private async Task InitializeQBitNinjaWallet()
        {
            while (!IsQBitNinjaWalletInitialized())
            {
                _qBitNinjaWalletClient.
            }
        }

        private bool IsQBitNinjaWalletInitialized()
        {
            var walletAddresses = _qBitNinjaWalletClient.GetAddresses().Result;

            if (walletAddresses.Length < AddressCount)
                return false;

            var same = true;
            for (var i = 0; i < AddressCount; i++)
                if (Safe.GetAddress(i) != walletAddresses[i].Address.ToWif())
                    same = false;
            if (same) return true;

            throw new Exception("QBitNinja wallet and HTTPSafeMonitor is out of sync.");
        }

        public SafeBalanceInfo GetSafeBalanceInfo()
        {
            //int i = 12000;
            //while (true)
            //{
            //    wallet.CreateAddressIfNotExists(new BitcoinPubKeyAddress(_safe.GetAddress(i)));
            //    //var success = wallet.CreateAddressIfNotExists(new BitcoinPubKeyAddress(_safe.GetAddress(i))).Result;
            //    i++;
            //    //Console.WriteLine(success);
            //}

            throw new NotImplementedException();
            //return new SafeBalanceInfo(_safe, );
        }

        public SafeHistory GetSafeHistory()
        {
            throw new NotImplementedException();
        }
    }
}