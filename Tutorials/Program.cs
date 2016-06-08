// ReSharper disable All

using System;
using System.ComponentModel;
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
            //Part2(); // Monitoring keys using HTTP
            Part3(); //  Monitoring key using SPV

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

            var httpMonitor = new HttpMonitor(network);

            var balanceInfo = httpMonitor.GetBalance("1ENCTCkqoJqy2XZ2m2Dy1bRax7hsSnC5Fc");
            Console.WriteLine(balanceInfo.Confirmed);
            Console.WriteLine(balanceInfo.Unconfirmed);


            //tx is exotic (has OP_RETURN)
            var transactionInfo =
                httpMonitor.GetTransactionInfo("8bae12b5f4c088d940733dcd1455efc6a3a69cf9340e17a981286d3778615684");
            //tx is normal
            //var transactionInfo = httpMonitor.GetTransactionInfo("8bbd7678d93da5da8736a84a69a1de83834bea732c65342687e8db549f153504");

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
                httpMonitor.GetAddressHistory(address);

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

            balanceInfo = httpMonitor.GetBalance(address);
            Console.WriteLine(@"httpMonitor.GetBalance(address): " +
                              (balanceInfo.Confirmed + balanceInfo.Unconfirmed));
        }

        private static void Part3()
        {
            var spvMonitor = new SpvMonitor(Network.MainNet);
            var addressToTrack = "1ENCTCkqoJqy2XZ2m2Dy1bRax7hsSnC5Fc";
            var transactionToTrack = "8bae12b5f4c088d940733dcd1455efc6a3a69cf9340e17a981286d3778615684";
            try
            {

                #region EstabilishConnectionAndSync

                spvMonitor.StartConnecting();
                
                // Report connection progress
                spvMonitor.ConnectionStateChanged += delegate(object sender, EventArgs args)
                {
                    var monitor = (SpvMonitor) sender;
                    Console.WriteLine($"Connection state: {monitor.ConnectionState}");
                };
                spvMonitor.ConnectionProgressPercentChanged += delegate (object sender, EventArgs args)
                {
                    var monitor = (SpvMonitor)sender;
                    Console.WriteLine($"Connecting: {monitor.ConnectionProgressPercent}%");
                };
                
                // Report syncronization progress
                spvMonitor.SyncStateChanged += delegate (object sender, EventArgs args)
                {
                    var monitor = (SpvMonitor)sender;
                    Console.WriteLine($"Sync state: {monitor.SyncState}");
                };
                spvMonitor.SyncProgressPercentChanged += delegate (object sender, EventArgs args)
                {
                    var monitor = (SpvMonitor)sender;
                    Console.WriteLine($"Syncing: {monitor.SyncProgressPercent}%");
                };

                // Let's wait until connected and synced
                while(spvMonitor.ConnectionState != State.Ready)
                    Thread.Sleep(100);
                while (spvMonitor.SyncState != State.Ready)
                    Thread.Sleep(100);
                #endregion

                //// You can ask for random info exactly the same way as you do with HttpMonitor
                //#region AskInfo
                
                //var balanceInfo = spvMonitor.GetBalance("1ENCTCkqoJqy2XZ2m2Dy1bRax7hsSnC5Fc");
                //Console.WriteLine(balanceInfo.Confirmed);
                //Console.WriteLine(balanceInfo.Unconfirmed);
                
                ////tx is exotic (has OP_RETURN)
                //var transactionInfo =
                //    spvMonitor.GetTransactionInfo("8bae12b5f4c088d940733dcd1455efc6a3a69cf9340e17a981286d3778615684");
                ////tx is normal
                ////var transactionInfo = httpMonitor.GetTransactionInfo("8bbd7678d93da5da8736a84a69a1de83834bea732c65342687e8db549f153504");

                //Console.WriteLine("txid: " + transactionInfo.Id);
                //Console.WriteLine("Network: " + transactionInfo.Network);
                //Console.WriteLine("Confirmed: " + transactionInfo.Confirmed);
                //Console.WriteLine("Total amount of all inputs: " + transactionInfo.TotalInputAmount);
                //Console.WriteLine("Total amount of all outputs: " + transactionInfo.TotalOutputAmount);
                //Console.WriteLine("Fee : " + transactionInfo.Fee);
                //Console.WriteLine("There are no exotic inputs or outputs, so all of them have been added successfully: "
                //                  + Environment.NewLine + transactionInfo.AllInOutsAdded);

                //Console.WriteLine(Environment.NewLine + "Input addresses and amounts: ");
                //foreach (var input in transactionInfo.Inputs)
                //{
                //    Console.WriteLine(input.Amount + " " + input.Address);
                //}
                //Console.WriteLine(Environment.NewLine + "Output addresses and amounts: ");
                //foreach (var output in transactionInfo.Outputs)
                //{
                //    Console.WriteLine(output.Amount + " " + output.Address);
                //}

                ////lot of transactions:
                ////var address = "13eh4wPLe1nCsh8FXJNpL6e9D1edWNT1Ub";
                ////only few transactions:
                //var address = "19V1JJ68Ee57tKnG7NikH4tU93xqMShCyD";
                //var history =
                //    spvMonitor.GetAddressHistory(address);

                //Console.WriteLine("Number of transactions: " + history.Records.Count);

                //var allTransactionsConfirmed = true;
                //foreach (var record in history.Records)
                //{
                //    Console.WriteLine("txid: " + record.TransactionId);
                //    allTransactionsConfirmed = allTransactionsConfirmed && record.Confirmed;
                //}
                //Console.WriteLine("All transactions are confirmed: " + allTransactionsConfirmed);

                //Console.WriteLine("Total received - Total spent = Balance");
                //Console.WriteLine(history.TotalReceived + " - " + history.TotalSpent + " = " +
                //                  (history.TotalReceived - history.TotalSpent));

                //balanceInfo = spvMonitor.GetBalance(address);
                //Console.WriteLine(@"spvMonitor.GetBalance(address): " +
                //                  (balanceInfo.Confirmed + balanceInfo.Unconfirmed));
                //#endregion

                // With SPV, you can also listen for changes
                //#region TrackChanges

                //spvMonitor.StartTrackingAddress(addressToTrack);
                //spvMonitor.StartTrackingTransaction(transactionToTrack);

                //spvMonitor.TrackedAddressBalanceChanged += delegate (object sender, EventArgs args)
                //{
                //    Console.WriteLine($"Balance changed for {args.Address}");

                //    BalanceInfo oldBalanceInfo = args.OldBalanceInfo;
                //    BalanceInfo newBalanceInfo = args.NewBalanceInfo;

                //    Console.WriteLine($"Unconfirmed balance change: {args.UnconfirmedChange}");
                //    Console.WriteLine($"Confirmed balance change: {args.ConfirmedChange}");

                //    Console.WriteLine($"OVERALL BALANCE CHANGE: {args.BalanceChange}");
                //};
                //spvMonitor.TrackedTransactionConfirmed += delegate (object sender, EventArgs args)
                //{
                //    Console.WriteLine($"Transaction has just confirmed, txid: {args.TransactionId}");
                //    TransactionInfo txinfo = args.TransactionInfo;
                    
                //    Console.WriteLine($"Total value exchanged in transaciton: {txinfo.TotalInputAmount}");
                //};

                ////spvMonitor.StopTrackingAddress(addressToTrack);
                ////spvMonitor.StopTrackingTransaction(transactionToTrack);

                //#endregion

            }
            finally
            {
                #region Disconnect
                spvMonitor.Disconnect();
                #endregion
            }
        }
    }
}