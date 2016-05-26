using System;
using System.IO;
using NBitcoin;

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

            Key privateKey = SeedExtKey.PrivateKey;
            byte[] chainCode = SeedExtKey.ChainCode;

            string encryptedBitcoinPrivateKeyString = privateKey.GetEncryptedBitcoinSecret(password, _network).ToWif();
            string chainCodeString = Convert.ToBase64String(chainCode);

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
            
            string encryptedBitcoinPrivateKeyString = walletFileRawContent.Seed;
            string chainCodeString = walletFileRawContent.ChainCode;

            byte[] chainCode = Convert.FromBase64String(chainCodeString);

            Network network;
            var networkString = walletFileRawContent.Network;
            if (networkString == Network.MainNet.ToString())
                network = Network.MainNet;
            else if (networkString == Network.TestNet.ToString())
                network = Network.TestNet;
            else throw new Exception("NotRecognizedNetworkInWalletFile");

            Safe safe = new Safe(password, walletFilePath, network);

            Key privateKey = Key.Parse(encryptedBitcoinPrivateKeyString, password, safe._network);
            ExtKey seedExtKey = new ExtKey(privateKey, chainCode);
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

            Mnemonic mnemonic = safe.SetSeed(password);

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