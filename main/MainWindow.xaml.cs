using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace main {

    public partial class MainWindow : Window, INotifyPropertyChanged {

        private int tickCount = 0;
        private double progressBarWidth = 330;
        
        // DLL Path
        //  private string usedDLLPath = "B:\\Main\\Main Projects\\! Abyss\\New\\build\\Abyss v1.4.dll";
        private string usedDLLPath = "C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\.minecraft\\libraries\\abyss.dll"; // C:\\Windows\\Cursors\\abyss.dll
        private string downloadDLLUrl = "https://github.com/Abyss-Client/Abyss/releases/latest/download/Abyss.dll";

        public event PropertyChangedEventHandler? PropertyChanged;

        public double ProgressBarWidth {
            get { return progressBarWidth; }
            set { progressBarWidth = value; OnPropertyChanged(); }
        }

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.025D);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {

            if (this.tickCount == 50) {
                Application.Current.Shutdown();
            } else {
                this.tickCount++;

                if (!Environment.HasShutdownStarted) {
                    if (ProgressBarWidth == 0) {
                        Thread.Sleep(250);

                        ((DispatcherTimer)sender).Stop();

                        Process? process = Process.GetProcessesByName("javaw").FirstOrDefault();
                        if (process != null && process.MainWindowTitle.Contains("1.8.9")) {
                            if (this.downloadDLL()) {
                                Thread.Sleep(2000);

                                try {
                                    if (File.Exists(this.usedDLLPath)) {
                                        Injector.InjectDLL(process.Id, usedDLLPath);
                                    }
                                } catch (Exception exc) {
                                    MessageBox.Show("Error: " + exc.Message + "!", "Abyss Injector, v1.3");
                                }
                            } else { 
                                MessageBox.Show("Error: Trying to inject using this path: \n " +
                                    " 'C:/Users/" + Environment.UserName + "/AppData/Roaming/.minecraft/libraries/abyss.dll' ! ", "Abyss Injector, v1.3");

                                Injector.InjectDLL(process.Id, usedDLLPath);
                            }

                            Application.Current.Shutdown();
                        } else {
                            MessageBox.Show("Error: Open Minecraft first!", "Abyss Injector, v1.3");
                            Application.Current.Shutdown();
                        }
                    } else {
                        ProgressBarWidth -= 10;
                    }
                } else {
                    Application.Current.Shutdown();
                }
            }
        }

        private void checkAgain() {
            if (this.tickCount == 50) {
                MessageBox.Show("Error: " + this.tickCount + " Ticks passed!", "Abyss Injector, v1.3");
                Application.Current.Shutdown();
            }
        }

        private bool downloadDLL() {
            this.checkAgain();

            try {
                if (File.Exists(this.usedDLLPath)) {
                    File.Delete(this.usedDLLPath);
                }
            } catch (Exception) {
                return false;
            }

            this.checkAgain();
            using (WebClient webClient = new WebClient()) {
                try {
                    webClient.DownloadFile(this.downloadDLLUrl, this.usedDLLPath);
                    return true;
                } catch (Exception e) {
                    MessageBox.Show("Error: " + e.Message + "!", "Abyss Injector, v1.3");
                    return false;
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            this.checkAgain();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    class Injector
    {

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = false)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        public static void InjectDLL(int processId, string dllPath) {

            IntPtr hProcess = OpenProcess(0x1F0FFF, false, processId);
            if (hProcess == IntPtr.Zero) {
                MessageBox.Show("Error: Could not open Process!", "Abyss Injector, v1.3");
                return;
            }

            IntPtr hKernel = GetModuleHandle("kernel32.dll");
            if (hKernel == IntPtr.Zero) {
                MessageBox.Show("Error: Could not get handle for kernel32 (DLL)!", "Abyss Injector, v1.3");
                return;
            }

            IntPtr loadLibraryAddr = GetProcAddress(hKernel, "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero) {
                MessageBox.Show("Error: Could not get address for LoadLibraryA!", "Abyss Injector, v1.3");
                return;
            }

            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), 0x1000, 0x40);
            if (allocMemAddress == IntPtr.Zero) {
                MessageBox.Show("Error: Could not allocate memory in the remote process!", "Abyss Injector, v1.3");
                return;
            }

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(dllPath);
            UIntPtr bytesWritten;
            if (!WriteProcessMemory(hProcess, allocMemAddress, bytes, (uint)bytes.Length, out bytesWritten)) {
                MessageBox.Show("Error: Could not write to memory in the remote process!", "Abyss Injector, v1.3");
                return;
            }

            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
            if (hThread == IntPtr.Zero) {
                MessageBox.Show("Error: Could not create remote thread!", "Abyss Injector, v1.3");
                return;
            }
        }
    }

}
