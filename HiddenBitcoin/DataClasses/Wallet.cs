using HiddenBitcoin.DataClasses.KeyManagement;
using HiddenBitcoin.DataClasses.Monitoring;

namespace HiddenBitcoin.DataClasses
{
    public class Wallet : SafeMonitor
    {
        public Wallet(Safe safe) : base(safe)
        {
            Safe = safe;
        }

        public Safe Safe { get; }
    }
}