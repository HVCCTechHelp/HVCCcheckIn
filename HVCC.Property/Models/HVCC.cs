namespace HVCC.Property
{
    partial class HVCCDataContext
    {
    }
}

namespace HVCC.Property.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    public partial class Property
    {

        public string LName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var un in UniqueOwnersLNames.OrderBy(x => x))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("/");
                    }
                    sb.Append(un);
                }
                return sb.ToString();
            }
            set
            {
                if (this.LName != value)
                {
                    this.LName = value;
                }
            }
        }

        public string FName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var un in UniqueOwnersFNames.OrderBy(x => x))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("/");
                    }
                    sb.Append(un);
                }
                return sb.ToString();
            }
            set
            {
                if (this.FName != value)
                {
                    this.FName = value;
                }
            }
        }

        public List<string> UniqueOwnersFNames
        {
            get
            {
                var list = (from x in this.Relationships
                            where x.RelationToOwner == "Owner"
                            select x.FName).Distinct().ToList();

                return list;
            }
        }

        public List<string> UniqueOwnersLNames
        {
            get
            {
                var list = (from x in this.Relationships
                            where x.RelationToOwner == "Owner"
                            select x.LName).Distinct().ToList();

                return list;
            }
        }

        public int Year
        {
            get
            {
                return this.AnnualPropertyInformations.Where(x => x.Year == DateTime.Now.Year).Select(x => x.Year).FirstOrDefault();
            }
            set
            {
                if (this.Year != value)
                {
                    this.Year = value;
                }
            }
        }


        public AnnualPropertyInformation CurrentAnnualPropertyInformation
        {
            get
            {
                return this.AnnualPropertyInformations.Where(x => x.Year == DateTime.Now.Year).Select(x => x).FirstOrDefault();
            }
        }
    }
}