namespace HiddenBitcoin.DataClasses.States
{
    public enum TransactionCreationState
    {
        NotInProgress,
        GatheringCoinsToSpend,
        BuildingTransaction,
        CheckingTransaction
    }
}
