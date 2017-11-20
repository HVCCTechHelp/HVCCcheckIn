namespace HVCC.Shell.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DatabaseRoleInfo
    {
        public DatabaseRoleInfo(string role, string database = null)
        {
            this.Role = role;
            this.Database = database;
        }

        public string Role { get; private set; }

        public string Database { get; private set; }
    }

}
