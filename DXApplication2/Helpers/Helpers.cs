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

    public class Helper
    {
        /// <summary>
        /// Loads image file into a byte array.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static byte[] LoadImage(string fileName)
        {
            byte[] _Buffer = null;
            try
            {
                FileStream _FileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                BinaryReader _BinaryReader = new BinaryReader(_FileStream);
                long _TotalBytes = new System.IO.FileInfo(fileName).Length;
                _Buffer = _BinaryReader.ReadBytes((Int32)_TotalBytes);
                _FileStream.Close();
                _FileStream.Dispose();
                _BinaryReader.Close();
                return _Buffer;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

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
        public static bool AddRelationship(IDataContext datacontext, Property selectedProperty, Relationship relationship) 
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

                    // Add this relationship to the pending database changes. Actual update isn't
                    // immeidate and is dependent on user clicking the 'save' button.
                    relationship.PropertyID = selectedProperty.PropertyID;
                    relationship.Customer = selectedProperty.Customer;
                    dc.Relationships.InsertOnSubmit(relationship);

                    // Because of the way I implemented the Relationship grid in the edit dialog,
                    // the new relationship also needs to be manually added to the VM collection of 
                    // the selected property.  [It is not automaticly added to the collection via the datacontext
                    // even after the new Relationship is added to the database.]
                    //selectedProperty.Relationships.Add(relationship);

                    ChangeSet cs = dc.GetChangeSet();
                    return true;
                }
                else
                {
                    //var x = (from r in selectedProperty.Relationships
                    //         where r.RelationshipID == relationship.RelationshipID
                    //         select r).FirstOrDefault();

                    //ChangeSet cs = dc.GetChangeSet();

                    return false;
                }
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
        public static bool RemoveRelationship(IDataContext datacontext, Property selectedProperty, Relationship relationship)
        {
            HVCCDataContext dc = datacontext as HVCCDataContext;

            try
            {
                // Check to see if this Relationship is in the database (has a non-zero ID), 
                // or is pending insertion (has a zero ID).
                if (0 != relationship.RelationshipID)
                {

                    // Test to see if the relationship being removed has any FacilitiesUage records.
                    // If not, we can delete the relationship record. Otherwise, we have to set
                    // the records to inactive.
                    IEnumerable<FacilityUsage> fuLList = (from x in dc.FacilityUsages
                                                          where x.RelationshipId == relationship.RelationshipID
                                                          select x);

                    // If no facility usage records are returned, it is safe to just delete the
                    // Relationship record; it won't violate the FK contraints.  Since the passed in Relationship object (relationship)
                    // is not attached to an enity set (it is local to the VM), we need to query the SelectedPropert.Relationship
                    // collection to get the Relationships record that is attached so it is attached and therefore can be removed
                    // from the datacontext.
                    if (0 == fuLList.Count())
                    {
                        Relationship r = (from x in selectedProperty.Relationships
                                          where x.RelationshipID == relationship.RelationshipID
                                          select x).FirstOrDefault();

                        dc.Relationships.DeleteOnSubmit(r);
                    }
                    // Otherwise, to deactivate the Relationship (related to F.U. records) we just
                    // reassign it to the PropertyID = 99-99-99-0
                    else
                    {
                        Property dummyProperty = GetDummyPropertyID(dc);
                        // Now reassign the Relationship to the dummy Property
                        relationship.PropertyID = dummyProperty.PropertyID;

                        // Lastly, reassign the Facility Usage records associated to the RelationshipID
                        // to the dummy PropertyID
                        foreach (FacilityUsage f in fuLList)
                        {
                            f.PropertyID = dummyProperty.PropertyID;
                        }
                    }
                }
                else
                {

                    // This is a pending insert, so we can simply remove the record from the in-memory
                    // store.  We raise a PropertiesList property change event to force any/all bound
                    // views to be updated.
                    //selectedProperty.Relationships.Remove(relationship);
                    dc.Relationships.DeleteOnSubmit(relationship);
                    ChangeSet cs = dc.GetChangeSet();  // <I> This is only for debugging.......
                }
                //}
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
        public static bool CheckForOwner(IDataContext datacontext, Property selectedProperty)
        {
            HVCCDataContext dc = datacontext as HVCCDataContext;

            try
            {
                ChangeSet cs = dc.GetChangeSet();

                Property dummyProperty = GetDummyPropertyID(dc);

                // The first thing we need to do is make sure we have at lease one active Relationship
                // that is also an Owner. New Relationship records will be added to the Selected.Relationship
                // collection, but their Active status will be null. 
                var addList = (from r in cs.Inserts
                               where (r as Relationship).RelationToOwner == "Owner"
                               select r);

                // ? What if they keep an existing Owner record.... We may need to check SelectedProperty.Relationships
                var updateList = (from r in cs.Updates
                                  where (r as Relationship).RelationToOwner == "Owner"
                                  && (r as Relationship).PropertyID != selectedProperty.PropertyID
                                  select r);

                // We use try/catch incase the results of the two queries return invalid results (exception). Otherwise
                // the 'try' will update the count.
                int ownerCount = 0;
                try
                {
                    ownerCount += addList.Count();
                }
                catch
                { }
                try
                {
                    ownerCount += updateList.Count();
                }
                catch
                { }
                if (0 == ownerCount) { return false; }
                else { return true; }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Relationship error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
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
