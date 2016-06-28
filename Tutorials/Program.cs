// ReSharper disable All
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using HiddenBitcoin.DataClasses;
using HiddenBitcoin.DataClasses.Balances;
using HiddenBitcoin.DataClasses.Histories;
using HiddenBitcoin.DataClasses.KeyManagement;
using HiddenBitcoin.DataClasses.Monitoring;
using HiddenBitcoin.DataClasses.Sending;

namespace Tutorials
{
    internal class Program
    {
        private static void Main()
        {
            //Part1(); // Storing keys
            //Part2Lesson1(); // Monitoring keys using HTTP
            Part2Lesson2(); // Monitoring safes using HTTP

            //TemporarilyTestHttpSafeMonitor();
            //TemporarilyTestHttpMonitor();

            Console.ReadLine();
        }

        private static void TemporarilyTestHttpMonitor()
        {
            var monitor = new HttpMonitor(Network.TestNet);

            var privateKey =
                "tprv8f3GDAef8nhKvQbC5KGEerqDGB2s2KgMNHS4kLDXXP7c8M6ZcxyEzyvsVA9C1ss9Fj5QEiCiKbyEfD97duhXjc2he58dDicMtLs3YfwNJb2";
            var address = "miLGbZvQ5sEd5BKCPXjEJzknMdENuFEzCy";

            var bal = monitor.GetAddressBalanceInfo(address);
            var his = monitor.GetAddressHistory(address);

            Console.WriteLine($"Balance: {bal.Balance}");
            Console.WriteLine($"Confirmed balance: {bal.Confirmed}");
            Console.WriteLine($"Unconfirmed balance: {bal.Unconfirmed}");
            Console.WriteLine($"TotalReceived: {his.TotalReceived}");
            Console.WriteLine($"TotalSpent: {his.TotalSpent}");
            Console.WriteLine($"TotalReceived - TotalSpent: {his.TotalReceived - his.TotalSpent}");
            Console.WriteLine($"TotalReceived - TotalSpent == Balance: {his.TotalReceived - his.TotalSpent == bal.Balance}");
            Console.WriteLine();
            Console.WriteLine("RECORDS:");

            foreach (var record in his.Records)
            {
                Console.WriteLine();
                Console.WriteLine($"DateTime: {record.DateTime}");
                Console.WriteLine($"Amount: {record.Amount}");
                Console.WriteLine($"Confirmed: {record.Confirmed}");
            }

            //var spender = new HttpSender(Network.TestNet);
            //var tx = spender.CreateTransaction(
            //    new List<string> { privateKey },
            //    new List<AddressAmountPair>
            //    {
            //        new AddressAmountPair
            //        {
            //            //Address = "miLGbZvQ5sEd5BKCPXjEJzknMdENuFEzCy",
            //            Address = "miNu2YVQLSqFcw91fRVuNHMmGkStMd5SFj",
            //            Amount = 1
            //        }
            //    });
            //Console.WriteLine();
            //Console.WriteLine("Transaction created");
            //spender.Send(tx.Id);
            //Console.WriteLine("Transaction sent");
        }

        private static void TemporarilyTestHttpSafeMonitor()
        {
            var network = Network.TestNet;
            #region SetupSafe
            
            string walletFilePath;
            if (network == Network.MainNet)
                walletFilePath = "MainNetHidden2.wallet";
            else
                walletFilePath = "TestNetHidden2.wallet";

            Safe safe;
            if (File.Exists(walletFilePath))
            {
                safe = Safe.Load("password", walletFilePath);
                if (safe.Network != network)
                    throw new Exception("Wrong network");
            }
            else
            {
                string mnemonic;
                safe = Safe.Create(out mnemonic, "password", walletFilePath, network);
            }

            Console.WriteLine("Safe has been set up");
            #endregion

            #region InitializeHttpSafeMonitor

            var safeMonitor = new HttpSafeMonitor(safe, addressCount: 100);

            // Report initialization progress
            safeMonitor.InitializationStateChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;
                Console.WriteLine($"Initialization state: {monitor.InitializationState}");
            };
            safeMonitor.InitializationProgressPercentChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;
                Console.WriteLine($"Initializing: {monitor.InitializationProgressPercent}%");
            };

            // Let's wait until initialized
            while (safeMonitor.InitializationState != State.Ready)
                Thread.Sleep(100);

            Console.WriteLine("SafeMonitor is ready to work with");
            #endregion
            
            var bal = safeMonitor.SafeBalanceInfo;
            var his = safeMonitor.SafeHistory;

            Console.WriteLine();
            Console.WriteLine($"Balance: {bal.Balance}");
            Console.WriteLine($"Confirmed balance: {bal.Confirmed}");
            Console.WriteLine($"Unconfirmed balance: {bal.Unconfirmed}");
            Console.WriteLine($"TotalReceived: {his.TotalReceived}");
            Console.WriteLine($"TotalSpent: {his.TotalSpent}");
            Console.WriteLine($"TotalReceived - TotalSpent: {his.TotalReceived - his.TotalSpent}");
            Console.WriteLine($"TotalReceived - TotalSpent == Balance: {his.TotalReceived - his.TotalSpent == bal.Balance}");
            Console.WriteLine();
            Console.WriteLine("RECORDS:");

            foreach (var record in his.Records)
            {
                Console.WriteLine();
                Console.WriteLine($"DateTime: {record.DateTime}");
                Console.WriteLine($"Amount: {record.Amount}");
                Console.WriteLine($"Confirmed: {record.Confirmed}");
            }

            safeMonitor.BalanceChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;

                Console.WriteLine();
                Console.WriteLine("Change happened");
                Console.WriteLine($"Balance: {monitor.SafeBalanceInfo.Balance}");
                Console.WriteLine($"Confirmed balance: {monitor.SafeBalanceInfo.Confirmed}");
                Console.WriteLine($"Unconfirmed balance: {monitor.SafeBalanceInfo.Unconfirmed}");
                Console.WriteLine($"TotalReceived: {monitor.SafeHistory.TotalReceived}");
                Console.WriteLine($"TotalSpent: {monitor.SafeHistory.TotalSpent}");
                Console.WriteLine($"TotalReceived - TotalSpent: {monitor.SafeHistory.TotalReceived - monitor.SafeHistory.TotalSpent}");
                Console.WriteLine($"TotalReceived - TotalSpent == Balance: {monitor.SafeHistory.TotalReceived - monitor.SafeHistory.TotalSpent == monitor.SafeBalanceInfo.Balance}");
                Console.WriteLine();
                Console.WriteLine("Last record:");
                var record = monitor.SafeHistory.Records.First();
                Console.WriteLine($"DateTime: {record.DateTime}");
                Console.WriteLine($"Amount: {record.Amount}");
                Console.WriteLine($"Confirmed: {record.Confirmed}");
            };
            Console.WriteLine();
            Console.WriteLine("Subscribed to changes");

            //var spender = new HttpSafeSender(safeMonitor.Safe);
            //var tx = spender.CreateTransaction(
            //    new List<AddressAmountPair>
            //    {
            //        new AddressAmountPair
            //        {
            //            //Address = safeMonitor.Safe.GetAddress(99), // internal address
            //            Address = "n2eMqTT929pb1RDNuqEnxdaLau1rxy3efi", // outer address
            //            Amount = 1
            //        }
            //    });
            //Console.WriteLine();
            //Console.WriteLine("Transaction created");
            //spender.Send(tx.Id);
            //Console.WriteLine("Transaction sent");
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

        private static void Part2Lesson1()
        {
            var network = Network.MainNet;

            // HttpMonitor can monitor The Blockchain through HTTP (QBitNinja)
            var httpMonitor = new HttpMonitor(network);

            // Checking address balances
            var balanceInfo = httpMonitor.GetAddressBalanceInfo("1ENCTCkqoJqy2XZ2m2Dy1bRax7hsSnC5Fc");
            Console.WriteLine($"Address balance: {balanceInfo.Balance}"); // 0,05474889
            Console.WriteLine($"Confirmed balance: {balanceInfo.Confirmed}"); // 0
            Console.WriteLine($"Unconfirmed balance: {balanceInfo.Unconfirmed}"); // 0,05474889

            // Get history of an address
            var history = httpMonitor.GetAddressHistory("1ENCTCkqoJqy2XZ2m2Dy1bRax7hsSnC5Fc");

            Console.WriteLine("Number of transactions: " + history.Records.Count);

            // Exercise: are all transaction confirmed?
            var allTransactionsConfirmed = true;
            foreach (var record in history.Records)
            {
                Console.WriteLine(record.TransactionId + " : " + record.Amount);
                allTransactionsConfirmed = allTransactionsConfirmed && record.Confirmed;
            }
            Console.WriteLine("All transactions are confirmed: " + allTransactionsConfirmed);

            // Exercise: get the balance of the address
            Console.WriteLine("Total received - Total spent = Balance");
            Console.WriteLine(history.TotalReceived + " - " + history.TotalSpent + " = " +
                              (history.TotalReceived - history.TotalSpent));

            // Get some data from the transaction
            var transactionInfo1 = httpMonitor.GetTransactionInfo(history.Records.First().TransactionId);

            Console.WriteLine("txid: " + transactionInfo1.Id);
            Console.WriteLine("Network: " + transactionInfo1.Network);
            Console.WriteLine("Confirmed: " + transactionInfo1.Confirmed);
            Console.WriteLine("Total amount of all inputs: " + transactionInfo1.TotalInputAmount);
            Console.WriteLine("Total amount of all outputs: " + transactionInfo1.TotalOutputAmount);
            Console.WriteLine("Fee : " + transactionInfo1.Fee);

            Console.WriteLine(Environment.NewLine + "Input addresses and amounts: ");
            foreach (var input in transactionInfo1.Inputs)
            {
                Console.WriteLine(input.Amount + " " + input.Address);
            }
            Console.WriteLine(Environment.NewLine + "Output addresses and amounts: ");
            foreach (var output in transactionInfo1.Outputs)
            {
                Console.WriteLine(output.Amount + " " + output.Address);
            }

            // Sometimes my API can't fully process a transaction, because it has OP_RETURN for example
            // It should not be a concern for a Bitcoin wallet that purely handles money, if a transaction output or input has not been added
            // that means it has some other purpose, a wallet API can dismiss it
            // This tx is exotic (has OP_RETURN)
            var transactionInfo2 =
                httpMonitor.GetTransactionInfo("8bae12b5f4c088d940733dcd1455efc6a3a69cf9340e17a981286d3778615684");
            Console.WriteLine(transactionInfo2.Id);
            Console.WriteLine("There are exotic inputs or outputs, so not all of them have been added successfully: "
                              + Environment.NewLine + !transactionInfo2.AllInOutsAdded);
        }

        private static void Part2Lesson2()
        {
            #region SetupSafe

            var network = Network.TestNet;

            string walletFilePath;
            if (network == Network.MainNet)
                walletFilePath = "MainNetHidden.wallet";
            else
                walletFilePath = "TestNetHidden.wallet";

            Safe safe;
            if (File.Exists(walletFilePath))
            {
                safe = Safe.Load("password", walletFilePath);
                if (safe.Network != network)
                    throw new Exception("Wrong network");
            }
            else
            {
                string mnemonic;
                safe = Safe.Create(out mnemonic, "password", walletFilePath, network);
            }

            #endregion

            #region InitializeHttpSafeMonitor

            var safeMonitor = new HttpSafeMonitor(safe, addressCount: 1000);

            Console.WriteLine(safeMonitor.Safe.GetAddress(0));
            Console.WriteLine(safeMonitor.Safe.GetAddress(10));
            Console.WriteLine(safeMonitor.Safe.GetAddress(999));
            Console.WriteLine(safeMonitor.Safe.GetAddress(2));


            // Report initialization progress
            safeMonitor.InitializationStateChanged += delegate(object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor) sender;
                Console.WriteLine($"Initialization state: {monitor.InitializationState}");
            };
            safeMonitor.InitializationProgressPercentChanged += delegate(object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor) sender;
                Console.WriteLine($"Initializing: {monitor.InitializationProgressPercent}%");
            };

            // Let's wait until initialized
            while (safeMonitor.InitializationState != State.Ready)
                Thread.Sleep(100);

            #endregion

            var safeBalanceInfo = safeMonitor.SafeBalanceInfo;
            Console.WriteLine($"Number of monitored addresses: {safeBalanceInfo.MonitoredAddressCount}");
            Console.WriteLine($"Balance: {safeBalanceInfo.Balance}");
            Console.WriteLine($"Confirmed: {safeBalanceInfo.Confirmed}");
            Console.WriteLine($"Unconfirmed: {safeBalanceInfo.Unconfirmed}");
            foreach (var balanceInfo in safeBalanceInfo.AddressBalances)
            {
                if (balanceInfo.Balance != 0)
                    Console.WriteLine($"{balanceInfo.Address}: {balanceInfo.Balance}");
            }

            var history = safeMonitor.SafeHistory;

            Console.WriteLine("totalreceived: " + history.TotalReceived);
            Console.WriteLine("totalspent: " + history.TotalSpent);
            foreach (var record in history.Records)
            {
                Console.WriteLine(record.Address + " " + record.Amount);
            }

            #region ListeningToChanges

            Console.WriteLine($"Balance of safe: {safeMonitor.SafeBalanceInfo.Balance}");
            Console.WriteLine($"Confirmed balance of safe: {safeMonitor.SafeBalanceInfo.Confirmed}");
            Console.WriteLine($"Unconfirmed balance of safe: {safeMonitor.SafeBalanceInfo.Unconfirmed}");
            
            safeMonitor.BalanceChanged += delegate(object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor) sender;

                Console.WriteLine();
                Console.WriteLine("Change happened");
                Console.WriteLine($"Balance of safe: {monitor.SafeBalanceInfo.Balance}");
                Console.WriteLine($"Confirmed balance of safe: {monitor.SafeBalanceInfo.Confirmed}");
                Console.WriteLine($"Unconfirmed balance of safe: {monitor.SafeBalanceInfo.Unconfirmed}");
                Console.WriteLine(
                    $"TransacitonId: {monitor.SafeHistory.Records.OrderBy(x => x.DateTime).Last().TransactionId}");
            };

            #endregion
        }
    }
}