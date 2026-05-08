namespace ERPSystem.Utils.Enums
{
    public enum AdditionalActStatus
    {
        Draft,            // editabil
        Finalized,        // blocat
        SentToClient,     // trimis la semnat
        SignedByClient,   // clientul a semnat
        Applied,          // semnat admin + efecte aplicate
        Cancelled,        // anulat
        Expired           // nu a mai fost aplicat la timp
    }
}
