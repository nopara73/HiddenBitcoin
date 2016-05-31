using HiddenBitcoin.DataClasses.KeyManagement;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses.Monitoring
{
    public class SafeMonitor
    {
        private readonly QBitNinjaClient _qBitNinjaClient;
        private readonly Safe _safe;

        public SafeMonitor(Safe safe)
        {
            _safe = safe;

            _qBitNinjaClient = new QBitNinjaClient(Network);
        }

        private NBitcoin.Network Network => Convert.ToNBitcoinNetwork(_safe.Network);
    }
}