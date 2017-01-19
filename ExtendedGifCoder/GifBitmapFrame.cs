using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExtendedGifEncoder
{
	public enum GifFrameDisposalMethod
	{
		None = 0,
		NotDispose = 1,
		RestoreToBackgroundColor = 2,
		RestoreToPrevious = 3,
		UndefinedDisposalMethod4 = 4,
		UndefinedDisposalMethod5 = 5,
		UndefinedDisposalMethod6 = 6,
		UndefinedDisposalMethod7 = 7
	}

	public class GifBitmapFrame
	{
		public const byte Separator = 0x2C;

		public GifBitmapFrame(BitmapFrame aFrame)
		{
			this.FFrame = aFrame;
		}

		public GifBitmapFrame(BitmapFrame aFrame, UInt16 aOffsetX, UInt16 aOffsetY)
			: this(aFrame)
		{
			this.FOffsetX = aOffsetX;
			this.FOffsetY = aOffsetY;
		}

		private BitmapFrame FFrame;
		public BitmapFrame Frame
		{
			get { return this.FFrame; }
		}

		private UInt16 FOffsetX;
		public UInt16 OffsetX
		{
			get { return this.FOffsetX; }
		}

		private UInt16 FOffsetY;
		public UInt16 OffsetY
		{
			get { return this.FOffsetY; }
		}

		public List<GifExtension> Extensions = new List<GifExtension>();

		private bool FUseGlobalPalette;
		public bool UseGlobalPalette
		{
			get { return FUseGlobalPalette; }
			set { this.FUseGlobalPalette = value; }
		}

		private bool FUseTransparency;
		public bool UseTransparency
		{
			get { return this.FUseTransparency; }
			set { this.FUseTransparency = value; }
		}

		private byte FTransparentColorIndex;
		public byte TransparentColorIndex
		{
			get { return this.FTransparentColorIndex; }
			set { this.FTransparentColorIndex = value; }
		}

		private UInt16 FDelay;
		public int Delay
		{
			get { return this.FDelay; }
			set { this.FDelay = (UInt16)value; }
		}

		private GifFrameDisposalMethod FDisposalMethod;
		public GifFrameDisposalMethod DisposalMethod
		{
			get { return FDisposalMethod; }
			set { this.FDisposalMethod = value; }
		}

		public GifExtension BehaviorExtention
		{
			get
			{
				var _data = new byte[4] { 
					this.BehaviorFlags, (byte)this.Delay, (byte)(this.Delay >> 8), this.TransparentColorIndex };
				var _behavior = new GifExtension(ExtensionType.ImageBehaviorExtension, _data);
				return _behavior;
			}

			set
			{
				if (value == null)
					return;

				this.BehaviorFlags = value.Data[0];
				this.Delay = BitConverter.ToUInt16(new byte[] { value.Data[1], value.Data[2]}, 0);
				this.TransparentColorIndex = value.Data[3];
			}
		}

		private byte BehaviorFlags
		{
			get
			{
				int _flags = 0;

				int _disposalMethod = (int)this.FDisposalMethod << 2;
				_flags |= _disposalMethod;
				if (this.FUseTransparency)
					_flags |= 0x01;

				return (byte)_flags;
			}

			set
			{
				this.DisposalMethod = (GifFrameDisposalMethod)(byte)((value & 0x1C) >> 2);
				this.FUseTransparency = (value & 0x01) != 0;
			}
		}

		private byte ImageFlags
		{
			get
			{
				int _flags = this.BitsPerPixel - 1;
				if (!this.UseGlobalPalette)
					_flags |= 0x80;
				return (byte)_flags;
			}

			set
			{
				this.UseGlobalPalette = (value & 0x80) == 0;
			}
		}

		public int Width
		{
			get { return this.FFrame.PixelWidth; }
		}

		public int Height
		{
			get { return this.FFrame.PixelHeight; }
		}

		public int BitsPerPixel
		{
			get { return this.FFrame.Format.BitsPerPixel; }
		}

		public void Save(Stream aStream)
		{
			this.BehaviorExtention.Save(aStream);

			foreach (var _extention in this.Extensions)
				_extention.Save(aStream);

			aStream.WriteByte(Separator);

			var _bw = new BinaryWriter(aStream);
			_bw.Write(this.FOffsetX);
			_bw.Write(this.FOffsetY);
			_bw.Write((UInt16)this.Width);
			_bw.Write((UInt16)this.Height);
			_bw.Write(this.ImageFlags);

			if (!this.UseGlobalPalette)
			{
				foreach(var _c in this.FFrame.Palette.Colors)
				{
					_bw.Write(_c.R);
					_bw.Write(_c.G);
					_bw.Write(_c.B);
				}
			}

			var _gifEncoder = new GifBitmapEncoder();
			_gifEncoder.Frames.Add(this.FFrame);

			var _buff = new MemoryStream();
			_gifEncoder.Save(_buff);

			// find image data start position
			var _fileHeaderSize = 13;
			_buff.Seek(_fileHeaderSize, SeekOrigin.Begin);

			GifExtension _ext = null;
			while ((_ext = GifExtension.Load(_buff)) != null) ;
			var _imageDataStart = (int)_buff.Position;

			var _data = _buff.ToArray();
			_buff.Close();

			var _imageHeaderSize = 9;
			int _imageDataStartPosition = _imageDataStart + _imageHeaderSize + this.FFrame.Palette.Colors.Count * 3;

			_bw.Write(_data, _imageDataStartPosition, _data.Length - _imageDataStartPosition - 1);
		}

		public GifExtension Load(Stream aStream)
		{
			GifExtension _extension = null;
			while ((_extension = GifExtension.Load(aStream)) != null)
			{
				this.Extensions.Add(_extension);
			}

			var _br = new BinaryReader(aStream);
			this.FOffsetX = _br.ReadUInt16();
			this.FOffsetY = _br.ReadUInt16();
			_br.ReadUInt16();					// don't store width, get from frame
			_br.ReadUInt16();					// don't store heiht, get from frame
			this.ImageFlags = _br.ReadByte();
			// palette stored in frame
			if (!this.UseGlobalPalette)
				aStream.Seek(this.FFrame.Palette.Colors.Count * 3, SeekOrigin.Current);

			_br.ReadByte();						// skip code size

			// image data stored in frame
			var _dataLength = _br.ReadByte();
 			while(_dataLength != 0)
			{
				aStream.Seek(_dataLength, SeekOrigin.Current);
				_dataLength = _br.ReadByte();
			}

			while ((_extension = GifExtension.Load(aStream)) != null &&
				_extension.ExtensionType != ExtensionType.ImageBehaviorExtension)
			{
				this.Extensions.Add(_extension);
			}

			return _extension;
		}

		// Trim background pixels
		public void Trim()
		{
			Color bc = this.Frame.Palette.Colors[0];
			int _top = -1;
			int _left = this.Frame.PixelWidth;
			byte[] imageData = new byte[this.Frame.PixelWidth * this.Frame.PixelHeight];
			this.Frame.CopyPixels(imageData, this.Frame.PixelWidth, 0);

			for (int i = 0; i < this.Frame.PixelHeight; i++)
			{
				for (int j = 0; j < this.Frame.PixelWidth; j++)
				{
					byte colorIndex = imageData[i * this.Frame.PixelWidth + j];
					Color c = this.Frame.Palette.Colors[colorIndex];
					if (c != bc)
					{
						if (_top < 0)
							_top = i;

						if (_left > j)
							_left = j;

						continue;
					}
				}
			}

			int _bottom = this.Frame.PixelHeight;
			int _right = 0;

			for (int i = this.Frame.PixelHeight - 1; i > 0; i--)
			{
				for (int j = this.Frame.PixelWidth - 1; j > 0; j--)
				{
					byte colorIndex = imageData[i * this.Frame.PixelWidth + j];
					Color c = this.Frame.Palette.Colors[colorIndex];
					if (c != bc)
					{
						if (_bottom == this.Frame.PixelHeight)
							_bottom = i;

						if (_right < j)
							_right = j;
					}
				}
			}

			int _width = _right - _left;
			int _height = _bottom - _top;

			this.FOffsetX += (ushort)(_left - this.Frame.Width / 2);
			this.FOffsetY += (ushort)(_top - this.Frame.Height / 2);

			byte[] trimedData = new byte[_width * _height];
			this.Frame.CopyPixels(new Int32Rect(_left, _top, _width, _height), trimedData, _width, 0);
			var source = BitmapFrame.Create(
				_width, _height, 96, 96, PixelFormats.Indexed8, this.Frame.Palette, trimedData, _width);
			this.FFrame = BitmapFrame.Create(source);
		}
	}
}
