using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDatabase
{
    public class DbResult : IEnumerable
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

        public IEnumerator GetEnumerator()
        {
            if (Value == null) throw new NotImplementedException();
            return Value switch
            {
                List<object> list => list.GetEnumerator(),
                List<List<object>> matrix => matrix.GetEnumerator(),
                _ => throw new InvalidOperationException()
            };
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

        public int IndexOfColumn(string columnName)
        {
            return _fieldNames.IndexOf(columnName);
        }

        public int GetRowIndex(Func<List<object>, bool> predicate)
        {
            var rowSelection = ((List<List<object>>)Value).FirstOrDefault(predicate);
            var indexRowSelection = ((List<List<object>>)Value).FindIndex(row => row == rowSelection);
            return indexRowSelection;
        }
        public void Clear()
        {
            Value = null;
            ExecutedSqlInfo.Clear();
        }
    }
}
