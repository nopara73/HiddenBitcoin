using System;
using System.IO;
using NBitcoin;
using NBitcoin.Stealth;

namespace HiddenBitcoin.DataClasses.KeyManagement
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
        public Network Network => Convert.ToHiddenBitcoinNetwork(_network);

        public string GetAddress(int index, bool clean = false)
        {
            var startPath = NormalHdPath;
            if (clean)
                startPath = CleanHdPath;

            var keyPath = new KeyPath(startPath + "/" + index);
            return _seedPrivateKey.Derive(keyPath).ScriptPubKey.GetDestinationAddress(_network).ToWif();
        }

        public string GetPrivateKey(int index, bool clean = false)
        {
            var startPath = NormalHdPath;
            if (clean)
                startPath = CleanHdPath;

            var keyPath = new KeyPath(startPath + "/" + index);
            return _seedPrivateKey.Derive(keyPath).GetWif(_network).ToWif();
        }

        public PrivateKeyAddressPair GetPrivateKeyAddressPair(int index, bool clean = false)
        {
            var startPath = NormalHdPath;
            if (clean)
                startPath = CleanHdPath;

            var keyPath = new KeyPath(startPath + "/" + index);
            var foo = _seedPrivateKey.Derive(keyPath).GetWif(_network);
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
            var chainCodeString = System.Convert.ToBase64String(chainCode);

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

            var encryptedBitcoinPrivateKeyString = walletFileRawContent.EncryptedSeed;
            var chainCodeString = walletFileRawContent.ChainCode;

            var chainCode = System.Convert.FromBase64String(chainCodeString);

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
        /// <param name="mnemonic">empty string</param>
        /// <param name="password"></param>
        /// <param name="walletFilePath"></param>
        /// <param name="network"></param>
        /// <returns>Safe</returns>
        public static Safe Create(out string mnemonic, string password, string walletFilePath, Network network)
        {
            var safe = new Safe(password, walletFilePath, network);

            mnemonic = safe.SetSeed(password).ToString();

            safe.Save(password, walletFilePath, network);

            return safe;
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

        #region Hierarchy

        private const string StealthPath = "0'";
        private const string NormalHdPath = "1'";
        private const string CleanHdPath = "2'";

        #endregion

        #region Stealth

        // ReSharper disable InconsistentNaming
        private Key _StealthSpendPrivateKey => _seedPrivateKey.Derive(new KeyPath(StealthPath + "/0'")).PrivateKey;
        public string StealthSpendPrivateKey => _StealthSpendPrivateKey.GetWif(_network).ToWif();
        private Key _StealthScanPrivateKey => _seedPrivateKey.Derive(new KeyPath(StealthPath + "/1'")).PrivateKey;
        public string StealthScanPrivateKey => _seedPrivateKey.Derive(1, true).Derive(1, true).GetWif(_network).ToWif();
        // ReSharper restore InconsistentNaming

        public string StealthAddress => new BitcoinStealthAddress
            (_StealthScanPrivateKey.PubKey, new[] {_StealthSpendPrivateKey.PubKey}, 1, null, _network
            ).ToWif();

        #endregion
    }
}