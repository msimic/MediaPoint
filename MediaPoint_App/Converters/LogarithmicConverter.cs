using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MediaPoint.Converters
{
	public class LogarithmicConverter : IValueConverter
	{
		int VOL_MAX = 100;  // volume 0..100
		int VOL_DLT = 40000; // 2000 = like ms mixer

		int GetDXVolume(double Volume)
		{
			return (int)Math.Round(Math.Pow(10, Volume / VOL_DLT) * VOL_MAX);
		}

		int SetDXVolume(double Volume)
		{
			int result;

			if (Volume <= 0)
				result = -10000;
			else
				result = (int)Math.Round(Math.Log10(Volume / VOL_MAX) * VOL_DLT);
			
			if (result < -10000)
				result = -10000;

			return result;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			double ret = (double)value;

			if (parameter.ToString() != "")
			{
				VOL_DLT = int.Parse(parameter.ToString());
			}

			if (ret != 0)
				ret = (double)GetDXVolume((1 - ret) * -10000) / 100;

			//ret = Math.Pow(10, ((double)value / 10));
			return ret;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			double ret = (double)value;

			if (parameter.ToString() != "")
			{
				VOL_DLT = int.Parse(parameter.ToString());
			}

			if (ret != 0)
				ret = ((double)SetDXVolume(ret * 100) + 10000) / 10000;
			
			return ret;
		}
	}
}
