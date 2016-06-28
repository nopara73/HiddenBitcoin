using System.Collections.Generic;
using System.Linq;
using HiddenBitcoin.DataClasses.KeyManagement;

namespace HiddenBitcoin.DataClasses.Sending
{
    public class HttpSafeSender : HttpSender
    {
        public HttpSafeSender(HttpSafe safe) : base(safe.Network)
        {
            AssertNetwork(safe.Network);
            Safe = safe;
        }

        public HttpSafe Safe { get; }

        public TransactionInfo CreateTransaction(List<AddressAmountPair> to, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = Safe.NotEmptyAddresses.Select(Safe.GetPrivateKey).ToList();

            return CreateTransaction(
                notEmptyPrivateKeys,
                to,
                feeType,
                Safe.UnusedAddresses.First(),
                message);
        }

        public TransactionInfo CreateSendAllTransaction(string toAddress, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = Safe.NotEmptyAddresses.Select(Safe.GetPrivateKey).ToList();

            return CreateSendAllTransaction(
                notEmptyPrivateKeys,
                toAddress,
                feeType,
                message
                );
        }
    }
}