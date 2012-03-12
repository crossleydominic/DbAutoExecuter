using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbAutoExecuter
{
	/// <summary>
	/// A marker attribute for db method calls to notify that
	/// the parameter type should be omitted from parameter names
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class OmitParameterTypeAttribute : Attribute
	{
	}
}
