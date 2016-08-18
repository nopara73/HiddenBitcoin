using System;
using System.Collections.Generic;
using System.Threading;
using HiddenBitcoin.DataClasses;
using HiddenBitcoin.DataClasses.KeyManagement;
using HiddenBitcoin.DataClasses.Monitoring;
using HiddenBitcoin.DataClasses.Sending;
using HiddenBitcoin.DataClasses.States;
using Xunit;

namespace Tests
{
    public class BuilderTests
    {
        [Theory]
        [InlineData(ConnectionType.RandomNode)]
        [InlineData(ConnectionType.Http)]
        public void HttpSafeBuilderBuildSpendAllTransactionWorks(ConnectionType propagationConnectionType)
        {
            Safe safe = Safe.Recover("cabbage drive wrestle fury goddess click riot mercy shy size error short", "",
                "foo", Network.TestNet);
            safe.DeleteWalletFile();
            var monitor = new HttpSafeMonitor(safe, 100);
            while (monitor.InitializationState != State.Ready)
                Thread.Sleep(100);
            Assert.True(monitor.GetSafeBalanceInfo().Balance > 0);

            var httpSafeBuilder = new HttpSafeBuilder(monitor.Safe);
            const string toAddress = "n16Yt8jpDf34nMbdJyZh1iqZkZFtAoLMbW";
            const string toPrivateKey = "tprv8fAkYGDbNMNLLSWvmXshBiMQuEuEjt5ZMxiX1CNrV5hQCQLFqmpt3urpzrvgJHCwD7bJYMrSMcy8UPa4P3KnB84u2t4rZL874E79MRon4bU";

            var tx1 = httpSafeBuilder.BuildSpendAllTransaction(toAddress);
            Sender.Send(propagationConnectionType, tx1, tryTimes: 3);
            Assert.True(monitor.GetSafeBalanceInfo().Balance == 0);

            var httpBuilder = new HttpBuilder(monitor.Network);
            var tx2 = httpBuilder.BuildSpendAllTransaction(
                new List<string>
                {
                    toPrivateKey
                },
                monitor.Safe.GetAddress(0));
            Sender.Send(propagationConnectionType, tx2, tryTimes: 3);
            Assert.True(monitor.GetSafeBalanceInfo().Balance > 0);
        }
    }
}
