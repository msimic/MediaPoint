using MediaPoint.VM.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.VM.Model
{
    public class PlayerAction
    {
        public PlayerActionEnum ActionId { get; set; }
        public Action Action { get; set; }
        public KeyboardShortcut Shortcut { get; set; }
        public KeyboardShortcut SystemShortcut { get; set; }
    }
}
