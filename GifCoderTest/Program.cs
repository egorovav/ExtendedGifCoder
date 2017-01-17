using Ja2DataImage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace GifCoderTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var _gifFileName = "truck_crush.gif";

			var _gifCoder = new GifBitmapCoder();
			var _input = new FileStream(_gifFileName, FileMode.Open);

			_gifCoder.Load(_input);

			_input.Close();

			for (int i = 0; i < _gifCoder.Frames.Count; i++) 
			{
				_gifCoder.Frames[i].Delay = i % 21;
			}

			var _outputFileName = "truck_crush_slow_mo.gif";
			var _output = new FileStream(_outputFileName, FileMode.Create);

			_gifCoder.Save(_output);

			_output.Close();
		}
	}
}
