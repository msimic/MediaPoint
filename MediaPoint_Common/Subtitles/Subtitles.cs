using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MediaPoint.Subtitles.Logic;
using MediaPoint.Subtitles.Logic.VobSub;
using MediaPoint.Subtitles.Logic.SubtitleFormats;
using System.Windows.Input;
using MediaPoint.Subtitles.Logic.BluRaySup;
using System.Text.RegularExpressions;
using MediaPoint.Helpers;
using MediaPoint.Common.Subtitles;
using MediaPoint.Common.Helpers;

namespace MediaPoint.Subtitles
{
	public class SubtitleItem
	{
		public enum SubtitleType
		{
            None,
			File,
			Embedded
		}

		public enum SubtitleSubType
		{
            None,
			Srt,
			Sub,
			VobSub,
			Ass,
			Pgs
		}

		public SubtitleType Type { get; private set; }
		public SubtitleSubType SubType { get; private set; }
		public string DisplayName { get; private set; }
		public string Path { get; private set; }

		public SubtitleItem(SubtitleType type, SubtitleSubType subType, string path, string name)
		{
			DisplayName = name;
			Path = path;
			Type = type;
			SubType = subType;
		}
	}

	public class Subtitles
	{
		string _fileName;
		Subtitle _subtitle = new Subtitle();
		Subtitle _subtitleAlternate;
		SubtitleFormat _oldSubtitleFormat;
		public double CurrentFrameRate { get; set; }
		string _subtitleAlternateFileName;

		public string MsgBoxTitle { get; set; }

		#region "Methods"

		public IEnumerable<Paragraph> SubsAt(TimeSpan time)
		{
			var ps = _subtitle.GetParagraphsAt(time);
			return ps;
		}

		public IEnumerable<Paragraph> SubsAt(long frame)
		{
			var ps = _subtitle.GetParagraphsAt(frame);
			return ps;
		}

		private bool IsVobSubFile(string subFileName, bool verbose)
		{
			try
			{
				bool isHeaderOk = HasVobSubHeader(subFileName);
				if (isHeaderOk)
				{
					if (!verbose)
						return true;

					string idxFileName = Path.Combine(Path.GetDirectoryName(subFileName), Path.GetFileNameWithoutExtension(subFileName) + ".idx");
					if (File.Exists(idxFileName))
						return true;
					return (MessageBox.Show(string.Format("IdxFileNotFoundWarning", idxFileName), MsgBoxTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes);
				}
				if (verbose)
					MessageBox.Show(string.Format("InvalidVobSubHeader", subFileName));
			}
			catch (Exception ex)
			{
				if (verbose)
					MessageBox.Show(ex.Message);
			}
			return false;
		}

		public static bool HasVobSubHeader(string subFileName)
		{
			var buffer = new byte[4];
			var fs = new FileStream(subFileName, FileMode.Open, FileAccess.Read, FileShare.Read) { Position = 0 };
			fs.Read(buffer, 0, 4);
			bool isHeaderOk = VobSubParser.IsMpeg2PackHeader(buffer) || VobSubParser.IsPrivateStream1(buffer, 0);
			fs.Close();
			return isHeaderOk;
		}

		public static bool IsBluRaySupFile(string subFileName)
		{
			var buffer = new byte[4];
			var fs = new FileStream(subFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) { Position = 0 };
			fs.Read(buffer, 0, 4);
			fs.Close();
			return (buffer[0] == 0x50 && buffer[1] == 0x47); // 80 + 71 - P G
		}

		private bool IsSpDvdSupFile(string subFileName)
		{
			byte[] buffer = new byte[SpHeader.SpHeaderLength];
			var fs = new FileStream(subFileName, FileMode.Open, FileAccess.Read, FileShare.Read) { Position = 0 };
			int bytesRead = fs.Read(buffer, 0, buffer.Length);
			if (bytesRead == buffer.Length)
			{
				var header = new SpHeader(buffer);
				if (header.Identifier == "SP" && header.NextBlockPosition > 4)
				{
					buffer = new byte[header.NextBlockPosition];
					bytesRead = fs.Read(buffer, 0, buffer.Length);
					if (bytesRead == buffer.Length)
					{
						buffer = new byte[SpHeader.SpHeaderLength];
						bytesRead = fs.Read(buffer, 0, buffer.Length);
						if (bytesRead == buffer.Length)
						{
							header = new SpHeader(buffer);
							fs.Close();
							return header.Identifier == "SP";
						}
					}
				}
			}
			fs.Close();
			return false;
		}

		private void ImportAndOcrVobSubSubtitleNew(string fileName)
		{
			if (IsVobSubFile(fileName, true))
			{
				//var vobSubOcr = new VobSubOcr();
				//if (vobSubOcr.Initialize(fileName, Configuration.Settings.VobSubOcr, true))
				//{
				//    if (vobSubOcr.ShowDialog(this) == DialogResult.OK)
				//    {
				//        MakeHistoryForUndo(_language.BeforeImportingVobSubFile);
				//        FileNew();
				//        _subtitle.Paragraphs.Clear();
				//        SetCurrentFormat(new SubRip().FriendlyName);
				//        _subtitle.WasLoadedWithFrameNumbers = false;
				//        _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
				//        foreach (Paragraph p in vobSubOcr.SubtitleFromOcr.Paragraphs)
				//        {
				//            _subtitle.Paragraphs.Add(p);
				//        }

				//        ShowSource();
				//        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
				//        _change = true;
				//        _subtitleListViewIndex = -1;
				//        SubtitleListview1.FirstVisibleIndex = -1;
				//        SubtitleListview1.SelectIndexAndEnsureVisible(0);

				//        _fileName = Path.ChangeExtension(vobSubOcr.FileName, ".srt");
				//        SetTitle();
				//        _converted = true;

				//        Configuration.Settings.Save();
				//    }
				//}
			}
		}

		private void ImportAndOcrBluRaySup(string fileName)
		{
			//StringBuilder log = new StringBuilder();
			//var subtitles = BluRaySupParser.ParseBluRaySup(fileName, log);
			//subtitles = SplitBitmaps(subtitles);
			//if (subtitles.Count > 0)
			//{
			//    var vobSubOcr = new VobSubOcr();
			//    vobSubOcr.Initialize(subtitles, Configuration.Settings.VobSubOcr, fileName);
			//    vobSubOcr.FileName = Path.GetFileName(fileName);
			//    if (vobSubOcr.ShowDialog(this) == DialogResult.OK)
			//    {
			//        MakeHistoryForUndo(_language.BeforeImportingBluRaySupFile);
			//        FileNew();
			//        _subtitle.Paragraphs.Clear();
			//        SetCurrentFormat(new SubRip().FriendlyName);
			//        _subtitle.WasLoadedWithFrameNumbers = false;
			//        _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
			//        foreach (Paragraph p in vobSubOcr.SubtitleFromOcr.Paragraphs)
			//        {
			//            _subtitle.Paragraphs.Add(p);
			//        }

			//        ShowSource();
			//        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
			//        _change = true;
			//        _subtitleListViewIndex = -1;
			//        SubtitleListview1.FirstVisibleIndex = -1;
			//        SubtitleListview1.SelectIndexAndEnsureVisible(0);

			//        _fileName = Path.ChangeExtension(vobSubOcr.FileName, ".srt");
			//        SetTitle();
			//        _converted = true;

			//        Configuration.Settings.Save();
			//    }
			//}
		}

		private void ImportAndOcrSpDvdSup(string fileName)
		{
			//var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read) { Position = 0 };

			//byte[] buffer = new byte[SpHeader.SpHeaderLength];
			//int bytesRead = fs.Read(buffer, 0, buffer.Length);
			//var header = new SpHeader(buffer);
			//var spList = new List<SpHeader>();

			//while (header.Identifier == "SP" && bytesRead > 0 && header.NextBlockPosition > 4)
			//{
			//    buffer = new byte[header.NextBlockPosition];
			//    bytesRead = fs.Read(buffer, 0, buffer.Length);
			//    if (bytesRead == buffer.Length)
			//    {
			//        header.AddPicture(buffer);
			//        spList.Add(header);
			//    }

			//    buffer = new byte[SpHeader.SpHeaderLength];
			//    bytesRead = fs.Read(buffer, 0, buffer.Length);
			//    header = new SpHeader(buffer);
			//}
			//fs.Close();

			//var vobSubOcr = new VobSubOcr();
			//vobSubOcr.Initialize(fileName, null, Configuration.Settings.VobSubOcr, spList);
			//if (vobSubOcr.ShowDialog(this) == DialogResult.OK)
			//{
			//    MakeHistoryForUndo(_language.BeforeImportingVobSubFile);
			//    FileNew();
			//    _subtitle.Paragraphs.Clear();
			//    SetCurrentFormat(new SubRip().FriendlyName);
			//    _subtitle.WasLoadedWithFrameNumbers = false;
			//    _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
			//    foreach (Paragraph p in vobSubOcr.SubtitleFromOcr.Paragraphs)
			//    {
			//        _subtitle.Paragraphs.Add(p);
			//    }

			//    ShowSource();
			//    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
			//    _change = true;
			//    _subtitleListViewIndex = -1;
			//    SubtitleListview1.FirstVisibleIndex = -1;
			//    SubtitleListview1.SelectIndexAndEnsureVisible(0);

			//    _fileName = Path.ChangeExtension(vobSubOcr.FileName, ".srt");
			//    SetTitle();
			//    _converted = true;

			//    Configuration.Settings.Save();
			//}
		}

		private void ImportSubtitleFromMatroskaFile(string fileName)
		{
			bool isValid;
			var matroska = new Matroska();
			var subtitleList = matroska.GetMatroskaSubtitleTracks(fileName, out isValid);
			if (isValid)
			{
				if (subtitleList.Count == 0)
				{
					MessageBox.Show("NoSubtitlesFound");
				}
				else
				{
					if (true)
					{
						if (subtitleList.Count > 1)
						{
							//MatroskaSubtitleChooser subtitleChooser = new MatroskaSubtitleChooser();
							//subtitleChooser.Initialize(subtitleList);
							//if (subtitleChooser.ShowDialog(this) == DialogResult.OK)
							//{
							//    LoadMatroskaSubtitle(subtitleList[subtitleChooser.SelectedIndex], fileName);
							//    if (Path.GetExtension(fileName).ToLower() == ".mkv")
							//        OpenVideo(fileName);
							//}
						}
						else
						{
							LoadMatroskaSubtitle(subtitleList[0], fileName);
						}
					}
				}
			}
			else
			{
				MessageBox.Show(string.Format("NotAValidMatroskaFileX {0}", fileName));
			}
		}

		private void MatroskaProgress(long position, long total)
		{
			//ShowStatus(string.Format("{0}, {1:0}%", _language.ParsingMatroskaFile, position * 100 / total));

			//if (DateTime.Now.Ticks % 10 == 0)
			//    Application.DoEvents();
		}

		private void LoadVobSubFromMatroska(MatroskaSubtitleInfo matroskaSubtitleInfo, string fileName)
		{
			if (matroskaSubtitleInfo.ContentEncodingType == 1)
			{
				MessageBox.Show("Encrypted vobsub content not supported");
			}

			bool isValid;
			var matroska = new Matroska();

			List<SubtitleSequence> sub = matroska.GetMatroskaSubtitle(fileName, (int)matroskaSubtitleInfo.TrackNumber, out isValid, MatroskaProgress);

			if (isValid)
			{
				_subtitle.Paragraphs.Clear();

				List<VobSubMergedPack> mergedVobSubPacks = new List<VobSubMergedPack>();
				MediaPoint.Subtitles.Logic.VobSub.Idx idx = new MediaPoint.Subtitles.Logic.VobSub.Idx(matroskaSubtitleInfo.CodecPrivate.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
				foreach (SubtitleSequence p in sub)
				{
					if (matroskaSubtitleInfo.ContentEncodingType == 0) // compressed with zlib
					{
						MemoryStream outStream = new MemoryStream();
						ComponentAce.Compression.Libs.zlib.ZOutputStream outZStream = new ComponentAce.Compression.Libs.zlib.ZOutputStream(outStream);
						MemoryStream inStream = new MemoryStream(p.BinaryData);
						byte[] buffer;
						try
						{
							CopyStream(inStream, outZStream);
							buffer = new byte[outZStream.TotalOut];
							outStream.Position = 0;
							outStream.Read(buffer, 0, buffer.Length);
						}
						finally
						{
							outStream.Close();
							outZStream.Close();
							inStream.Close();
						}
						mergedVobSubPacks.Add(new VobSubMergedPack(buffer, TimeSpan.FromMilliseconds(p.StartMilliseconds), 32, null));
					}
					else
					{
						mergedVobSubPacks.Add(new VobSubMergedPack(p.BinaryData, TimeSpan.FromMilliseconds(p.StartMilliseconds), 32, null));
					}
					mergedVobSubPacks[mergedVobSubPacks.Count - 1].EndTime = TimeSpan.FromMilliseconds(p.EndMilliseconds);

					// fix overlapping (some versions of Handbrake makes overlapping time codes - thx Hawke)
					if (mergedVobSubPacks.Count > 1 && mergedVobSubPacks[mergedVobSubPacks.Count - 2].EndTime > mergedVobSubPacks[mergedVobSubPacks.Count - 1].StartTime)
						mergedVobSubPacks[mergedVobSubPacks.Count - 2].EndTime = TimeSpan.FromMilliseconds(mergedVobSubPacks[mergedVobSubPacks.Count - 1].StartTime.TotalMilliseconds - 1);

					var current = mergedVobSubPacks[mergedVobSubPacks.Count - 1];
					_subtitle.Paragraphs.Add(new Paragraph("VobSub", current.StartTime.TotalMilliseconds, current.EndTime.TotalMilliseconds) {VobSubMergedPack = current});
				}

				_subtitle.WasLoadedWithFrameNumbers = false;

				_fileName = Path.GetFileNameWithoutExtension(fileName);

				Configuration.Settings.Save();

			}
		}

		public static void CopyStream(System.IO.Stream input, System.IO.Stream output)
		{
			byte[] buffer = new byte[2000];
			int len;
			while ((len = input.Read(buffer, 0, 2000)) > 0)
			{
				output.Write(buffer, 0, len);
			}
			output.Flush();
		}

		private void LoadBluRaySubFromMatroska(MatroskaSubtitleInfo matroskaSubtitleInfo, string fileName)
		{
			if (matroskaSubtitleInfo.ContentEncodingType == 1)
			{
				MessageBox.Show("Encrypted vobsub content not supported");
			}

			bool isValid;
			var matroska = new Matroska();

			List<SubtitleSequence> sub = matroska.GetMatroskaSubtitle(fileName, (int)matroskaSubtitleInfo.TrackNumber, out isValid, MatroskaProgress);
			int noOfErrors = 0;
			string lastError = string.Empty;

			if (isValid)
			{
				_subtitle.Paragraphs.Clear();
				List<BluRaySupPicture> subtitles = new List<BluRaySupPicture>();
				StringBuilder log = new StringBuilder();
				foreach (SubtitleSequence p in sub)
				{
					byte[] buffer = null;
					if (matroskaSubtitleInfo.ContentEncodingType == 0) // compressed with zlib
					{
						MemoryStream outStream = new MemoryStream();
						ComponentAce.Compression.Libs.zlib.ZOutputStream outZStream = new ComponentAce.Compression.Libs.zlib.ZOutputStream(outStream);
						MemoryStream inStream = new MemoryStream(p.BinaryData);
						try
						{
							CopyStream(inStream, outZStream);
							buffer = new byte[outZStream.TotalOut];
							outStream.Position = 0;
							outStream.Read(buffer, 0, buffer.Length);
						}
						catch (Exception exception)
						{
							TimeCode tc = new TimeCode(TimeSpan.FromMilliseconds(p.StartMilliseconds));
							lastError = tc.ToString() + ": " + exception.Message + ": " + exception.StackTrace;
							noOfErrors++;
						}
						finally
						{
							outStream.Close();
							outZStream.Close();
							inStream.Close();
						}
					}
					else
					{
						buffer = p.BinaryData;
					}
					if (buffer != null && buffer.Length > 100)
					{
						MemoryStream ms = new MemoryStream(buffer);
						var list = BluRaySupParser.ParseBluRaySup(ms, log, true);
						foreach (var sup in list)
						{
							sup.StartTime = p.StartMilliseconds;
							sup.EndTime = p.EndMilliseconds;
							subtitles.Add(sup);

							// fix overlapping
							if (subtitles.Count > 1 && sub[subtitles.Count - 2].EndMilliseconds > sub[subtitles.Count - 1].StartMilliseconds)
								subtitles[subtitles.Count - 2].EndTime = subtitles[subtitles.Count - 1].StartTime - 1;
						}
						ms.Close();
					}
				}

				if (noOfErrors > 0)
				{
					MessageBox.Show(string.Format("{0} errror(s) occured during extraction of bdsup\r\n\r\n{1}", noOfErrors, lastError));
				}

				SetCurrentFormat(new SubRip().FriendlyName);
				_subtitle.WasLoadedWithFrameNumbers = false;
				_subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);

				_fileName = string.Empty;

				Configuration.Settings.Save();
			}
		}

		public void SetCurrentFormat(string subtitleFormatFriendlyName)
		{

			foreach (SubtitleFormat format in SubtitleFormat.AllSubtitleFormats)
			{
				if (format.FriendlyName == subtitleFormatFriendlyName)
				{
					SetCurrentFormat(format);
					break;
				}
			}
		}

		public void SetCurrentFormat(SubtitleFormat format)
		{
			//if (format.IsVobSubIndexFile)
			//{
			//    //comboBoxSubtitleFormats.Items.Clear();
			//    //comboBoxSubtitleFormats.Items.Add(format.FriendlyName);

			//    //SubtitleListview1.HideNonVobSubColumns();
			//}
			//else if (comboBoxSubtitleFormats.Items.Count == 1)
			//{
			//    SetFormatToSubRip();
			//    SubtitleListview1.ShowAllColumns();
			//}

			//int i = 0;
			//foreach (object obj in comboBoxSubtitleFormats.Items)
			//{
			//    if (obj.ToString() == format.FriendlyName)
			//        comboBoxSubtitleFormats.SelectedIndex = i;
			//    i++;
			//}
		}

		private void LoadMatroskaSubtitle(MatroskaSubtitleInfo matroskaSubtitleInfo, string fileName)
		{
			bool isValid;
			bool isSsa = false;
			var matroska = new Matroska();

			SubtitleFormat format;

			if (matroskaSubtitleInfo.CodecId.ToUpper() == "S_VOBSUB")
			{
				LoadVobSubFromMatroska(matroskaSubtitleInfo, fileName);
				return;
			}
			if (matroskaSubtitleInfo.CodecId.ToUpper() == "S_HDMV/PGS")
			{
				LoadBluRaySubFromMatroska(matroskaSubtitleInfo, fileName);
				return;
			}
			else if (matroskaSubtitleInfo.CodecPrivate.ToLower().Contains("[script info]"))
			{
				format = new SubStationAlpha();
				isSsa = true;
			}
			else
			{
				format = new SubRip();
			}

			List<SubtitleSequence> sub = matroska.GetMatroskaSubtitle(fileName, (int)matroskaSubtitleInfo.TrackNumber, out isValid, MatroskaProgress);

			if (isValid)
			{
				_subtitle.Paragraphs.Clear();

				if (isSsa)
				{
					int commaCount = 100;

					foreach (SubtitleSequence p in sub)
					{
						string s1 = p.Text;
						if (s1.Contains(@"{\"))
							s1 = s1.Substring(0, s1.IndexOf(@"{\"));
						int temp = s1.Split(',').Length;
						if (temp < commaCount)
							commaCount = temp;
					}

					foreach (SubtitleSequence p in sub)
					{
						string s = string.Empty;
						string[] arr = p.Text.Split(',');
						if (arr.Length >= commaCount)
						{
							for (int i = commaCount; i <= arr.Length; i++)
							{
								if (s.Length > 0)
									s += ",";
								s += arr[i - 1];
							}
						}
						_subtitle.Paragraphs.Add(new Paragraph(s, p.StartMilliseconds, p.EndMilliseconds));
					}
				}
				else
				{
					foreach (SubtitleSequence p in sub)
					{
						_subtitle.Paragraphs.Add(new Paragraph(p.Text, p.StartMilliseconds, p.EndMilliseconds));
					}
				}

				_subtitle.Renumber(1);
				_subtitle.WasLoadedWithFrameNumbers = false;
				if (fileName.ToLower().EndsWith(".mkv"))
				{
					_fileName = fileName.Substring(0, fileName.Length - 4);
				}

				if (format.FriendlyName == new SubStationAlpha().FriendlyName)
					_subtitle.Header = matroskaSubtitleInfo.CodecPrivate;
				//ShowSource();
			}
		}

		private bool ImportSubtitleFromMp4(string fileName)
		{
			var mp4Parser = new MediaPoint.Subtitles.Logic.Mp4.Mp4Parser(fileName);
			var mp4SubtitleTracks = mp4Parser.GetSubtitleTracks();
			if (mp4SubtitleTracks.Count == 0)
			{
				MessageBox.Show("NoSubtitlesFound");
				return false;
			}
			else if (mp4SubtitleTracks.Count == 1)
			{
				LoadMp4Subtitle(fileName, mp4SubtitleTracks[0]);
				return true;
			}
			else
			{
				//var subtitleChooser = new MatroskaSubtitleChooser();
				//subtitleChooser.Initialize(mp4SubtitleTracks);
				//if (subtitleChooser.ShowDialog(this) == DialogResult.OK)
				//{
				//    LoadMp4Subtitle(fileName, mp4SubtitleTracks[subtitleChooser.SelectedIndex]);
				//    return true;
				//}
				return false;
			}
		}

		private void LoadMp4Subtitle(string fileName, MediaPoint.Subtitles.Logic.Mp4.Boxes.Trak mp4SubtitleTrack)
		{
			if (mp4SubtitleTrack.Mdia.IsVobSubSubtitle)
			{
				//var subPicturesWithTimeCodes = new List<VobSubOcr.SubPicturesWithSeparateTimeCodes>();
				//for (int i = 0; i < mp4SubtitleTrack.Mdia.Minf.Stbl.EndTimeCodes.Count; i++)
				//{
				//    if (mp4SubtitleTrack.Mdia.Minf.Stbl.SubPictures.Count > i)
				//    {
				//        var start = TimeSpan.FromSeconds(mp4SubtitleTrack.Mdia.Minf.Stbl.StartTimeCodes[i]);
				//        var end = TimeSpan.FromSeconds(mp4SubtitleTrack.Mdia.Minf.Stbl.EndTimeCodes[i]);
				//        subPicturesWithTimeCodes.Add(new VobSubOcr.SubPicturesWithSeparateTimeCodes(mp4SubtitleTrack.Mdia.Minf.Stbl.SubPictures[i], start, end));
				//    }
				//}

				//var formSubOcr = new VobSubOcr();
				//formSubOcr.Initialize(subPicturesWithTimeCodes, Configuration.Settings.VobSubOcr, fileName); //TODO - language???
				//if (formSubOcr.ShowDialog(this) == DialogResult.OK)
				//{
				//    MakeHistoryForUndo(_language.BeforeImportFromMatroskaFile);
				//    _subtitleListViewIndex = -1;
				//    FileNew();
				//    _subtitle.WasLoadedWithFrameNumbers = false;
				//    foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
				//        _subtitle.Paragraphs.Add(p);

				//    ShowSource();
				//    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
				//    _change = true;
				//    _subtitleListViewIndex = -1;
				//    SubtitleListview1.FirstVisibleIndex = -1;
				//    SubtitleListview1.SelectIndexAndEnsureVisible(0);

				//    _fileName = Path.GetFileNameWithoutExtension(fileName);
				//    _converted = true;
				//    Text = Title;

				//    Configuration.Settings.Save();
				//}
			}
			else
			{

				for (int i = 0; i < mp4SubtitleTrack.Mdia.Minf.Stbl.EndTimeCodes.Count; i++)
				{
					if (mp4SubtitleTrack.Mdia.Minf.Stbl.Texts.Count > i)
					{
						var start = TimeSpan.FromSeconds(mp4SubtitleTrack.Mdia.Minf.Stbl.StartTimeCodes[i]);
						var end = TimeSpan.FromSeconds(mp4SubtitleTrack.Mdia.Minf.Stbl.EndTimeCodes[i]);
						string text = mp4SubtitleTrack.Mdia.Minf.Stbl.Texts[i];
						_subtitle.Paragraphs.Add(new Paragraph(text, start.TotalMilliseconds, end.TotalMilliseconds));
					}
				}

				_subtitle.Renumber(1);
				_subtitle.WasLoadedWithFrameNumbers = false;

				_fileName = fileName.Substring(0, fileName.Length - 4);

			}
		}

		private SubtitleFormat GetCurrentSubtitleFormat()
		{
			return Utilities.GetSubtitleFormatByFriendlyName(_subtitle.OriginalFormat.FriendlyName.ToString());
		}

		private List<string> SetFormatToSubRip()
		{
			List<string> ret = new List<string>();
			foreach (SubtitleFormat format in SubtitleFormat.AllSubtitleFormats)
			{
				if (!format.IsVobSubIndexFile)
					ret.Add(format.FriendlyName);
			}

			return ret;
		}

		private bool LoadAlternateSubtitleFile(string fileName)
		{
			if (!File.Exists(fileName))
				return false;

			if (Path.GetExtension(fileName).ToLower() == ".sub" && IsVobSubFile(fileName, false))
				return false;

			var fi = new FileInfo(fileName);
			if (fi.Length > 1024 * 1024 * 10) // max 10 mb
			{
				if (MessageBox.Show(string.Format("Largert han 10mb continue? {0}",
													fileName), MsgBoxTitle, MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
					return false;
			}

			Encoding encoding;
			_subtitleAlternate = new Subtitle();
			_subtitleAlternateFileName = fileName;
			SubtitleFormat format = _subtitleAlternate.LoadSubtitle(fileName, out encoding, null);
			if (format == null)
				return false;

			if (format.IsFrameBased)
				_subtitleAlternate.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
			else
				_subtitleAlternate.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);

			return true;
		}

		private void ImportAndOcrSon(string fileName, Son format, List<string> list)
		{
			Subtitle sub = new Subtitle();
			format.LoadSubtitle(_subtitle, list, fileName);
			_subtitle.FileName = fileName;
			//var formSubOcr = new VobSubOcr();
			//formSubOcr.Initialize(sub, Configuration.Settings.VobSubOcr, true);
			//if (formSubOcr.ShowDialog(this) == DialogResult.OK)
			//{
			//    MakeHistoryForUndo(_language.BeforeImportingBdnXml);
			//    FileNew();
			//    _subtitle.Paragraphs.Clear();
			SetCurrentFormat(new SubRip().FriendlyName);
			_subtitle.WasLoadedWithFrameNumbers = false;
			_subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
			//foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
			//{
			//    _subtitle.Paragraphs.Add(p);
			//}

			_fileName = Path.ChangeExtension(_subtitle.FileName, ".srt");


		}

		private void ImportAndOcrBdnXml(string fileName, BdnXml bdnXml, List<string> list)
		{
			Subtitle bdnSubtitle = new Subtitle();
			bdnXml.LoadSubtitle(_subtitle, list, fileName);
			_subtitle.FileName = fileName;
			//var formSubOcr = new VobSubOcr();
			//formSubOcr.Initialize(bdnSubtitle, Configuration.Settings.VobSubOcr, false);
			//if (formSubOcr.ShowDialog(this) == DialogResult.OK)
			//{
			//    MakeHistoryForUndo(_language.BeforeImportingBdnXml);
			//    FileNew();
			//    _subtitle.Paragraphs.Clear();
			SetCurrentFormat(new SubRip().FriendlyName);
			_subtitle.WasLoadedWithFrameNumbers = false;
			_subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
			//foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
			//{
			//    _subtitle.Paragraphs.Add(p);
			//}

			//ShowSource();
			//SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
			//_change = true;
			//_subtitleListViewIndex = -1;
			//SubtitleListview1.FirstVisibleIndex = -1;
			//SubtitleListview1.SelectIndexAndEnsureVisible(0);

			_fileName = Path.ChangeExtension(_subtitle.FileName, ".srt");
			//SetTitle();
			//_converted = true;

		}

		private void ShowUnknownSubtitle()
		{
			//var unknownSubtitle = new UnknownSubtitle();
			//unknownSubtitle.Initialize(Title);
			//unknownSubtitle.ShowDialog(this);
		}

		#endregion

		public void LoadEmbeddedSub(string videoFile, SubtitleItem sub)
		{
			var matroska = new Matroska();
			bool isValid;
			var subtitleList = matroska.GetMatroskaSubtitleTracks(videoFile, out isValid);
			if (sub.SubType == SubtitleItem.SubtitleSubType.VobSub)
			{
				LoadVobSubFromMatroska(subtitleList.First(s => s.TrackNumber.ToString() == sub.Path), videoFile);
			}
			if (sub.SubType == SubtitleItem.SubtitleSubType.Pgs)
			{
			}
			if (sub.SubType == SubtitleItem.SubtitleSubType.Srt)
			{
				LoadMatroskaSubtitle(subtitleList.First(s => s.TrackNumber.ToString() == sub.Path), videoFile);
			}
		}

		public long ListEmbeddedSubtitles(string videoFile, out List<SubtitleItem> subFiles, bool loadNow = false)
		{

			var matroska = new Matroska();
			subFiles = new List<SubtitleItem>();
			bool isValid;
			long trackSelected = -1;
            var subtitleList = matroska.GetMatroskaSubtitleTracks(videoFile, out isValid);
			if (isValid)
            foreach (var sub in subtitleList)
			{
				if (sub.CodecId.ToUpper() == "S_VOBSUB")
				{
					if (loadNow) LoadVobSubFromMatroska(sub, videoFile);
					trackSelected = sub.TrackNumber;
					subFiles.Add(new SubtitleItem(SubtitleItem.SubtitleType.Embedded,
						SubtitleItem.SubtitleSubType.VobSub, 
						sub.TrackNumber.ToString(),
						FormatEmbeddedName(sub)));
				}
				else if (sub.CodecId.ToUpper() == "S_HDMV/PGS")
				{
					if (loadNow) LoadBluRaySubFromMatroska(sub, videoFile);
					trackSelected = sub.TrackNumber;
					subFiles.Add(new SubtitleItem(SubtitleItem.SubtitleType.Embedded,
						SubtitleItem.SubtitleSubType.Pgs,
						sub.TrackNumber.ToString(),
                        FormatEmbeddedName(sub)));
				}
				else
				{
					if (loadNow) LoadMatroskaSubtitle(sub, videoFile);
					trackSelected = sub.TrackNumber;
					subFiles.Add(new SubtitleItem(SubtitleItem.SubtitleType.Embedded,
						SubtitleItem.SubtitleSubType.Srt,
						sub.TrackNumber.ToString(),
                        FormatEmbeddedName(sub)));
				}
			}
			return trackSelected;
		}

        string FormatEmbeddedName(MatroskaSubtitleInfo sub)
        {
            string language = sub.Language;
            var lng = LanguageISOTranslator.ISO839_2B[language];
            if (lng != null)
            {
                language = "[" + lng.EnglishName + "] ";
            }
            return string.Format("Embedded: {0}{1} ({2})", language, sub.Name, sub.CodecId);
        }

        string FormatName(string file, string videoFile)
        {
            string ret = Path.GetFileNameWithoutExtension(file);
            string ext = Path.GetExtension(file);
            if (ext.StartsWith(".")) ext = ext.Remove(0, 1);
            string vf = Path.GetFileNameWithoutExtension(videoFile);
            if (ret.ToLowerInvariant().StartsWith(vf.ToLowerInvariant()))
            {
                ret = ret.Remove(0, vf.Length);
            }
            if (ret.StartsWith("_"))
            {
                ret = ret.Remove(0, 1);
            }
            var lng = LanguageISOTranslator.ISO839_2B[ret];
            if (lng != null)
            {
                ret = lng.EnglishName;
            }
            ret = string.Format("File: {0} ({1})", ret, ext);
            return ret;
        }

		public string LoadSubtitles(string videoFile, out List<SubtitleItem> subFiles, Encoding overrideEnc = null, bool fillNow = false)
		{
			lock (this)
			{
				ClearSubtitles();
                subFiles = new List<SubtitleItem>();

                string subEp = SubtitleUtil.FindSeasonAndEpisode(videoFile);

				var dir = Path.GetDirectoryName(videoFile);

				var files = new List<string>(Directory.GetFiles(dir));
				files.RemoveAll(f => Path.GetExtension(f).ToLowerInvariant() == Path.GetExtension(videoFile));


				foreach (var file in files)
				{
					if (Path.GetExtension(file).ToLower() == ".srt" ||
						Path.GetExtension(file).ToLower() == ".sub" ||
						Path.GetExtension(file).ToLower() == ".ass" ||
						Path.GetExtension(file).ToLower() == ".ssa")

						switch (Path.GetExtension(file).ToLower())
						{
							case ".srt":
								subFiles.Add(new SubtitleItem(SubtitleItem.SubtitleType.File, SubtitleItem.SubtitleSubType.Srt, file, FormatName(file, videoFile)));
								break;
							case ".sub":
                                subFiles.Add(new SubtitleItem(SubtitleItem.SubtitleType.File, SubtitleItem.SubtitleSubType.Sub, file, FormatName(file, videoFile)));
								break;
							case ".ass":
							case ".ssa":
                                subFiles.Add(new SubtitleItem(SubtitleItem.SubtitleType.File, SubtitleItem.SubtitleSubType.Ass, file, FormatName(file, videoFile)));
								break;
						}
				}

                if (subEp != "")
                {
                    subFiles.RemoveAll(m => SubtitleUtil.FindSeasonAndEpisode(m.Path) != subEp);
                }

                if (subEp == "" && subFiles.Count == 1 && (Path.GetExtension(subFiles[0].Path).ToLower() == ".srt" ||
                                         Path.GetExtension(subFiles[0].Path).ToLower() == ".sub" ||
                                         Path.GetExtension(subFiles[0].Path).ToLower() == ".ass" ||
                                         Path.GetExtension(subFiles[0].Path).ToLower() == ".ssa"))
				{
                    if (fillNow) OpenSubtitle(subFiles[0].Path, overrideEnc, null, null);
                    return subFiles[0].Path;
				}
				else if (File.Exists(Path.ChangeExtension(videoFile.ToLower(), ".srt")))
				{
					if (fillNow) OpenSubtitle(Path.ChangeExtension(videoFile, ".srt"), overrideEnc, null, null);
					return Path.ChangeExtension(videoFile, ".srt");
				}
				else if (File.Exists(Path.ChangeExtension(videoFile.ToLower(), ".sub")))
				{
					if (fillNow) OpenSubtitle(Path.ChangeExtension(videoFile, ".sub"), overrideEnc, null, null);
					return Path.ChangeExtension(videoFile, ".sub");
				}
				else if (File.Exists(Path.ChangeExtension(videoFile.ToLower(), ".ssa")))
				{
					if (fillNow) OpenSubtitle(Path.ChangeExtension(videoFile, ".ssa"), overrideEnc, null, null);
					return Path.ChangeExtension(videoFile, ".ssa");
				}
				else if (File.Exists(Path.ChangeExtension(videoFile.ToLower(), ".ass")))
				{
					if (fillNow) OpenSubtitle(Path.ChangeExtension(videoFile, ".ass"), overrideEnc, null, null);
					return Path.ChangeExtension(videoFile, ".ass");
				}
                else if (subEp == "" && Directory.GetFiles(dir, "*.srt", SearchOption.TopDirectoryOnly).Count() == 1)
				{
					var file = Directory.GetFiles(dir, "*.srt", SearchOption.TopDirectoryOnly).First();
					if (fillNow) OpenSubtitle(file, overrideEnc, null, null);
					return file;
				}
                else if (subEp == "" && Directory.GetFiles(dir, "*.sub", SearchOption.TopDirectoryOnly).Count() == 1)
				{
					var file = Directory.GetFiles(dir, "*.sub", SearchOption.TopDirectoryOnly).First();
					if (fillNow) OpenSubtitle(file, overrideEnc, null, null);
					return file;
				}
                else if (subEp == "" && Directory.GetFiles(dir, "*.ssa", SearchOption.TopDirectoryOnly).Count() == 1)
				{
					var file = Directory.GetFiles(dir, "*.ssa", SearchOption.TopDirectoryOnly).First();
					if (fillNow) OpenSubtitle(file, overrideEnc, null, null);
					return file;
				}
                else if (subEp == "" && Directory.GetFiles(dir, "*.ass", SearchOption.TopDirectoryOnly).Count() == 1)
				{
					var file = Directory.GetFiles(dir, "*.ass", SearchOption.TopDirectoryOnly).First();
					if (fillNow) OpenSubtitle(file, overrideEnc, null, null);
					return file;
				}
				else
				{
                    return OrderedFilenames(videoFile, subFiles, overrideEnc, fillNow);
				}
			}
		}

        public static string OrderedFilenames(string videoFile, List<SubtitleItem> subFiles, Encoding overrideEnc, bool fillNow)
        {
            var os = subFiles.Select(s => {
                string s1 = Path.GetFileNameWithoutExtension(s.Path).ToLowerInvariant();
                string s2 = Path.GetFileNameWithoutExtension(videoFile).ToLowerInvariant();
                int maxl = Math.Max(s1.Length, s2.Length);
                double m = SubtitleUtil.WordMatches(s1, s2);
                double l = (double)Levenshtein.Compare(s1, s2);
                l = (maxl - l) / maxl;
                var r = new
                {
                    score = m * l,
                    sub = s
                };
                
                return r;
            });
            
            var first = os.OrderBy(s => s.score).Reverse().FirstOrDefault();

            return first != null ? first.sub.Path : null;
        }

		public void ClearSubtitles()
		{
			_subtitle.Paragraphs.Clear();
		}

		public void OpenSubtitle(string fileName, Encoding enc, string videoFileName, string originalFileName)
		{
			lock (this)
			{
				Encoding encoding = enc;
				OpenFileDialog openFileDialog1 = new OpenFileDialog();

				if (File.Exists(fileName))
				{

					// save last first visible index + first selected index from listview
					//if (!string.IsNullOrEmpty(_fileName))
					//    Configuration.Settings.RecentFiles.Add(_fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, originalFileName);

					openFileDialog1.InitialDirectory = Path.GetDirectoryName(fileName);

					if (Path.GetExtension(fileName).ToLower() == ".sub" && IsVobSubFile(fileName, false))
					{
						if (MessageBox.Show("ImportThisVobSubSubtitle", Application.Current.MainWindow.Title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
						{
							ImportAndOcrVobSubSubtitleNew(fileName);
						}
						return;
					}

					if (Path.GetExtension(fileName).ToLower() == ".sup")
					{
						if (IsBluRaySupFile(fileName))
						{
							ImportAndOcrBluRaySup(fileName);
							return;
						}
						else if (IsSpDvdSupFile(fileName))
						{
							ImportAndOcrSpDvdSup(fileName);
							return;
						}
					}

					if (Path.GetExtension(fileName).ToLower() == ".mkv" || Path.GetExtension(fileName).ToLower() == ".mks")
					{
						Matroska mkv = new Matroska();
						bool isValid = false;
						bool hasConstantFrameRate = false;
						double frameRate = 0;
						int width = 0;
						int height = 0;
						double milliseconds = 0;
						string videoCodec = string.Empty;
						mkv.GetMatroskaInfo(fileName, ref isValid, ref hasConstantFrameRate, ref frameRate, ref width, ref height, ref milliseconds, ref videoCodec);
						if (isValid)
						{
							ImportSubtitleFromMatroskaFile(fileName);
							return;
						}
					}

					var fi = new FileInfo(fileName);

					//if (Path.GetExtension(fileName).ToLower() == ".ts" && fi.Length > 10000  && IsTransportStream(fileName)) //TODO: Also check mpg, mpeg - and file header!
					//{
					//    ImportSubtitleFromTransportStream(fileName);
					//    return;
					//}

					if ((Path.GetExtension(fileName).ToLower() == ".mp4" || Path.GetExtension(fileName).ToLower() == ".m4v" || Path.GetExtension(fileName).ToLower() == ".3gp")
						&& fi.Length > 10000)
					{
						ImportSubtitleFromMp4(fileName);
						return;
					}

					if (fi.Length > 1024 * 1024 * 10) // max 10 mb
					{
						if (MessageBox.Show(string.Format("Larger than 10MB, continue? {0}",
														  fileName), MsgBoxTitle, MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
							return;
					}

					SubtitleFormat format = _subtitle.LoadSubtitle(fileName, out encoding, encoding);

				    if (format == null)
					{
						var ebu = new Ebu();
						if (ebu.IsMine(null, fileName))
						{
							ebu.LoadSubtitle(_subtitle, null, fileName);
							_oldSubtitleFormat = ebu;
							SetFormatToSubRip();
						    format = GetCurrentSubtitleFormat();
						}
					}

					if (format == null)
					{
						var pac = new Pac();
						if (pac.IsMine(null, fileName))
						{
							pac.LoadSubtitle(_subtitle, null, fileName);
							_oldSubtitleFormat = pac;
							SetFormatToSubRip();
						    format = GetCurrentSubtitleFormat();
						}
					}

					if (format == null)
					{
						var cavena890 = new Cavena890();
						if (cavena890.IsMine(null, fileName))
						{
							cavena890.LoadSubtitle(_subtitle, null, fileName);
							_oldSubtitleFormat = cavena890;
							SetFormatToSubRip();
						    format = GetCurrentSubtitleFormat();
						}
					}

					if (format == null)
					{
						var spt = new Spt();
						if (spt.IsMine(null, fileName))
						{
							spt.LoadSubtitle(_subtitle, null, fileName);
							_oldSubtitleFormat = spt;
							SetFormatToSubRip();
						    format = GetCurrentSubtitleFormat();
						}
					}

					if (format == null && Path.GetExtension(fileName).ToLower() == ".wsb")
					{
						string[] arr = File.ReadAllLines(fileName);
						List<string> list = new List<string>();
						foreach (string l in arr)
							list.Add(l);
						var wsb = new Wsb();
						if (wsb.IsMine(list, fileName))
						{
							wsb.LoadSubtitle(_subtitle, list, fileName);
							_oldSubtitleFormat = wsb;
							SetFormatToSubRip();
						    format = GetCurrentSubtitleFormat();
						}
					}

					if (format == null)
					{
						var bdnXml = new BdnXml();
						string[] arr = File.ReadAllLines(fileName);
						List<string> list = new List<string>();
						foreach (string l in arr)
							list.Add(l);
						if (bdnXml.IsMine(list, fileName))
						{
							if (true)
							{
								ImportAndOcrBdnXml(fileName, bdnXml, list);
							}
							return;
						}
					}

					if (format == null || format.Name == new Scenarist().Name)
					{
						var son = new Son();
						string[] arr = File.ReadAllLines(fileName);
						List<string> list = new List<string>();
						foreach (string l in arr)
							list.Add(l);
						if (son.IsMine(list, fileName))
						{
							if (true)
							{
								ImportAndOcrSon(fileName, son, list);
							}
							return;
						}
					}

					//_fileDateTime = File.GetLastWriteTime(fileName);

					if (GetCurrentSubtitleFormat().IsFrameBased)
						_subtitle.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
					else
						_subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);

					if (format != null)
					{
						if (Configuration.Settings.General.RemoveBlankLinesWhenOpening)
						{
							_subtitle.RemoveEmptyLines();
						}

						//_subtitleListViewIndex = -1;

						if (format.FriendlyName == new Sami().FriendlyName)
							encoding = Encoding.Default;

						SetCurrentFormat(format);

						_subtitleAlternateFileName = null;
						if (LoadAlternateSubtitleFile(originalFileName))
							_subtitleAlternateFileName = originalFileName;

						//textBoxSource.Text = _subtitle.ToText(format);
						//SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
						//if (SubtitleListview1.Items.Count > 0)
						//    SubtitleListview1.Items[0].Selected = true;
						//_findHelper = null;
						//_spellCheckForm = null;
						//_videoFileName = null;
						//_videoAudioTrackNumber = -1;
						//labelVideoInfo.Text = Configuration.Settings.Language.General.NoVideoLoaded;
						//audioVisualizer.WavePeaks = null;
						//audioVisualizer.ResetSpectrogram();
						//audioVisualizer.Invalidate();



						//SetUndockedWindowsTitle();

						//if (justConverted)
						//{
						//    _converted = true;
						//    ShowStatus(string.Format(_language.LoadedSubtitleX, _fileName) + " - " + string.Format(_language.ConvertedToX, format.FriendlyName));
						//}

						//if (encoding == Encoding.UTF7)
						//    comboBoxEncoding.Text = "UTF-7";
						//else if (encoding == Encoding.UTF8)
						//    comboBoxEncoding.Text = "UTF-8";
						//else if (encoding == System.Text.Encoding.Unicode)
						//    comboBoxEncoding.Text = "Unicode";
						//else if (encoding == System.Text.Encoding.BigEndianUnicode)
						//    comboBoxEncoding.Text = "Unicode (big endian)";
						//else
						//{
						//    comboBoxEncoding.Items[0] = "ANSI - " + encoding.CodePage.ToString();
						//    comboBoxEncoding.SelectedIndex = 0;
						//}
					}
					else
					{
						var info = new FileInfo(fileName);
						if (info.Length < 50)
						{
							//_findHelper = null;
							//_spellCheckForm = null;
							//_videoFileName = null;
							//_videoAudioTrackNumber = -1;
							//labelVideoInfo.Text = Configuration.Settings.Language.General.NoVideoLoaded;
							//audioVisualizer.WavePeaks = null;
							//audioVisualizer.ResetSpectrogram();
							//audioVisualizer.Invalidate();

							//Configuration.Settings.RecentFiles.Add(fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, _subtitleAlternateFileName);
							//Configuration.Settings.Save();
							//UpdateRecentFilesUI();
							//_fileName = fileName;
							//SetTitle();
							//ShowStatus(string.Format(_language.LoadedEmptyOrShort, _fileName));
							//_sourceViewChange = false;
							//_change = false;
							//_converted = false;

							MessageBox.Show("FileIsEmptyOrShort");
						}
						else
							ShowUnknownSubtitle();
					}
				}
				else
				{
					MessageBox.Show(string.Format("FileNotFound {0}", fileName));
				}
			}
		}
	}
}
