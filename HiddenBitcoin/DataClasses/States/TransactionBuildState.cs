namespace HiddenBitcoin.DataClasses.States
{
    public enum TransactionBuildState
    {
        NotInProgress,
        GatheringCoinsToSpend,
        BuildingTransaction,
        CheckingTransaction
    }
}