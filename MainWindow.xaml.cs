using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace SHNK_Booster
{
    // هنا تم إرجاع تعريف الكلاس المفقود
    public partial class MainWindow : Window
    {
        // 🔗 متغيرات التحديث (تأكد من رقم النسخة هنا)
        private readonly string currentVersion = "1.0.3";
        private readonly string versionUrl = "https://raw.githubusercontent.com/sh9k/SHNK-Booster/main/version.txt";
        private readonly string downloadUrl = "https://github.com/sh9k/SHNK-Booster/releases/latest/download/SHNK-BOOSTER.exe";

        private DispatcherTimer timer;
        private ulong lastIdleTime;
        private ulong lastSystemTime;

        public MainWindow()
        {
            InitializeComponent();

            // تنظيف مخلفات التحديث السابق والتحقق من الجديد
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
            catch (Exception)
            {
                // إخفاء رسالة الخطأ إذا لم يكن هناك إنترنت
            }
        }

        // 🚀 الدالة السحرية للتحديث الصامت بالخلفية
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

                    // الخدعة: تغيير اسم الملف الحالي وهو يعمل إلى .old
                    if (File.Exists(backupExe)) File.Delete(backupExe);
                    File.Move(currentExe, backupExe);

                    // حفظ الملف الجديد المحمل بنفس الاسم الأصلي
                    File.WriteAllBytes(currentExe, fileBytes);

                    StatusText.Text = "UPDATE COMPLETE ✅ RESTARTING...";
                    await Task.Delay(1000); // انتظار ثانية ليرى المستخدم الرسالة

                    // تشغيل النسخة الجديدة وإغلاق القديمة
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

        // 🧹 دالة تنظيف الملف القديم بعد التحديث
        private void CleanupOldUpdates()
        {
            try
            {
                string backupExe = Process.GetCurrentProcess().MainModule.FileName + ".old";
                if (File.Exists(backupExe))
                {
                    File.Delete(backupExe);
                }
            }
            catch { }
        }

        // ==========================================
        // 🛠️ دوال التحكم بالنافذة (Title Bar)
        // ==========================================
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ==========================================
        // 🛠️ استدعاءات النواة (WinAPI Low-Level)
        // ==========================================
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

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
                ulong currentKernelTime = ((ulong)kernelTime.dwHighDateTime << 32) | kernelTime.dwLowDateTime;
                ulong currentUserTime = ((ulong)userTime.dwHighDateTime << 32) | userTime.dwLowDateTime;

                ulong currentSystemTime = currentKernelTime + currentUserTime;

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
        private void BoostBtn_Click(object sender, RoutedEventArgs e)
        {
            ApplyBoost(false);
        }

        private void TurboBtn_Click(object sender, RoutedEventArgs e)
        {
            ApplyBoost(true);
        }

        private void UltraBtn_Click(object sender, RoutedEventArgs e)
        {
            UltraMode();
        }

        private void RestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            Restore();
        }

        void ApplyBoost(bool isTurbo)
        {
            var processes = Process.GetProcessesByName("AndroidEmulatorEx");

            if (processes.Length == 0)
            {
                StatusText.Text = "OPEN GAMELOOP FIRST ⚠️";
                return;
            }

            StatusText.Text = isTurbo ? "TURBO ACTIVATED 🔥" : "BOOST APPLIED ✅";

            foreach (var p in processes)
            {
                try { p.PriorityClass = ProcessPriorityClass.High; } catch { }
            }

            if (isTurbo) TrimHeavyApps();
        }

        void UltraMode()
        {
            var processes = Process.GetProcessesByName("AndroidEmulatorEx");

            if (processes.Length == 0)
            {
                StatusText.Text = "OPEN GAMELOOP FIRST ⚠️";
                return;
            }

            StatusText.Text = "ULTRA MODE 🚀 MAX PERFORMANCE";

            foreach (var p in processes)
            {
                try { p.PriorityClass = ProcessPriorityClass.RealTime; } catch { }
            }

            TrimHeavyApps();

            try { EmptyWorkingSet((IntPtr)(-1)); } catch { }
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
            var processes = Process.GetProcessesByName("AndroidEmulatorEx");

            foreach (var p in processes)
            {
                try { p.PriorityClass = ProcessPriorityClass.Normal; } catch { }
            }

            StatusText.Text = "SYSTEM RESTORED ✅";
        }
    }
}
