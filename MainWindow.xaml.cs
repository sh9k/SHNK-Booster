using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Windows.Media;

namespace SHNK_Booster
{
    public partial class MainWindow : Window
    {
        // 🔗 متغيرات التحديث
        private readonly string currentVersion = "1.0.6";
        private readonly string versionUrl = "https://raw.githubusercontent.com/sh9k/SHNK-Booster/main/version.txt";
        private readonly string downloadUrl = "https://github.com/sh9k/SHNK-Booster/releases/latest/download/SHNK-BOOSTER.exe";

        private DispatcherTimer timer;
        private ulong lastIdleTime;
        private ulong lastSystemTime;

        // ==========================================
        // 🎮 متغيرات وضع المحاكي (الاحترافية)
        // ==========================================
        // الوضع الافتراضي عند التشغيل هو GameLoop بجميع محركاته المحتملة
        private string[] targetProcesses = { "AndroidEmulatorEx", "AndroidEmulator", "aow_exe" };
        private string emulatorDisplayName = "GAMELOOP";

        // ==========================================
        // 🛡️ متغيرات ودوال التيربو الفخمة
        // ==========================================
        private bool isTurboActive = false;

        // قائمة بالخدمات الثقيلة التي سنوقفها للعب
        private readonly string[] heavyWindowsServices = { "wuauserv", "BITS", "WSearch", "SysMain", "Spooler" };

        // 🥷 دالة لتنفيذ أوامر الويندوز العميقة بصمت
        private void ExecuteHiddenCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process p = Process.Start(psi))
                {
                    p?.WaitForExit(4000);
                }
            }
            catch { }
        }
        // ==========================================

        public MainWindow()
        {
            InitializeComponent();

            CleanupOldUpdates();
            CheckForUpdates();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private async void CheckForUpdates()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SHNK-Booster-App");
                    string onlineVersion = (await client.GetStringAsync(versionUrl + "?t=" + DateTime.Now.Ticks)).Trim();

                    if (onlineVersion != currentVersion)
                    {
                        var result = MessageBox.Show($"يوجد تحديث جديد (v{onlineVersion})! هل تريد تحميله وتثبيته الآن؟", "تحديث متوفر", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (result == MessageBoxResult.Yes)
                        {
                            await DownloadAndApplyUpdate();
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private async Task DownloadAndApplyUpdate()
        {
            try
            {
                MessageBox.Show("جاري تحميل التحديث في الخلفية... سيتم إعادة تشغيل البرنامج تلقائياً عند الانتهاء.", "تحديث", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = "DOWNLOADING UPDATE... ⏳";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SHNK-Booster-App");
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);

                    string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                    string backupExe = currentExe + ".old";

                    if (File.Exists(backupExe)) File.Delete(backupExe);
                    File.Move(currentExe, backupExe);

                    File.WriteAllBytes(currentExe, fileBytes);

                    StatusText.Text = "UPDATE COMPLETE ✅ RESTARTING...";
                    await Task.Delay(1000);

                    Process.Start(currentExe);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل التحديث: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "UPDATE FAILED ❌";
            }
        }

        private void CleanupOldUpdates()
        {
            try
            {
                string backupExe = Process.GetCurrentProcess().MainModule.FileName + ".old";
                if (File.Exists(backupExe)) File.Delete(backupExe);
            }
            catch { }
        }

        // ==========================================
        // 🔄 أزرار التبديل بين المحاكيات
        // ==========================================
        private void BtnGameLoop_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 استهداف جميع محركات جيم لوب (الجديدة والقديمة)
            targetProcesses = new string[] { "AndroidEmulatorEx", "AndroidEmulator", "aow_exe" };
            emulatorDisplayName = "GAMELOOP";

            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            StatusText.Foreground = Brushes.Cyan;
            StatusText.Text = "GAMELOOP MODE SELECTED 🔵";
        }

        private void BtnLDPlayer_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 استهداف المحرك الحقيقي للـ LDPlayer مع الخدمات والواجهة
            targetProcesses = new string[] { "Ld9BoxHeadless", "Ld9BoxSVC", "dnplayer" };
            emulatorDisplayName = "LDPLAYER";

            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C2C2C"));
            StatusText.Foreground = Brushes.Orange;
            StatusText.Text = "LDPLAYER MODE SELECTED 🟡";
        }

        // ==========================================
        // 🛠️ دوال التحكم بالنافذة 
        // ==========================================
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        // ==========================================
        // 🛠️ استدعاءات النواة (WinAPI Low-Level)
        // ==========================================
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength, dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME { public uint dwLowDateTime, dwHighDateTime; }

        // ==========================================
        // 📊 قراءة ومراقبة الموارد
        // ==========================================
        private void Timer_Tick(object sender, EventArgs e)
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                RamText.Text = $"RAM: {memStatus.dwMemoryLoad}%";
            }

            if (GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime))
            {
                ulong currentIdleTime = ((ulong)idleTime.dwHighDateTime << 32) | idleTime.dwLowDateTime;
                ulong currentSystemTime = (((ulong)kernelTime.dwHighDateTime << 32) | kernelTime.dwLowDateTime) + (((ulong)userTime.dwHighDateTime << 32) | userTime.dwLowDateTime);

                if (lastSystemTime > 0)
                {
                    ulong systemTimeDelta = currentSystemTime - lastSystemTime;
                    ulong idleTimeDelta = currentIdleTime - lastIdleTime;

                    if (systemTimeDelta > 0)
                    {
                        ulong cpuUsage = (systemTimeDelta - idleTimeDelta) * 100 / systemTimeDelta;
                        CpuText.Text = $"CPU: {cpuUsage}%";
                    }
                }

                lastIdleTime = currentIdleTime;
                lastSystemTime = currentSystemTime;
            }
        }

        // ==========================================
        // 🚀 أزرار الأداء
        // ==========================================
        private async void BoostBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "CLEANING SYSTEM... ⏳";

                int deletedFilesCount = await Task.Run(() =>
                {
                    int count = 0;
                    string[] foldersToClean = { Path.GetTempPath(), @"C:\Windows\Temp", @"C:\Windows\Prefetch" };
                    foreach (string folder in foldersToClean)
                    {
                        if (Directory.Exists(folder))
                        {
                            try { foreach (string file in Directory.GetFiles(folder)) { try { File.Delete(file); count++; } catch { } } } catch { }
                        }
                    }
                    return count;
                });

                // التفريغ الذكي للبرامج الثقيلة فقط
                TrimHeavyApps();
                ApplyBoost(false);

                MessageBox.Show($"تم تنظيف النظام بنجاح! 🚀\nتم مسح {deletedFilesCount} ملف غير ضروري.", "SHNK BOOSTER", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "BOOST FAILED ❌";
                MessageBox.Show("حدث خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TurboBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckIfEmulatorRunning()) return;

                StatusText.Text = "ACTIVATING TURBO... 🔥";

                await Task.Run(() => ExecuteHiddenCommand("ipconfig /flushdns"));
                await Task.Run(() =>
                {
                    foreach (string service in heavyWindowsServices) ExecuteHiddenCommand($"net stop \"{service}\" /y");
                });

                ApplyBoost(true);
                isTurboActive = true;

                MessageBox.Show("تم تفعيل وضع التيربو الاحترافي! 🚀\n- تم إيقاف التحديثات والفهرسة.\n- تم تحسين شبكة الإنترنت.\n- تم توجيه كل الموارد للمحاكي.", "SHNK TURBO", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "TURBO FAILED ❌";
                MessageBox.Show("حدث خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UltraBtn_Click(object sender, RoutedEventArgs e) => UltraMode();

        private async void RestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "RESTORING SYSTEM... ♻️";

                if (isTurboActive)
                {
                    await Task.Run(() =>
                    {
                        foreach (string service in heavyWindowsServices) ExecuteHiddenCommand($"net start \"{service}\" /y");
                    });
                    isTurboActive = false;
                }

                Restore();

                MessageBox.Show("تمت استعادة النظام بنجاح! ✅\nعادت جميع خدمات الويندوز والشبكة للعمل بشكل طبيعي وآمن.", "SHNK RESTORE", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "RESTORE FAILED ❌";
                MessageBox.Show("حدث خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // 🧠 دوال المعالجة الأساسية
        // ==========================================
        bool CheckIfEmulatorRunning()
        {
            foreach (string procName in targetProcesses)
            {
                if (Process.GetProcessesByName(procName).Length > 0) return true;
            }
            StatusText.Text = $"OPEN {emulatorDisplayName} FIRST ⚠️";
            return false;
        }

        void ApplyBoost(bool isTurbo)
        {
            if (!CheckIfEmulatorRunning()) return;

            StatusText.Text = isTurbo ? "TURBO ACTIVATED 🔥" : "BOOST APPLIED ✅";

            foreach (string procName in targetProcesses)
            {
                foreach (var p in Process.GetProcessesByName(procName))
                {
                    try { p.PriorityClass = ProcessPriorityClass.High; } catch { }
                }
            }

            if (isTurbo) TrimHeavyApps();
        }

        void UltraMode()
        {
            if (!CheckIfEmulatorRunning()) return;

            StatusText.Text = "ULTRA MODE 🚀 MAX PERFORMANCE";

            foreach (string procName in targetProcesses)
            {
                foreach (var p in Process.GetProcessesByName(procName))
                {
                    try { p.PriorityClass = ProcessPriorityClass.RealTime; } catch { }
                }
            }

            TrimHeavyApps();
        }

        void TrimHeavyApps()
        {
            string[] heavyApps = { "chrome", "msedge", "discord", "steam" };

            foreach (var app in heavyApps)
            {
                foreach (var p in Process.GetProcessesByName(app))
                {
                    try { EmptyWorkingSet(p.Handle); } catch { }
                }
            }
        }

        void Restore()
        {
            foreach (string procName in targetProcesses)
            {
                foreach (var p in Process.GetProcessesByName(procName))
                {
                    try { p.PriorityClass = ProcessPriorityClass.Normal; } catch { }
                }
            }

            StatusText.Text = "SYSTEM RESTORED ✅";
        }
    }
}
