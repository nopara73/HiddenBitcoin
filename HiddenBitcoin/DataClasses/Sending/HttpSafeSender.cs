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

        public TransactionInfo Send(List<AddressAmountPair> to, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = Safe.NotEmptyAddresses.Select(Safe.GetPrivateKey).ToList();

            return Send(
                notEmptyPrivateKeys,
                to,
                feeType,
                Safe.UnusedAddresses.First(),
                message);
        }

        public TransactionInfo SendAll(string toAddress, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = Safe.NotEmptyAddresses.Select(Safe.GetPrivateKey).ToList();

            return SendAll(
                notEmptyPrivateKeys,
                toAddress,
                feeType,
                message
                );
        }
    }
}