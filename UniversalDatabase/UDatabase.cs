using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace UniversalDatabase
{
    public class UDatabase
    {
        private static UDatabase _instance;
        private static readonly object SyncRoot = new ();

        private DbConnection _connection;
        private DbTransaction _transaction;

        public DbResult Result { get; private set; }


        public event EventHandler<MEventArgs> ExecutingQuery;
        private void OnExecutingQuery(MEventArgs e)
        {
            ExecutingQuery?.Invoke(this, e);
        }

        public event EventHandler<MEventArgs> ExecutedQuery;
        private void OnExecutedQuery(MEventArgs e)
        {
            ExecutedQuery?.Invoke(this, e);
        }
        private UDatabase(DbConnection connection)
        {
            _connection = connection;
            _connection.Open();
            _transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public static UDatabase GetInstance(DbConnection connection = null)
        {
            lock (SyncRoot)
            {
                if (_instance == null && connection != null)
                    _instance = new UDatabase(connection);
            }
            return _instance;
        }

        public void Commit() => _transaction.Commit();
        public void Rollback() => _transaction.Rollback();

        private DbCommand GetCommand(string inputCommand)
        {
            var command = _connection.CreateCommand();
            command.Transaction = _transaction;
            command.CommandText = inputCommand;

            return command;
        }

        public void GetField(string sql, IEnumerable<object> parameterCollection)
        {
            OnExecutingQuery(null);
            try
            {
                using var resultCommand = GetCommand(sql);
                resultCommand.Parameters.AddRange(parameterCollection.ToArray());
                object res = resultCommand.ExecuteScalar();
                Result = new (res, null);
            }
            catch (Exception exc) when (exc.GetType() == typeof(DbException))
            {
                // ignore
            }
            OnExecutedQuery(new MEventArgs(sql, parameterCollection));
        }

        public void GetRow(string sql, IEnumerable<object> parameterCollection)
        {
            OnExecutingQuery(null);
            try
            {
                using var resultCommand = GetCommand(sql);
                resultCommand.Parameters.AddRange(parameterCollection.ToArray());

                var reader = resultCommand.ExecuteReader();
                var fields = new List<object>();
                object[] a = new object[reader.FieldCount];
                if (reader.Read())
                {
                    reader.GetValues(a);
                    fields.AddRange(a.ToList());
                }
                var executedSqlInfo = reader.GetSchemaTable();
                reader.Close();

                Result = new (fields, executedSqlInfo);
            }
            catch (Exception exc) when (exc.GetType() == typeof(DbException))
            {
                // ignore
            }
            OnExecutedQuery(new MEventArgs(sql, parameterCollection));
        }

        public void GetRows(string sql, IEnumerable<object> parameterCollection)
        {
            OnExecutingQuery(null);
            try
            {
                using var resultCommand = GetCommand(sql);
                resultCommand.Parameters.AddRange(parameterCollection.ToArray());

                var reader = resultCommand.ExecuteReader();
                var rows = new List<List<object>>();
                object[] a = new object[reader.FieldCount];
                while (reader.Read())
                {
                    reader.GetValues(a);
                    rows.Add(a.ToList());
                }
                var executedSqlInfo = reader.GetSchemaTable();
                reader.Close();

                Result = new (rows, executedSqlInfo);
            }
            catch (Exception exc) when (exc.GetType() == typeof(DbException))
            {
                // ignore
            }
            OnExecutedQuery(new MEventArgs(sql, parameterCollection));
        }

        public int ExecuteNonQuery(string sql, IEnumerable<object> parameterCollection, bool autoCommit = true)
        {
            OnExecutingQuery(null);
            int rowEffected = 0;
            var collection = parameterCollection.ToList();
            try
            {
                var command = GetCommand(sql);
                command.Parameters.AddRange(collection.ToArray());
                rowEffected = command.ExecuteNonQuery();
                if (autoCommit)
                    command.Transaction?.Commit();
            }
            catch (Exception exc) when (exc.GetType() == typeof(DbException))
            {
                // ignore
            }
            OnExecutedQuery(new MEventArgs(sql, parameterCollection));
            return rowEffected;
        }
    }
}
