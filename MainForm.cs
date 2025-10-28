using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;

namespace PhasmoCheatV_Loader
{
    public partial class MainForm : Form
    {
        private string currentTempDllPath = null;
        private bool isDragging = false;
        private Point startPoint = new Point(0, 0);
        private string fullText = "PhasmoCheatV Loader is a free cheat for Phasmophobia, created by the VCom Team.\n\n" +
                          "You can use it without any limits.\n\n" +
                          "The official version is available on our Telegram channel, website, and selected forums.";

        private int currentIndex = 0;
        public static string logs = "";

        public static string versionSig = "30 2E 31 34 2E 32 2E 31";
        string version = MemoryScanner.FindVersion("Phasmophobia", versionSig);

        private int notificalState = 0;
        private int stayCounter = 0;
        public enum NotificationType
        {
            Info,
            Warning,
            Error
        }


        public MainForm()
        {
            MessageBox.Show("This modification is free! If you bought it, you lost money.", "INFO MONEY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            InitializeComponent();
            label5.Text = "";
            InfoTimer.Interval = 50;
            InfoTimer.Tick += InfoTimer_Tick;
            InfoTimer.Start();
            NotificalPanel.Visible = false;
            notificalTimer.Interval = 50;
            notificalTimer.Tick += NotificalTimer_Tick;
            FindVersionTimer.Interval = 1500;
            FindVersionTimer.Tick += FindVersionTimer_Tick;
            FindVersionTimer.Start();
            guna2TextBox1.Multiline = true;
            guna2TextBox1.TextAlign = HorizontalAlignment.Center;
            guna2TextBox1.Text = "Waiting\nPhasmophobia...";
            guna2TextBox1.ReadOnly = true;
            guna2TextBox1.TabStop = false;
            guna2TextBox1.Enter += (s, e) => this.ActiveControl = null;
            guna2TextBox1.MouseDown += (s, e) => guna2TextBox1.SelectionLength = 0;
            guna2TextBox1.GotFocus += (s, e) => guna2TextBox1.SelectionLength = 0;

        }

        private void ShowNotification(NotificationType type, string message)
        {
            Color borderColor;
            string headerText;
            switch (type)
            {
                case NotificationType.Info:
                    borderColor = Color.FromArgb(0, 122, 204);
                    headerText = "INFO";
                    break;

                case NotificationType.Warning:
                    borderColor = Color.FromArgb(255, 160, 0);
                    headerText = "WARNING";
                    break;

                case NotificationType.Error:
                    borderColor = Color.FromArgb(220, 20, 60);
                    headerText = "ERROR";
                    break;

                default:
                    borderColor = Color.Gray;
                    headerText = "NOTICE";
                    break;
            }
            NotificalPanel.BorderColor = borderColor;
            NotificationHeader.ForeColor = borderColor;
            NotificationHeader.Text = headerText;
            guna2TextBox2.MaxLength = 69;
            if (message.Length > 66)
            {
                message = message.Substring(0, 69) + "...";
            }
            guna2TextBox2.Text = message;
            NotificalPanel.Visible = true;
            notificalState = 1;
            stayCounter = 0;
            notificalTimer.Start();
        }

        private void NotificalTimer_Tick(object sender, EventArgs e)
        {
            switch (notificalState)
            {
                case 1:
                    notificalState = 2;
                    break;

                case 2:
                    stayCounter++;
                    if (stayCounter >= 100)
                    {
                        notificalState = 3;
                    }
                    break;

                case 3:
                    NotificalPanel.Visible = false;
                    notificalTimer.Stop();
                    notificalState = 0;
                    break;
            }
        }
        private void FindVersionTimer_Tick(object sender, EventArgs e)
        {
            CheckForVersion();
        }
        private void CheckForVersion()
        {
            try
            {
                if (Process.GetProcessesByName("Phasmophobia").Length == 0)
                {
                    guna2TextBox1.Text = "Waiting\nPhasmophobia...";
                    return;
                }

                version = MemoryScanner.FindVersion("Phasmophobia", versionSig);

                if (!string.IsNullOrEmpty(version))
                {
                    guna2TextBox1.Text = $"Current version:\r\n{version}";
                    Console.WriteLine("Game version: " + version);

                    FindVersionTimer.Stop();
                }
                else
                {
                    guna2TextBox1.Text = "Waiting\nPhasmophobia...";
                    Console.WriteLine("Version not found.");
                }
            }
            catch
            {
                guna2TextBox1.Text = "Waiting\nPhasmophobia...";
            }
        }
        private void MiniBTN_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void CloseBTN_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;
            startPoint = new Point(e.X, e.Y);
        }
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point p = PointToScreen(e.Location);
                this.Location = new Point(p.X - startPoint.X, p.Y - startPoint.Y);
            }
        }
        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
        private void InfoTimer_Tick(object sender, EventArgs e)
        {
            if (currentIndex < fullText.Length)
            {
                label5.Text += fullText[currentIndex];
                currentIndex++;
            }
            else
            {
                InfoTimer.Stop();
            }
        }
        private void InjectBtn_Click(object sender, EventArgs e)
        {
            if (InjectBtn.Text == "Yes")
            {
                if (string.IsNullOrEmpty(currentTempDllPath) || !File.Exists(currentTempDllPath))
                {
                    logs += $"\n[{DateTime.Now:HH:mm:ss}] No temporary DLL to inject.\n";
                    this.Invoke(new Action(() =>
                    {
                        ShowNotification(NotificationType.Error, "No temporary DLL found to inject. Please press inject again.");
                    }));
                    return;
                }

                Thread injectThread1 = new Thread(() => InjectProcess(currentTempDllPath));
                injectThread1.IsBackground = true;
                injectThread1.Start();
                return;
            }
            string dataDllPath = Path.Combine(Application.StartupPath, "data", "cheat.dll");

            if (!File.Exists(dataDllPath))
            {
                logs += $"\n[{DateTime.Now:HH:mm:ss}] cheat.dll not found in data folder!\n";
                this.Invoke(new Action(() =>
                {
                    ShowNotification(NotificationType.Error, "cheat.dll not found in data folder!");
                }));
                return;
            }
            if (!IsGameRunning("Phasmophobia"))
            {
                logs += $"\n[{DateTime.Now:HH:mm:ss}] Game is not running!\n";
                this.Invoke(new Action(() =>
                {
                    ShowNotification(NotificationType.Error, "Phasmophobia is not running!");
                }));
                return;
            }
            string version = MemoryScanner.FindVersion("Phasmophobia", versionSig);
            if (version != "0.14.2.1")
            {
                string oldButtonText = InjectBtn.Text;
                string oldTextBoxText = guna2TextBox1.Text;
                InjectBtn.Text = "Yes";
                guna2TextBox1.Text = "Version incorrect. Injecting?";

                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 15000;
                timer.Tick += (s, args) =>
                {
                    if (InjectBtn.Text == "Yes")
                    {
                        InjectBtn.Text = oldButtonText;
                        guna2TextBox1.Text = $"Current version:\r\n{version}";
                    }
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();

                return;
            }
            string tempFileName = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName().Replace('.', '_')}.dll");
            try
            {
                File.Copy(dataDllPath, tempFileName, true);
                currentTempDllPath = tempFileName; // <-- запоминаем путь, чтобы "Yes" ветка знала, что инжектить
                logs += $"\n[{DateTime.Now:HH:mm:ss}] cheat.dll copied to temp as {Path.GetFileName(tempFileName)}\n";
            }
            catch (Exception ex)
            {
                logs += $"\n[{DateTime.Now:HH:mm:ss}] Failed to copy cheat.dll: {ex.Message}\n";
                this.Invoke(new Action(() =>
                {
                    ShowNotification(NotificationType.Error, $"Failed to copy cheat.dll: {ex.Message}");
                }));
                return;
            }
            Thread injectThread = new Thread(() => InjectProcess(tempFileName));
            injectThread.IsBackground = true;
            injectThread.Start();
        }

        private bool IsGameRunning(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }
        private void InjectProcess(string tempFileNames)
        {
            try
            {
                logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Starting injection process...\n";
                int processId = 0;
                int attempts = 0;
                while (processId == 0 && attempts < 3)
                {
                    processId = Injector.Injector.GetProcessIdByName("Phasmophobia.exe");
                    Thread.Sleep(30);
                    attempts++;
                }
                if (processId == 0)
                {
                    logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Phasmophobia.exe not found after {attempts} attempts!\n";
                    this.Invoke(new Action(() =>
                    {
                        ShowNotification(NotificationType.Error, "Phasmophobia.exe not found! Make sure the game is running.");
                    }));
                    return;
                }
                logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Process found! PID: {processId}\n";

                bool injectionResult = Injector.Injector.InjectDLL(processId, tempFileNames);

                if (injectionResult)
                {
                    logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Injection completed successfully!\n";
                    this.Invoke(new Action(() =>
                    {
                        ShowNotification(NotificationType.Info, "Injection completed successfully!");
                    }));
                }
                else
                {
                    logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Injection failed!\n";
                    this.Invoke(new Action(() =>
                    {
                        ShowNotification(NotificationType.Error, "Injection failed!");
                    }));
                }
            }
            catch (Exception ex)
            {
                logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Error during injection: {ex.Message}\n";
                this.Invoke(new Action(() =>
                {
                    ShowNotification(NotificationType.Error, $"Error inject: {ex.Message}");
                }));
            }
        }
        private void LogsBtn_Click(object sender, EventArgs e)
        {
            LogsForm form = new LogsForm();
            form.ShowDialog();
        }
        private void FAQBtn1_Click(object sender, EventArgs e)
        {
            FAQForm.FAQText = "Hi, first you should understand what error is happening, check if the game is running, and try restarting the game or checking the modification version. Disable the antivirus. Try to run as an administrator. If nothing helps, contact the creator for help. This can be done in the comments or in paragraph 4 of the FAQ.";
            FAQForm faq1 = new FAQForm();
            faq1.ShowDialog();
        }
        private void FAQBtn2_Click(object sender, EventArgs e)
        {
            FAQForm.FAQText = "Hi, first you should check the version of the game and the version supported by the cheat, you can find it on the website or in the telegram channel. If the version doesn't match or our loader can't find it in the game automatically, it will ask you whether to inject the cheat and then you decide what to do.";
            FAQForm faq2 = new FAQForm();
            faq2.ShowDialog();
        }
        private void FAQBtn3_Click(object sender, EventArgs e)
        {
            FAQForm.FAQText = "Hi, as the creators, we declare that it is almost impossible to get blocked for our modification, except to collect more than 100 complaints about your account, send Player.log to the game developers, and change the game files.";
            FAQForm faq2 = new FAQForm();
            faq2.ShowDialog();
        }
        private void guna2Button1_Click(object sender, EventArgs e)
        {
            FAQForm.FAQText = "Hello, communication with the creator is possible on any of the forums where the author is ViniLog, as well as in telegram or discord, they are all listed on the forums.";
            FAQForm faq2 = new FAQForm();
            faq2.ShowDialog();
        }
        private void FAQBtn4_Click(object sender, EventArgs e)
        {
            FAQForm.FAQText = "Hi, if the cheat is infected, but the menu is not visible, make sure that you have installed the latest versions of Visual C++ game components, updated DirectX, disabled Windows Defender or other Antivirus, or excluded the folder with this menu. If all this is done, try deleting the modification folder: C:\\PhasmoCheatV . If nothing helps, contact the author by sending Player.log";
            FAQForm faq2 = new FAQForm();
            faq2.ShowDialog();
        }
    }
}