namespace HVCC.Shell.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class UnmatchedBalance
    {
        public int OwnerID { get; set; }
        public string MailTo { get; set; }
        public decimal QBbalance { get; set; }
        public decimal HVbalance { get; set; }
    }
}
