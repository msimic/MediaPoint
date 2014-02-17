using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MediaPoint.VM.Config;
using Microsoft.Win32;
using System.Linq;

namespace InstallTool
{
    static class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName([MarshalAs(UnmanagedType.LPTStr)]string lpszLongPath, [MarshalAs(UnmanagedType.LPTStr)]StringBuilder lpszShortPath, uint cchBuffer);

        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(HChangeNotifyEventID wEventId,
                                           HChangeNotifyFlags uFlags,
                                           IntPtr dwItem1,
                                           IntPtr dwItem2);

        private static string _userName;

        public static string ToShortPathName(string longName)
        {
            const uint bufferSize = 256;

            // don´t allocate stringbuilder here but outside of the function for fast access
            var shortNameBuffer = new StringBuilder((int)bufferSize);

            GetShortPathName(longName, shortNameBuffer, bufferSize);

            return shortNameBuffer.ToString();
        }

        

        public static void Delete(this RegistryKey key, RegistryKey owner)
        {
            foreach (var valueName in key.GetValueNames())
            {
                key.DeleteValue(valueName);
            }
            key.Flush();

            var skNames = key.GetSubKeyNames();
            
            foreach (var subKeyName in skNames)
            {
                var k = SwallowRegKey(key, subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                k.Delete(key);
                k.Close();
            }

            if (skNames.Length == 0)
            {
                string sk = key.Name.ToLowerInvariant().Substring(owner.Name.ToLowerInvariant().Length + 1);
                owner.DeleteSubKey(sk);
                owner.Flush();
            }
        }

        static string BuildNgenString(string ngenExe, string exe, List<string> dlls, bool install = true)
        {
            // ngen install c:\myfiles\MyLib.dll /ExeConfig:c:\myapps\MyApp.exe
            string ret = "";

            foreach (var dll in dlls)
            {
                ret += string.Format(@" ""{0}"" {3}install ""{1}"" /ExeConfig:""{2}"" ", ngenExe, dll, exe, (!install ? "un" : "")) + Environment.NewLine;
            }

            ret += string.Format(@" ""{0}"" {2}install ""{1}"" ", ngenExe, exe, (!install ? "un" : "")) + Environment.NewLine;
            return ret;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 4 && args[1].ToLowerInvariant() == "/ngen" || args[1].ToLowerInvariant() == "/ungen")
                {
                    bool uninstalling = args[1].ToLowerInvariant() == "/ungen";

                    var ngen = args[2];
                    var exe = args[4];
                    var dlls = new List<string>();
                    for (int i = 5; i < args.Length; i += 2)
                    {
                        if (args[i].ToLowerInvariant() == "/dll" && i + 1 < args.Length)
                        {
                            dlls.Add(args[i + 1]);
                        }
                    }

                    var ngenString = BuildNgenString(ngen, exe, dlls, !uninstalling);
#if DEBUG
                    ngenString = ngenString.Replace("[INSTALLDIR]", @"C:\Program Files (x86)\MediaPoint\");
                    ngenString = ngenString.Replace("[WindowsFolder]", @"C:\Windows\");
#endif

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = @"cmd.exe"; // Specify exe name.
                    startInfo.Arguments = "/k";
                    startInfo.UseShellExecute = false;
                    startInfo.ErrorDialog = false;
                    startInfo.RedirectStandardInput = true;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    //
                    // Start the process.
                    //
                    Process process = Process.Start(startInfo);


                    string[] batchFile = ngenString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    int cmdIndex = 0;

                    while (!process.HasExited)
                    {
                        if (cmdIndex < batchFile.Length)
                        {
                            process.StandardInput.WriteLine(batchFile[cmdIndex++]);
                        }
                        if (cmdIndex >= batchFile.Length)
                        {
                            process.StandardInput.WriteLine("exit");
                            process.StandardInput.WriteLine("");
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    var s = process.StandardOutput.ReadToEnd();
                    var ok = process.WaitForExit(30000);
                    var lines = s.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToArray(); ;
                    lines = lines.Take(Math.Min(lines.Length, 15)).ToArray();
                    var ret = !ok ? -1 : (((process.ExitCode == 0) || uninstalling) ? 0 : -1);
                    if (ret != 0) MessageBox.Show("Ngen failed:" + Environment.NewLine + String.Join(Environment.NewLine, lines), "InstallTool.exe Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(ret);
                }
                else if (args.Length == 5 && args[1].ToLowerInvariant() == "/associate")
                {
                    var type = args[2].ToLowerInvariant();
                    _userName = args[4].ToLowerInvariant();
                    var file = ToShortPathName(args[3].ToLowerInvariant());
                    var dict = type == "video" ? SupportedFiles.Video : SupportedFiles.Audio;

                    var userSid = new NTAccount(_userName).Translate(typeof(SecurityIdentifier)).Value;
                    var currentuser = Registry.Users.OpenSubKey(userSid, true);

                    var classesRoot = Registry.LocalMachine.OpenSubKey(@"Software\Classes", true);

                    //MessageBox.Show(string.Format("associate: {0} '{1}' {2} {3} {4} {5}", type, file, dict.Count, _userName, currentuser.Name, classesRoot.Name));

                    foreach (var ft in dict)
                    {
                        CheckFileAssociation(classesRoot, currentuser,
                                            file, //Application exe path
                                            ft.Key, //Document file extension
                                            ft.Value, //Document type description
                                            file + ",0", //Document type icon
                                            "Play media with MediaPoint", //Action name
                                            "Open" //File command
                                            );
                    }

                    SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                }

                Environment.Exit(0);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + "Stack:" + Environment.NewLine + ex.StackTrace, "InstallTool.exe Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }
        }

        private static void CheckFileAssociation(RegistryKey classesRoot, RegistryKey currentUser, string ExePath, string FileExt, string Description, string Icon, string ActionName, string FileCommand)
        {
            var sk = classesRoot.OpenSubKey("." + FileExt, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (sk != null)
            {
                classesRoot.DeleteSubKeyTree("." + FileExt);
                sk.Close();
            }

            sk = SwallowRegKey(classesRoot, "MediaPoint." + FileExt, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (sk != null)
            {
                sk.Delete(classesRoot);
                sk.Close();
            }

            if (currentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\." + FileExt) != null)
            {
                sk = SwallowRegKey(currentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\." + FileExt,
                                   RegistryKeyPermissionCheck.ReadWriteSubTree);

                if (sk != null)
                {
                    var k = SwallowRegKey(currentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts", RegistryKeyPermissionCheck.ReadWriteSubTree);
                    sk.Delete(k);
                    sk.Close();
                }
            }

            RegistryKey regKey = classesRoot.CreateSubKey("." + FileExt);
            regKey.SetValue("", "MediaPoint." + FileExt);
            regKey = classesRoot.CreateSubKey("MediaPoint." + FileExt);
            regKey.SetValue("", Description);
            regKey.SetValue("EditFlags", 0);
            regKey.SetValue("BrowserFlags", 8);
            if (!string.IsNullOrEmpty(Icon))
            {
                regKey = classesRoot.CreateSubKey("MediaPoint." + FileExt + @"\DefaultIcon");
                regKey.SetValue("", Icon, RegistryValueKind.ExpandString);
            }
            regKey = classesRoot.CreateSubKey("MediaPoint." + FileExt + @"\shell");
            regKey.SetValue("", FileCommand);
            regKey = classesRoot.CreateSubKey("MediaPoint." + FileExt + @"\shell\" + FileCommand);
            regKey.SetValue("", ActionName);
            regKey = classesRoot.CreateSubKey("MediaPoint." + FileExt + @"\shell\" + FileCommand + @"\command");
            regKey.SetValue("", "\"" + ExePath + "\" \"%1\"");

        }

        static RegistryKey SwallowRegKey(RegistryKey root, string key, RegistryKeyPermissionCheck newPermission)
        {
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            //MessageBox.Show("swallow1");
            RegistryKey rk = root.OpenSubKey(key, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);//Get the registry key desired with ChangePermissions Rights.
            if (rk == null) return null;
            RegistrySecurity rs = new RegistrySecurity();
            rs.AddAccessRule(new RegistryAccessRule(userName, RegistryRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AccessControlType.Allow));//Create access rule giving full control to the Administrator user.
            rk.SetAccessControl(rs); //Apply the new access rule to this Registry Key.
            rk.Close();
            //rk = root.OpenSubKey(key, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl); // Opens the key again with full control.
            //rs.SetOwner(new NTAccount(userName));// Set the securitys owner to be Administrator
            //rk.SetAccessControl(rs);// Set the key with the changed permission so Administrator is now owner.
            //rk.Close();
            rk = root.OpenSubKey(key, newPermission, RegistryRights.FullControl);
            //MessageBox.Show("swallow2");
            return rk;
        }
    }

    [Flags]
    enum HChangeNotifyEventID
    {
        /// <summary>
        /// All events have occurred.
        /// </summary>
        SHCNE_ALLEVENTS = 0x7FFFFFFF,

        /// <summary>
        /// A file type association has changed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
        /// must be specified in the <i>uFlags</i> parameter.
        /// <i>dwItem1</i> and <i>dwItem2</i> are not used and must be <see langword="null"/>.
        /// </summary>
        SHCNE_ASSOCCHANGED = 0x08000000,

        /// <summary>
        /// The attributes of an item or folder have changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item or folder that has changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_ATTRIBUTES = 0x00000800,

        /// <summary>
        /// A nonfolder item has been created.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item that was created.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_CREATE = 0x00000002,

        /// <summary>
        /// A nonfolder item has been deleted.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item that was deleted.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DELETE = 0x00000004,

        /// <summary>
        /// A drive has been added.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was added.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEADD = 0x00000100,

        /// <summary>
        /// A drive has been added and the Shell should create a new window for the drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was added.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEADDGUI = 0x00010000,

        /// <summary>
        /// A drive has been removed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEREMOVED = 0x00000080,

        /// <summary>
        /// Not currently used.
        /// </summary>
        SHCNE_EXTENDED_EVENT = 0x04000000,

        /// <summary>
        /// The amount of free space on a drive has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive on which the free space changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_FREESPACE = 0x00040000,

        /// <summary>
        /// Storage media has been inserted into a drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that contains the new media.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MEDIAINSERTED = 0x00000020,

        /// <summary>
        /// Storage media has been removed from a drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive from which the media was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MEDIAREMOVED = 0x00000040,

        /// <summary>
        /// A folder has been created. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
        /// or <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that was created.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MKDIR = 0x00000008,

        /// <summary>
        /// A folder on the local computer is being shared via the network.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that is being shared.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_NETSHARE = 0x00000200,

        /// <summary>
        /// A folder on the local computer is no longer being shared via the network.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that is no longer being shared.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_NETUNSHARE = 0x00000400,

        /// <summary>
        /// The name of a folder has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the previous pointer to an item identifier list (PIDL) or name of the folder.
        /// <i>dwItem2</i> contains the new PIDL or name of the folder.
        /// </summary>
        SHCNE_RENAMEFOLDER = 0x00020000,

        /// <summary>
        /// The name of a nonfolder item has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the previous PIDL or name of the item.
        /// <i>dwItem2</i> contains the new PIDL or name of the item.
        /// </summary>
        SHCNE_RENAMEITEM = 0x00000001,

        /// <summary>
        /// A folder has been removed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_RMDIR = 0x00000010,

        /// <summary>
        /// The computer has disconnected from a server.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the server from which the computer was disconnected.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_SERVERDISCONNECT = 0x00004000,

        /// <summary>
        /// The contents of an existing folder have changed,
        /// but the folder still exists and has not been renamed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that has changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// If a folder has been created, deleted, or renamed, use SHCNE_MKDIR, SHCNE_RMDIR, or
        /// SHCNE_RENAMEFOLDER, respectively, instead.
        /// </summary>
        SHCNE_UPDATEDIR = 0x00001000,

        /// <summary>
        /// An image in the system image list has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_DWORD"/> must be specified in <i>uFlags</i>.
        /// </summary>
        SHCNE_UPDATEIMAGE = 0x00008000,

    }

    [Flags]
    public enum HChangeNotifyFlags
    {
        /// <summary>
        /// The <i>dwItem1</i> and <i>dwItem2</i> parameters are DWORD values.
        /// </summary>
        SHCNF_DWORD = 0x0003,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of ITEMIDLIST structures that
        /// represent the item(s) affected by the change.
        /// Each ITEMIDLIST must be relative to the desktop folder.
        /// </summary>
        SHCNF_IDLIST = 0x0000,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
        /// maximum length MAX_PATH that contain the full path names
        /// of the items affected by the change.
        /// </summary>
        SHCNF_PATHA = 0x0001,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
        /// maximum length MAX_PATH that contain the full path names
        /// of the items affected by the change.
        /// </summary>
        SHCNF_PATHW = 0x0005,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
        /// represent the friendly names of the printer(s) affected by the change.
        /// </summary>
        SHCNF_PRINTERA = 0x0002,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
        /// represent the friendly names of the printer(s) affected by the change.
        /// </summary>
        SHCNF_PRINTERW = 0x0006,
        /// <summary>
        /// The function should not return until the notification
        /// has been delivered to all affected components.
        /// As this flag modifies other data-type flags, it cannot by used by itself.
        /// </summary>
        SHCNF_FLUSH = 0x1000,
        /// <summary>
        /// The function should begin delivering notifications to all affected components
        /// but should return as soon as the notification process has begun.
        /// As this flag modifies other data-type flags, it cannot by used by itself.
        /// </summary>
        SHCNF_FLUSHNOWAIT = 0x2000
    }
}
