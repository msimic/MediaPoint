using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using MediaPoint.App.Properties;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Reflection;
using MediaPoint.App.Extensions;
using System.Security.AccessControl;
using MediaPoint.VM.ViewInterfaces;

namespace MediaPoint.App.Themes
{
	public class StyleLoader : IStyleLoader
	{
		private readonly ResourceDictionary _dic;

		public StyleLoader(ResourceDictionary dic)
		{
			_dic = dic;
		}

		public static string CurrentStyleFolder
		{
			get;
			set;
		}

		public static readonly string THEME_PREFIX = "Theme";

		public static bool HasPermission(string path, FileSystemRights permission)
		{
			bool isFile = true;
			// get the file attributes for file or directory
			FileAttributes attr = File.GetAttributes(path);

			//detect whether its a directory or file
			if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
				isFile = false;

			var Allow = false;
			var Deny = false;

			AuthorizationRuleCollection accessRules;

			if (isFile) {
				var accessControlList = File.GetAccessControl(path);
				if (accessControlList == null)
					return false;
				accessRules = accessControlList.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
			}
			else
			{
				var accessControlList = Directory.GetAccessControl(path);
				if (accessControlList == null)
					return false;
				accessRules = accessControlList.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
			}

			if (accessRules == null)
				return false;

			foreach (FileSystemAccessRule rule in accessRules)
			{
				if ((permission & rule.FileSystemRights) != permission) continue;

				if (rule.AccessControlType == AccessControlType.Allow)
					Allow = true;
				else if (rule.AccessControlType == AccessControlType.Deny)
					Deny = true;
			}

			return Allow && !Deny;
		}

		public static string GetName(object obj)
		{
			// If not, try reflection to get the value of a Name property.
			try { return (string)obj.GetType().GetProperty("Name").GetValue(obj, null); }
			catch
			{
				// Last of all, try reflection to get the value of a Name field.
				try { return (string)obj.GetType().GetField("Name").GetValue(obj); }
				catch { return null; }
			}
		}

		public class ThemeException : Exception
		{
			public ThemeException(string message) : base(message) { }
		}

		public string LoadStyle(string name)
		{
			if (CurrentStyleFolder != name)
				return LoadStyles("MediaPoint", name, _dic);
			else
				return CurrentStyleFolder;
		}

		public static string[] GetAllStyles()
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MediaPoint");
			string fileName = Path.Combine(path, @"Themes\");

            List<string> dirs = new List<string>();


			if (Directory.Exists(fileName) && HasPermission(fileName, FileSystemRights.Read))
			{
                dirs.AddRange(Directory.GetDirectories(fileName).Select(d => new DirectoryInfo(d).Name).ToArray());
            }
            
			path = Assembly.GetExecutingAssembly().GetPath();
			fileName = Path.Combine(path, @"Themes\");

			if (Directory.Exists(fileName) && HasPermission(fileName, FileSystemRights.Read))
			{
                dirs.AddRange(Directory.GetDirectories(fileName).Select(d => new DirectoryInfo(d).Name).ToArray());
			}

            return dirs.Distinct().ToArray();
		}

		public string LoadStyles(string appname, string styleName, ResourceDictionary appDic)
		{

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appname);
			string fileName = Path.Combine(path, @"Themes\" + styleName + @"\style.xaml");

			if (!File.Exists(fileName) || !HasPermission(fileName, FileSystemRights.Read))
			{
				path = Assembly.GetExecutingAssembly().GetPath();
				fileName = Path.Combine(path, @"Themes\" + styleName + @"\style.xaml");

				if (!File.Exists(fileName) || !HasPermission(fileName, FileSystemRights.Read))
				{
					throw new ThemeException("Style: " + styleName + " does not exist or cannot be accessed.");
				}
			}

			try
			{
				var themeName = "";
				using (FileStream fs = new FileStream(fileName, FileMode.Open))
				{
					ResourceDictionary dic = (ResourceDictionary)XamlReader.Load(fs);
					if (!dic.Contains(THEME_PREFIX))
						throw new ThemeException("A theme for MediaPoint needs to have a TextBlock marked with x:Name=\"Theme\" while the text should be the name of the theme.");
					themeName = ((TextBlock)dic[THEME_PREFIX]).Text;
					appDic.BeginInit();
					var theme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(rd =>
					                                                                   	{
					                                                                   		return rd.Contains(THEME_PREFIX);
					                                                                   	});
					if (theme != null) appDic.MergedDictionaries.Remove(theme);
					appDic.MergedDictionaries.Add(dic);
					appDic.EndInit();
					CurrentStyleFolder = Path.Combine(path, @"Themes\" + styleName);
					return themeName;
				}
			}
			catch (Exception ex)
			{
				throw new ThemeException("Style: " + styleName + " contains an invalid WPF ResourceDictionary.\r\n\r\n" + ex.Message + "\r\n\r\n" + ex.StackTrace);
			}
		}

		public string Name
		{
			get { return "StyleLoader"; }
		}
	}
}
