using MediaPoint.MVVM.Services;
using MediaPoint.VM.Model;

namespace MediaPoint.VM.ViewInterfaces
{
	public interface IStyleLoader : IService
	{
		ThemeInfo LoadStyle(ThemeInfo name);
	}
}
