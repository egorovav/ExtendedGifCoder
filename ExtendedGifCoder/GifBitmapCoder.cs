using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExtendedGifEncoder
{
	public class GifBitmapCoder
	{
		private const byte Terminator = 0x3B;

		public GifBitmapCoder()
		{

		}

		public GifBitmapCoder(BitmapPalette aGlobalPalette)
		{
			this.FGlobalPalette = aGlobalPalette;
		}

		public List<GifBitmapFrame> Frames = new List<GifBitmapFrame>();
		private BitmapPalette FGlobalPalette;
		public List<GifExtension> Extensions = new List<GifExtension>();

		private byte FBackgroundColorIndex;

		public void AddFrame(GifBitmapFrame aFrame)
		{
			this.Frames.Add(aFrame);
		}

		private void CalculateScreenSize()
		{
			int _minOffsetX = Int32.MaxValue;
			int _minOffsetY = Int32.MaxValue;
			var maxBottomShifted = this.Frames[0];
			var maxRightShifted = this.Frames[0];

			foreach (var _frame in this.Frames)
			{
				_minOffsetX = Math.Min(_minOffsetX, _frame.OffsetX);
				_minOffsetY = Math.Min(_minOffsetY, _frame.OffsetY);

				if (maxRightShifted.OffsetX + maxRightShifted.Width < _frame.Width + _frame.OffsetX)
					maxRightShifted = _frame;

				if (maxBottomShifted.OffsetX + maxBottomShifted.Height < _frame.Height + _frame.OffsetY)
					maxBottomShifted = _frame;
			}

			this.FScreenWidth = (UInt16)(maxRightShifted.Width + maxRightShifted.OffsetX - _minOffsetX);
			this.FScreenHeight = (UInt16)(maxBottomShifted.Height + maxBottomShifted.OffsetY - _minOffsetY);
		}

		private UInt16 FScreenWidth;
		private UInt16 FScreenHeight;

		private int Flags
		{
			get
			{
				if (this.Frames.Count == 0)
					return 0;

				int _bitsPerPixel = this.Frames[0].BitsPerPixel - 1;
				int _flags = 0;
				_flags |= _bitsPerPixel << 4;

				if (this.FGlobalPalette != null)
				{
					_flags |= 0x80;
					_flags |= _bitsPerPixel;
				}
				return _flags;
			}
		}

		const string FormatId = "GIF89a";

		public void Save(Stream aStream)
		{
			this.CalculateScreenSize();

			var _formatIdData = Encoding.ASCII.GetBytes(FormatId);
			aStream.Write(_formatIdData, 0, _formatIdData.Length);

			var _bw = new BinaryWriter(aStream);
			_bw.Write(this.FScreenWidth);
			_bw.Write(this.FScreenHeight);
			aStream.WriteByte((byte)this.Flags);
			aStream.WriteByte(this.FBackgroundColorIndex);
			aStream.WriteByte(0);								// pixel shape

			if (this.FGlobalPalette != null)
			{
				foreach (var _c in this.FGlobalPalette.Colors)
				{
					_bw.Write(_c.R);
					_bw.Write(_c.G);
					_bw.Write(_c.B);
				}
			}

			foreach (var _extension in this.Extensions)
				_extension.Save(aStream);

			foreach (var _frame in this.Frames)
				_frame.Save(aStream);

			aStream.WriteByte(Terminator); 
		}

		public void Load(Stream aStream)
		{
			var _decoder = new GifBitmapDecoder(aStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

			aStream.Position = 0;
			var _formatIdData = new byte[FormatId.Length];
			aStream.Read(_formatIdData, 0, _formatIdData.Length);
			var _br = new BinaryReader(aStream);
			this.FScreenWidth = _br.ReadUInt16();
			this.FScreenHeight = _br.ReadUInt16();
			int _flags = _br.ReadByte();
								
			this.FBackgroundColorIndex = _br.ReadByte();
			aStream.ReadByte();								// skip pixel shape

			if ((_flags & 0x80) != 0)
			{
				int _bitsPerPixel = (_flags & 0x07) + 1;
				int _paletteLength = (int)Math.Pow(2, _bitsPerPixel);
				var _colors = new List<Color>(_paletteLength);

				for(int i = 0; i < _paletteLength; i++)
				{
					var _c = new Color();
					_c.R = _br.ReadByte();
					_c.G = _br.ReadByte();
					_c.B = _br.ReadByte();
					_colors.Add(_c);
				}

				this.FGlobalPalette = new BitmapPalette(_colors);
			}

			GifExtension _extension = null;
			while((_extension = GifExtension.Load(aStream)) != null && 
				_extension.ExtensionType != ExtensionType.ImageBehaviorExtension)
			{
				this.Extensions.Add(_extension);
			}

			for (int i = 0; i < _decoder.Frames.Count; i++)
			{
				var _currentFrame = new GifBitmapFrame(_decoder.Frames[i]);
				_currentFrame.BehaviorExtention = _extension;
				_extension = _currentFrame.Load(aStream);
				this.Frames.Add(_currentFrame);
			}

		}
	}
}
