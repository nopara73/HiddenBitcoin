using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using HiddenBitcoin.DataClasses.States;
using NBitcoin;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses.Sending
{
    public class HttpSender : Sender, INotifyPropertyChanged
    {
        private static readonly object CreatingTransaction = new object();

        protected readonly QBitNinjaClient Client;
        private TransactionCreationState _transactionCreationState;

        public HttpSender(Network network) : base(network)
        {
            Client = new QBitNinjaClient(_Network);
            TransactionCreationState = TransactionCreationState.NotInProgress;
        }

        public TransactionCreationState TransactionCreationState
        {
            get { return _transactionCreationState; }
            private set
            {
                if (value == _transactionCreationState) return;
                _transactionCreationState = value;
                OnPropertyChanged();
                OnTransactionCreationStateChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler TransactionCreationStateChanged;

        protected virtual void OnTransactionCreationStateChanged()
        {
            TransactionCreationStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public override TransactionInfo CreateTransaction(List<string> fromPrivateKeys, List<AddressAmountPair> to,
            FeeType feeType = FeeType.Fastest, string changeAddress = "", string message = "", bool spendAll = false,
            bool spendUnconfirmed = false)
        {
            lock (CreatingTransaction)
            {
                try
                {
                    TransactionCreationState = TransactionCreationState.GatheringCoinsToSpend;
                    // Set secrets
                    var secrets = fromPrivateKeys.Select(Convert.ToISecret).ToList();

                    // Set changeScriptPubKey
                    var changeScriptPubKey = GetChangeScriptPubKey(ref changeAddress, spendAll, secrets);

                    // Gather coins can be spend
                    var unspentCoins = GetUnspentCoins(secrets);

                    TransactionCreationState = TransactionCreationState.BuildingTransaction;
                    // Build the transaction
                    var builder = new TransactionBuilder();
                    var transaction = new Transaction();
                    var coinsToSpend = new List<Coin>();
                    // Get a small fee and adjust later
                    var fee = FeeApi.GetRecommendedFee(100, FeeType.Hour);

                    if (spendAll)
                    {
                        coinsToSpend = unspentCoins;

                        // Get the a small fee estimation just to build a fake transaction to get size of it
                        var amountToReceive = coinsToSpend.Sum(x => x.Amount.ToDecimal(MoneyUnit.BTC)) - fee;

                        // build fake transaction
                        transaction = CreateSendAllTransaction(to, message, secrets, coinsToSpend, fee, amountToReceive,
                            out builder);

                        // estimate fee and build real transaction
                        amountToReceive += fee;
                        fee = FeeApi.GetRecommendedFee(builder.EstimateSize(transaction), feeType);
                        amountToReceive -= fee;
                        transaction = CreateSendAllTransaction(to, message, secrets, coinsToSpend, fee, amountToReceive,
                            out builder);
                    }
                    else
                    {
                        var orderedUnspentCoins = unspentCoins.OrderByDescending(x => x.Amount.ToDecimal(MoneyUnit.BTC));

                        var transactionCost = to.Sum(pair => pair.Amount);
                        // Get the a small fee estimation just to build a fake transaction to get size of it
                        transactionCost += fee;

                        var haveEnough = false;
                        foreach (var coin in orderedUnspentCoins)
                        {
                            coinsToSpend.Add(coin);
                            // if doesn't reach amount, continue adding next coin
                            if (coinsToSpend.Sum(x => x.Amount.ToDecimal(MoneyUnit.BTC)) <= transactionCost) continue;
                            // if it does reach build fake transaction to estimate fee
                            transaction = CreateTransaction(to, secrets, changeScriptPubKey, coinsToSpend, fee,
                                out builder,
                                message);

                            // 2. Estimate fee and add it to the transactionCost
                            transactionCost -= fee;
                            fee = FeeApi.GetRecommendedFee(builder.EstimateSize(transaction), feeType);
                            transactionCost += fee;
                            // does it still reach amount? if not continue adding next coin
                            if (coinsToSpend.Sum(x => x.Amount.ToDecimal(MoneyUnit.BTC)) <= transactionCost) continue;
                            // if it does reach build the real transaction with the new fee
                            transaction = CreateTransaction(to, secrets, changeScriptPubKey, coinsToSpend, fee,
                                out builder,
                                message);

                            haveEnough = true;
                            break;
                        }
                        if (!haveEnough)
                            throw new Exception("Not enough funds");
                    }

                    TransactionCreationState = TransactionCreationState.CheckingTransaction;
                    builder.Check(transaction);
                    if (!builder.Verify(transaction))
                        throw new Exception("Wrong transaction");
                    CreatedTransactions.Add(transaction);
                    return new TransactionInfo(coinsToSpend, transaction.Outputs.AsCoins(), Network,
                        transaction.GetHash().ToString(), false, fee);
                }
                finally
                {
                    TransactionCreationState = TransactionCreationState.NotInProgress;
                }
            }
        }

        private Script GetChangeScriptPubKey(ref string changeAddress, bool spendAll, IEnumerable<ISecret> secrets)
        {
            if (spendAll)
                changeAddress = "";
            Script changeScriptPubKey;
            if (changeAddress == "")
                changeScriptPubKey = secrets.First().PrivateKey.ScriptPubKey;
            else
            {
                var changeBitcoinAddress = BitcoinAddress.Create(changeAddress);
                AssertNetwork(changeBitcoinAddress.Network);
                changeScriptPubKey = changeBitcoinAddress.ScriptPubKey;
            }

            return changeScriptPubKey;
        }

        private List<Coin> GetUnspentCoins(IEnumerable<ISecret> secrets)
        {
            var unspentCoins = new List<Coin>();
            foreach (var secret in secrets)
            {
                var destination = secret.PrivateKey.ScriptPubKey.GetDestinationAddress(_Network);

                var balanceModel = Client.GetBalance(destination, true).Result;
                foreach (var operation in balanceModel.Operations)
                {
                    unspentCoins.AddRange(operation.ReceivedCoins.Select(coin => coin as Coin));
                }
            }

            return unspentCoins;
        }

        private Transaction CreateSendAllTransaction(IEnumerable<AddressAmountPair> to, string message,
            IEnumerable<ISecret> secrets, IReadOnlyCollection<Coin> coinsToSpend, decimal fee, decimal amountToReceive,
            out TransactionBuilder builder)
        {
            var scriptPubKeyStringsToSpend =
                coinsToSpend.Select(coinToSpend => coinToSpend.ScriptPubKey.ToString()).ToList();
            var secretsToSpend =
                secrets.Where(secret => scriptPubKeyStringsToSpend.Contains(secret.PrivateKey.ScriptPubKey.ToString()))
                    .ToList();
            builder = new TransactionBuilder();
            builder = builder
                .AddCoins(coinsToSpend)
                .AddKeys(secretsToSpend.ToArray());

            var address = BitcoinAddress.Create(to.First().Address);
            AssertNetwork(address.Network);
            builder = builder.Send(address, new Money(amountToReceive, MoneyUnit.BTC));

            builder = builder
                .SendFees(Money.Coins(fee));
            var transaction = builder.BuildTransaction(false);

            if (message == "") return builder.SignTransaction(transaction);

            var bytes = Encoding.UTF8.GetBytes(message);
            transaction.Outputs.Add(new TxOut
            {
                Value = Money.Zero,
                ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
            });

            return builder.SignTransaction(transaction);
        }

        private Transaction CreateTransaction(IEnumerable<AddressAmountPair> to, IEnumerable<ISecret> secrets,
            Script changeScriptPubKey, IReadOnlyCollection<Coin> coinsToSpend, decimal fee,
            out TransactionBuilder builder, string message)
        {
            var scriptPubKeyStringsToSpend =
                coinsToSpend.Select(coinToSpend => coinToSpend.ScriptPubKey.ToString()).ToList();
            var secretsToSpend =
                secrets.Where(secret => scriptPubKeyStringsToSpend.Contains(secret.PrivateKey.ScriptPubKey.ToString()))
                    .ToList();
            builder = new TransactionBuilder();
            builder = builder
                .AddCoins(coinsToSpend)
                .AddKeys(secretsToSpend.ToArray());
            foreach (var addressAmountPair in to)
            {
                var address = BitcoinAddress.Create(addressAmountPair.Address);
                AssertNetwork(address.Network);
                builder = builder.Send(address, new Money(addressAmountPair.Amount, MoneyUnit.BTC));
            }
            builder = builder
                .SetChange(changeScriptPubKey)
                .SendFees(Money.Coins(fee));
            var transaction = builder.BuildTransaction(false);

            if (message == "") return builder.SignTransaction(transaction);

            var bytes = Encoding.UTF8.GetBytes(message);
            transaction.Outputs.Add(new TxOut
            {
                Value = Money.Zero,
                ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
            });

            return builder.SignTransaction(transaction);
        }

        public override void Send(string transactionId)
        {
            foreach (var transaction in CreatedTransactions)
            {
                if (transaction.GetHash() != new uint256(transactionId)) continue;
                var broadcastResponse = Client.Broadcast(transaction).Result;

                if (!broadcastResponse.Success)
                    throw new Exception($"ErrorCode: {broadcastResponse.Error.ErrorCode}" + Environment.NewLine
                                        + broadcastResponse.Error.Reason);
                return;
            }
            throw new Exception("Transaction has not been created");
        }
    }
}