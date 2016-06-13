using HiddenBitcoin.DataClasses.Balances;
using HiddenBitcoin.DataClasses.Histories;
using NBitcoin;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class HttpMonitor : Monitor
    {
        protected readonly QBitNinjaClient _client;

        public HttpMonitor(Network network) : base(network)
        {
            _client = new QBitNinjaClient(_Network);
        }

        public override AddressBalanceInfo GetAddressBalanceInfo(string address)
        {
            var nBitcoinAddress = new BitcoinPubKeyAddress(address);
            AssertNetwork(nBitcoinAddress.Network);

            var balanceSummary = _client.GetBalanceSummary(nBitcoinAddress).Result;

            var confirmedBalance = balanceSummary.Confirmed.Amount.ToDecimal(MoneyUnit.BTC);
            var unconfirmedBalance = balanceSummary.UnConfirmed.Amount.ToDecimal(MoneyUnit.BTC);

            return new AddressBalanceInfo(address, unconfirmedBalance, confirmedBalance);
        }

        public override TransactionInfo GetTransactionInfo(string transactionId)
        {
            // TODO AssertNetwork(can you get network from transactionId?);

            var transactionIdUint256 = new uint256(transactionId);
            var transactionResponse = _client.GetTransaction(transactionIdUint256).Result;

            return new TransactionInfo(transactionResponse, Network);
        }

        public override AddressHistory GetAddressHistory(string address)
        {
            var nBitcoinAddress = new BitcoinPubKeyAddress(address);
            AssertNetwork(nBitcoinAddress.Network);

            var operations = _client.GetBalance(new BitcoinPubKeyAddress(address)).Result.Operations;

            return new AddressHistory(address, operations);
        }
    }
}