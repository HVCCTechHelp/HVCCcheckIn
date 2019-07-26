namespace HVCC.Shell.Common.Resources
{
    public class Enumerations
    {
        public enum TransactionType
        {
            None,
            Invoice,
            Payment
        };
        public enum TransactionState
        {
            New,        // The Transaction is new; does not exist in the database
            Edit,       // The transaction exists and is being edited
            EditSkip,   // The transaction may be new or existing, it has been edited once already but not committed, and is now changing a subsequent time.
            Delete,     // The transaction is being deleted from the database
            PendingNew, // The transaction has been changed and is awaiting commitment (save)
            PendingEdit
        };

        public enum SortOrder
        {
            Assending,
            Decending
        };

        public enum RelationshipActions
        {
            error,
            Deactivate,
            Delete,
            Remove
        };
    }
}
