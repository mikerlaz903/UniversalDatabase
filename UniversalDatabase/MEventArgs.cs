using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDatabase
{
    public class MEventArgs : EventArgs
    {
        public string SqlName { get; set; }
        public IEnumerable<DbParameter> ParameterCollection { get; set; }

        public MEventArgs(string sqlName, IEnumerable<object> parameterCollection)
        {
            SqlName = sqlName;
            ParameterCollection = (IEnumerable<DbParameter>)parameterCollection;
        }
    }
}
