namespace HVCC.Shell.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DevExpress.Xpf.LayoutControl;
    using DevExpress.Xpf.Docking;
    using System.Windows;
    using System.IO;
    using System.Windows.Media.Imaging;
    using System.Drawing;
    using HVCC.Shell.Common;
    using HVCC.Shell.Models;
    using HVCC.Shell.Common.Interfaces;
    using System.Data.Linq;
    using System.Collections.ObjectModel;
    using HVCC.Shell.Validation;

    public class Helper
    {
        public static Binary LoadDefalutImage(IDataContext datacontext)
        {
            HVCCDataContext dc = datacontext as HVCCDataContext;

            ApplicationDefault defaults = (from p in dc.ApplicationDefaults
                                           select p).FirstOrDefault();

            return defaults.Photo;
        }

        public static Dictionary<int, int> PopulateDates()
        {
            Dictionary<int, int> daysInMonth = new Dictionary<int, int>();
            daysInMonth.Add(1, 31);
            daysInMonth.Add(2, 28);
            daysInMonth.Add(3, 31);
            daysInMonth.Add(4, 30);
            daysInMonth.Add(5, 31);
            daysInMonth.Add(6, 30);
            daysInMonth.Add(7, 31);
            daysInMonth.Add(8, 31);
            daysInMonth.Add(9, 30);
            daysInMonth.Add(10, 31);
            daysInMonth.Add(11, 30);
            daysInMonth.Add(12, 31);
            return daysInMonth;
        }

        /// <summary>
        /// Extracts the Section-Block-Lot-SubLot information from a Property.Customer string
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public static Property ConvertCustomerToProperty(string customer)
        {
            Property propRecord = new Property();
            string custFormat = "xx-xx-xxx";
            bool hasChar = false;
            string[] fields = customer.Split('-');

            //// Check for alpha-characters in the Lot field
            if (fields.Count() == 3)
            {
                foreach (char value in fields[2])
                {
                    bool isDigit = char.IsDigit(value);
                    if (!isDigit)
                    {
                        hasChar = true;
                        break;
                    }
                }
            }

            //// Special Case:  The 'customer' string in QuickBooks is wonky. Most likely it has a sub-lot with a space
            ////                in place of a dash  (ex: xx-xx-xxxx x)
            if (customer.Length > custFormat.Length && fields.Count() == 3)
            {
                string[] subFields = fields[2].Split(' ');
                propRecord.Section = Int32.Parse(fields[0]);
                propRecord.Block = Int32.Parse(fields[1]);
                propRecord.Lot = Int32.Parse(subFields[0]);
                propRecord.SubLot = subFields[1];
            }
            //// Special Case:  The string is in the format "xx-xx-xxx-xx"
            else if (customer.Length > custFormat.Length && fields.Count() == 4)
            {
                propRecord.Section = Int32.Parse(fields[0]);
                propRecord.Block = Int32.Parse(fields[1]);
                propRecord.Lot = Int32.Parse(fields[2]);
                propRecord.SubLot = fields[3];
            }
            // Special Case: The 'customer' string is unusual.... "06- Trac C"
            else if (fields.Count() == 2)
            {
                propRecord.Section = Int32.Parse(fields[0]);
                propRecord.Block = 0;
                propRecord.Lot = 0;
                propRecord.SubLot = fields[1];
            }
            // Special Case: The string is in the format: xx-xx-xxA
            else if (hasChar)
            {
                propRecord.Section = Int32.Parse(fields[0]);
                propRecord.Block = Int32.Parse(fields[1]);
                propRecord.Lot = Int32.Parse(fields[2].Substring(0, 2));
                propRecord.SubLot = fields[2][2].ToString();
            }
            //// Normal Case:  string is in the format "xx-xx-xxx"
            else
            {
                propRecord.Section = Int32.Parse(fields[0]);
                propRecord.Block = Int32.Parse(fields[1]);
                propRecord.Lot = Int32.Parse(fields[2]);
                propRecord.SubLot = "0";
            }

            return propRecord;
        }

        /// <summary>
        /// Adds a relationship for the selected property
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns></returns>
        public static bool AddRelationship(IDataContext datacontext, Owner owner, Relationship relationship)
        {
            HVCCDataContext dc = datacontext as HVCCDataContext;

            try
            {
                /// Before we add this new relationship, check to see if the owner is registered to 
                /// mutiple Owner records under different 'MailTo' names.
                /// 
                IEnumerable<Owner_X_Relationship> oXr = (from r in dc.Owner_X_Relationships
                                                         where r.OwnerID == owner.OwnerID
                                                         select r);
                foreach (Owner_X_Relationship x in oXr)
                {
                    IEnumerable<Owner_X_Relationship> associations = (from p in dc.Owner_X_Relationships
                                                                      where p.RelationshipID == x.RelationshipID
                                                                      select p);
                    if (associations.Count() > 1)
                    {
                        relationship.HasMutipleAssociations = true;
                        throw new RelationshipAddException();
                    }
                }

                /// Check to see if this Relationship is in the database (has a non-zero ID), 
                /// or is pending insertion (has a zero ID).  We only want to process new
                /// relationships.
                if (0 == relationship.RelationshipID)
                {
                    /// Add the default HVCC image to the relationship record.  
                    relationship.Photo = LoadDefalutImage(dc);
                    relationship.Active = true;
                    Owner_X_Relationship OXR = new Owner_X_Relationship() { RelationshipID = relationship.RelationshipID, Owner = owner, OwnerID = owner.OwnerID };
                    relationship.Owner_X_Relationships.Add(OXR);
                    dc.Relationships.InsertOnSubmit(relationship);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error activating new Relationship. " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Deactivate a relationship from the selected property either by making it inactive or deleting it 
        /// from the Relationships table.
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns></returns>
        public static bool RemoveRelationship(IDataContext datacontext, Relationship relationship, string action = null)
        {
            HVCCDataContext dc = datacontext as HVCCDataContext;

            try
            {
                /// Test to see if the relationship is associated to more than one Owner record.
                /// 
                IEnumerable<Owner_X_Relationship> associations = (from p in dc.Owner_X_Relationships
                                                                  where p.RelationshipID == relationship.RelationshipID
                                                                  select p);
                if (associations.Count() > 1)
                {
                    relationship.HasMutipleAssociations = true;
                    throw new RelationshipContraintException(relationship.RelationshipID);
                }

                /// Check to see if this Relationship is in the database (has a non-zero ID), 
                /// or is pending insertion (has a zero ID).
                /// 
                if (0 != relationship.RelationshipID
                    && (!relationship.RelationToOwner.Contains("Owner") || !relationship.RelationToOwner.Contains("Representative"))
                    && string.IsNullOrEmpty(action))
                {

                    /// Test to see if the relationship being removed has any FacilitiesUage records.
                    /// If not, we can delete the relationship record. Otherwise, we have to set
                    /// the records to inactive.
                    /// 
                    IEnumerable<FacilityUsage> fuList = (from x in dc.FacilityUsages
                                                         where x.RelationshipId == relationship.RelationshipID
                                                         select x);

                    /// If no facility usage records are returned, it is safe to just delete the
                    /// Relationship record. Since the passed in Relationship object (relationship)
                    /// is not attached to an enity set (it is local to the VM), we need to query the Relationship
                    /// table.
                    /// 
                    if (0 == fuList.Count())
                    {
                        dc.Owner_X_Relationships.DeleteAllOnSubmit(relationship.Owner_X_Relationships);
                        dc.Relationships.DeleteOnSubmit(relationship);
                    }
                    /// Otherwise, to deactivate the Relationship (related to F.U. records) we just
                    /// set the Active flag false.
                    /// 
                    else
                    {
                        relationship.Active = false;
                    }
                }
                /// If this is an Ownership Change, we just make the relationship inactive
                /// 
                else if (null != action && action.Contains("ChangeOwner"))
                {
                    relationship.Active = false;
                }
                /// Otherwise, the RelationshipID is 0, so it is a pending insert
                /// 
                else
                {

                    /// This is a pending insert, so we can simply remove the record from the in-memory
                    /// store.  We raise a PropertiesList property change event to force any/all bound
                    /// views to be updated.
                    /// 
                    dc.Relationships.DeleteOnSubmit(relationship);
                }
                ChangeSet cs = dc.GetChangeSet();  // <I> This is only for debugging.......
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Check for valid Ownership Relationship to Property relationship
        /// </summary>
        /// <returns></returns>
        public static bool CheckForOwner(ObservableCollection<Relationship> relationships)
        {
            try
            {
                foreach (Relationship r in relationships)
                {
                    if ("Owner" == r.RelationToOwner)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Relationship error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static ObservableCollection<Relationship> GetOwnersFromMailTo(string mailTo)
        {
            // The Mailto string should be in one of three formats:
            //    1). FirstName LastName
            //    2). FirstName/FirstName Lastname  (Ex. Fred/Willma Flintstone)
            //    3). FirstName LastName & FirstName LastName  (Fred Flintstone & Barney Rubble)
            // All other formats are considered to be non-standard
            string[] strings = null;
            string[] substrings = null;
            ObservableCollection<Relationship> rList = new ObservableCollection<Relationship>();

            // Test for format-1:
            strings = mailTo.Trim().Split(' ');
            if (strings.Count() == 2
                && !mailTo.Contains("/")
                && !mailTo.Contains("&"))
            {
                rList.Add(new Relationship { FName = strings[0], LName = strings[1], RelationToOwner = "Owner" });
            }

            // Test for format-2:
            strings = mailTo.Trim().Split('/');
            if (strings.Count() == 2
                && mailTo.Contains("/"))
            {
                substrings = strings[1].Trim().Split(' ');
                if (substrings.Count() == 2)
                {
                    rList.Add(new Relationship { FName = strings[0], LName = substrings[1], RelationToOwner = "Owner" });
                    rList.Add(new Relationship { FName = substrings[0], LName = substrings[1], RelationToOwner = "Owner" });
                }
            }

            // Test for format-3:
            strings = mailTo.Split('&');
            if (strings.Count() == 2
                && mailTo.Contains("&"))
            {
                substrings = strings[0].Trim().Split(' ');
                rList.Add(new Relationship { FName = substrings[0], LName = substrings[1], RelationToOwner = "Owner" });
                substrings = strings[1].Trim().Split(' ');
                rList.Add(new Relationship { FName = substrings[0], LName = substrings[1], RelationToOwner = "Owner" });
            }

            return rList;
        }
    }
}
