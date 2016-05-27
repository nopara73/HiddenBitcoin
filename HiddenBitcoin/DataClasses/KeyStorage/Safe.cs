using System;
using System.IO;
using NBitcoin;
using NBitcoin.Stealth;

namespace HiddenBitcoin.DataClasses.KeyStorage
{
    public class Safe
    {
        private NBitcoin.Network _network;
        private ExtKey _seedPrivateKey;

        private Safe(string password, string walletFilePath, Network network, string mnemonicString = null)
        {
            SetNetwork(network);

            if (mnemonicString != null)
            {
                SetSeed(password, mnemonicString);
            }

            WalletFilePath = walletFilePath;
        }

        public string WalletFilePath { get; private set; }
        public string Seed => _seedPrivateKey.GetWif(_network).ToWif();
        public string SeedPublicKey => _seedPrivateKey.Neuter().GetWif(_network).ToWif();

        public Network Network
        {
            get
            {
                if (_network == NBitcoin.Network.Main)
                    return Network.MainNet;
                if (_network == NBitcoin.Network.TestNet)
                    return Network.TestNet;
                throw new InvalidOperationException("WrongNetwork");
            }
        }

        public string GetAddress(int index)
        {
            return _seedPrivateKey.Derive(index, true).ScriptPubKey.GetDestinationAddress(_network).ToWif();
        }

        public string GetPrivateKey(int index)
        {
            return _seedPrivateKey.Derive(index, true).GetWif(_network).ToWif();
        }

        public PrivateKeyAddressPair GetPrivateKeyAddressPair(int index)
        {
            var foo = _seedPrivateKey.Derive(index, true).GetWif(_network);
            return new PrivateKeyAddressPair
            {
                PrivateKey = foo.ToWif(),
                Address = foo.ScriptPubKey.GetDestinationAddress(_network).ToWif()
            };
        }

        private void Save(string password, string walletFilePath, Network network)
        {
            if (File.Exists(walletFilePath))
                throw new Exception("WalletFileAlreadyExists");

            var directoryPath = Path.GetDirectoryName(Path.GetFullPath(walletFilePath));
            if (directoryPath != null) Directory.CreateDirectory(directoryPath);

            var privateKey = _seedPrivateKey.PrivateKey;
            var chainCode = _seedPrivateKey.ChainCode;

            var encryptedBitcoinPrivateKeyString = privateKey.GetEncryptedBitcoinSecret(password, _network).ToWif();
            var chainCodeString = Convert.ToBase64String(chainCode);

            var networkString = network.ToString();

            WalletFileSerializer.Serialize(walletFilePath,
                encryptedBitcoinPrivateKeyString,
                chainCodeString,
                networkString);
        }

        public static Safe Load(string password, string walletFilePath)
        {
            if (!File.Exists(walletFilePath))
                throw new Exception("WalletFileDoesNotExists");

            var walletFileRawContent = WalletFileSerializer.Deserialize(walletFilePath);

            var encryptedBitcoinPrivateKeyString = walletFileRawContent.Seed;
            var chainCodeString = walletFileRawContent.ChainCode;

            var chainCode = Convert.FromBase64String(chainCodeString);

            Network network;
            var networkString = walletFileRawContent.Network;
            if (networkString == Network.MainNet.ToString())
                network = Network.MainNet;
            else if (networkString == Network.TestNet.ToString())
                network = Network.TestNet;
            else throw new Exception("NotRecognizedNetworkInWalletFile");

            var safe = new Safe(password, walletFilePath, network);

            var privateKey = Key.Parse(encryptedBitcoinPrivateKeyString, password, safe._network);
            var seedExtKey = new ExtKey(privateKey, chainCode);
            safe.SetSeed(seedExtKey);

            return safe;
        }

        /// <summary>
        ///     Creates a mnemonic, a seed, encrypts it and stores in the specified path.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="walletFilePath"></param>
        /// <param name="network"></param>
        /// <returns>Safe and Mnemonic</returns>
        public static InitialSafe Create(string password, string walletFilePath, Network network)
        {
            var safe = new Safe(password, walletFilePath, network);

            var mnemonic = safe.SetSeed(password);

            safe.Save(password, walletFilePath, network);

            var initialSafe = new InitialSafe
            {
                Mnemonic = mnemonic.ToString(),
                Safe = safe
            };

            return initialSafe;
        }

        public static Safe Recover(string mnemonic, string password, string walletFilePath, Network network)
        {
            var safe = new Safe(password, walletFilePath, network, mnemonic);
            safe.Save(password, walletFilePath, network);
            return safe;
        }

        private Mnemonic SetSeed(string password, string mnemonicString = null)
        {
            var mnemonic =
                mnemonicString == null
                    ? new Mnemonic(Wordlist.English, WordCount.Twelve)
                    : new Mnemonic(mnemonicString);

            _seedPrivateKey = mnemonic.DeriveExtKey(password);

            return mnemonic;
        }

        private void SetSeed(ExtKey seedExtKey)
        {
            _seedPrivateKey = seedExtKey;
        }

        private void SetNetwork(Network network)
        {
            if (network == Network.MainNet)
                _network = NBitcoin.Network.Main;
            else if (network == Network.TestNet)
                _network = NBitcoin.Network.TestNet;
            else throw new Exception("WrongNetwork");
        }

        #region Stealth

        // ReSharper disable InconsistentNaming
        private Key _spendPrivateKey => _seedPrivateKey.PrivateKey;
        public string SpendPrivateKey => _spendPrivateKey.GetWif(_network).ToWif();
        private Key _scanPrivateKey => _seedPrivateKey.Derive(0, hardened: true).PrivateKey;
        public string ScanPrivateKey => _scanPrivateKey.GetWif(_network).ToWif();
        // ReSharper restore InconsistentNaming

        public string StealthAddress => new BitcoinStealthAddress
            (_scanPrivateKey.PubKey, new[] {_spendPrivateKey.PubKey}, 1, null, _network
            ).ToWif();

        #endregion
    }
}