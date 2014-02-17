using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPoint.MVVM;

namespace MediaPoint.VM
{
    public class Track : ViewModel
    {
        public string Title
        {
            get { return GetValue(() => Title); }
            set { SetValue(() => Title, value); }
        }

        public bool IsSelected
        {
            get { return GetValue(() => IsSelected); }
            set { SetValue(() => IsSelected, value); }
        }

        public int Position
        {
            get { return GetValue(() => Position); }
            set { SetValue(() => Position, value); }
        }

        public bool IsPlaying
        {
            get { return GetValue(() => IsPlaying); }
            set { SetValue(() => IsPlaying, value); }
        }

        public Uri Uri
        {
            get { return GetValue(() => Uri); }
            set { SetValue(() => Uri, value); }
        }
    }
}
