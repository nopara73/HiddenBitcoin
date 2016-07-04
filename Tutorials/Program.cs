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
using HiddenBitcoin.DataClasses.States;
using static System.Console;

namespace Tutorials
{
    internal class Program
    {
        private static void Main()
        {
            //Part1(); // Storing keys
            //Part2Lesson1(); // Monitoring keys using HTTP
            //Part2Lesson2(); // Monitoring safes using HTTP

            //TemporarilyTestHttpSafeMonitor();
            //TemporarilyTestHttpMonitor();
            TemporarilySendTest();

            ReadLine();
        }

        private static void TemporarilyTestHttpMonitor()
        {
            var monitor = new HttpMonitor(Network.TestNet);

            var privateKey =
                "tprv8f3GDAef8nhKvQbC5KGEerqDGB2s2KgMNHS4kLDXXP7c8M6ZcxyEzyvsVA9C1ss9Fj5QEiCiKbyEfD97duhXjc2he58dDicMtLs3YfwNJb2";
            var address = "miLGbZvQ5sEd5BKCPXjEJzknMdENuFEzCy";

            var bal = monitor.GetAddressBalanceInfo(address);
            var his = monitor.GetAddressHistory(address);

            WriteLine($"Balance: {bal.Balance}");
            WriteLine($"Confirmed balance: {bal.Confirmed}");
            WriteLine($"Unconfirmed balance: {bal.Unconfirmed}");
            WriteLine($"TotalReceived: {his.TotalReceived}");
            WriteLine($"TotalSpent: {his.TotalSpent}");
            WriteLine($"TotalReceived - TotalSpent: {his.TotalReceived - his.TotalSpent}");
            WriteLine($"TotalReceived - TotalSpent == Balance: {his.TotalReceived - his.TotalSpent == bal.Balance}");
            WriteLine();
            WriteLine("RECORDS:");

            foreach (var record in his.Records)
            {
                WriteLine();
                WriteLine($"DateTime: {record.DateTime}");
                WriteLine($"Amount: {record.Amount}");
                WriteLine($"Confirmed: {record.Confirmed}");
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

            WriteLine("Safe has been set up");
            #endregion

            #region InitializeHttpSafeMonitor

            var safeMonitor = new HttpSafeMonitor(safe, addressCount: 100);

            // Report initialization progress
            safeMonitor.InitializationStateChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;
                WriteLine($"Initialization state: {monitor.InitializationState}");
            };
            safeMonitor.InitializationProgressPercentChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;
                WriteLine($"Initializing: {monitor.InitializationProgressPercent}%");
            };

            // Let's wait until initialized
            while (safeMonitor.InitializationState != State.Ready)
                Thread.Sleep(100);

            WriteLine("SafeMonitor is ready to work with");
            #endregion
            
            var bal = safeMonitor.SafeBalanceInfo;
            var his = safeMonitor.SafeHistory;

            WriteLine();
            WriteLine($"Balance: {bal.Balance}");
            WriteLine($"Confirmed balance: {bal.Confirmed}");
            WriteLine($"Unconfirmed balance: {bal.Unconfirmed}");
            WriteLine($"TotalReceived: {his.TotalReceived}");
            WriteLine($"TotalSpent: {his.TotalSpent}");
            WriteLine($"TotalReceived - TotalSpent: {his.TotalReceived - his.TotalSpent}");
            WriteLine($"TotalReceived - TotalSpent == Balance: {his.TotalReceived - his.TotalSpent == bal.Balance}");
            WriteLine();
            WriteLine("RECORDS:");

            foreach (var record in his.Records)
            {
                WriteLine();
                WriteLine($"DateTime: {record.DateTime}");
                WriteLine($"Amount: {record.Amount}");
                WriteLine($"Confirmed: {record.Confirmed}");
            }

            safeMonitor.BalanceChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;

                WriteLine();
                WriteLine("Change happened");
                WriteLine($"Balance: {monitor.SafeBalanceInfo.Balance}");
                WriteLine($"Confirmed balance: {monitor.SafeBalanceInfo.Confirmed}");
                WriteLine($"Unconfirmed balance: {monitor.SafeBalanceInfo.Unconfirmed}");
                WriteLine($"TotalReceived: {monitor.SafeHistory.TotalReceived}");
                WriteLine($"TotalSpent: {monitor.SafeHistory.TotalSpent}");
                WriteLine($"TotalReceived - TotalSpent: {monitor.SafeHistory.TotalReceived - monitor.SafeHistory.TotalSpent}");
                WriteLine($"TotalReceived - TotalSpent == Balance: {monitor.SafeHistory.TotalReceived - monitor.SafeHistory.TotalSpent == monitor.SafeBalanceInfo.Balance}");
                WriteLine();
                WriteLine("Last record:");
                var record = monitor.SafeHistory.Records.First();
                WriteLine($"DateTime: {record.DateTime}");
                WriteLine($"Amount: {record.Amount}");
                WriteLine($"Confirmed: {record.Confirmed}");
            };
            WriteLine();
            WriteLine("Subscribed to changes");

            //var spender = new HttpSafeSender(safeMonitor.Safe);
            //var tx = spender.CreateTransaction(
            //    new List<AddressAmountPair>
            //    {
            //        new AddressAmountPair
            //        {
            //            Address = safeMonitor.Safe.GetAddress(99), // internal address
            //            //Address = "n2eMqTT929pb1RDNuqEnxdaLau1rxy3efi", // outer address
            //            Amount = 1
            //        }
            //    });
            //Console.WriteLine();
            //Console.WriteLine("Transaction created");
            //spender.Send(tx.Id);
            //Console.WriteLine("Transaction sent");
        }

        private static void TemporarilySendTest()
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

            WriteLine("Safe has been set up");
            #endregion

            #region InitializeHttpSafeMonitor

            var safeMonitor = new HttpSafeMonitor(safe, addressCount: 100);

            // Report initialization progress
            safeMonitor.InitializationStateChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;
                WriteLine($"Initialization state: {monitor.InitializationState}");
            };
            safeMonitor.InitializationProgressPercentChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;
                WriteLine($"Initializing: {monitor.InitializationProgressPercent}%");
            };

            // Let's wait until initialized
            while (safeMonitor.InitializationState != State.Ready)
                Thread.Sleep(100);

            WriteLine("SafeMonitor is ready to work with");
            #endregion

            #region FeedBack
            var bal = safeMonitor.SafeBalanceInfo;
            var his = safeMonitor.SafeHistory;

            WriteLine();
            WriteLine($"Balance: {bal.Balance}");
            WriteLine($"Confirmed balance: {bal.Confirmed}");
            WriteLine($"Unconfirmed balance: {bal.Unconfirmed}");
            WriteLine($"TotalReceived: {his.TotalReceived}");
            WriteLine($"TotalSpent: {his.TotalSpent}");
            WriteLine($"TotalReceived - TotalSpent: {his.TotalReceived - his.TotalSpent}");
            WriteLine($"TotalReceived - TotalSpent == Balance: {his.TotalReceived - his.TotalSpent == bal.Balance}");
            WriteLine();
            WriteLine("RECORDS:");

            foreach (var record in his.Records)
            {
                WriteLine();
                WriteLine($"DateTime: {record.DateTime}");
                WriteLine($"Amount: {record.Amount}");
                WriteLine($"Confirmed: {record.Confirmed}");
            }

            safeMonitor.BalanceChanged += delegate (object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor)sender;

                WriteLine();
                WriteLine("Change happened");
                WriteLine($"Balance: {monitor.SafeBalanceInfo.Balance}");
                WriteLine($"Confirmed balance: {monitor.SafeBalanceInfo.Confirmed}");
                WriteLine($"Unconfirmed balance: {monitor.SafeBalanceInfo.Unconfirmed}");
                WriteLine($"TotalReceived: {monitor.SafeHistory.TotalReceived}");
                WriteLine($"TotalSpent: {monitor.SafeHistory.TotalSpent}");
                WriteLine($"TotalReceived - TotalSpent: {monitor.SafeHistory.TotalReceived - monitor.SafeHistory.TotalSpent}");
                WriteLine($"TotalReceived - TotalSpent == Balance: {monitor.SafeHistory.TotalReceived - monitor.SafeHistory.TotalSpent == monitor.SafeBalanceInfo.Balance}");
                WriteLine();
                WriteLine("Last record:");
                var record = monitor.SafeHistory.Records.First();
                WriteLine($"DateTime: {record.DateTime}");
                WriteLine($"Amount: {record.Amount}");
                WriteLine($"Confirmed: {record.Confirmed}");
            };
            WriteLine();
            WriteLine("Subscribed to changes");

            #endregion

            WriteLine(safeMonitor.Safe.GetAddress(96));
            var spender = new HttpSafeSender(safeMonitor.Safe);
            //var tx = spender.CreateTransaction(
            //    new List<AddressAmountPair>
            //    {
            //        new AddressAmountPair
            //        {
            //            Address = safeMonitor.Safe.GetAddress(99), // internal address
            //            //Address = "n2eMqTT929pb1RDNuqEnxdaLau1rxy3efi", // outer address
            //            Amount = 1
            //        }
            //    });
            //var tx = spender.CreateSpendAllTransaction(safeMonitor.Safe.GetAddress(97));
            //Console.WriteLine();
            //Console.WriteLine("Transaction created");
            //spender.Send(tx.Id);
            //Console.WriteLine("Transaction sent");
            //Thread.Sleep(1000);
            //var tx2 = spender.CreateSpendAllTransaction(safeMonitor.Safe.GetAddress(96));
            //spender.Send(tx2.Id);
            //Console.WriteLine("Transaction sent");
            //Thread.Sleep(1000);
            //var tx3 = spender.CreateSpendAllTransaction(safeMonitor.Safe.GetAddress(95));
            //spender.Send(tx3.Id);
            //Console.WriteLine("Transaction sent");
            //Thread.Sleep(1000);
            //var tx4 = spender.CreateSpendAllTransaction(safeMonitor.Safe.GetAddress(94));
            //spender.Send(tx4.Id);
            //Console.WriteLine("All transaction sent");
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
                WriteLine(mnemonic);

                // Let's recover the safe to an other file
                var recoveredSafe = Safe.Recover(mnemonic, password, recoveredWalletFilePath, network);

                // You can also load an existing safe from file with your password
                var loadedSafe = Safe.Load(password, walletFilePath);

                // After we load a safe it's not a bad idea to check if it is on the expected network
                if (network != loadedSafe.Network)
                    throw new Exception("WrongNetwork");

                // Let's write out a few things
                // The seed private key
                WriteLine(loadedSafe.Seed);
                // You can generate addresses with the public key, but you cannot spend them
                WriteLine(loadedSafe.SeedPublicKey);

                // The third child address
                WriteLine(loadedSafe.GetAddress(2));
                // The first child private key
                WriteLine(loadedSafe.GetPrivateKey(0));

                // The first ten privkey address pair
                for (var i = 0; i < 10; i++)
                {
                    var privateKeyAddressPair = loadedSafe.GetPrivateKeyAddressPair(i);
                    WriteLine(privateKeyAddressPair.Address);
                    WriteLine(privateKeyAddressPair.PrivateKey);
                }

                // List out the first 10 address
                for (var i = 0; i < 10; i++)
                {
                    WriteLine(safe.GetAddress(i));
                }

                // You can get the dark wallet type stealth address of the safe
                WriteLine(loadedSafe.StealthAddress);
                // Also the scan and spendkey
                WriteLine(loadedSafe.StealthScanPrivateKey);
                WriteLine(loadedSafe.StealthSpendPrivateKey);

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
            WriteLine($"Address balance: {balanceInfo.Balance}"); // 0,05474889
            WriteLine($"Confirmed balance: {balanceInfo.Confirmed}"); // 0
            WriteLine($"Unconfirmed balance: {balanceInfo.Unconfirmed}"); // 0,05474889

            // Get history of an address
            var history = httpMonitor.GetAddressHistory("1ENCTCkqoJqy2XZ2m2Dy1bRax7hsSnC5Fc");

            WriteLine("Number of transactions: " + history.Records.Count);

            // Exercise: are all transaction confirmed?
            var allTransactionsConfirmed = true;
            foreach (var record in history.Records)
            {
                WriteLine(record.TransactionId + " : " + record.Amount);
                allTransactionsConfirmed = allTransactionsConfirmed && record.Confirmed;
            }
            WriteLine("All transactions are confirmed: " + allTransactionsConfirmed);

            // Exercise: get the balance of the address
            WriteLine("Total received - Total spent = Balance");
            WriteLine(history.TotalReceived + " - " + history.TotalSpent + " = " +
                              (history.TotalReceived - history.TotalSpent));

            // Get some data from the transaction
            var transactionInfo1 = httpMonitor.GetTransactionInfo(history.Records.First().TransactionId);

            WriteLine("txid: " + transactionInfo1.Id);
            WriteLine("Network: " + transactionInfo1.Network);
            WriteLine("Confirmed: " + transactionInfo1.Confirmed);
            WriteLine("Total amount of all inputs: " + transactionInfo1.TotalInputAmount);
            WriteLine("Total amount of all outputs: " + transactionInfo1.TotalOutputAmount);
            WriteLine("Fee : " + transactionInfo1.Fee);

            WriteLine(Environment.NewLine + "Input addresses and amounts: ");
            foreach (var input in transactionInfo1.Inputs)
            {
                WriteLine(input.Amount + " " + input.Address);
            }
            WriteLine(Environment.NewLine + "Output addresses and amounts: ");
            foreach (var output in transactionInfo1.Outputs)
            {
                WriteLine(output.Amount + " " + output.Address);
            }

            // Sometimes my API can't fully process a transaction, because it has OP_RETURN for example
            // It should not be a concern for a Bitcoin wallet that purely handles money, if a transaction output or input has not been added
            // that means it has some other purpose, a wallet API can dismiss it
            // This tx is exotic (has OP_RETURN)
            var transactionInfo2 =
                httpMonitor.GetTransactionInfo("8bae12b5f4c088d940733dcd1455efc6a3a69cf9340e17a981286d3778615684");
            WriteLine(transactionInfo2.Id);
            WriteLine("There are exotic inputs or outputs, so not all of them have been added successfully: "
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

            var safeMonitor = new HttpSafeMonitor(safe, addressCount: 100);


            // Report initialization progress
            safeMonitor.InitializationStateChanged += delegate(object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor) sender;
                WriteLine($"Initialization state: {monitor.InitializationState}");
            };
            safeMonitor.InitializationProgressPercentChanged += delegate(object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor) sender;
                WriteLine($"Initializing: {monitor.InitializationProgressPercent}%");
            };

            // Let's wait until initialized
            while (safeMonitor.InitializationState != State.Ready)
                Thread.Sleep(100);

            #endregion

            var safeBalanceInfo = safeMonitor.SafeBalanceInfo;
            WriteLine($"Number of monitored addresses: {safeBalanceInfo.MonitoredAddressCount}");
            WriteLine($"Balance: {safeBalanceInfo.Balance}");
            WriteLine($"Confirmed: {safeBalanceInfo.Confirmed}");
            WriteLine($"Unconfirmed: {safeBalanceInfo.Unconfirmed}");
            foreach (var balanceInfo in safeBalanceInfo.AddressBalances)
            {
                if (balanceInfo.Balance != 0)
                    WriteLine($"{balanceInfo.Address}: {balanceInfo.Balance}");
            }

            var history = safeMonitor.SafeHistory;

            WriteLine("totalreceived: " + history.TotalReceived);
            WriteLine("totalspent: " + history.TotalSpent);
            foreach (var record in history.Records)
            {
                WriteLine(record.Address + " " + record.Amount);
            }

            #region ListeningToChanges

            WriteLine($"Balance of safe: {safeMonitor.SafeBalanceInfo.Balance}");
            WriteLine($"Confirmed balance of safe: {safeMonitor.SafeBalanceInfo.Confirmed}");
            WriteLine($"Unconfirmed balance of safe: {safeMonitor.SafeBalanceInfo.Unconfirmed}");
            
            safeMonitor.BalanceChanged += delegate(object sender, EventArgs args)
            {
                var monitor = (HttpSafeMonitor) sender;

                WriteLine();
                WriteLine("Change happened");
                WriteLine($"Balance of safe: {monitor.SafeBalanceInfo.Balance}");
                WriteLine($"Confirmed balance of safe: {monitor.SafeBalanceInfo.Confirmed}");
                WriteLine($"Unconfirmed balance of safe: {monitor.SafeBalanceInfo.Unconfirmed}");
                WriteLine(
                    $"TransacitonId: {monitor.SafeHistory.Records.OrderBy(x => x.DateTime).Last().TransactionId}");
            };

            #endregion
        }
    }
}