using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HiddenBitcoin.DataClasses.KeyManagement;
using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.SPV;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class SpvSafeMonitor
    {
        private const string TrackerFilePath = "Tracker.dat";
        private const string ChainFilePath = "Chain.dat";
        private const string AddressManagerFilePath = "AddressManager.dat";

        public readonly Safe Safe;
        private NodeConnectionParameters _connectionParameters;
        private bool _disposed;
        private NodesGroup _group;
        private Wallet _wallet;
        private Wallet _cleanWallet;

        public SpvSafeMonitor(Safe safe)
        {
            Safe = safe;

            PeriodicProgressPercentAdjust();
        }

        public int SyncProgressPercent { get; private set; }
        public int ConnectionProgressPercent { get; private set; }
        public bool Connected => ConnectionProgressPercent == 100;
        public bool Synced => SyncProgressPercent == 100;

        // ReSharper disable once InconsistentNaming
        private NBitcoin.Network _Network => Convert.ToNBitcoinNetwork(Safe.Network);
        public Network Network => Safe.Network;

        public BalanceInfo GetBalance()
        {
            if (!Connected)
                throw new Exception("NotConnected");
            if (!Synced)
                throw new Exception("NotSynced");

            var transactions = _wallet.GetTransactions();

            var unconfirmedBalance = transactions.Summary.Confirmed.Amount.ToDecimal(MoneyUnit.BTC);
            var confirmedBalance = transactions.Summary.UnConfirmed.Amount.ToDecimal(MoneyUnit.BTC);

            return new BalanceInfo(unconfirmedBalance, confirmedBalance);
        }

        private async void CreateWallets()
        {
            ExtPubKey rootKey = new BitcoinExtKey(Safe.GetPrivateKey(0)).Neuter().ExtPubKey;
            ExtPubKey cleanRootKey = new BitcoinExtKey(Safe.GetPrivateKey(0, true)).Neuter().ExtPubKey;

            var creation = new WalletCreation
            {
                SignatureRequired = 1,
                UseP2SH = false,
                Network = _Network,
                RootKeys = new[] {rootKey},
                PurgeConnectionOnFilterChange = true,
                Name = Guid.NewGuid().ToString()
            };
            var cleanCreation = new WalletCreation
            {
                SignatureRequired = 1,
                UseP2SH = false,
                Network = _Network,
                RootKeys = new[] { cleanRootKey },
                PurgeConnectionOnFilterChange = true,
                Name = Guid.NewGuid().ToString()
            };

            _wallet = await CreateWallet(creation);
            _cleanWallet = await CreateWallet(cleanCreation);
            if (_connectionParameters == null) return;

            _wallet.Configure(_connectionParameters);
            _cleanWallet.Configure(_connectionParameters);
            _wallet.Connect();
            _cleanWallet.Connect();
            
            var kp = _wallet.GetKeyPath(new BitcoinExtKey(Safe.GetPrivateKey(5)).ScriptPubKey);
            Console.WriteLine("KEYPATH:");
            Console.WriteLine(kp);
            Console.WriteLine();

            var foo = GetTracker();
            foo.Add(new BitcoinPubKeyAddress(Safe.GetAddress(5)));

            while (true)
            {
                Thread.Sleep(1000);
                foreach (var tx in foo.GetWalletTransactions(GetChain()))
                {
                    Console.WriteLine(tx.Balance);
                }
            }
            
        }

        private Task<Wallet> CreateWallet(WalletCreation creation)
        {
            return Task.Factory.StartNew(() => new Wallet(creation));
        }

        public async void StartConnecting()
        {
            await Task.Factory.StartNew(() =>
            {
                var parameters = new NodeConnectionParameters();
                //So we find nodes faster
                parameters.TemplateBehaviors.Add(new AddressManagerBehavior(GetAddressManager()));
                //So we don't have to load the chain each time we start
                parameters.TemplateBehaviors.Add(new ChainBehavior(GetChain()));
                //Tracker knows which scriptPubKey and outpoints to track, it monitors all your wallets at the same
                parameters.TemplateBehaviors.Add(new TrackerBehavior(GetTracker()));
                if (_disposed) return;
                _group = new NodesGroup(_Network, parameters, new NodeRequirement
                {
                    RequiredServices = NodeServices.Network //Needed for SPV
                })
                {MaximumNodeConnection = 3};
                _group.Connect();
                _connectionParameters = parameters;
            });

            CreateWallets();

            PeriodicSave();
            PeriodicKick();
        }

        public void Disconnect()
        {
            _disposed = true;
            SaveAsync();
            _group?.Disconnect();
        }

        private AddressManager GetAddressManager()
        {
            if (_connectionParameters != null)
            {
                return _connectionParameters.TemplateBehaviors.Find<AddressManagerBehavior>().AddressManager;
            }
            try
            {
                return AddressManager.LoadPeerFile(AddressManagerFilePath);
            }
            catch
            {
                return new AddressManager();
            }
        }

        private ConcurrentChain GetChain()
        {
            if (_connectionParameters != null)
            {
                return _connectionParameters.TemplateBehaviors.Find<ChainBehavior>().Chain;
            }
            var chain = new ConcurrentChain(_Network);
            try
            {
                chain.Load(File.ReadAllBytes(ChainFilePath));
            }
            catch
            {
                // ignored
            }
            return chain;
        }

        private Tracker GetTracker()
        {
            if (_connectionParameters != null)
            {
                return _connectionParameters.TemplateBehaviors.Find<TrackerBehavior>().Tracker;
            }
            try
            {
                using (var fs = File.OpenRead(TrackerFilePath))
                {
                    return Tracker.Load(fs);
                }
            }
            catch
            {
                // ignored
            }
            return new Tracker();
        }

        private async void PeriodicSave()
        {
            while (!_disposed)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                SaveAsync();
            }
        }

        private void SaveAsync()
        {
            Task.Factory.StartNew(() =>
            {
                GetAddressManager().SavePeerFile(AddressManagerFilePath, _Network);
                using (var fs = File.Open(ChainFilePath, FileMode.Create))
                {
                    GetChain().WriteTo(fs);
                }
                using (var fs = File.Open(TrackerFilePath, FileMode.Create))
                {
                    GetTracker().Save(fs);
                }
            });
        }

        private async void PeriodicKick()
        {
            while (!_disposed)
            {
                await Task.Delay(TimeSpan.FromMinutes(7));
                _group.Purge("For privacy concerns, will renew bloom filters on fresh nodes");
            }
        }

        private async void PeriodicProgressPercentAdjust()
        {
            while (!_disposed)
            {
                await Task.Delay(500);

                if (_group == null)
                    ConnectionProgressPercent = 0;
                else if (_group.ConnectedNodes == null)
                    ConnectionProgressPercent = 0;
                else
                {
                    var nodeCount = _group.ConnectedNodes.Count;
                    var maxNode = _group.MaximumNodeConnection;
                    ConnectionProgressPercent = (int) Math.Round((double) (100*nodeCount)/maxNode);
                }

                if (_group == null)
                    SyncProgressPercent = 0;
                else if (_group.ConnectedNodes == null)
                    SyncProgressPercent = 0;
                else if (_group.ConnectedNodes.Count == 0)
                    SyncProgressPercent = 0;
                else if (_group.ConnectedNodes.First().PeerVersion == null)
                    SyncProgressPercent = 0;
                else
                {
                    var localHeight = GetChain().Height;
                    var startHeight = _group.ConnectedNodes.First().PeerVersion.StartHeight;
                    // Can't find how to get the blockchain height, but it'll do it for this case
                    SyncProgressPercent = (int) Math.Round((double) (100*localHeight)/startHeight);
                }

                if (ConnectionProgressPercent == 100)
                {
                    if (_wallet.State != WalletState.Connected)
                        ConnectionProgressPercent--;
                    if (_cleanWallet.State != WalletState.Connected)
                        ConnectionProgressPercent--;
                }

                if (ConnectionProgressPercent > 100) ConnectionProgressPercent = 100;
                if (SyncProgressPercent > 100) SyncProgressPercent = 100;
            }
        }
    }
}