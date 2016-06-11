using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.SPV;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class SpvMonitor : Monitor, INotifyPropertyChanged
    {
        public static object Saving = new object();
        private bool _disposed;

        private readonly string _addressManagerFilePath;
        private readonly string _chainFilePath;
        private readonly string _trackerFilePath;
        private const string SpvFolderPath = "Spv";

        private NodeConnectionParameters _connectionParameters;
        private int _connectionProgressPercent;

        private State _connectionState;
        private int _syncProgressPercent;
        private State _syncState;

        public SpvMonitor(Network network) : base(network)
        {
            ConnectionState = State.NotStarted;
            SyncState = State.NotStarted;
            ConnectionProgressPercent = 0;
            SyncProgressPercent = 0;

            Directory.CreateDirectory(SpvFolderPath);
            _addressManagerFilePath = Path.Combine(SpvFolderPath, $@"AddressManager{Network}.dat");
            _chainFilePath = Path.Combine(SpvFolderPath, $@"LocalChain{Network}.dat");
            _trackerFilePath = Path.Combine(SpvFolderPath, $@"Tracker{Network}.dat");

            InitializeConnectionParameters();
            InitializeNodesGroup();
        }

        private void InitializeNodesGroup()
        {
            _group = new NodesGroup(_Network, _connectionParameters, new NodeRequirement
            {
                RequiredServices = NodeServices.Network // Needed for SPV
            })
            {
                MaximumNodeConnection = 4,
                AllowSameGroup = false
            };
        }

        public State ConnectionState
        {
            get { return _connectionState; }
            private set
            {
                if (value == _connectionState) return;
                _connectionState = value;
                OnPropertyChanged();
                OnConnectionStateChanged();
            }
        }

        public int ConnectionProgressPercent
        {
            get { return _connectionProgressPercent; }
            private set
            {
                if (value == _connectionProgressPercent) return;
                switch (value)
                {
                    case 0:
                        ConnectionState = State.NotStarted;
                        break;
                    case 100:
                        ConnectionState = State.Ready;
                        break;
                    default:
                        if (value > 0 && value < 100) ConnectionState = State.InProgress;
                        else
                            throw new ArgumentOutOfRangeException(
                                $"ConnectionProgressPercent cannot be {value}. It must be >=0 and <=100");
                        break;
                }
                _connectionProgressPercent = value;
                OnPropertyChanged();
                OnConnectionProgressPercentChanged();
            }
        }

        public State SyncState
        {
            get { return _syncState; }
            private set
            {
                if (value == _syncState) return;
                _syncState = value;
                OnPropertyChanged();
                OnSyncStateChanged();
            }
        }

        public int SyncProgressPercent
        {
            get { return _syncProgressPercent; }
            private set
            {
                if (value == _syncProgressPercent) return;
                switch (value)
                {
                    case 0:
                        SyncState = State.NotStarted;
                        break;
                    case 100:
                        SyncState = State.Ready;
                        break;
                    default:
                        if (value > 0 && value < 100) SyncState = State.InProgress;
                        else
                            throw new ArgumentOutOfRangeException(
                                $"SyncProgressPercent cannot be {value}. It must be >=0 and <=100");
                        break;
                }
                _syncProgressPercent = value;
                OnPropertyChanged();
                OnSyncProgressPercentChanged();
            }
        }

        private AddressManager AddressManager
        {
            get
            {
                if (_connectionParameters != null)
                    foreach (var behavior in _connectionParameters.TemplateBehaviors)
                    {
                        var addressManagerBehavior = behavior as AddressManagerBehavior;
                        if (addressManagerBehavior != null)
                            return addressManagerBehavior.AddressManager;
                    }
                try
                {
                    lock (Saving)
                        return AddressManager.LoadPeerFile(_addressManagerFilePath);
                }
                catch
                {
                    return new AddressManager();
                }
            }
        }

        private ConcurrentChain LocalChain
        {
            get
            {
                if (_connectionParameters != null)
                    foreach (var behavior in _connectionParameters.TemplateBehaviors)
                    {
                        var chainBehavior = behavior as ChainBehavior;
                        if (chainBehavior != null)
                            return chainBehavior.Chain;
                    }
                var chain = new ConcurrentChain(_Network);
                try
                {
                    lock (Saving)
                        chain.Load(File.ReadAllBytes(_chainFilePath));
                }
                catch
                {
                    // ignored
                }

                return chain;
            }
        }

        private Tracker Tracker
        {
            get
            {
                if (_connectionParameters != null)
                    foreach (var behavior in _connectionParameters.TemplateBehaviors)
                    {
                        var trackerBehavior = behavior as TrackerBehavior;
                        if (trackerBehavior != null)
                            return trackerBehavior.Tracker;
                    }
                try
                {
                    lock (Saving)
                        using (var fs = File.OpenRead(_trackerFilePath))
                            return Tracker.Load(fs);
                }
                catch
                {
                    return new Tracker();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void InitializeConnectionParameters()
        {
            _connectionParameters = new NodeConnectionParameters();

            //So we find nodes faster
            _connectionParameters.TemplateBehaviors.Add(new AddressManagerBehavior(AddressManager));
            //So we don't have to load the chain each time we start
            _connectionParameters.TemplateBehaviors.Add(new ChainBehavior(LocalChain));
            //Tracker knows which scriptPubKey and outpoints to track, it monitors all your wallets at the same
            _connectionParameters.TemplateBehaviors.Add(new TrackerBehavior(Tracker));
        }

        public override BalanceInfo GetBalance(string address)
        {
            var nBitcoinAddress = new BitcoinPubKeyAddress(address);
            AssertNetwork(nBitcoinAddress.Network);

            Tracker.Add(nBitcoinAddress);
            foreach (var data in Tracker.GetDataToTrack())
            {
                Console.WriteLine("foo");
            }
            Tracker.

            throw new NotImplementedException();
        }

        public override TransactionInfo GetTransactionInfo(string transactionId)
        {
            // TODO AssertNetwork(can you get network from transactionId?);
            throw new NotImplementedException();
        }

        public override AddressHistory GetAddressHistory(string address)
        {
            var nBitcoinAddress = new BitcoinPubKeyAddress(address);
            AssertNetwork(nBitcoinAddress.Network);

            throw new NotImplementedException();
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler ConnectionStateChanged;
        public event EventHandler SyncStateChanged;
        public event EventHandler ConnectionProgressPercentChanged;
        public event EventHandler SyncProgressPercentChanged;

        protected virtual void OnConnectionStateChanged()
        {
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnSyncStateChanged()
        {
            SyncStateChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnConnectionProgressPercentChanged()
        {
            ConnectionProgressPercentChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnSyncProgressPercentChanged()
        {
            SyncProgressPercentChanged?.Invoke(this, EventArgs.Empty);
        }

        private NodesGroup _group;
        public async void StartConnecting()
        {
            if (_disposed) return;
            await Task.Run(() =>
            {
                _group.Connect();
            });
            PeriodicSave();
            PeriodicKick();
            PeriodicStateAdjust();
        }
        private async void PeriodicKick()
        {
            while (!_disposed)
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
                _group.Purge("For privacy concerns, will renew bloom filters on fresh nodes.");
            }
        }
        private async void PeriodicSave()
        {
            while (!_disposed)
            {
                await Task.Delay(30000);
                SaveAsync();
            }
        }
        private void SaveAsync()
        {
            Task.Run(() =>
            {
                lock (Saving)
                {
                    AddressManager.SavePeerFile(_addressManagerFilePath, _Network);

                    using (var fs = File.Open(_trackerFilePath, FileMode.Create))
                    {
                        Tracker.Save(fs);
                    }
                }
                SaveChainAsync();
            });
        }
        private void SaveChainAsync()
        {
            Task.Run(() =>
            {
                lock (Saving)
                {
                    using (var fs = File.Open(_chainFilePath, FileMode.Create))
                    {
                        LocalChain.WriteTo(fs);
                    }
                }
            });
        }

        public void Disconnect()
        {
            _disposed = true;
            SaveAsync();
            _group?.Disconnect();
        }

        private int _oldSyncProgressPercent;
        private async void PeriodicStateAdjust()
        {
            while (!_disposed)
            {
                await Task.Delay(100);

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
                    var localHeight = LocalChain.Height;
                    // TODO Can't find how to get the blockchain height, but it'll do it for this case
                    var startHeight = _group.ConnectedNodes.First().PeerVersion.StartHeight;
                    SyncProgressPercent = (int) Math.Round((double) (100*localHeight)/startHeight);
                }

                if (ConnectionProgressPercent > 100) ConnectionProgressPercent = 100;
                if (SyncProgressPercent > 100) SyncProgressPercent = 100;

                if (SyncProgressPercent - _oldSyncProgressPercent >= 3)
                {
                    _oldSyncProgressPercent = SyncProgressPercent;
                    SaveChainAsync();
                }
            }
        }
    }
}