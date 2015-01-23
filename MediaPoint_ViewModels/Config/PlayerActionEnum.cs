using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.VM.Config
{
    public enum PlayerActionEnum
    {
        Framestep,
        PlayPause,
        SubsDelayForward,
        SubsDelayBackward,
        SeekForward,
        SeekBackward,
        IncreaseSubsSize,
        DecreaseSubsSize,
        IncreaseVolume,
        DecreaseVolume,
        NextTrack,
        PreviousTrack,
        ToggleFullscreen,
        ExitFullscreen,
        SaveScreenshot,
        IncreasePanScan,
        DecreasePanScan
    }
}
