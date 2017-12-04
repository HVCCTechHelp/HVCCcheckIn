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
    using HVCC.Shell.Models;

    public class Helper
    {
        //public static bool GetHasError(DependencyObject obj)
        //{
        //    return (bool)obj.GetValue(HasErrorProperty);
        //}

        //public static void SetHasError(DependencyObject obj, bool value)
        //{
        //    obj.SetValue(HasErrorProperty, value);
        //}

        //public static readonly DependencyProperty HasErrorProperty = DependencyProperty.RegisterAttached("HasError", typeof(bool), typeof(Helper), null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="isDirty"></param>
        //public static void UpdateCaption(BaseLayoutItem panel, bool isDirty)
        //{
        //    string[] caption = panel.Caption.ToString().Split('*');
        //    if (isDirty)
        //    {
        //        panel.Caption = caption[0].TrimEnd(' ') + "* ";
        //    }
        //    else
        //    {
        //        panel.Caption = caption[0].TrimEnd(' ');
        //    }
        //}

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

        /// <summary>
        /// returns an array of bytes for a BitmapImage
        /// </summary>
        /// <param name="bitmapImage"></param>
        /// <returns></returns>
        //public static byte[] BitmapImageToArray(BitmapImage bitmapImage)
        //{
        //    byte[] bytes;
        //    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
        //    encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        encoder.Save(ms);
        //        bytes = ms.ToArray();
        //        return bytes;
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        //public static BitmapImage ArrayToBitmapImage(byte[] imageData)
        //{
        //    if (imageData == null || imageData.Length == 0) return null;
        //    var image = new BitmapImage();
        //    using (var mem = new MemoryStream(imageData))
        //    {
        //        mem.Position = 0;
        //        image.BeginInit();
        //        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        //        image.CacheOption = BitmapCacheOption.OnLoad;
        //        image.UriSource = null;
        //        image.StreamSource = mem;
        //        image.EndInit();
        //    }
        //    image.Freeze();
        //    return image;
        //}

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
