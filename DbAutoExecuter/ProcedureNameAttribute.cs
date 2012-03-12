using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbAutoExecuter
{
	/// <summary>
	/// An attribute to allow a method to be associated with a stored procedure
	/// for a db store proc call.  Using the attribute will override the method name
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class ProcedureNameAttribute :DbEntityNameAttribute
	{
		/// <summary>
		/// Creates a new instance
		/// </summary>
		public ProcedureNameAttribute(string procedureName) : base(procedureName) { }
	}
}
