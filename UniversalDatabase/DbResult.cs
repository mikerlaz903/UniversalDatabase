using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UniversalDatabase.Exceptions;

namespace UniversalDatabase
{
    public class DbResult : IEnumerable
    {
        private readonly UOptions _options;
        private readonly List<string> _fieldNames = new List<string>();


        public object Value { get; private set; }
        public DataTable ExecutedSqlInfo { get; }
        public dynamic MappedValue { get; private set; }

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
                StringComparison comp = StringComparison.Ordinal;
                if ((_options & UOptions.IgnoreCare) == UOptions.IgnoreCare)
                    comp = StringComparison.OrdinalIgnoreCase;

                if (Value is List<object> { Count: 0 } || Value is List<List<object>> { Count: 0 })
                    throw new IndexOutOfRangeException("Enumerable have no elements");
                if (_fieldNames.FindIndex(row => row.Equals(colName, comp)) == -1)
                    throw new ColumnNotExistsException("The column does not appear in the executed query");

                return Value switch
                {
                    List<object> list => list[_fieldNames.FindIndex(row => row.Equals(colName, comp))],
                    List<List<object>> matrix => (from row in matrix select row[_fieldNames.FindIndex(match => match.Equals(colName, comp))]).ToList(),
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

        public DbResult(object result, DataTable info, UOptions options)
        {
            Value = result;
            ExecutedSqlInfo = info;

            _options = options;

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
        public void Map<TEntity>() where TEntity : class
        {
            switch (Value)
            {
                case List<object> list:
                    MappedValue = Activator.CreateInstance<TEntity>();
                    Map<TEntity>(ListSetter, list);
                    break;
                case List<List<object>> matrix:
                    {
                        MappedValue = new List<TEntity>();
                        for (var index = 0; index < matrix.Count; index++)
                            ((List<TEntity>)MappedValue).Add(Activator.CreateInstance<TEntity>());
                        Map<TEntity>(MatrixSetter, matrix);
                        break;
                    }
            }
        }

        private void Map<TEntity>(Action<PropertyInfo, object, int> propertySetter, object obj)
        {
            var entityPropertiesInfo = typeof(TEntity).GetProperties();
            foreach (var propertyInfo in entityPropertiesInfo)
            {
                var processedPropertyName = propertyInfo.Name
                    .Replace("_", "")
                    .ToLower();
                var matchedName = _fieldNames.Find(match =>
                    string.Compare(processedPropertyName, match
                        .Replace("_", "")
                        .ToLower(), StringComparison.Ordinal) == 0);
                if (matchedName == string.Empty)
                    continue;

                var type = typeof(TEntity);
                var prop = type.GetProperty(propertyInfo.Name);
                if (prop == null)
                    continue;

                var matchedColumnIndex = IndexOfColumn(matchedName);

                propertySetter(prop, obj, matchedColumnIndex);
            }
        }

        private void ListSetter(PropertyInfo prop, object obj, int matchedColumnIndex)
        {
            List<object> list = (List<object>)obj;

            prop.SetValue(MappedValue,
                list[matchedColumnIndex] is DBNull ? null : list[matchedColumnIndex],
                null);
        }

        private void MatrixSetter(PropertyInfo prop, object obj, int matchedColumnIndex)
        {
            List<List<object>> matrix = (List<List<object>>)obj;

            for (var index = 0; index < MappedValue.Count; index++)
                prop.SetValue(MappedValue[index],
                    matrix[index][matchedColumnIndex] is DBNull ? null : matrix[index][matchedColumnIndex],
                    null);

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
