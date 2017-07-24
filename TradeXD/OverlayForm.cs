using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using TradeXD.Controls;
using TradeXD.Properties;

namespace TradeXD
{
    public sealed partial class OverlayForm : Form
    {
        // ReSharper disable once InconsistentNaming
        private const uint WINEVENT_OUTOFCONTEXT = 0x0;

        // ReSharper disable once InconsistentNaming
        private const uint WINEVENT_SKIPOWNPROCESS = 0x2;

        // ReSharper disable once InconsistentNaming
        private const uint EVENT_SYSTEM_FOREGROUND = 0x3;

        // ReSharper disable once InconsistentNaming
        private const uint EVENT_SYSTEM_MOVESIZESTART = 0xA;

        // ReSharper disable once InconsistentNaming
        private const uint EVENT_SYSTEM_MOVESIZEEND = 0xB;

        private readonly PoEWebClient _client;
        private readonly KeyboardHookListener _keyboardListener;
        private readonly MouseHookListener _mouseListener;
        private readonly StashPanel _stashPanel;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenu _trayMenu;

        private WinEventProc _foregroundListener;
        private WinEventProc _moveSizeEndListener;
        private WinEventProc _moveSizeStartListener;

        public OverlayForm()
        {
            InitializeComponent();
            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            ResizeRedraw = true;
            ShowIcon = false;
            ShowInTaskbar = false;
            Text = @"TradeXD";

            StartListeningForWindowChanges();

            _client = new PoEWebClient();
            try
            {
                (bool successful, string email, string cipher, string salt) = PoEHelper.LoadCredentials();
                string password = PoEHelper.Decrypt(cipher, salt);
                if (successful && Login(email, password))
                {
                    PoEHelper.SaveCredentials(email, password);
                    password = "";
                }
                else
                {
                    LoginForm loginForm = new LoginForm(this);
                    loginForm.Show();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoginForm loginForm = new LoginForm(this);
                loginForm.Show();
            }

            Refresh();

            _stashPanel = new StashPanel(Size) {Visible = false};
            Controls.Add(_stashPanel);

            _trayMenu = new ContextMenu();
            _trayMenu.MenuItems.Add("Settings", OnSettings);
            _trayMenu.MenuItems.Add("Exit", OnExit);
            _trayIcon = new NotifyIcon
            {
                Text = Text,
                Icon = new Icon(SystemIcons.Application, 40, 40),
                ContextMenu = _trayMenu,
                Visible = true
            };

            _mouseListener = new MouseHookListener(new GlobalHooker()) {Enabled = true};
            _keyboardListener = new KeyboardHookListener(new GlobalHooker()) {Enabled = true};
            _mouseListener.MouseUp += MouseUpListener;
            _keyboardListener.KeyUp += KeyboardUpListener;
        }

        private void MouseUpListener(object sender, MouseEventArgs e)
        {
        }

        private void KeyboardUpListener(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Settings.Default.SortHotkey)
            {
                Console.WriteLine($@"Sorting 1 tab in {_client.League}");
                ItemTab tab = _client.GetTabItems(0);
                RecipeSorter recipeSorter = new RecipeSorter(false, tab.IsQuad, tab.Items);
                Console.WriteLine(recipeSorter);
                Console.WriteLine($@"Set availability: {recipeSorter.IsSetAvailable()}");
                var recipeSet = recipeSorter.GetItemSet();
                foreach (Item item in recipeSet)
                    Console.WriteLine($@"Ilvl {item.Level} {item.BaseType}");
                Console.WriteLine(recipeSorter);
                Console.WriteLine(@"Finished sort.");
                GC.Collect();
            }
            else if (e.KeyCode == Keys.NumPad1)
            {
                if (Cursor.Current != null) Cursor = new Cursor(Cursor.Current.Handle);
                Cursor.Position = _stashPanel.GetItemPoint(0, 0, true);
            }
            else if (e.KeyCode == Keys.NumPad2)
            {
                if (Cursor.Current != null) Cursor = new Cursor(Cursor.Current.Handle);
                Cursor.Position = _stashPanel.GetItemPoint(1, 0, true);
            }
            else if (e.KeyCode == Keys.NumPad3)
            {
                if (Cursor.Current != null) Cursor = new Cursor(Cursor.Current.Handle);
                Cursor.Position = _stashPanel.GetItemPoint(2, 0, true);
            }
        }

        private void OnSettings(object sender, EventArgs e)
        {
        }

        private void OnExit(object sender, EventArgs e)
        {
            _trayIcon.Dispose();
            Application.Exit();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int style = GetWindowLong(Handle, -20);
            SetWindowLong(Handle, -20, style | 0x80000 | 0x20);
        }

        internal bool Login(string email, string password)
        {
            if (!_client.Login(email, password)) return false;
            return true;
        }

        private void SetWindowRect()
        {
            Rectangle rect = PoEHelper.GetClientSize();
            Location = rect.Location;
            Size = rect.Size;
            foreach (StashPanel p in Controls.OfType<StashPanel>())
                p.SetSize(Size);
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventProc lpfnWinEventProc, int idProcess, int idThread, uint dwflags);

        [DllImport("user32.dll")]
        internal static extern int UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public void StartListeningForWindowChanges()
        {
            _foregroundListener = HandleForegroundEvent;
            _moveSizeStartListener = HandleMoveSizeStartEvent;
            _moveSizeEndListener = HandleMoveSizeEndEvent;
            SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _foregroundListener, 0, 0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            SetWinEventHook(EVENT_SYSTEM_MOVESIZESTART, EVENT_SYSTEM_MOVESIZESTART, IntPtr.Zero, _moveSizeStartListener,
                0, 0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            SetWinEventHook(EVENT_SYSTEM_MOVESIZEEND, EVENT_SYSTEM_MOVESIZEEND, IntPtr.Zero, _moveSizeEndListener, 0, 0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        }

        private static string GetActiveProcessName()
        {
            IntPtr handle = GetForegroundWindow();
            GetWindowThreadProcessId(handle, out uint pid);
            Process process = Process.GetProcessById((int) pid);
            return process.ProcessName;
        }

        private void SetWindowVisibility()
        {
            switch (GetActiveProcessName())
            {
                case "PathOfExile":
                case "PathOfExile_x64":
                case "PathOfExileSteam":
                case "PathOfExileSteam_x64":
                    SetWindowRect();
                    Show();
                    break;
                default:
                    Hide();
                    break;
            }
        }

        private void HandleForegroundEvent(IntPtr hWinEventHook, uint iEvent, IntPtr hWnd, int idObject, int idChild,
            int dwEventThread, int dwmsEventTime)
        {
            SetWindowVisibility();
        }

        private void HandleMoveSizeStartEvent(IntPtr hWinEventHook, uint iEvent, IntPtr hWnd, int idObject, int idChild,
            int dwEventThread, int dwmsEventTime)
        {
            Hide();
        }

        private void HandleMoveSizeEndEvent(IntPtr hWinEventHook, uint iEvent, IntPtr hWnd, int idObject, int idChild,
            int dwEventThread, int dwmsEventTime)
        {
            SetWindowRect();
            Show();
        }

        internal delegate void WinEventProc(IntPtr hWinEventHook, uint iEvent, IntPtr hWnd, int idObject, int idChild,
            int dwEventThread, int dwmsEventTime);
    }
}