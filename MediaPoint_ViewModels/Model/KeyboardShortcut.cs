using MediaPoint.VM.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace MediaPoint.VM.Model
{
    public class KeyboardShortcut
    {
        public bool Control { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public Key Key { get; set; }
        public PlayerActionEnum ActionId { get; set; }
        public bool External { get; set; }

        public string AsString
        {
            get
            {
                string ret = "";
                if (Control) ret += "CTRL+";
                if (Shift) ret += "SHIFT+";
                if (Alt) ret += "ALT+";
                ret += Key.ToString();
                return ret;
            }
        }

        public bool Execute(IEnumerable<PlayerAction> actions) 
        {
            var playerAction = actions.FirstOrDefault(pa => pa.ActionId == ActionId);
            if (playerAction != null)
            {
                playerAction.Action();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
