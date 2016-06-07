using System;
using System.IO;
using System.Linq;
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

        public SpvSafeMonitor(Safe safe)
        {
            Safe = safe;

            PeriodicProgressPercentAdjust();

            SyncTrackerKeys();
        }

        private void SyncTrackerKeys()
        {
            var tracker = GetTracker();

            for (int i = 0; i < 1000; i++)
            {
                var address = new BitcoinPubKeyAddress(Safe.GetAddress(i));
                var cleanAddress = new BitcoinPubKeyAddress(Safe.GetAddress(i, true));

                tracker.Add(address);
                tracker.Add(cleanAddress);
            }
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
            
            var tracker = GetTracker();
            var transactions = tracker.GetWalletTransactions(GetChain());

            var unconfirmedBalance = transactions.Summary.Confirmed.Amount.ToDecimal(MoneyUnit.BTC);
            var confirmedBalance = transactions.Summary.UnConfirmed.Amount.ToDecimal(MoneyUnit.BTC);

            return new BalanceInfo(unconfirmedBalance, confirmedBalance);
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

                if (ConnectionProgressPercent > 100) ConnectionProgressPercent = 100;
                if (SyncProgressPercent > 100) SyncProgressPercent = 100;
            }
        }
    }
}