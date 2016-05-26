using System;
using System.IO;
using Newtonsoft.Json;

namespace HiddenBitcoin.DataClasses.KeyStorage
{
    internal class WalletFileSerializer
    {
        [JsonConstructor]
        private WalletFileSerializer(string encryptedBitcoinPrivateKey, string chainCode, string network)
        {
            Seed = encryptedBitcoinPrivateKey;
            ChainCode = chainCode;
            Network = network;
        }

        // KEEP THEM PUBLIC OTHERWISE IT WILL NOT SERIALIZE!
        public string Seed { get; set; }
        public string ChainCode { get; set; }
        public string Network { get; set; }

        internal static void Serialize(string walletFilePath, string encryptedBitcoinPrivateKey, string chainCode,
            string network)
        {
            var content =
                JsonConvert.SerializeObject(new WalletFileSerializer(encryptedBitcoinPrivateKey, chainCode, network));

            if (File.Exists(walletFilePath))
                throw new Exception("WalletFileAlreadyExists");

            var directoryPath = Path.GetDirectoryName(Path.GetFullPath(walletFilePath));
            if (directoryPath != null) Directory.CreateDirectory(directoryPath);

            File.WriteAllText(walletFilePath, content);
        }

        internal static WalletFileSerializer Deserialize(string path)
        {
            if (!File.Exists(path))
                throw new Exception("WalletFileDoesNotExists");

            var contentString = File.ReadAllText(path);
            var walletFileSerializer = JsonConvert.DeserializeObject<WalletFileSerializer>(contentString);

            return new WalletFileSerializer(walletFileSerializer.Seed, walletFileSerializer.ChainCode,
                walletFileSerializer.Network);
        }
    }
}