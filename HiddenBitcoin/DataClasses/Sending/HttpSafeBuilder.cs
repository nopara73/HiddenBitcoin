using System.Collections.Generic;
using System.Linq;
using HiddenBitcoin.DataClasses.KeyManagement;

namespace HiddenBitcoin.DataClasses.Sending
{
    public class HttpSafeBuilder : HttpBuilder
    {
        public HttpSafeBuilder(HttpSafe safe) : base(safe.Network)
        {
            AssertNetwork(safe.Network);
            Safe = safe;
        }

        public HttpSafe Safe { get; }

        public TransactionInfo BuildTransaction(List<AddressAmountPair> to, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = Safe.NotEmptyAddresses.Select(Safe.GetPrivateKey).ToList();

            return BuildTransaction(
                notEmptyPrivateKeys,
                to,
                feeType,
                Safe.UnusedAddresses.First(),
                message
                );
        }

        public TransactionInfo BuildSpendAllTransaction(string toAddress, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = Safe.NotEmptyAddresses.Select(Safe.GetPrivateKey).ToList();

            return BuildSpendAllTransaction(
                notEmptyPrivateKeys,
                toAddress,
                feeType,
                message
                );
        }
    }
}