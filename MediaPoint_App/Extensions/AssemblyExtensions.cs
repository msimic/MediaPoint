using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace MediaPoint.App.Extensions
{
	public static class AssemblyExtensions
	{
		/// <summary>
		/// Gets the assembly title.
		/// </summary>
		/// <value>The assembly title.</value>
		public static string GetAssemblyTitle(this Assembly ass)
		{

				// Get all Title attributes on this assembly
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				// If there is at least one Title attribute
				if (attributes.Length > 0)
				{
					// Select the first one
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					// If it is not an empty string, return it
					if (titleAttribute.Title != "")
						return titleAttribute.Title;
				}
				// If there was no Title attribute, or if the Title attribute was the empty string, return the .exe name
				return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
			
		}

		/// <summary>
		/// Returns the directory where the assembl yi slocated
		/// </summary>
		/// <param name="ass">Assemly</param>
		/// <returns>Assembly directory</returns>
		public static string GetPath(this Assembly ass)
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}
	}
}
