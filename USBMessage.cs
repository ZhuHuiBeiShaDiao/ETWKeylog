using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace logger
{

	class USBMessage
	{
		public class UrbHeader
		{
			public ushort Length { get; set; }
			public ushort Function { get; set; }
			public USBDStatus Status { get; set; }
			public UInt64 UsbdDeviceHandle { get; set; }
			public UInt64 UsbdFlags { get; set; }
		}

		public class BulkOrInterruptDescriptor
		{
			public UInt64 PipeHandle { get; set; }
			public uint TransferFlags { get; set; }
			public ulong TransferBufferLength { get; set; }
			public UInt64 TransferBuffer { get; set; }
			public UInt64 TransferBufferMDL { get; set; }
			public UInt64 UrbLink { get; set; }
			public byte[] Hca { get; set; }
			public byte[] Payload { get; set; }
		}

		public int DataLength { get; set; }

		public UrbHeader Header { get; set; } = new UrbHeader();

		public BulkOrInterruptDescriptor BulkOrInterrupt { get; set; } = new BulkOrInterruptDescriptor();

		public USBMessage(byte[] data)
		{
			if(data.Length >= 24 )
				UnpackMessage(data);
			else 
				throw new InvalidDataException("USB packet header is malformed");
		}

		private void UnpackMessage(byte[] data)
		{
			DataLength = data.Length;

			int streamPtr = 0;

			Header.Length = BitConverter.ToUInt16(data, streamPtr);
			streamPtr += 2;

			Header.Function = BitConverter.ToUInt16(data, streamPtr);
			streamPtr += 2;

			Header.Status = (USBDStatus)BitConverter.ToUInt32(data, streamPtr);
			streamPtr += 4;

			Header.UsbdDeviceHandle = BitConverter.ToUInt64(data, streamPtr);
			streamPtr += 8;

			Header.UsbdFlags = BitConverter.ToUInt64(data, streamPtr);
			streamPtr += 8;

			if (data.Length < streamPtr + 8) return;
			BulkOrInterrupt.PipeHandle = BitConverter.ToUInt64(data, streamPtr);
			streamPtr += 8;

			if (data.Length < streamPtr + 4) return;
			BulkOrInterrupt.TransferFlags = BitConverter.ToUInt32(data, streamPtr);
			streamPtr += 4;

			if (data.Length < streamPtr + 4) return;
			BulkOrInterrupt.TransferBufferLength = BitConverter.ToUInt32(data, streamPtr);
			streamPtr += 4;

			if (data.Length < streamPtr + 8) return;
			BulkOrInterrupt.TransferBuffer = BitConverter.ToUInt64(data, streamPtr);
			streamPtr += 8;

			if (data.Length < streamPtr + 8) return;
			BulkOrInterrupt.TransferBufferMDL = BitConverter.ToUInt64(data, streamPtr);
			streamPtr += 8;

			if (data.Length < streamPtr + 8) return;
			BulkOrInterrupt.UrbLink = BitConverter.ToUInt64(data, streamPtr);
			streamPtr += 8;

			if (data.Length-1 < streamPtr + 64) return;
			BulkOrInterrupt.Hca = new byte[64];
			for (var i = 1; i <= 64; ++i)
			{
				BulkOrInterrupt.Hca[i-1] = data[streamPtr + i];
			}
			streamPtr += 64;

			if (data.Length-1 < streamPtr + 8) return;
			BulkOrInterrupt.Payload = new byte[8];
			for (var i = 1; i <= 8; ++i)
			{
				BulkOrInterrupt.Payload[i-1] = data[streamPtr + i];
			}
		}



	}
}
