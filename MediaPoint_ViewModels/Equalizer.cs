using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPoint.MVVM;
using MediaPoint.MVVM.Services;
using MediaPoint.VM.ViewInterfaces;

namespace MediaPoint.VM
{
    public class Equalizer : ViewModel
    {
        private Dictionary<int, int> _values = new Dictionary<int, int>() { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 }, { 8, 0 }, { 9, 0 }, { 10, 0}};
        private int[] _frequencyPerBand = { 0, 3, 9, 16, 29, 48, 100, 141, 280, 559, 1024 };

        public int this[int i]
        {
            get
            {
                return _values[i];
            }
            set
            {
                _values[i] = value;
                OnEqualizerChanged(i, value);
                OnPropertyChanged("Item[]");
            }
        }

        public Equalizer()
        {
            AllChannels = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        }

        public int Channel
        {
            get { return GetValue(() => Channel); }
            set { SetValue(() => Channel, value); }
        }

        public int[] AllChannels
        {
            get { return GetValue(() => AllChannels); }
            set { SetValue(() => AllChannels, value); }
        }

        void OnEqualizerChanged(int index, int value)
        {
            var eq = ServiceLocator.GetService<IEqualizer>();
            eq.SetBand(-1, _frequencyPerBand[index], (sbyte)value);
        }
    }
}
