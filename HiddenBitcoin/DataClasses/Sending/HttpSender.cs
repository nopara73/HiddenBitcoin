using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace HiddenBitcoin.DataClasses.Sending
{
    public class HttpSender : Sender
    {
        protected readonly QBitNinjaClient Client;

        public HttpSender(Network network) : base(network)
        {
            Client = new QBitNinjaClient(_Network);
        }

        public override TransactionInfo CreateSendAllTransaction(List<string> fromPrivateKeys, string toAddress,
            FeeType feeType = FeeType.Fastest, string message = "")
        {
            var addressAmountPair = new AddressAmountPair
            {
                Address = toAddress,
                Amount = 0 // doesn't matter, we send all
            };

            return CreateTransaction(
                fromPrivateKeys,
                new List<AddressAmountPair> {addressAmountPair},
                feeType,
                message: message,
                sendAll: true
                );
        }

        public override TransactionInfo CreateTransaction(List<string> fromPrivateKeys, List<AddressAmountPair> to,
            FeeType feeType = FeeType.Fastest, string changeAddress = "", string message = "", bool sendAll = false)
        {
            // Set privatekeys
            var fromKeys = new List<Key>();
            var fromExtKeys = new List<ExtKey>();
            
            foreach (var privatekey in fromPrivateKeys)
            {
                try
                {
                    var bitcoinSecret = new BitcoinSecret(privatekey);
                    AssertNetwork(bitcoinSecret.Network);
                    fromKeys.Add(bitcoinSecret.PrivateKey);
                }
                catch
                {
                    try
                    {
                        var bitcoinExtKey = new BitcoinExtKey(privatekey);
                        AssertNetwork(bitcoinExtKey.Network);
                        fromExtKeys.Add(bitcoinExtKey.ExtKey);
                    }
                    catch
                    {
                        throw  new Exception($"Private key in wrong format: {privatekey}");
                    }
                }
            }

            var secrets = fromExtKeys.Cast<ISecret>().ToList();
            secrets.AddRange(fromKeys.Cast<ISecret>());

            // Set changeScriptPubKey
            if (sendAll)
                changeAddress = "";
            Script changeScriptPubKey;
            if (changeAddress != "")
            {
                var changeBitcoinAddress = BitcoinAddress.Create(changeAddress);
                AssertNetwork(changeBitcoinAddress.Network);
                changeScriptPubKey = changeBitcoinAddress.ScriptPubKey;
            }
            else
                changeScriptPubKey = secrets.First().PrivateKey.ScriptPubKey;

            // Gather coins can be spend
            var unspentCoins = new List<Coin>();
            foreach (var key in secrets)
            {
                var destination = key.PrivateKey.ScriptPubKey.GetDestinationAddress(_Network);
                BalanceModel balanceModel = Client.GetBalance(destination, true).Result;
                foreach (var operation in balanceModel.Operations)
                {
                    foreach (var coin in operation.ReceivedCoins)
                        unspentCoins.Add(coin as Coin);
                }
            }

            //Gather coins to spend
            TransactionBuilder builder = new TransactionBuilder();
            Transaction transaction = new Transaction();
            var coinsToSpend = new List<Coin>();
            var fee = FeeApi.GetRecommendedFee(100, FeeType.Hour);
            if (!sendAll)
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
                    BuildTransaction(to, secrets, changeScriptPubKey, coinsToSpend, fee, out builder, out transaction, message);

                    // 2. Estimate fee and add it to the transactionCost
                    transactionCost -= fee;
                    fee = FeeApi.GetRecommendedFee(builder.EstimateSize(transaction));
                    transactionCost += fee;
                    // does it still reach amount? if not continue adding next coin
                    if (coinsToSpend.Sum(x => x.Amount.ToDecimal(MoneyUnit.BTC)) <= transactionCost) continue;
                    // if it does reach build the real transaction with the new fee
                    BuildTransaction(to, secrets, changeScriptPubKey, coinsToSpend, fee, out builder, out transaction, message);
                    
                    haveEnough = true;
                    break;
                }
                if (!haveEnough)
                    throw new Exception("Not enough funds");

            }
            else
            {
                coinsToSpend = unspentCoins;

                // Get the a small fee estimation just to build a fake transaction to get size of it
                var amountToReceive = coinsToSpend.Sum(x => x.Amount.ToDecimal(MoneyUnit.BTC)) - fee;

                // build fake transaction
                BuildSendAllTransaction(to, message, secrets, coinsToSpend, fee, amountToReceive, out builder, out transaction);

                // estimate fee and build real transaction
                amountToReceive += fee;
                fee = FeeApi.GetRecommendedFee(builder.EstimateSize(transaction));
                amountToReceive -= fee;
                BuildSendAllTransaction(to, message, secrets, coinsToSpend, fee, amountToReceive, out builder, out transaction);
            }

            builder.Check(transaction);
            if (!builder.Verify(transaction)) throw new Exception("Wrong transaction"); // todo temporarily exception
            CreatedTransactions.Add(transaction);
            return new TransactionInfo(coinsToSpend, transaction.Outputs.AsCoins(), Network, transaction.GetHash().ToString(), false, fee);
        }

        private void BuildSendAllTransaction(List<AddressAmountPair> to, string message, List<ISecret> secrets, List<Coin> coinsToSpend, decimal fee, decimal amountToReceive, out TransactionBuilder builder, out Transaction transaction)
        {
            var scriptPubKeyStringsToSpend = coinsToSpend.Select(coinToSpend => coinToSpend.ScriptPubKey.ToString()).ToList();
            var secretsToSpend = secrets.Where(secret => scriptPubKeyStringsToSpend.Contains(secret.PrivateKey.ScriptPubKey.ToString())).ToList();
            builder = new TransactionBuilder();
            builder = builder
.AddCoins(coinsToSpend)
.AddKeys(secretsToSpend.ToArray());

            var address = BitcoinAddress.Create(to.First().Address);
            AssertNetwork(address.Network);
            builder = builder.Send(address, new Money(amountToReceive, MoneyUnit.BTC));

            builder = builder
                .SendFees(Money.Coins(fee));
            transaction = builder.BuildTransaction(false);

            if (message != "")
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                transaction.Outputs.Add(new TxOut
                {
                    Value = Money.Zero,
                    ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
                });
            }

            transaction = builder.SignTransaction(transaction);
        }

        private void BuildTransaction(List<AddressAmountPair> to, List<ISecret> secrets, Script changeScriptPubKey, List<Coin> coinsToSpend, decimal fee, out TransactionBuilder builder, out Transaction transaction, string message)
        {
            var scriptPubKeyStringsToSpend = coinsToSpend.Select(coinToSpend => coinToSpend.ScriptPubKey.ToString()).ToList();
            var secretsToSpend = secrets.Where(secret => scriptPubKeyStringsToSpend.Contains(secret.PrivateKey.ScriptPubKey.ToString())).ToList();
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
            transaction = builder.BuildTransaction(false);

            if (message != "")
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                transaction.Outputs.Add(new TxOut
                {
                    Value = Money.Zero,
                    ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
                });
            }

            transaction = builder.SignTransaction(transaction);
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