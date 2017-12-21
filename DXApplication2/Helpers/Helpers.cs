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

    public class Helper
    {
        public static Binary LoadDefalutImage(IDataContext datacontext)
        {
            HVCCDataContext dc = datacontext as HVCCDataContext;

            ApplicationDefault defaults = (from p in dc.ApplicationDefaults
                                   select p).FirstOrDefault();

            return defaults.Photo;
        }

        /// <summary>
        /// Extracts the Section-Block-Lot-SubLot information from a Property.Customer string
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public static Property ConvertCustomerToProperty( string customer)
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
            else if(customer.Length > custFormat.Length && fields.Count() == 4)
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
                propRecord.Lot = Int32.Parse(fields[2].Substring(0,2));
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
                // Check to see if this Relationship is in the database (has a non-zero ID), 
                // or is pending insertion (has a zero ID).  We only want to process new
                // relationships.
                if (0 == relationship.RelationshipID)
                {
                    // Add the default HVCC image to the relationship record.  
                    relationship.Photo = LoadDefalutImage(dc);
                    relationship.Active = true;
                    relationship.OwnerID = owner.OwnerID;
                    dc.Relationships.InsertOnSubmit(relationship);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error activating new Relationship/n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                //// Check to see if this Relationship is in the database (has a non-zero ID), 
                //// or is pending insertion (has a zero ID).
                if (0 != relationship.RelationshipID 
                    && !relationship.RelationToOwner.Contains("Owner")
                    && string.IsNullOrEmpty(action))
                {

                    // Test to see if the relationship being removed has any FacilitiesUage records.
                    // If not, we can delete the relationship record. Otherwise, we have to set
                    // the records to inactive.
                    IEnumerable<FacilityUsage> fuList = (from x in dc.FacilityUsages
                                                         where x.RelationshipId == relationship.RelationshipID
                                                         select x);

                    // If no facility usage records are returned, it is safe to just delete the
                    // Relationship record. Since the passed in Relationship object (relationship)
                    // is not attached to an enity set (it is local to the VM), we need to query the Relationship
                    // table.
                    if (0 == fuList.Count())
                    {
                        dc.Relationships.DeleteOnSubmit(relationship);
                    }
                    // Otherwise, to deactivate the Relationship (related to F.U. records) we just
                    // set the Active flag false.
                    else
                    {
                        relationship.Active = false;
                    }
                }
                // If this is an Ownership Change, we just make the relationship inactive
                else if (action.Contains("ChangeOwner"))
                {
                    relationship.Active = false;
                }
                // Otherwise, the RelationshipID is 0, so it is a pending insert
                else
                {

                    // This is a pending insert, so we can simply remove the record from the in-memory
                    // store.  We raise a PropertiesList property change event to force any/all bound
                    // views to be updated.
                    //selectedProperty.Relationships.Remove(relationship);
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

        /// <summary>
        /// Return the placholder Property record that deactivated Relationship records are assign to
        /// </summary>
        /// <param name="datacontext"></param>
        /// <returns></returns>
        private static Property GetDummyPropertyID(IDataContext datacontext)
        {
            HVCCDataContext dc = datacontext as HVCCDataContext;

            Property dummyProperty = (from p in dc.Properties
                                      where p.Customer == "99-99-99-0"
                                      select p).FirstOrDefault();

            return dummyProperty;
        }

        /// <summary>
        /// Attempts to extract the owner name(s) from the BillTo string
        /// </summary>
        /// <param name="theProperty"></param>
        /// <returns></returns>
        //public static List<Relationship> ExtractOwner(Property theProperty)
        //{
        //    char[] charsToTrim = {' ', '\t' };
        //    string workingString;
        //    string[] fullArray = theProperty.BillTo.Split(',');
        //    string[] workingArray;
        //    List<Relationship> owners = new List<Relationship>();
        //    Relationship firstOwner = new Relationship();

        //    if (0 == theProperty.PropertyID)
        //    {
        //        return owners;
        //    }
        //    else
        //    {
        //        //// Check to see if there is more than one element
        //        if (fullArray.Count() > 1)
        //        {
        //            //// The first element should be the last name of the owner. However, there
        //            //// may be mutiple owners separated by a slash, or some other odd format present
        //            workingArray = fullArray[0].Split('/');
        //            if (1 == workingArray.Count())
        //            {
        //                workingString = fullArray[0].Trim(charsToTrim);
        //                workingArray = workingString.Split(' '); // make sure the string doesn't have a <sp>
        //                if (1 == workingArray.Count())
        //                {
        //                    firstOwner.LName = fullArray[0];
        //                }
        //                else // it appears there was a <sp>, so we use the split working array
        //                {
        //                    firstOwner.LName = workingArray[0];
        //                }

        //                //// Now try to extract out the first name(s).  There is odd formatting here to deal with as well....
        //                workingString = fullArray[1].TrimStart();
        //                workingArray = workingString.Split(' ');
        //                firstOwner.FName = workingArray[0];
        //                firstOwner.RelationToOwner = "Owner";
        //                firstOwner.PropertyID = theProperty.PropertyID;
        //                owners.Add(firstOwner);

        //                //// Check to see if there are two first names (separated by &)
        //                if (fullArray[1].Contains("&"))
        //                {
        //                    Relationship secondOwner = new Relationship();
        //                    secondOwner.RelationToOwner = firstOwner.RelationToOwner;
        //                    secondOwner.PropertyID = firstOwner.PropertyID;
        //                    secondOwner.LName = firstOwner.LName;
        //                    secondOwner.FName = workingArray[2];
        //                    owners.Add(secondOwner);
        //                }
        //                return owners;
        //            }
        //            else
        //            {
        //                return owners;
        //            }
        //        }
        //        else
        //        {
        //            return owners;
        //        }
        //    }
        //}

        /// <summary>
        /// Returns a slash delimited list of unique names from a list
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        //public static string UniqueNames(List<string> names)
        //{
        //    List<string> nameList = (from l in names
        //                              select l).Distinct().ToList();
        //    string tString = String.Empty;
        //    foreach (string s in nameList)
        //    {
        //        tString = tString + s + "/";
        //    }
        //    int lastChr = tString.Length - 1;
        //    return tString.Substring(0, lastChr);
        //}
    }
}
