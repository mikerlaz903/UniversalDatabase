using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDatabase.Exceptions
{
    public class ColumnNotExistsException : Exception
    {
        public ColumnNotExistsException()
        {

        }
        public ColumnNotExistsException(string message) : 
            base(message)
        {

        }
        public ColumnNotExistsException(string message, Exception inner) :
            base(message, inner)
        {

        }
    }
}
