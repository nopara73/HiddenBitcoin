using System.Linq;
using System.Threading;
using HiddenBitcoin.DataClasses;
using HiddenBitcoin.DataClasses.KeyManagement;
using HiddenBitcoin.DataClasses.Monitoring;
using HiddenBitcoin.DataClasses.States;
using Xunit;

namespace Tests
{
    public class MonitorTests
    {
        [Fact]
        public void HttpMonitorWorks()
        {
            var monitor = new HttpMonitor(Network.MainNet);
            var history = monitor.GetAddressHistory("15emqTTjzCVRFWTRyBiQUgBnK3DBhShFBU");

            var recordCount = history.Records.Count;
            Assert.True(recordCount > 16);
            Assert.Equal(history.Records.Last().Amount, 0.03802378m);
            Assert.Equal(history.Records[recordCount - 2].TransactionId,
                "1798a59c37828db66e8c4d57a90324fa8039aef72b872df5876edfb31b102c5f");
            Assert.Equal(history.Records[recordCount - 3].Amount, -0.06335627m);
            Assert.True(history.Records[recordCount - 3].Confirmed);

            string mnemonic;
            var safe = Safe.Create(out mnemonic, "", "foo", Network.MainNet);
            safe.DeleteWalletFile();
            var history2 = monitor.GetAddressHistory(safe.GetAddress(2));
            Assert.True(history2.Records.Count == 0);

            var balanceInfo = monitor.GetAddressBalanceInfo("15emqTTjzCVRFWTRyBiQUgBnK3DBhShFBU");
            var balanceInfo2 = monitor.GetAddressBalanceInfo(safe.GetAddress(2));

            Assert.Equal(balanceInfo.Balance, history.TotalReceived - history.TotalSpent);
            Assert.Equal(balanceInfo2.Balance, history2.TotalReceived - history2.TotalSpent);

            var confirmedBalance = history.Records.Where(record => record.Confirmed).Sum(record => record.Amount);
            var unconfirmedBalance = history.Records.Where(record => !record.Confirmed).Sum(record => record.Amount);

            Assert.Equal(balanceInfo.Unconfirmed, unconfirmedBalance);
            Assert.Equal(balanceInfo.Confirmed, confirmedBalance);

            
        }

        [Fact]
        public void GetTransactionInfoWorks()
        {
            var monitor = new HttpMonitor(Network.MainNet);
            var transactionInfo = monitor.GetTransactionInfo("2d0a108be81fe0d807a0cdd65233158bac1642081033c21b379d776f278015a4");

            Assert.True(transactionInfo.AllInOutsAdded);
            Assert.True(transactionInfo.Confirmed);
            Assert.Equal(transactionInfo.Network, Network.MainNet);
            Assert.True(transactionInfo.Fee > 0);
            Assert.True(transactionInfo.Inputs.Count > 10);
            Assert.Equal(transactionInfo.TotalOutputAmount, 15m);
            Assert.Equal(transactionInfo.Outputs.First().Address, "1EDt6Pe5psPLrAKmq7xawCFo9LxtKoJz7g");
        }

        [Fact]
        public void EmptySafeHttpMonitorWorks()
        {
            string mnemonic;
            var safe = Safe.Create(out mnemonic, "", "foo", Network.MainNet);
            safe.DeleteWalletFile();

            var monitor = new HttpSafeMonitor(safe, 10);

            while (monitor.InitializationProgressPercent != 100)
            {
                Thread.Sleep(100);
            }

            Assert.Equal(monitor.AddressCount, 10);
            Assert.Equal(monitor.InitializationState, State.Ready);

            Assert.Equal(monitor.Safe.NotEmptyAddresses.Count, 0);
            Assert.Equal(monitor.Safe.UnusedAddresses.Count, 10);
            Assert.Equal(monitor.Safe.AddressCount, 10);
            Assert.Equal(monitor.Safe.Addresses.Count, 10);

            Assert.Equal(monitor.SafeHistory.Records.Count, 0);
            Assert.Equal(monitor.SafeHistory.TotalReceived, 0);
            Assert.Equal(monitor.SafeHistory.TotalSpent, 0);
            Assert.Equal(monitor.SafeBalanceInfo.MonitoredAddressCount, 10);
            Assert.Equal(monitor.SafeBalanceInfo.Balance, 0);
            Assert.Equal(monitor.SafeBalanceInfo.Confirmed, 0);
            Assert.Equal(monitor.SafeBalanceInfo.Unconfirmed, 0);
            foreach (var balance in monitor.SafeBalanceInfo.AddressBalances)
            {
                Assert.Equal(balance.Balance, 0);
            }
        }
    }
}
