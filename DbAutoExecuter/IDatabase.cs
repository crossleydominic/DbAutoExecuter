using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace DbAutoExecuter
{
    public interface IDatabase
    {
        DataSet ExecuteDataSet(string storedProcName, IDataParameter[] parameters);
        DataSet ExecuteDataSet(string storedProcName, IDataParameter[] parameters, IDbTransaction transaction);

        object ExecuteScalar(string storedProcName, IDataParameter[] parameters);
        object ExecuteScalar(string storedProcName, IDataParameter[] parameters, IDbTransaction transaction);

        T ExecuteScalar<T>(string storedProcName, IDataParameter[] parameters);
        T ExecuteScalar<T>(string storedProcName, IDataParameter[] parameters, IDbTransaction transaction);

        IDataReader ExecuteDataReader(string storedProcName, IDataParameter[] parameters);
        IDataReader ExecuteDataReader(string storedProcName, IDataParameter[] parameters, IDbTransaction transaction);

        void ExecuteNonQuery(string storedProcName, IDataParameter[] parameters);
        void ExecuteNonQuery(string storedProcName, IDataParameter[] parameters, IDbTransaction transaction);

        IDataParameter CreateParameter(string paramName, DbType type, object value, ParameterDirection direction);
    }
}
