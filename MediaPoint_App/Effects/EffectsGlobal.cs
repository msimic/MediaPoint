using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MediaPoint.App.Effects
{
	public static class EffectsGlobal
	{
		private static string AssemblyShortName
		{
			get
			{
				if (_assemblyShortName == null)
				{
					Assembly a = typeof(WaterEffect).Assembly;

					// Pull out the short name.
					_assemblyShortName = a.ToString().Split(',')[0];
				}

				return _assemblyShortName;
			}
		}

		private static string _assemblyShortName;

		public static Uri MakePackUri(string relativeFile)
		{

			StringBuilder uriString = new StringBuilder(); ;
			uriString.Append("/" + AssemblyShortName + ";component/" + relativeFile);
			return new Uri(uriString.ToString(), UriKind.RelativeOrAbsolute);
		}
	}
}
