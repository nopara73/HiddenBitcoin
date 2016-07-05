using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using QBitNinja.Client.Models;

namespace HiddenBitcoin.DataClasses
{
    public class TransactionInfo
    {
        private readonly List<InOutInfo> _inputs;
        private readonly NBitcoin.Network _network;
        private readonly List<InOutInfo> _outputs;
        private readonly List<ICoin> _receivedCoins;
        private readonly List<ICoin> _spentCoins;

        internal TransactionInfo(GetTransactionResponse transactionResponse, Network network)
        {
            _network = Convert.ToNBitcoinNetwork(network);

            _spentCoins = transactionResponse.SpentCoins;
            _receivedCoins = transactionResponse.ReceivedCoins;

            Id = transactionResponse.TransactionId.ToString();
            Confirmed = transactionResponse.Block != null;
            Fee = transactionResponse.Fees.ToDecimal(MoneyUnit.BTC);

            AllInOutsAdded =
                FillInOutInfoList(out _inputs, _spentCoins)
                &&
                FillInOutInfoList(out _outputs, _receivedCoins);
        }

        internal TransactionInfo(IEnumerable<Coin> spentCoins, IEnumerable<Coin> receivedCoins, Network network,
            string transactionId, bool confirmed, decimal fee)
        {
            _network = Convert.ToNBitcoinNetwork(network);

            _spentCoins = new List<ICoin>();
            _receivedCoins = new List<ICoin>();
            foreach (var coin in spentCoins)
            {
                _spentCoins.Add(coin);
            }
            foreach (var coin in receivedCoins)
            {
                _receivedCoins.Add(coin);
            }

            Id = transactionId;
            Confirmed = confirmed;
            Fee = fee;

            AllInOutsAdded =
                FillInOutInfoList(out _inputs, _spentCoins)
                &&
                FillInOutInfoList(out _outputs, _receivedCoins);
        }

        public List<InOutInfo> Inputs => _inputs;
        public List<InOutInfo> Outputs => _outputs;
        public bool AllInOutsAdded { get; private set; }
        public string Id { get; }
        public bool Confirmed { get; }
        public decimal Fee { get; }
        public Network Network => Convert.ToHiddenBitcoinNetwork(_network);
        public decimal TotalInputAmount => SumAmounts(_spentCoins);
        public decimal TotalOutputAmount => SumAmounts(_receivedCoins);

        private bool FillInOutInfoList(out List<InOutInfo> inOutInfoList, List<ICoin> coins)
        {
            inOutInfoList = new List<InOutInfo>();
            var allInOutsAdded = true;

            foreach (var coin in coins)
            {
                try
                {
                    var outputInfo = new InOutInfo(
                        coin.GetScriptCode().GetDestinationAddress(_network).ToWif(),
                        ((Money) coin.Amount).ToDecimal(MoneyUnit.BTC));

                    inOutInfoList.Add(outputInfo);
                }
                catch
                {
                    allInOutsAdded = false;
                }
            }

            return allInOutsAdded;
        }

        private decimal SumAmounts(List<ICoin> coins)
        {
            return coins.Sum(x => ((Money) x.Amount).ToDecimal(MoneyUnit.BTC));
        }
    }
}