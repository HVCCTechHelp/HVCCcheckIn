using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HVCC.Shell.Validation
{
    [Serializable]
    class RelationshipContraintException : Exception
    {
        public RelationshipContraintException()
        {

        }

        public RelationshipContraintException(int id)
            : base(String.Format("Cannot delete relationship ID {0} because of multiple association constraint", id))
        {

        }

    }
    [Serializable]
    class RelationshipAddException : Exception
    {
        public RelationshipAddException()
            : base(String.Format("Cannot add relationship because of multiple association constraint"))
        {

        }

    }

}
