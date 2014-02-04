using MediaPoint.MVVM.Services;

namespace MediaPoint.VM.ViewInterfaces
{
	public interface IStyleLoader : IService
	{
		string LoadStyle(string name);
	}
}
