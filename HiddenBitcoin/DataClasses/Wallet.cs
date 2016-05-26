using HiddenBitcoin.DataClasses.KeyStorage;

namespace HiddenBitcoin.DataClasses
{
    public class Wallet : WalletMonitor
    {
        public Wallet(Safe safe) : base(safe.SeedPublicKey)
        {
            Safe = safe;
        }

        public Safe Safe { get; }
    }
}