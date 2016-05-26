using System;
using System.IO;
using NBitcoin;
using NBitcoin.Stealth;

namespace HiddenBitcoin.DataClasses.KeyStorage
{
    public class Safe
    {
        private NBitcoin.Network _network;

        private Safe(string password, string walletFilePath, Network network, string mnemonicString = null)
        {
            SetNetwork(network);

            if (mnemonicString != null)
            {
                SetSeed(password, mnemonicString);
            }

            WalletFilePath = walletFilePath;
        }

        public ExtKey SeedExtKey { get; private set; }
        public string Seed => SeedExtKey.GetWif(_network).ToWif();
        public string SeedPublicKey => SeedExtKey.Neuter().GetWif(_network).ToWif();
        public string WalletFilePath { get; private set; }

        #region Stealth

        // As long as the safe is fully trusted no need different keys scan and spendkey
        // ReSharper disable InconsistentNaming
        private Key _spendPrivateKey => SeedExtKey.PrivateKey;
        public string SpendPrivateKey => _spendPrivateKey.GetWif(_network).ToWif();
        private Key _scanPrivateKey => _spendPrivateKey;
        public string ScanPrivateKey => _scanPrivateKey.GetWif(_network).ToWif();
        // ReSharper restore InconsistentNaming

        public string StealthAddress => new BitcoinStealthAddress
            (
                scanKey: _scanPrivateKey.PubKey,
                pubKeys: new[] { _spendPrivateKey.PubKey },
                signatureCount: 1,
                bitfield: null,
                network: _network
            ).ToWif();
        #endregion

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

        private void Save(string password, string walletFilePath, Network network)
        {
            if (File.Exists(walletFilePath))
                throw new Exception("WalletFileAlreadyExists");

            var directoryPath = Path.GetDirectoryName(Path.GetFullPath(walletFilePath));
            if (directoryPath != null) Directory.CreateDirectory(directoryPath);

            var privateKey = SeedExtKey.PrivateKey;
            var chainCode = SeedExtKey.ChainCode;

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

            SeedExtKey = mnemonic.DeriveExtKey(password);

            return mnemonic;
        }

        private void SetSeed(ExtKey seedExtKey)
        {
            SeedExtKey = seedExtKey;
        }

        private void SetNetwork(Network network)
        {
            if (network == Network.MainNet)
                _network = NBitcoin.Network.Main;
            else if (network == Network.TestNet)
                _network = NBitcoin.Network.TestNet;
            else throw new Exception("WrongNetwork");
        }
    }
}