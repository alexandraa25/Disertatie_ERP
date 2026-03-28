namespace ERPSystem.Utils.Enums
{
    public enum AdditionalActStatus
    {
        Draft,          // creat, editabil
        Finalized,      // needitabil
        SentToClient,   // trimis la semnat
        SignedByClient ,
        Active,    
        Applied,// semnat de ambele părți
        Cancelled,       // anulat manual
        Completed,
        Expired,
        Suspended
    }
}
