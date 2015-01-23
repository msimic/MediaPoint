using MediaPoint.MVVM.Services;
using MediaPoint.VM.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.VM.Services.Model
{
    public interface IActionExecutor : IService
    {
        void ExecuteAction(PlayerActionEnum action);
    }
}
