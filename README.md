# ExtendedGifCoder
This project provides extended capabilities for creating the GIF files. Such as seting frame delay, frame оffset, frame transparency color.

For example, such simple code appends to GIF file some slow mo effect.

                    var _gifFileName = "truck_crush.gif";
		var _gifCoder = new GifBitmapCoder();
		var _input = new FileStream(_gifFileName, FileMode.Open);
		_gifCoder.Load(_input);
		_input.Close();

		for (int i = 0; i < _gifCoder.Frames.Count; i++) 
			_gifCoder.Frames[i].Delay = i % 22;

		var _outputFileName = "truck_crush_slow_mo.gif";
		var _output = new FileStream(_outputFileName, FileMode.Create);
		_gifCoder.Save(_output);
		_output.Close();
 
