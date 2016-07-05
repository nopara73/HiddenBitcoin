using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NBitcoin;
using NBitcoin.Protocol;
using QBitNinja.Client;

namespace HiddenBitcoin.DataClasses.Sending
{
    public static class Sender
    {
        internal static List<Transaction> BuiltTransactions = new List<Transaction>();

        public static void Send(ConnectionType connectionType, TransactionInfo transactionInfo)
        {
            if (connectionType == ConnectionType.Http)
            {
                var client = new QBitNinjaClient(Convert.ToNBitcoinNetwork(transactionInfo.Network));
                var transaction = FindTransaction(transactionInfo);

                var broadcastResponse = client.Broadcast(transaction).Result;
                if (!broadcastResponse.Success)
                    throw new Exception($"ErrorCode: {broadcastResponse.Error.ErrorCode}" + Environment.NewLine
                                        + broadcastResponse.Error.Reason);
            }
            if (connectionType == ConnectionType.RandomNode)
            {
                var parameters = new NodeConnectionParameters();
                var group = new NodesGroup(Convert.ToNBitcoinNetwork(transactionInfo.Network), parameters, new NodeRequirement
                {
                    RequiredServices = NodeServices.Nothing
                })
                { MaximumNodeConnection = 1 };
                group.Connect();

                while (group.ConnectedNodes.Count == 0)
                {
                    Thread.Sleep(100);
                }
                var transaction = FindTransaction(transactionInfo);
                var payload = new TxPayload(transaction);
                group.ConnectedNodes.First().SendMessage(payload);

                var txids = group.ConnectedNodes.First().GetMempool();
                if(!txids.Contains(transaction.GetHash()))
                    throw new Exception("Transaction has not been broadcasted, try again!");
            }
        }

        private static Transaction FindTransaction(TransactionInfo transactionInfo)
        {
            var tx = BuiltTransactions.FirstOrDefault(transaction => transaction.GetHash() == new uint256(transactionInfo.Id));
            if (tx != null) return tx;
            throw new Exception("Transaction has not been created");
        }
    }
}