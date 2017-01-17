using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ja2DataImage
{
	public enum ExtensionType : byte
	{
		CommentExtension = 0xFE,
		ApplicationExtension = 0xFF,
		ImageBehaviorExtension = 0xF9
	}

	public class GifExtension
	{
		public const byte Separator = 0x21;

		public GifExtension()
		{

		}

		public GifExtension(ExtensionType aType)
		{
			this.FExtensionType = aType;
		}

		public GifExtension(ExtensionType aType, byte[] aData)
			: this(aType)
		{
			this.FData = aData;
		}

		protected ExtensionType FExtensionType;
		public ExtensionType ExtensionType
		{
			get { return this.FExtensionType; }
		}

		// only single data block supported
		protected byte[] FData;
		public byte[] Data
		{
			get { return this.FData; }
		}

		public virtual void Save(Stream aStream)
		{
			aStream.WriteByte(Separator);
			aStream.WriteByte((byte)this.FExtensionType);
			aStream.WriteByte((byte)this.FData.Length);
			aStream.Write(this.FData, 0, this.FData.Length);
			aStream.WriteByte(0);
		}

		protected virtual void LoadData(Stream aStream)
		{
			int _dataLength = aStream.ReadByte();
			while (_dataLength != 0)
			{
				if (this.FData == null)
				{
					this.FData = new byte[_dataLength];
					aStream.Read(this.FData, 0, this.FData.Length);
				}
				else
				{
					var _buff = new byte[this.FData.Length];
					Array.Copy(this.FData, _buff, _buff.Length);
					this.FData = new byte[this.FData.Length + _dataLength];
					Array.Copy(_buff, this.FData, _buff.Length);
					aStream.Read(this.FData, _buff.Length, _dataLength);
				}

				_dataLength = aStream.ReadByte();
			}
		}

		public static GifExtension Load(Stream aStream)
		{
			var _br = new BinaryReader(aStream);
			if (_br.ReadByte() != Separator)
				return null;

			var _extensionType = (ExtensionType)_br.ReadByte();
			GifExtension _extension = null;
			switch(_extensionType)
			{
				case ExtensionType.ApplicationExtension :
					{
						_extension = new GifApplicationExtension();
						break;
					}
				case ExtensionType.CommentExtension :
					{
						_extension = new GifCommentExtension();
						break;
					}
				case ExtensionType.ImageBehaviorExtension :
					{
						_extension = new GifExtension(_extensionType);
						break;
					}
			}
			_extension.FExtensionType = _extensionType;
			_extension.LoadData(aStream);
			return _extension;
		}
	}

	public class GifApplicationExtension : GifExtension
	{
		public GifApplicationExtension()
		{

		}

		public GifApplicationExtension(string aApplicationId, byte[] aData)
			: base(ExtensionType.ApplicationExtension)
		{
			this.FApplicationId = aApplicationId;
			this.FData = aData;
		}

		private string FApplicationId;
		public string ApplicationId
		{
			get { return this.FApplicationId; }
		}

		public override void Save(Stream aStream)
		{
			aStream.WriteByte(Separator);
			aStream.WriteByte((byte)this.FExtensionType);
			var _appIdData = Encoding.ASCII.GetBytes(this.FApplicationId);
			aStream.WriteByte((byte)(_appIdData.Length));
			aStream.Write(_appIdData, 0, _appIdData.Length);
			aStream.WriteByte((byte)this.FData.Length);
			aStream.Write(this.FData, 0, this.FData.Length);
			aStream.WriteByte(0);
		}

		protected override void LoadData(Stream aStream)
		{
			int _appIdDataLength = aStream.ReadByte();
			var _appIdData = new byte[_appIdDataLength];
			aStream.Read(_appIdData, 0, _appIdData.Length);
			this.FApplicationId = Encoding.ASCII.GetString(_appIdData);
			base.LoadData(aStream);
			
		}
	}

	public class GifCommentExtension : GifExtension
	{
		public GifCommentExtension()
		{

		}

		public GifCommentExtension(string aComment)
			: base(ExtensionType.CommentExtension)
		{
			this.FData = Encoding.ASCII.GetBytes(aComment);
		}

		public string Comment
		{
			get { return Encoding.ASCII.GetString(this.FData);  }
		}
	}
}
