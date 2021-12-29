using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComponentFactory.Krypton.Toolkit;

namespace TLOPO_AntiWhiteScreen
{
    public partial class Form1 : KryptonForm
    {
        public string TargetDir = "C:/Program Files/TLOPO";

        public Form1()
        {
            InitializeComponent();

        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            //the main brain
            while (true)
            {
                //check if tlopo is running
                if (Process.GetProcessesByName("tlopo").Length > 0)
                {
                    // is running
                    GameRunning.Text = "True";
                    GameRunning.ForeColor = Color.Green;
                }
                else
                {
                    // isnt running
                    GameRunning.Text = "False";
                    GameRunning.ForeColor = Color.Red;
                }
                //get total ram, and available ram
                Int64 phav = PerformanceInfo.GetPhysicalAvailableMemoryInMiB();
                Int64 tot = PerformanceInfo.GetTotalMemoryInMiB();
                //turn it into percents
                decimal percentFree = ((decimal)phav / (decimal)tot) * 100;
                decimal percentOccupied = 100 - percentFree;
                //update progressbar
                RamAvailability.Value = (int)percentOccupied;
                //check if available ram is less than 1gb
                if (phav < 1000)
                {
                    //not enough ram
                    RamAvailable.Text = "Not enough available";
                    RamAvailable.ForeColor = Color.Red;
                } else
                {
                    //enough ram
                    RamAvailable.Text = "Enough RAM available";
                    RamAvailable.ForeColor = Color.Green;
                }
                
                //check if tlopo directory exists
                if (Directory.Exists(TargetDir))
                {
                    //it exists, check for cache-test folder
                    if (Directory.Exists(TargetDir+"/cache-test"))
                    {
                        //it exists, time to clean up
                        CacheCleared.Text = "Cache NOT cleared";
                        CacheCleared.ForeColor = Color.Red;
                        //get all subfiles
                        string[] files = Directory.GetFiles(TargetDir+"/cache-test");
                        //delete subfiles
                        foreach (string file in files)
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                        }
                        //delay to prevent constantly deleting subfiles while logging in
                        await Task.Delay(2000);
                        //check if files still exist
                        string[] filesAgain = Directory.GetFiles(TargetDir + "/cache-test");
                        if (filesAgain.Length == 0)
                        {
                            //no more files to delete, remove main folder
                            try
                            {
                                //try to delete main folder, placed in try catch to prevent errors incase subfiles are created while deleting main folder
                                Directory.Delete(TargetDir + "/cache-test", false);
                            }
                            catch (IOException)
                            {
                                //throw away error, next loop will attempt again, cant be assed making this recursively delete subfiles until its confirmed to all be gone
                                throw;
                            }
                        }
                        
                    } else
                    {
                        //no cache-test folder found, assume cleared
                        CacheCleared.Text = "Cache cleared";
                        CacheCleared.ForeColor = Color.Green;
                    }
                } else
                {
                    //tlopo directory not found
                    CacheCleared.Text = "TLOPO folder not found";
                    CacheCleared.ForeColor = Color.Red;
                }
                //pause the loop to stop cpu from having a stroke, and allow GUI to update
                await Task.Delay(5000);
            }
        }

        //read from memory
        public static class PerformanceInfo
        {
            [DllImport("psapi.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

            [StructLayout(LayoutKind.Sequential)]
            public struct PerformanceInformation
            {
                public int Size;
                public IntPtr CommitTotal;
                public IntPtr CommitLimit;
                public IntPtr CommitPeak;
                public IntPtr PhysicalTotal;
                public IntPtr PhysicalAvailable;
                public IntPtr SystemCache;
                public IntPtr KernelTotal;
                public IntPtr KernelPaged;
                public IntPtr KernelNonPaged;
                public IntPtr PageSize;
                public int HandlesCount;
                public int ProcessCount;
                public int ThreadCount;
            }

            public static Int64 GetPhysicalAvailableMemoryInMiB()
            {
                PerformanceInformation pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64((pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64() / 1048576));
                }
                else
                {
                    return -1;
                }

            }

            public static Int64 GetTotalMemoryInMiB()
            {
                PerformanceInformation pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64((pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1048576));
                }
                else
                {
                    return -1;
                }

            }
        }

        private void kryptonButton1_Click(object sender, EventArgs e)
        {
            //user wants to change default location, warn them first
            DialogResult dialogResult = MessageBox.Show("Are you sure you know what you are doing?\n\nPerfoming this action may delete files that you do not wish to delete.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
            if (dialogResult == DialogResult.Yes)
            {
                //user REALLY wants to change default location, ok, open folder explorer
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.ShowNewFolderButton = false;
                fbd.RootFolder = System.Environment.SpecialFolder.MyComputer;

                //check if user confirmed TLOPO location
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    //user confirmed, update target directory with new directory value
                    TargetDir = fbd.SelectedPath;
                }
            }
        }
    }
}
