using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DirectShowLib;

namespace MediaPoint.Common.MediaFoundation
{
	public class StreamPin : BasePin, IAsyncReader
	{
		private readonly MemoryStream _stream;

		public StreamPin(MemoryStream m, BaseFilter filter, string name) : base(PinDirection.Output, filter)
		{
			_stream = m;
			_Name = name;
		}

		public override unsafe HRESULT OnReceiveConnection(IPin pReceivePin, AMMediaType pmt)
		{
			_ConnectedPin = pReceivePin;
			_MediaType = pmt;
			
			return HRESULT.S_OK;
		}

		public override unsafe HRESULT OnDisconnect()
		{
			_ConnectedPin = null;
			_MediaType = null;
			return HRESULT.S_OK;
		}

		public override int BeginFlush()
		{
			return 0;
		}

        public override int EndFlush()
		{
			_stream.Flush();
			return 0;
		}

		public int Length(out long pTotal, out long pAvailable)
		{
			pTotal = _stream.Length;
			pAvailable = _stream.Length - _stream.Position;
			return 0;
		}

		public int Request(IMediaSample pSample, IntPtr dwUser)
		{
			return 1;
		}

		public int RequestAllocator(IMemAllocator pPreferred, AllocatorProperties pProps, out IMemAllocator ppActual)
		{
			ppActual = null;
			return 1;
		}

		public int SyncRead(long llPosition, int lLength, IntPtr pBuffer)
		{
			byte[] buff = new byte[lLength];
			_stream.Seek(llPosition, SeekOrigin.Begin);
			_stream.Read(buff, 0, lLength);
			//Marshal.ReAllocHGlobal(pBuffer,,lLength);
			Marshal.Copy(buff, 0, pBuffer, buff.Length);
			return 0;
		}

		public int SyncReadAligned(IMediaSample pSample)
		{
			return 1;
		}

		public int WaitForNext(int dwTimeout, out IMediaSample ppSample, out IntPtr pdwUser)
		{
			ppSample = null;
			pdwUser = IntPtr.Zero;
			return 1;
		}
	}

	[ComVisible(true)]
	[Guid("00000228-5733-4c70-9192-13057E2BAF00")]
	public class StreamSourceFilter : BaseFilter, IFileSourceFilter
	{
		public delegate void CreateGraphDelegate(IGraphBuilder graph, string actualFile);

		/// <summary>
		/// Populate this before setting MediaElement.Source
		/// uri/Play(). This will create your graph.
		/// </summary>
		public static CreateGraphDelegate OnRender;
		string _FileName = "fake";

		public StreamSourceFilter()
		{
			GC.SuppressFinalize(this);
			this.Pins.Add(new StreamPin(null, this, "outpin"));
		}

		protected override int OnJoinFilterGraph()
		{
			Debug.WriteLine("StreamSourceFilter joined graph");

			return base.OnJoinFilterGraph();
		}

		#region IFileSourceFilter Members

		public int Load(string pszFileName, AMMediaType pmt)
		{
			_FileName = pszFileName;

			if (OnRender != null)
				OnRender((IGraphBuilder)_Graph, _FileName);

			return (int)HRESULT.S_OK;
		}

		public int GetCurFile(out string pszFileName, AMMediaType pmt)
		{
			pszFileName = _FileName;
			return (int)HRESULT.S_OK;
		}

		#endregion

		public static void Register()
		{
			//RegisterForProtocol(typeof(StreamSourceFilter), _ProtocolName, _ProtocolName + " MediaElementProxy");
		}

		public static void UnRegister()
		{
			//UnRegisterForProtocol(typeof(StreamSourceFilter), _ProtocolName, true);
		}

	}
}
