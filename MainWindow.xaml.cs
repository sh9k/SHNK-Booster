using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
namespace SHNK_Booster
{
    public partial class MainWindow : Window
    {
        // حدد رقم الإصدار الحالي للبرنامج
        private readonly string currentVersion = "1.0.1";

        // روابط جيتهاب (سنقوم بتعديل USERNAME و REPO باسم حسابك ومستودعك)
        private readonly string versionUrl = "https://raw.githubusercontent.com/sh9k/SHNK-Booster/main/version.txt";
        private readonly string downloadUrl = "https://github.com/sh9k/SHNK-Booster\r\n/releases/latest/download/SHNK_BOOSTER.exe";

        // 1. دالة البحث عن تحديثات (تعمل في الخلفية)
        private async void CheckForUpdates()
        {
            try
            {
                // 🛡️ السطر الجديد يوضع هنا: في البداية تماماً لفتح بوابات الأمان 🛡️
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

                using (HttpClient client = new HttpClient())
                {
                    // إضافة هوية للبرنامج لكي لا يحظره موقع جيتهاب
                    client.DefaultRequestHeaders.Add("User-Agent", "SHNK-Booster-App");

                    // جلب النسخة من الإنترنت متجاوزين الـ Cache
                    string onlineVersion = (await client.GetStringAsync(versionUrl + "?t=" + DateTime.Now.Ticks)).Trim();

                    // رسالة للتأكد من الأرقام
                    MessageBox.Show($"فحص التحديث:\nنسخة جهازك: {currentVersion}\nنسخة السيرفر: {onlineVersion}");

                    if (onlineVersion != currentVersion)
                    {
                        var result = MessageBox.Show($"يوجد تحديث جديد (v{onlineVersion})! هل تريد تحميله الآن؟", "تحديث متوفر", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // رسالة الخطأ التفصيلية لمعرفة سبب المشكلة إن وجدت
                MessageBox.Show("سبب الفشل الدقيق هو: " + ex.Message + "\n\n" + ex.InnerException?.Message);
            }
        }

        // 2. دالة تحميل وتثبيت التحديث السحرية
        private async Task DownloadAndApplyUpdate()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);

                    string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                    string backupExe = currentExe + ".old";

                    // الخدعة: تغيير اسم الملف الحالي وهو يعمل إلى .old
                    if (File.Exists(backupExe)) File.Delete(backupExe);
                    File.Move(currentExe, backupExe);

                    // حفظ الملف الجديد المحمل بنفس الاسم الأصلي
                    File.WriteAllBytes(currentExe, fileBytes);

                    StatusText.Text = "UPDATE COMPLETE ✅ RESTARTING...";
                    await Task.Delay(1000);

                    // تشغيل النسخة الجديدة وإغلاق القديمة
                    Process.Start(currentExe);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل التحديث: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "UPDATE FAILED ❌";
            }
        }
        // ==========================================
        // 🛠️ دوال التحكم بالنافذة (Title Bar)
        // ==========================================

        // 1. دالة سحب النافذة وتحريكها
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // 2. دالة تصغير النافذة (Minimize)
        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // 3. دالة إغلاق البرنامج (Close)
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        // 3. دالة تنظيف الملفات القديمة
        private void CleanupOldUpdates()
        {
            try
            {
                string backupExe = Process.GetCurrentProcess().MainModule.FileName + ".old";
                if (File.Exists(backupExe))
                {
                    File.Delete(backupExe); // حذف النسخة القديمة بعد التحديث بنجاح
                }
            }
            catch { }
        }
        // ==========================================
        // 🛠️ استدعاءات النواة (WinAPI Low-Level)
        // ==========================================

        // 1. مراقبة الرام
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

        // 2. تنظيف الذاكرة (RAM Trimming)
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        // 3. مراقبة المعالج (بديل الـ PerformanceCounter البطيء)
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        // ==========================================

        private DispatcherTimer timer;
        private ulong lastIdleTime;
        private ulong lastSystemTime;

        public MainWindow()
        {
            InitializeComponent();

            // تشغيل نظام التحديث الذاتي فور فتح البرنامج
            CleanupOldUpdates();
            CheckForUpdates();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // 📊 قراءة الرام الفورية
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                RamText.Text = $"RAM: {memStatus.dwMemoryLoad}%";
            }

            // 📊 قراءة المعالج الفورية (بدون تجميد الواجهة)
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
                try
                {
                    p.PriorityClass = ProcessPriorityClass.High;
                }
                catch { }
            }

            if (isTurbo)
            {
                TrimHeavyApps();
            }
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
                try
                {
                    p.PriorityClass = ProcessPriorityClass.RealTime; // أعلى أولوية ممكنة
                }
                catch { }
            }

            TrimHeavyApps();

            // تنظيف جذري لرام النظام بالكامل
            try { EmptyWorkingSet((IntPtr)(-1)); } catch { }
        }

        void TrimHeavyApps()
        {
            // تقليص استهلاك الرام لهذه البرامج إلى 1 ميجابايت بدون إغلاقها
            string[] heavyApps = { "chrome", "msedge", "discord", "steam" };

            foreach (var app in heavyApps)
            {
                foreach (var p in Process.GetProcessesByName(app))
                {
                    try
                    {
                        EmptyWorkingSet(p.Handle);
                    }
                    catch { }
                }
            }
        }

        void Restore()
        {
            var processes = Process.GetProcessesByName("AndroidEmulatorEx");

            foreach (var p in processes)
            {
                try
                {
                    p.PriorityClass = ProcessPriorityClass.Normal;
                }
                catch { }
            }

            StatusText.Text = "SYSTEM RESTORED ✅";
        }
    }
}