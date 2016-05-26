using HiddenBitcoin.DataClasses.KeyStorage;

namespace HiddenBitcoin.DataClasses
{
    public class Wallet: WalletMonitor
    {
        public Safe Safe { get; }

        public Wallet(Safe safe): base(safe.SeedPublicKey)
        {
            Safe = safe;
        }

        // get key
        // push transaction
    }
}
