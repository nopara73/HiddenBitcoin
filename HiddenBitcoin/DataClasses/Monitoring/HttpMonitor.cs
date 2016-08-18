using HiddenBitcoin.DataClasses.Balances;
using HiddenBitcoin.DataClasses.Histories;
using NBitcoin;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class HttpMonitor : Monitor
    {
        protected readonly QBitNinjaClient Client;

        public HttpMonitor(Network network) : base(network)
        {
            Client = new QBitNinjaClient(_Network);
        }

        public override AddressBalanceInfo GetAddressBalanceInfo(string address)
        {
            AssertNetwork(new BitcoinPubKeyAddress(address).Network);

            var confirmedBalance = 0m;
            var unconfirmedBalance = 0m;

            foreach (var record in GetAddressHistory(address).Records)
                if (record.Confirmed)
                    confirmedBalance += record.Amount;
                else unconfirmedBalance += record.Amount;

            return new AddressBalanceInfo(address, unconfirmedBalance, confirmedBalance);
        }

        public override TransactionInfo GetTransactionInfo(string transactionId)
        {
            // TODO AssertNetwork(can you get network from transactionId?);

            var transactionIdUint256 = new uint256(transactionId);
            var transactionResponse = Client.GetTransaction(transactionIdUint256).Result;
            
            return new TransactionInfo(transactionResponse, Network);
        }

        public override AddressHistory GetAddressHistory(string address)
        {
            var nBitcoinAddress = new BitcoinPubKeyAddress(address);
            AssertNetwork(nBitcoinAddress.Network);

            var operations = Client.GetBalance(new BitcoinPubKeyAddress(address)).Result.Operations;

            return new AddressHistory(address, operations);
        }
    }
}