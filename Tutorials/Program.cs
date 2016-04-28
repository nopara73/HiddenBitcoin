// ReSharper disable All

using System;
using System.IO;
using HiddenBitcoin;
using HiddenBitcoin.DataClasses;

namespace Tutorials
{
    internal class Program
    {
        private static void Main()
        {
            Chapter1(); // Storing keys.

            Console.ReadLine();
        }

        private static void Chapter1()
        {
            var walletFilePath = @"Wallets\hiddenWallet.hid"; // extension can be anything
            var recoveredWalletFilePath = @"Wallets\sameWallet.hid";
            try
            {
                // First specify a network, it can be TestNet or MainNet
                // You can quickly get some free testnet coins to play with, just google it
                var network = Network.TestNet;

                var password = "ILoveHiddenWallet";

                // Create a safe at the specified path with a password on the specified network
                // The Safe class helps you mange your seed properly
                // More accurately it stores a password encrypted mnemonic.
                // I like to call the safe file wallet file, because the user might encounter it and it is familiar with this terminology.
                var hiddenSafe = Safe.Create(password, walletFilePath, network);

                // Safe creation has created a mnemonic, too, you can use it to recover or duplicate the safe
                Console.WriteLine(hiddenSafe.Mnemonic);

                // Let's recover the safe to an other file
                var mnemonic = hiddenSafe.Mnemonic;
                var recoveredSafe = Safe.Recover(mnemonic, password, recoveredWalletFilePath, network);

                // You can also load an existing safe from file with your password
                var loadedSafe = Safe.Load(password, walletFilePath);

                // After we load a safe it's not a bad idea to check if it is on the expected network
                if (network != loadedSafe.Network)
                    throw new Exception("WrongNetwork");

                // Finally let's write out the seed
                // The seed, what we want to protect so much
                // The seed, why we have 7 lines of code above instead of just one.
                Console.WriteLine(loadedSafe.Seed);
                Console.WriteLine(loadedSafe.SeedPublicKey);
            }
            finally
            {
                // Cleanup in order to be able to execute this chapter repeatedly
                File.Delete(walletFilePath);
                File.Delete(recoveredWalletFilePath);
            }
        }
    }
}