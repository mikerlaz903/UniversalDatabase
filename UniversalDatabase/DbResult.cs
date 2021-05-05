using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDatabase
{
    public class DbResult
    {
        private readonly List<string> _fieldNames = new List<string>();
        public object Value { get; private set; }
        public DataTable ExecutedSqlInfo { get; }

        public object this[int col]
        {
            get
            {
                return Value switch
                {
                    List<object> list => list[col],
                    List<List<object>> matrix => (from row in matrix select row[col]).ToList(),
                    _ => throw new InvalidOperationException("Indexer can be applied only after IEnumerable result")
                };
            }
        }

        public object this[string colName]
        {
            get
            {
                return Value switch
                {
                    List<object> list => list[_fieldNames.FindIndex(row => row.Equals(colName))],
                    List<List<object>> matrix => (from row in matrix select row[_fieldNames.FindIndex(match => match.Equals(colName))]).ToList(),
                    _ => throw new InvalidOperationException("Indexer can be applied only after IEnumerable result")
                };
            }
        }

        public DbResult(object result, DataTable info)
        {
            _fieldNames = new List<string>();

            Value = result;
            ExecutedSqlInfo = info;

            DefineFieldNames();
        }

        private void DefineFieldNames()
        {
            if (ExecutedSqlInfo == null) return;
            foreach (var row in ExecutedSqlInfo.AsEnumerable())
            {
                _fieldNames.Add(row.Field<string>("ColumnName"));
            }
        }

        public void Clear()
        {
            Value = null;
            ExecutedSqlInfo.Clear();
        }
    }
}
