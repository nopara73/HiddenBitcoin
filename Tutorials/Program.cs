// ReSharper disable All

using System;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using HiddenBitcoin.DataClasses;
using HiddenBitcoin.DataClasses.KeyManagement;
using HiddenBitcoin.DataClasses.Monitoring;

namespace Tutorials
{
    internal class Program
    {
        private static void Main()
        {
            //Part1(); // Storing keys
            //Part2(); // Monitoring keys
            Part2Lesson2(); // Monitoring Safes

            Console.ReadLine();
        }

        private static void Part1()
        {
            var walletFilePath = @"Wallets\hiddenWallet.hid"; // extension can be anything
            var recoveredWalletFilePath = @"Wallets\sameWallet.hid";
            try
            {
                // First specify a network, it can be TestNet or MainNet
                // You can quickly get some free testnet coins to play with, just google it
                var network = Network.MainNet;

                var password = "password";

                // Create a safe at the specified path with a password on the specified network
                // The Safe class helps you mange your seed properly
                // I like to call the safe file to wallet file, 
                // because the user might encounters with it and he is familiar with this terminology.
                string mnemonic;
                var safe = Safe.Create(out mnemonic, password, walletFilePath, network);

                // Safe creation has created a mnemonic, too, you can use it to recover (or duplicate) the safe
                Console.WriteLine(mnemonic);

                // Let's recover the safe to an other file
                var recoveredSafe = Safe.Recover(mnemonic, password, recoveredWalletFilePath, network);

                // You can also load an existing safe from file with your password
                var loadedSafe = Safe.Load(password, walletFilePath);

                // After we load a safe it's not a bad idea to check if it is on the expected network
                if (network != loadedSafe.Network)
                    throw new Exception("WrongNetwork");

                // Let's write out a few things
                // The seed private key
                Console.WriteLine(loadedSafe.Seed);
                // You can generate addresses with the public key, but you cannot spend them
                Console.WriteLine(loadedSafe.SeedPublicKey);

                // The third child address
                Console.WriteLine(loadedSafe.GetAddress(2));
                // The first child private key
                Console.WriteLine(loadedSafe.GetPrivateKey(0));

                // The first ten privkey address pair
                for (var i = 0; i < 10; i++)
                {
                    var privateKeyAddressPair = loadedSafe.GetPrivateKeyAddressPair(i);
                    Console.WriteLine(privateKeyAddressPair.Address);
                    Console.WriteLine(privateKeyAddressPair.PrivateKey);
                }

                // List out the first 10 address
                for (var i = 0; i < 10; i++)
                {
                    Console.WriteLine(safe.GetAddress(i));
                }

                // You can get the dark wallet type stealth address of the safe
                Console.WriteLine(loadedSafe.StealthAddress);
                // Also the scan and spendkey
                Console.WriteLine(loadedSafe.StealthScanPrivateKey);
                Console.WriteLine(loadedSafe.StealthSpendPrivateKey);

                // The wallet has a clean part of it, more on that later
                Console.WriteLine(safe.GetAddress(3, clean: true));
                Console.WriteLine(safe.GetPrivateKey(5, clean: true));

                //Safe[] safes = new[] {hiddenSafe, recoveredSafe, loadedSafe};
                //foreach (var safe in safes)
                //{
                //    Console.WriteLine(safe.Network);
                //    Console.WriteLine(safe.Seed);
                //    Console.WriteLine(safe.SeedPublicKey);
                //    Console.WriteLine(safe.WalletFilePath);
                //}
            }
            finally
            {
                // Cleanup in order to be able to execute this chapter repeatedly
                File.Delete(walletFilePath);
                File.Delete(recoveredWalletFilePath);
            }
        }

        private static void Part2()
        {
            var network = Network.MainNet;

            var blockchainMonitor = new HttpMonitor(network);

            var balanceInfo = blockchainMonitor.GetBalance("1ENCTCkqoJqy2XZ2m2Dy1bRax7hsSnC5Fc");
            Console.WriteLine(balanceInfo.Address);
            Console.WriteLine(balanceInfo.Confirmed);
            Console.WriteLine(balanceInfo.Unconfirmed);


            //tx is exotic (has OP_RETURN)
            var transactionInfo =
                blockchainMonitor.GetTransactionInfo("8bae12b5f4c088d940733dcd1455efc6a3a69cf9340e17a981286d3778615684");
            //tx is normal
            //var transactionInfo = blockchainMonitor.GetTransactionInfo("8bbd7678d93da5da8736a84a69a1de83834bea732c65342687e8db549f153504");

            Console.WriteLine("txid: " + transactionInfo.Id);
            Console.WriteLine("Network: " + transactionInfo.Network);
            Console.WriteLine("Confirmed: " + transactionInfo.Confirmed);
            Console.WriteLine("Total amount of all inputs: " + transactionInfo.TotalInputAmount);
            Console.WriteLine("Total amount of all outputs: " + transactionInfo.TotalOutputAmount);
            Console.WriteLine("Fee : " + transactionInfo.Fee);
            Console.WriteLine("There are no exotic inputs or outputs, so all of them have been added successfully: "
                              + Environment.NewLine + transactionInfo.AllInOutsAdded);

            Console.WriteLine(Environment.NewLine + "Input addresses and amounts: ");
            foreach (var input in transactionInfo.Inputs)
            {
                Console.WriteLine(input.Amount + " " + input.Address);
            }
            Console.WriteLine(Environment.NewLine + "Output addresses and amounts: ");
            foreach (var output in transactionInfo.Outputs)
            {
                Console.WriteLine(output.Amount + " " + output.Address);
            }

            //lot of transactions:
            //var address = "13eh4wPLe1nCsh8FXJNpL6e9D1edWNT1Ub";
            //only few transactions:
            var address = "19V1JJ68Ee57tKnG7NikH4tU93xqMShCyD";
            var history =
                blockchainMonitor.GetAddressHistory(address);

            Console.WriteLine("Number of transactions: " + history.Records.Count);

            var allTransactionsConfirmed = true;
            foreach (var record in history.Records)
            {
                Console.WriteLine("txid: " + record.TransactionId);
                allTransactionsConfirmed = allTransactionsConfirmed && record.Confirmed;
            }
            Console.WriteLine("All transactions are confirmed: " + allTransactionsConfirmed);

            Console.WriteLine("Total received - Total spent = Balance");
            Console.WriteLine(history.TotalReceived + " - " + history.TotalSpent + " = " +
                              (history.TotalReceived - history.TotalSpent));

            balanceInfo = blockchainMonitor.GetBalance(address);
            Console.WriteLine(@"blockchainMonitor.GetBalance(address): " +
                              (balanceInfo.Confirmed + balanceInfo.Unconfirmed));
        }

        private static void Part2Lesson2()
        {
            var safePath = "Hidden.wallet";
            string mnemonic;
            Safe safe;
            if (!File.Exists(safePath))
            {
                safe = Safe.Create(out mnemonic, "password", safePath, Network.TestNet);
            }
            else
            {
                safe = Safe.Load("password", safePath);
            }

            var safeMonitor = new SpvSafeMonitor(safe);
            try
            {
                safeMonitor.StartConnecting();
                while (safeMonitor.ConnectionProgressPercent != 100)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine($"Connected: {safeMonitor.ConnectionProgressPercent}%");
                    Console.WriteLine($"Synced: {safeMonitor.SyncProgressPercent}%");
                }


            }
            finally
            {
                safeMonitor.Disconnect();
            }
        }
    }
}