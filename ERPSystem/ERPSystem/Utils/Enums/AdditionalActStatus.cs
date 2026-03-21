namespace ERPSystem.Utils.Enums
{
    public enum AdditionalActStatus
    {
        Draft,          // creat, editabil
        Finalized,      // needitabil
        PendingSign,    // trimis la semnat
        Signed,         // semnat de ambele părți
        Approved,       // aplicat în contract
        Rejected,       // respins
        Cancelled       // anulat manual
    }
}
