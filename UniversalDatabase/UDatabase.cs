using System;
using System.Data;
using System.Data.Common;

namespace UniversalDatabase
{
    public class UDatabase
    {
        private static UDatabase _instance;
        private static readonly object _syncRoot = new object();

        private static DbConnection _connection;
        private static DbTransaction _transaction;

        private UDatabase(DbConnection connetion)
        {
            _connection = connetion;
            _connection.Open();
            _transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public static UDatabase GetInstance(DbConnection connetion = null)
        {
            lock (_syncRoot)
            {
                if (_instance == null && connetion != null)
                    _instance = new UDatabase(connetion);
            }
            return _instance;
        }

        private DbCommand GetCommand(string inputCommand)
        {
            var command = _connection.CreateCommand();
            command.Transaction = _transaction;
            command.CommandText = inputCommand;

            return command;
        }
    }
}
