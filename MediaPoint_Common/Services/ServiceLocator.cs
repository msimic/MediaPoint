using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.MVVM.Services
{
	public interface IService
	{
		string Name { get; }
	}

	public class ServiceLocator
	{
		private static Dictionary<Type, IService> _services = null;

		static ServiceLocator()
		{
			_services = new Dictionary<Type, IService>();
		}

		public static void RegisterOverrideService<T>(T service) where T : class, IService
		{
			if (_services.ContainsKey(typeof(T))) {
				_services.Remove(typeof(T));
			}
			_services.Add(typeof(T), service);
		}

		public static void RegisterService<T>(T service) where T : class, IService
		{
            System.Diagnostics.Debug.Assert(_services.ContainsKey(typeof(T)) == false, "Service already exists");

			_services.Add(typeof(T), service);
		}

		public static T GetService<T>() where T : class
		{
			if (_services.ContainsKey(typeof(T)) == true)
			{
				return _services[typeof(T)] as T;
			}

			return null;
		}

        public static void UnregisterService<T>() where T : class, IService
        {
            if(_services.ContainsKey(typeof(T)) == true)
            {
                _services.Remove(typeof (T));
            }
        }
	}
}