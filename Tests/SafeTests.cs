using System;
using HiddenBitcoin.DataClasses;
using HiddenBitcoin.DataClasses.KeyManagement;
using Xunit;

namespace Tests
{
    public class SafeTests
    {
        [Theory]
        [InlineData(Network.MainNet)]
        [InlineData(Network.TestNet)]
        public void NetworkIsRight(Network network)
        {
            string mnemonic;
            const string walletFilePath = "testWallet.dat";
            const string password = "password";
            var safe = Safe.Create(out mnemonic, password, walletFilePath, network);
            Assert.Equal(network, safe.Network);

            var loadedSafe = Safe.Load(password, walletFilePath);
            Assert.Equal(network, loadedSafe.Network);

            var recoverdSafe = Safe.Recover(mnemonic, password, "recoveredTestWallet.dat", network);
            Assert.Equal(network, recoverdSafe.Network);
			
            safe.DeleteWalletFile();
            recoverdSafe.DeleteWalletFile();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(9999)]
        public void ProperlyLoadRecover(int index)
        {
            const Network network = Network.TestNet;
            string mnemonic;
            const string walletFilePath = "testWallet.dat";
            const string password = "password";
            var safe = Safe.Create(out mnemonic, password, walletFilePath, network);

            var loadedSafe = Safe.Load(password, walletFilePath);
            Assert.Equal(safe.Seed, loadedSafe.Seed);

            var recoverdSafe = Safe.Recover(mnemonic, password, "recoveredTestWallet.dat", network);
            Assert.Equal(safe.Seed, loadedSafe.Seed);

            Assert.Equal(loadedSafe.SeedPublicKey, recoverdSafe.SeedPublicKey);
            Assert.Equal(loadedSafe.StealthAddress, recoverdSafe.StealthAddress);
            Assert.Equal(loadedSafe.StealthScanPrivateKey, recoverdSafe.StealthScanPrivateKey);
            Assert.Equal(loadedSafe.StealthSpendPrivateKey, recoverdSafe.StealthSpendPrivateKey);
            Assert.Equal(loadedSafe.GetAddress(index), recoverdSafe.GetAddress(index));
            Assert.Equal(loadedSafe.GetPrivateKey(index), recoverdSafe.GetPrivateKey(index));
            Assert.Equal(loadedSafe.GetPrivateKeyAddressPair(index).Address, recoverdSafe.GetPrivateKeyAddressPair(index).Address);
            Assert.Equal(loadedSafe.GetPrivateKeyAddressPair(index).PrivateKey, recoverdSafe.GetPrivateKeyAddressPair(index).PrivateKey);
            
            safe.DeleteWalletFile();
            recoverdSafe.DeleteWalletFile();
        }

        [Fact]
        public void LimitedSafeTest()
        {
            const Network network = Network.TestNet;
            string mnemonic;
            const string walletFilePath = "testWallet.dat";
            const string password = "password";
            var safe = Safe.Create(out mnemonic, password, walletFilePath, network);

            const int limit = 100;
            var limitedSafe = new LimitedSafe(safe, limit);

            Assert.Equal(safe.GetAddress(0), limitedSafe.GetAddress(0));
            Assert.Equal(limit, limitedSafe.AddressCount);
            Assert.Contains(safe.GetAddress(limit-1), limitedSafe.Addresses);
            Assert.Equal(safe.GetAddress(7), limitedSafe.Addresses[7]);

            Assert.Throws<IndexOutOfRangeException>(() => limitedSafe.GetAddress(limit));
            Assert.Throws<IndexOutOfRangeException>(() => limitedSafe.GetPrivateKey(limit));
            Assert.Throws<IndexOutOfRangeException>(() => limitedSafe.GetPrivateKeyAddressPair(limit));
            
            safe.DeleteWalletFile();
        }
    }
}
