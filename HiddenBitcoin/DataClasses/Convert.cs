using System;

namespace HiddenBitcoin.DataClasses
{
    internal static class Convert
    {
        internal static NBitcoin.Network ToNBitcoinNetwork(Network hNetwork)
        {
            if (hNetwork == Network.MainNet)
                return NBitcoin.Network.Main;
            if (hNetwork == Network.TestNet)
                return NBitcoin.Network.TestNet;
            throw new InvalidOperationException("WrongNetwork");
        }

        internal static Network ToHiddenBitcoinNetwork(NBitcoin.Network nNetwork)
        {
            if (nNetwork == NBitcoin.Network.Main)
                return Network.MainNet;
            if (nNetwork == NBitcoin.Network.TestNet)
                return Network.TestNet;
            throw new InvalidOperationException("WrongNetwork");
        }
    }
}