using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GAssist
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new AppController());
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Ctrl+Shift+G global kısayolunu yöneten controller
    // ────────────────────────────────────────────────────────────────
    internal class AppController : ApplicationContext
    {
        private readonly OverlayForm   _overlay;
        private readonly HotkeyWindow  _hotkey;
        private readonly NotifyIcon    _tray;

        private const int HOTKEY_ID  = 1;
        private const int MOD_CTRL   = 0x0002;
        private const int MOD_SHIFT  = 0x0004;
        private const int VK_G       = 0x47;

        public AppController()
        {
            _overlay = new OverlayForm();

            // Tray icon
            _tray = new NotifyIcon
            {
                Icon    = SystemIcons.Application,
                Visible = true,
                Text    = "G-Assist"
            };
            var menu = new ContextMenuStrip();
            menu.Items.Add("Göster / Gizle", null, (s, e) => ToggleOverlay());
            menu.Items.Add("Çıkış",          null, (s, e) => ExitApp());
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (s, e) => ToggleOverlay();

            // Global kısayol
            _hotkey = new HotkeyWindow();
            _hotkey.HotkeyPressed += (s, e) => ToggleOverlay();
            RegisterHotKey(_hotkey.Handle, HOTKEY_ID, MOD_CTRL | MOD_SHIFT, VK_G);

            _overlay.Show();
        }

        private void ToggleOverlay()
        {
            if (_overlay.Visible) _overlay.Hide();
            else                  _overlay.Show();
        }

        private void ExitApp()
        {
            UnregisterHotKey(_hotkey.Handle, HOTKEY_ID);
            _tray.Visible = false;
            Application.Exit();
        }

        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }

    // Kısayol mesajlarını dinleyen gizli pencere
    internal class HotkeyWindow : NativeWindow
    {
        private const int WM_HOTKEY = 0x0312;
        public event EventHandler? HotkeyPressed;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY) HotkeyPressed?.Invoke(this, EventArgs.Empty);
            base.WndProc(ref m);
        }
    }
}
