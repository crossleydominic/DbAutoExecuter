using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbAutoExecuter
{
	/// <summary>
	/// An attribute to allow a method to be associated with a package/schema
	/// for a db store proc call.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class PackageNameAttribute : DbEntityNameAttribute
	{
		/// <summary>
		/// Creates a new PackageNameInstance
		/// </summary>
		public PackageNameAttribute(string packageName) : base(packageName) { }
	}
}
