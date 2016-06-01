using System;
using HiddenBitcoin.DataClasses.KeyManagement;
using NBitcoin;
using NBitcoin.Stealth;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class BlockchainMonitor
    {
        private readonly QBitNinjaClient _client;
        private readonly NBitcoin.Network _network;

        public BlockchainMonitor(Network network)
        {
            _network = Convert.ToNBitcoinNetwork(network);

            _client = new QBitNinjaClient(_network);
        }

        public Network Network => Convert.ToHiddenBitcoinNetwork(_network);

        public BalanceInfo GetBalance(string address)
        {
            var balanceSummary = _client.GetBalanceSummary(new BitcoinPubKeyAddress(address)).Result;

            var confirmedBalance = balanceSummary.Confirmed.Amount.ToDecimal(MoneyUnit.BTC);
            var unconfirmedBalance = balanceSummary.UnConfirmed.Amount.ToDecimal(MoneyUnit.BTC);

            return new BalanceInfo(address, unconfirmedBalance, confirmedBalance);
        }

        public TransactionInfo GetTransactionInfo(string transactionId)
        {
            var transactionIdUint256 = new uint256(transactionId);
            var transactionResponse = _client.GetTransaction(transactionIdUint256).Result;

            return new TransactionInfo(transactionResponse, Network);
        }

        public AddressHistory GetAddressHistory(string address)
        {
            var operations = _client.GetBalance(new BitcoinPubKeyAddress(address)).Result.Operations;

            return new AddressHistory(address, operations);
        }
    }
}