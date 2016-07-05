using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HiddenBitcoin.DataClasses.Balances;
using HiddenBitcoin.DataClasses.Histories;
using HiddenBitcoin.DataClasses.KeyManagement;
using HiddenBitcoin.DataClasses.States;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class HttpSafeMonitor : HttpMonitor, INotifyPropertyChanged
    {
        private readonly WalletClient _qBitNinjaWalletClient;

        private int _initializationProgressPercent;
        private State _initializationState;
        private SafeBalanceInfo _safeBalanceInfo;
        private SafeHistory _safeHistory;

        private int _syncProgressPercent;
        internal Safe BaseSafe;

        public HttpSafeMonitor(Safe safe, int addressCount) : base(safe.Network)
        {
            AssertNetwork(safe.Network);
            AddressCount = addressCount;
            BaseSafe = safe;
            Safe = new HttpSafe(this);

            _qBitNinjaWalletClient = Client.GetWalletClient(QBitNinjaWalletName);
            _qBitNinjaWalletClient.CreateIfNotExists().Wait();

            StartInitializingQBitNinjaWallet();
        }

        public SafeHistory SafeHistory
        {
            get
            {
                if (_safeHistory == null)
                    UpdateSafeHistoryAndBalanceInfo();
                return _safeHistory;
            }
            private set
            {
                var changeHappened = false;
                if (_safeHistory != null)
                {
                    if (_safeHistory.Records.Count != value.Records.Count)
                        changeHappened = true;
                    else
                    {
                        for (var i = 0; i < _safeHistory.Records.Count; i++)
                        {
                            if (_safeHistory.Records[i].Confirmed != value.Records[i].Confirmed)
                                changeHappened = true;
                            if (_safeHistory.Records[i].TransactionId != value.Records[i].TransactionId)
                                changeHappened = true; // Malleability check
                        }
                    }
                }

                _safeHistory = value;
                AdjustState(AddressCount);
                if (changeHappened) OnBalanceChanged();
            }
        }

        public State InitializationState
        {
            get { return _initializationState; }
            private set
            {
                if (value == _initializationState) return;
                _initializationState = value;
                OnPropertyChanged();
                OnInitializationStateChanged();
            }
        }

        public int InitializationProgressPercent
        {
            get { return _initializationProgressPercent; }
            private set
            {
                if (value == _initializationProgressPercent) return;

                switch (value)
                {
                    case 0:
                        InitializationState = State.NotStarted;
                        break;
                    case 100:
                        InitializationState = State.Ready;
                        break;
                    default:
                        if (value > 0 && value < 100) InitializationState = State.InProgress;
                        else
                            throw new ArgumentOutOfRangeException(
                                $"InitializationProgressPercent cannot be {value}. It must be >=0 and <=100");
                        break;
                }
                _initializationProgressPercent = value;
                OnPropertyChanged();
                OnInitializationProgressPercentChanged();
            }
        }

        public int AddressCount { get; }

        public HttpSafe Safe { get; }

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

        public SafeBalanceInfo SafeBalanceInfo
        {
            get
            {
                if (_safeBalanceInfo == null)
                    UpdateSafeHistoryAndBalanceInfo();
                return _safeBalanceInfo;
            }
            set
            {
                _safeBalanceInfo = value;

                AdjustState(AddressCount);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // ReSharper disable once FunctionNeverReturns https://youtrack.jetbrains.com/issue/RSRP-425337
        private async void PeriodicUpdate()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                if (_syncProgressPercent == 100)
                    UpdateSafeHistoryAndBalanceInfo();
            }
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler InitializationStateChanged;
        public event EventHandler InitializationProgressPercentChanged;
        public event EventHandler BalanceChanged;

        protected virtual void OnInitializationStateChanged()
        {
            InitializationStateChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnInitializationProgressPercentChanged()
        {
            InitializationProgressPercentChanged?.Invoke(this, EventArgs.Empty);
        }

        public async void StartInitializingQBitNinjaWallet()
        {
            await Task.Run(() =>
            {
                InitializationState = State.NotStarted;
                InitializationProgressPercent = 0;
                List<string> outOfSyncAddresses;
                do
                {
                    outOfSyncAddresses = GetOutOfSyncAddresses();
                    AdjustState(AddressCount - outOfSyncAddresses.Count);

                    if (outOfSyncAddresses.Count == 0) continue;

                    foreach (var address in outOfSyncAddresses)
                    {
                        AdjustState(outOfSyncAddresses.IndexOf(address));
                        _qBitNinjaWalletClient.CreateAddressIfNotExists(new BitcoinPubKeyAddress(address));
                    }
                } while (outOfSyncAddresses.Count != 0);
            });

            PeriodicUpdate();
        }

        private void AdjustState(int syncedAddressCount)
        {
            if (syncedAddressCount < 0 || syncedAddressCount > AddressCount)
                throw new ArgumentOutOfRangeException(
                    $"syncedAddressCount cannot be {syncedAddressCount}. It must be >=0 and <=AddressCount");

            _syncProgressPercent = (int) Math.Round((double) (100*syncedAddressCount)/AddressCount);
            if (_syncProgressPercent == 100)
            {
                if (_safeHistory == null || _safeBalanceInfo == null)
                    InitializationProgressPercent = 99;
                else
                    InitializationProgressPercent = 100;
            }
            else InitializationProgressPercent = _syncProgressPercent;
        }

        private List<string> GetOutOfSyncAddresses()
        {
            var qbitAddresses = _qBitNinjaWalletClient.GetAddresses().Result.Select(x => x.Address.ToWif()).ToList();
            var safeAddresses = new HashSet<string>();
            for (var i = 0; i < AddressCount; i++)
            {
                safeAddresses.Add(Safe.GetAddress(i));
            }

            if (qbitAddresses.Any(qbitAddress => !safeAddresses.Contains(qbitAddress)))
            {
                throw new Exception("QBitNinja wallet and HTTPSafeMonitor is out of sync.");
            }

            return safeAddresses.Where(safeAddress => !qbitAddresses.Contains(safeAddress)).ToList();
        }

        public SafeBalanceInfo GetSafeBalanceInfo()
        {
            AssertSynced();

            UpdateSafeHistoryAndBalanceInfo();

            return SafeBalanceInfo;
        }

        private void UpdateSafeHistoryAndBalanceInfo()
        {
            AssertSynced();

            var balanceOperations = _qBitNinjaWalletClient.GetBalance().Result.Operations;

            // Find all the operations concerned to one address
            // address, balanceoperationlist
            var addressOperationPairs = new HashSet<Tuple<string, BalanceOperation>>();
            // address, unconfirmed, confirmed
            var receivedAddressAmountPairs = new HashSet<Tuple<string, decimal, decimal>>();
            var spentAddressAmountPairs = new HashSet<Tuple<string, decimal, decimal>>();
            foreach (var operation in balanceOperations)
            {
                foreach (var coin in operation.ReceivedCoins)
                {
                    string address;
                    if (!SafeContainsCoin(out address, coin)) continue;

                    var amount = ((Money) coin.Amount).ToDecimal(MoneyUnit.BTC);

                    receivedAddressAmountPairs.Add(operation.Confirmations == 0
                        ? new Tuple<string, decimal, decimal>(address, amount, 0m)
                        : new Tuple<string, decimal, decimal>(address, 0m, amount));

                    addressOperationPairs.Add(new Tuple<string, BalanceOperation>(address, operation));
                }

                foreach (var coin in operation.SpentCoins)
                {
                    string address;
                    if (!SafeContainsCoin(out address, coin)) continue;

                    var amount = ((Money) coin.Amount).ToDecimal(MoneyUnit.BTC);
                    spentAddressAmountPairs.Add(operation.Confirmations == 0
                        ? new Tuple<string, decimal, decimal>(address, amount, 0m)
                        : new Tuple<string, decimal, decimal>(address, 0m, amount));

                    addressOperationPairs.Add(new Tuple<string, BalanceOperation>(address, operation));
                }
            }

            var addressOperationsDict = new Dictionary<string, HashSet<BalanceOperation>>();
            foreach (var pair in addressOperationPairs)
                if (addressOperationsDict.Keys.Contains(pair.Item1))
                    addressOperationsDict[pair.Item1].Add(pair.Item2);
                else
                    addressOperationsDict.Add(pair.Item1, new HashSet<BalanceOperation> {pair.Item2});

            var addressHistories =
                addressOperationsDict.Select(pair => new AddressHistory(pair.Key, pair.Value)).ToList();

            foreach (var address in Safe.Addresses)
            {
                if (!addressHistories.Select(x => x.Address).Contains(address))
                    addressHistories.Add(new AddressHistory(address, new List<BalanceOperation>()));
            }

            var addressBalanceInfoList = new List<AddressBalanceInfo>();
            foreach (var addressHistory in addressHistories)
            {
                var unconfirmed = 0m;
                var confirmed = 0m;
                foreach (var record in addressHistory.Records)
                {
                    if (record.Confirmed)
                        confirmed += record.Amount;
                    else unconfirmed += record.Amount;
                }

                var addressBalanceInfo = new AddressBalanceInfo(addressHistory.Address, unconfirmed, confirmed);
                addressBalanceInfoList.Add(addressBalanceInfo);
            }

            SafeBalanceInfo = new SafeBalanceInfo(Safe, addressBalanceInfoList);
            SafeHistory = new SafeHistory(Safe, addressHistories);
        }

        public SafeHistory GetSafeHistory()
        {
            AssertSynced();

            UpdateSafeHistoryAndBalanceInfo();

            return SafeHistory;
        }

        private bool SafeContainsCoin(out string address, ICoin coin)
        {
            try
            {
                address = coin.GetScriptCode().GetDestinationAddress(_Network).ToWif();
            }
            catch
            {
                // Not concerned, safe can't contain something like this
                address = null;
                return false;
            }
            return Safe.Addresses.Contains(address);
        }

        private void AssertSynced()
        {
            if (_syncProgressPercent != 100)
                throw new Exception("HttpSafeMonitor is not synced with QBitNinja wallet.");
        }

        protected virtual void OnBalanceChanged()
        {
            BalanceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}