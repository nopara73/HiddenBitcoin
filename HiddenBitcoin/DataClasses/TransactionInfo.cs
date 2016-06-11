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
        private readonly GetTransactionResponse _transactionResponse;
        
        internal TransactionInfo(GetTransactionResponse transactionResponse, Network network)
        {
            _transactionResponse = transactionResponse;

            _network = Convert.ToNBitcoinNetwork(network);

            _spentCoins = _transactionResponse.SpentCoins;
            _receivedCoins = _transactionResponse.ReceivedCoins;


            AllInOutsAdded =
                FillInOutInfoList(out _inputs, _spentCoins)
                &&
                FillInOutInfoList(out _outputs, _receivedCoins);
        }

        public List<InOutInfo> Inputs => _inputs;
        public List<InOutInfo> Outputs => _outputs;
        public bool AllInOutsAdded { get; private set; }
        public string Id => _transactionResponse.TransactionId.ToString();
        public bool Confirmed => _transactionResponse.Block != null;
        public decimal Fee => _transactionResponse.Fees.ToDecimal(MoneyUnit.BTC);
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