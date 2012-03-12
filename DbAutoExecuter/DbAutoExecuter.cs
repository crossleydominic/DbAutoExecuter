using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;

namespace DbAutoExecuter
{
	/// <summary>
	/// A class to automatically call a db stored proc based upon 
	/// the calling methods reflected metadata
	/// </summary>
	public static class DbAutoExecuter
	{
		#region Private constants

		/// <summary>
		/// The string used to prefix input parameters
		/// </summary>
		private const string IN_DIRECTION = "IN_";

		/// <summary>
		/// The string used to prefix output parameters
		/// </summary>
		private const string OUT_DIRECTION = "OUT_";

		/// <summary>
		/// The string used to prefix string arguments
		/// </summary>
		private const string STRING_TYPE = "str";

		/// <summary>
		/// The string used to prefix integer arguments
		/// </summary>
		private const string INTEGRAL_TYPE = "int";

		/// <summary>
		/// The string used to prefix date arguments
		/// </summary>
		private const string DATE_TYPE = "dat";

		/// <summary>
		/// The string used to prefix fixed point numeric types
		/// </summary>
		private const string PRECISE_NUMERIC_TYPE = "num";

		/// <summary>
		/// The string used to prefix floating point numeric types
		/// </summary>
		private const string APPROXIMATE_NUMERIC_TYPE = "num";

		#endregion

		#region Public static methods

		/// <summary>
		/// Executes a NonQuery against a stored procedure described by the MethodBase's metadata.
		/// </summary>
		/// <param name="method">
		/// The method metadata used to construct the stored procedure call
		/// </param>
		/// <param name="db">
		/// The DbHelper that will be used to make the db call.
		/// </param>
		/// <param name="trans">
		/// The transaction the stored procedure call will be made through. Set to null if a transaction
		/// isn't required
		/// </param>
		/// <param name="args">
		/// The list of input arguments to the stored procedure. These MUST be in the same order as they
		/// are passed into the MethodBase method.
		/// Do NOT include output arguments in this array.
		/// </param>
		/// <returns>
		/// A list of objects that are filled in from the output parameters from the stored procedure.
		/// The objects in the list are in the same order as the output parameters are declared on the
		/// MethodBase's method.
		/// </returns>
        public static List<object> ExecuteNonQuery(MethodBase method, IDatabase db, IDbTransaction trans, params object[] args)
        {
            List<object> outputValues = new List<object>();

            string dbMethod = GetFullObjectName(method);

            List<IDataParameter> dbParameters = GetDbParameters(method, db, args);

            if (trans != null)
            {
                db.ExecuteNonQuery(dbMethod, dbParameters.ToArray(), trans);
            }
            else
            {
                db.ExecuteNonQuery(dbMethod, dbParameters.ToArray());
            }

            int outputIndex = 0;

            //Get all the declared output parameters from the method metadata.
            ParameterInfo[] outputParams = method.GetParameters().Where((p) => { return p.IsOut; }).ToArray();

            try
            {
                foreach (IDataParameter dbParam in dbParameters.Where((p) => { return p.Direction == ParameterDirection.Output; }))
                {
                    bool isOutputABoolean = false;

                    //Determine whether or not we need to marshal an int back into a bool
                    if (outputIndex < outputParams.Length &&
                        outputParams[outputIndex].ParameterType.GetElementType() == typeof(bool))
                    {
                        isOutputABoolean = true;
                    }

                    if (isOutputABoolean)
                    {
                        outputValues.Add(AsBool(dbParam.Value));
                    }
                    else
                    {
                        outputValues.Add(dbParam.Value);
                    }

                    outputIndex++;
                }

            }
            catch (IndexOutOfRangeException ioe)
            {
                throw new InvalidOperationException("The output parameters for the CLR method do not match the parameters for the stored procedure.", ioe);
            }

            return outputValues;
        }

		/// <summary>
		/// Executes a stored procedure described by the MethodBases's metadata and returns
		/// a dataset.
		/// </summary>
		/// <param name="method">
		/// The method metadata used to construct the stored procedure call
		/// </param>
		/// <param name="db">
		/// The DbHelper that will be used to make the db call.
		/// </param>
		/// <param name="trans">
		/// The transaction the stored procedure call will be made through. Set to null if a transaction
		/// isn't required
		/// </param>
		/// <param name="args">
		/// The list of input arguments to the stored procedure. These MUST be in the same order as they
		/// are passed into the MethodBase method.
		/// Do NOT include output arguments in this array.
		/// </param>
		/// <returns>
		/// A dataset that is returned from the stored procedure call.
		/// </returns>
        public static DataSet ExecuteDataSet(MethodBase method, IDatabase db, IDbTransaction trans, params object[] args)
        {
            string dbMethod = GetFullObjectName(method);

            List<IDataParameter> dbParameters = GetDbParameters(method, db, args);

            DataSet returnDs = null;

            if (trans != null)
            {
                returnDs = db.ExecuteDataSet(dbMethod, dbParameters.ToArray(), trans);
            }
            else
            {
                returnDs = db.ExecuteDataSet(dbMethod, dbParameters.ToArray());
            }

            return returnDs;
        }

		/// <summary>
		/// Executes a stored procedure described by the MethodBases's metadata and returns
		/// a DataReader.
		/// </summary>
		/// <param name="method">
		/// The method metadata used to construct the stored procedure call
		/// </param>
		/// <param name="db">
		/// The DbHelper that will be used to make the db call.
		/// </param>
		/// <param name="trans">
		/// The transaction the stored procedure call will be made through. Set to null if a transaction
		/// isn't required
		/// </param>
		/// <param name="args">
		/// The list of input arguments to the stored procedure. These MUST be in the same order as they
		/// are passed into the MethodBase method.
		/// Do NOT include output arguments in this array.
		/// </param>
		/// <returns>
		/// A DataReader that is returned from the stored procedure call.
		/// </returns>
		public static IDataReader ExecuteDataReader(MethodBase method, IDatabase db, IDbTransaction trans, params object[] args)
		{
			string dbMethod = GetFullObjectName(method);

			List<IDataParameter> dbParameters = GetDbParameters(method, db, args);

			IDataReader reader = null;

            if (trans != null)
            {
                reader = db.ExecuteDataReader(dbMethod, dbParameters.ToArray(), trans);
            }
            else
            {
                reader = db.ExecuteDataReader(dbMethod, dbParameters.ToArray());
            }

			return reader;
		}

		#endregion

		#region Private static methods

		/// <summary>
		/// Constructs a list of IDataParameters from the method information supplied in method
		/// </summary>
		/// <param name="method">
		/// The method to get the metadata from for constructing the data parameters to call the stored procedure
		/// </param>
		/// <param name="db">
		/// A db helper that will be used to create the data parameters
		/// </param>
		/// <param name="args">
		/// The list of arguments that will be used to create the data parameters.
		/// The length of position of each of these arguments must match the type and position of the arguments
		/// declared on the supplied MethodBase.
		/// </param>
		/// <returns>
		/// A list of IDataParameters that can be used to call the stored procedure.
		/// </returns>
		private static List<IDataParameter> GetDbParameters(MethodBase method, IDatabase db, object[] args)
		{
			List<IDataParameter> dbParameters = new List<IDataParameter>();

			ParameterInfo[] parameters = method.GetParameters();

			bool omitDirection = (GetCustomAttribute<OmitParameterDirectionAttribute>(method) != null);
			bool omitType = (GetCustomAttribute<OmitParameterTypeAttribute>(method) != null);

			int inputArgIndex = 0;
			foreach (ParameterInfo paramInfo in parameters)
			{
				//Skip Transaction parameters
				if (paramInfo.ParameterType == typeof(IDbTransaction) ||
					paramInfo.ParameterType.FindInterfaces((typeObj, criteria) => 
					{ 
						return typeObj.Equals(criteria); 
					}, 
					typeof(IDbTransaction)).Length > 0)
				{
					continue;
				}

				string parameterName = string.Empty;
				bool isInputParameter = (paramInfo.IsOut == false);

				if (omitDirection == false)
				{
					parameterName += (isInputParameter ? IN_DIRECTION : OUT_DIRECTION);
				}

				if (omitType == false)
				{
					parameterName += MapClrTypeToDbTypeName(paramInfo.ParameterType);
				}

				if (omitType == false)
					//If we're including the type we need to capitilise the first letter
					//of the parameter name so that the stored proc parameter becomes
					//"IN_strArg1" instead of "IN_strarg1".
				{
					parameterName += (char.ToUpper(paramInfo.Name[0]) + paramInfo.Name.Substring(1));
				}
				else
					//If we're omitting the type then the parameter name will be generated
					//with a lower case first letter, which is ok.
				{
					parameterName += paramInfo.Name;
				}

				//Special case, if the input parameter is a bool then we'll substitute
				//an integer value in there instead.  This is because Oracle has a shit-fit when
				//trying to deal with boolean types.
				if(paramInfo.ParameterType == typeof(bool) && isInputParameter)
				{
					args[inputArgIndex] = ((bool)args[inputArgIndex]) ? 1 : 0;
				}

				IDataParameter newParam = db.CreateParameter(
						parameterName,
						MapClrTypeToDbType(paramInfo.ParameterType),
						(isInputParameter ? args[inputArgIndex] : null),
						(isInputParameter ? ParameterDirection.Input : ParameterDirection.Output));
				
				// Another special case. For output DB parameters in SQLServer, a scale must be
				// specified or the value gets rounded. Setting to an aribtrary maximum of 6dp
				if (newParam.Direction == ParameterDirection.Output &&
					newParam.DbType == DbType.Decimal &&
					newParam is System.Data.SqlClient.SqlParameter)
				{
					((System.Data.SqlClient.SqlParameter)newParam).Scale = 6;
				}

				dbParameters.Add(newParam);

				inputArgIndex++;
			}

			return dbParameters;
		}

		/// <summary>
		/// Attempts to cast an object back to a bool.  Used by the automatic bool-to-integer conversion code.
		/// </summary>
		/// <param name="obj">
		/// An object that MUST contain a boxed Int32 that represents the boolean value.
		/// </param>
		/// <returns>
		/// If obj is 0 then False, otherwise True.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when obj is not a boxed Int32.
		/// </exception>
		private static bool AsBool(object obj)
		{
			if ((obj is int) == false)
			{
				throw new InvalidOperationException("The returned boolean type from the stored proc is not of the expected type");
			}

			return (((int)obj) == 0 ? false : true);
		}

		/// <summary>
		/// Takes a Type and maps it to a DbType for use in the call to DbHelper.CreateParameter
		/// </summary>
		/// <param name="type">
		/// The Type of argument we are trying to build for the stored procedure
		/// </param>
		/// <returns>
		/// A DbType if a mapping between Type and DbType is allowed.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the supplied Type argument is not convertible to a DbType.
		/// </exception>
		private static DbType MapClrTypeToDbType(Type type)
		{
			if (type.IsByRef)
				//Out-parameters need converting back into there pointed-at types.
			{
				type = type.GetElementType();
			}

			if (type == typeof(int))
			{
				return DbType.Int32;
			}
			else if (type == typeof(uint))
			{
				return DbType.UInt32;
			}
			else if (type == typeof(long))
			{
				return DbType.Int64;
			}
			else if (type == typeof(ulong))
			{
				return DbType.UInt64;
			}
			else if (type == typeof(short))
			{
				return DbType.Int16;
			}
			else if (type == typeof(ushort))
			{
				return DbType.UInt16;
			}
			else if (type == typeof(decimal))
			{
				return DbType.Decimal;
			}
			else if (type == typeof(float))
			{
				return DbType.Single;			 	
			}
			else if (type == typeof(double))
			{
				return DbType.Double;
			}
			else if (type == typeof(string))
			{
				return DbType.String;
			}
			else if (type == typeof(DateTime))
			{
				return DbType.DateTime;
			}
			else if (type == typeof(bool))
				//Oracle has trouble with booleans so we'll use ints (0/1/-1) to model a boolean value
			{
				return DbType.Int32;
			}
			else
			{
				throw new InvalidOperationException(string.Format("Cannot map {0} to a db type", type.Name));
			}
		}

		/// <summary>
		/// Takes a Type and maps it to a type name for use in the stored procedures arguments name.
		/// </summary>
		/// <param name="type">
		/// The Type of argument that we need to get a type name for.
		/// </param>
		/// <returns>
		/// A type name that will be used in the argument name if a mapping exists.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the supplied Type cannot be mapped to a type name.
		/// </exception>
		private static string MapClrTypeToDbTypeName(Type type)
		{
			if (type.IsByRef)
				//Out-parameters need converting back into there pointed-at types.
			{
				type = type.GetElementType();
			}

			if (type == typeof(int) ||
				type == typeof(long) ||
				type == typeof(short) ||
				type == typeof(uint) ||
				type == typeof(ulong) ||
				type == typeof(ushort))
			{
				return INTEGRAL_TYPE;
			}
			else if (type == typeof(DateTime))
			{
				return DATE_TYPE;
			}
			else if (type == typeof(decimal))
			{
				return PRECISE_NUMERIC_TYPE;
			}
			else if (type == typeof(string))
			{
				return STRING_TYPE;
			}
			else if (type == typeof(float)||
				type == typeof(double))
			{
				return APPROXIMATE_NUMERIC_TYPE;
			}
			else if (type == typeof(bool))
				//Oracle has trouble with booleans so we'll use ints (0/1/-1) to model a boolean value
			{
				return INTEGRAL_TYPE;
			}
			else
			{
				throw new InvalidOperationException(string.Format("Cannot map {0} to a db type name", type.Name));
			}
		}

		/// <summary>
		/// Gets the full name of the Stored procedure (package/schema name + procedure name).
		/// </summary>
		/// <param name="method">
		/// The MethodBase object that contains the metadata on what the stored procedure name should be
		/// </param>
		/// <returns>
		/// The fully qualified stored procedure name.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the MethodBase object does not have a PackageName custom attribute associated with it.
		/// </exception>
		private static string GetFullObjectName(MethodBase method)
		{
			string packageName;
			string procedureName;

			PackageNameAttribute packageNameAttr = GetCustomAttribute<PackageNameAttribute>(method);
			ProcedureNameAttribute procNameAttr = GetCustomAttribute<ProcedureNameAttribute>(method);

			if (packageNameAttr == null)
			{
				throw new InvalidOperationException("All auto-execute db methods must have a package/schema name.");
			}

			packageName = packageNameAttr.Value;

			if (procNameAttr == null)
			{
				procedureName = method.Name;
			}
			else
			{
				procedureName = procNameAttr.Value;
			}

			return packageName + "." + procedureName;
		}

		/// <summary>
		/// A helper method to grab a specific type of custom attributes from a MethodBase object.
		/// </summary>
		private static T GetCustomAttribute<T>(MethodBase method) where T : class
		{
			object[] customAttrs = method.GetCustomAttributes(typeof(T), true);

			if (customAttrs != null &&
				customAttrs.Length > 0)
			{
				return (T)customAttrs[0];
			}
			return null;
		}

		#endregion
	}
}
