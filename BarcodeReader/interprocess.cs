using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.Threading;
using System.Diagnostics;

namespace BarcodeReader
{
  
    public class ProcessHelper
    {
        public static void CloseWaiter()
        {
            string windowTitle = "FeedMe";
            Thread.Sleep(1000);
            windowTitle = windowTitle.ToLower();
            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    if (process.MainWindowTitle.ToLower().Contains(windowTitle))
                    {
                        process.CloseMainWindow();
                    }
                }
            }
        }
    }

  public class Invoker
    {
        public Action<string> sDel;
        private MainForm owner;

        public Invoker(MainForm wOwner)
        {
            owner = wOwner;
        }

        public void Invoke(string sArg)
        {
            owner.Invoke(sDel, sArg);
        }

        public void BeginInvoke(string sArg)
        {
            owner.BeginInvoke(sDel, sArg);
        }
    }

    public class Pipeserver
    {
        public static MainForm owner;
        public static Invoker ownerInvoker;
        public static string pipeName;
        private static NamedPipeServerStream pipeServer;
        private static readonly int BufferSize = 256;
        private static void ExecuteCommand(String sCommand)
        {
            owner.ExecuteCommand(sCommand);
        }

        public static void createPipeServer()
        {
            Decoder decoder = Encoding.Default.GetDecoder();
            Byte[] bytes = new Byte[BufferSize];
            char[] chars = new char[BufferSize];
            int numBytes = 0;
            StringBuilder msg = new StringBuilder();
            ownerInvoker = new Invoker(owner);
            ownerInvoker.sDel = ExecuteCommand;
            pipeName = "BarcodeServer";
            try
            {
                pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                                                       PipeTransmissionMode.Message,
                                                       PipeOptions.Asynchronous);
                while (true)
                {
                    pipeServer.WaitForConnection();
                    do
                    {
                        msg.Length = 0;
                        do
                        {
                            numBytes = pipeServer.Read(bytes, 0, BufferSize);
                            if (numBytes > 0)
                            {
                                int numChars = decoder.GetCharCount(bytes, 0, numBytes);
                                decoder.GetChars(bytes, 0, numBytes, chars, 0, false);
                                msg.Append(chars, 0, numChars);
                            }
                        } while (numBytes > 0 && !pipeServer.IsMessageComplete);
                        decoder.Reset();
                        if (numBytes > 0)
                        {
                            ownerInvoker.Invoke(msg.ToString());
                        }
                    } while (numBytes != 0);
                    pipeServer.Disconnect();
                }
            }
            catch (Exception)
            {
                //throw new Exception("Failed to create pipeServer! the detailed messages are: " + ex.Message);
                //MessageBox.Show(ex.Message);
            }
        }

        internal static void Close()
        {
            if (pipeServer == null)
                return;
            if(pipeServer.IsConnected)
                pipeServer.Disconnect();
            pipeServer.Close();
        }
    }
}
