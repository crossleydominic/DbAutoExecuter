using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbAutoExecuter
{
	/// <summary>
	/// A base class that allows an attribute to be associated with some kind of db entity name
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited=true)]
	public abstract class DbEntityNameAttribute : Attribute
	{
		private string _value;

		/// <summary>
		/// Creates a new instance of the attribute
		/// </summary>
		public DbEntityNameAttribute(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException("DbEntityName value cannot be null", "value");
			}

			_value = value;
		}

		/// <summary>
		/// Gets the db entity name value
		/// </summary>
		public string Value
		{
			get { return _value; }
		}
	}
}
