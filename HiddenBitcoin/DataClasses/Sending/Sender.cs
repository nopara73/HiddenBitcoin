using System;
using System.Collections.Generic;
using NBitcoin;
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
                foreach (var transaction in BuiltTransactions)
                {
                    if (transaction.GetHash() != new uint256(transactionInfo.Id)) continue;
                    var broadcastResponse = client.Broadcast(transaction).Result;

                    if (!broadcastResponse.Success)
                        throw new Exception($"ErrorCode: {broadcastResponse.Error.ErrorCode}" + Environment.NewLine
                                            + broadcastResponse.Error.Reason);
                    return;
                }
                throw new Exception("Transaction has not been created");
            }
            if (connectionType == ConnectionType.Spv)
            {
                throw new NotImplementedException();
            }
        }
    }
}