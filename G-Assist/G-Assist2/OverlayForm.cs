using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace GAssist
{
    public partial class OverlayForm : Form
    {
        // ─── Sabitler ───────────────────────────────────────────────
        private const int FORM_WIDTH  = 420;
        private const int FORM_HEIGHT = 520;
        private const int CORNER_RADIUS = 12;

        // Audi RS renk paleti
        private static readonly Color ColorBg        = Color.FromArgb(240, 10, 10, 10);   // %94 siyah
        private static readonly Color ColorPanel     = Color.FromArgb(255, 18, 18, 18);
        private static readonly Color ColorRed       = Color.FromArgb(255, 196, 18, 18);  // RS Kırmızı
        private static readonly Color ColorRedHover  = Color.FromArgb(255, 220, 30, 30);
        private static readonly Color ColorWhite     = Color.FromArgb(255, 240, 240, 240);
        private static readonly Color ColorGray      = Color.FromArgb(255, 90, 90, 90);
        private static readonly Color ColorBorder    = Color.FromArgb(255, 50, 50, 50);
        private static readonly Color ColorInputBg   = Color.FromArgb(255, 28, 28, 28);
        private static readonly Color ColorUserBubble = Color.FromArgb(255, 196, 18, 18);
        private static readonly Color ColorAIBubble  = Color.FromArgb(255, 30, 30, 30);

        // ─── UI Kontrolleri ─────────────────────────────────────────
        private Panel  pnlTitleBar   = null!;
        private Label  lblTitle      = null!;
        private Label  lblSubtitle   = null!;
        private Button btnMinimize   = null!;
        private Button btnClose      = null!;
        private Panel  pnlChat       = null!;
        private FlowLayoutPanel flowMessages = null!;
        private Panel  pnlInput      = null!;
        private TextBox txtInput     = null!;
        private Button btnSend       = null!;
        private Button btnScreen     = null!;
        private Panel  pnlStatus     = null!;
        private Label  lblStatus     = null!;
        private PictureBox picLogo   = null!;

        // ─── Drag desteği ───────────────────────────────────────────
        private bool   _dragging;
        private Point  _dragStart;

        // ─── Claude API ─────────────────────────────────────────────
        private static readonly HttpClient _http = new();
        private readonly List<object> _history = new();
        private const string API_KEY_FILE = "apikey.txt";
        private string _apiKey = "";

        // ─── Ctor ────────────────────────────────────────────────────
        public OverlayForm()
        {
            BuildUI();
            PositionBottomRight();
            LoadApiKey();
            AddWelcomeMessage();
        }

        // ────────────────────────────────────────────────────────────
        // UI İnşa
        // ────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Form ayarları
            FormBorderStyle = FormBorderStyle.None;
            BackColor       = Color.FromArgb(10, 10, 10);
            Width  = FORM_WIDTH;
            Height = FORM_HEIGHT;
            TopMost         = true;
            ShowInTaskbar   = false;
            Opacity         = 0.97;
            DoubleBuffered  = true;

            // Yuvarlak köşe region
            var path = RoundedRect(new Rectangle(0, 0, Width, Height), CORNER_RADIUS);
            Region = new Region(path);

            // ── Title Bar ──────────────────────────────────────────
            pnlTitleBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 54,
                BackColor = Color.FromArgb(255, 8, 8, 8)
            };
            pnlTitleBar.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.MouseMove += TitleBar_MouseMove;
            pnlTitleBar.MouseUp   += (s, e) => _dragging = false;
            pnlTitleBar.Paint     += TitleBar_Paint;

            // Logo (RS rozeti çizim)
            picLogo = new PictureBox
            {
                Size     = new Size(38, 38),
                Location = new Point(10, 8),
                BackColor= Color.Transparent
            };
            picLogo.Paint += PicLogo_Paint;

            lblTitle = new Label
            {
                Text      = "G-ASSIST",
                ForeColor = ColorWhite,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                Location  = new Point(54, 8),
                AutoSize  = true
            };
            lblSubtitle = new Label
            {
                Text      = "AI Gaming Assistant",
                ForeColor = ColorRed,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                Location  = new Point(55, 30),
                AutoSize  = true
            };

            btnClose = MakeTitleBtn("✕", FORM_WIDTH - 36, ColorGray, Color.FromArgb(255, 180, 20, 20));
            btnClose.Click += (s, e) => this.Hide();

            btnMinimize = MakeTitleBtn("─", FORM_WIDTH - 68, ColorGray, ColorBorder);
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            pnlTitleBar.Controls.AddRange(new Control[] { picLogo, lblTitle, lblSubtitle, btnMinimize, btnClose });

            // ── Chat alanı ──────────────────────────────────────────
            pnlChat = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = ColorPanel,
                Padding   = new Padding(6)
            };

            flowMessages = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = ColorPanel,
                Padding       = new Padding(4)
            };
            pnlChat.Controls.Add(flowMessages);

            // ── Status bar ──────────────────────────────────────────
            pnlStatus = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 22,
                BackColor = Color.FromArgb(255, 8, 8, 8)
            };
            lblStatus = new Label
            {
                Text      = "⬤  Hazır  |  Ctrl+Shift+G ile aç/kapat",
                ForeColor = ColorGray,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 7f),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlStatus.Controls.Add(lblStatus);

            // ── Input alanı ─────────────────────────────────────────
            pnlInput = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 82,
                BackColor = Color.FromArgb(255, 12, 12, 12),
                Padding   = new Padding(8, 6, 8, 6)
            };

            txtInput = new TextBox
            {
                Multiline   = true,
                BackColor   = ColorInputBg,
                ForeColor   = ColorWhite,
                BorderStyle = BorderStyle.None,
                Font        = new Font("Segoe UI", 9.5f),
                Location    = new Point(8, 8),
                Size        = new Size(FORM_WIDTH - 100, 62),
                ScrollBars  = ScrollBars.Vertical
            };
            SetPlaceholder(txtInput, "Bir şey sor veya ekranı analiz ettir...");
            txtInput.KeyDown += TxtInput_KeyDown;

            btnSend = MakeActionBtn("➤ GÖNDER", new Point(FORM_WIDTH - 88, 8), new Size(78, 28), ColorRed);
            btnSend.Click += async (s, e) => await SendMessage();

            btnScreen = MakeActionBtn("📷 EKRAN", new Point(FORM_WIDTH - 88, 42), new Size(78, 28), Color.FromArgb(255, 40, 40, 40));
            btnScreen.Click += async (s, e) => await CaptureAndAnalyze();

            pnlInput.Controls.AddRange(new Control[] { txtInput, btnSend, btnScreen });

            // ── Kırmızı çizgi ayırıcı ───────────────────────────────
            var divider = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 2,
                BackColor = ColorRed
            };

            // ── Form'a ekle ─────────────────────────────────────────
            Controls.Add(pnlChat);
            Controls.Add(divider);
            Controls.Add(pnlInput);
            Controls.Add(pnlStatus);
            Controls.Add(pnlTitleBar);
        }

        // ────────────────────────────────────────────────────────────
        // Yardımcı: Buton fabrikaları
        // ────────────────────────────────────────────────────────────
        private Button MakeTitleBtn(string text, int x, Color fore, Color hoverBack)
        {
            var b = new Button
            {
                Text      = text,
                ForeColor = fore,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(28, 28),
                Location  = new Point(x, 13),
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize   = 0;
            b.FlatAppearance.MouseOverBackColor = hoverBack;
            return b;
        }

        private Button MakeActionBtn(string text, Point loc, Size sz, Color back)
        {
            var b = new Button
            {
                Text      = text,
                BackColor = back,
                ForeColor = ColorWhite,
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Size      = sz,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ColorRedHover;
            return b;
        }

        // ────────────────────────────────────────────────────────────
        // Paint olayları
        // ────────────────────────────────────────────────────────────
        private void TitleBar_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            // Alt kırmızı çizgi
            using var pen = new Pen(ColorRed, 2);
            g.DrawLine(pen, 0, pnlTitleBar.Height - 1, pnlTitleBar.Width, pnlTitleBar.Height - 1);
        }

        private void PicLogo_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Dış çember (kırmızı)
            using var outerBrush = new SolidBrush(ColorRed);
            g.FillEllipse(outerBrush, 0, 0, 36, 36);

            // İç çember (siyah)
            using var innerBrush = new SolidBrush(Color.FromArgb(255, 10, 10, 10));
            g.FillEllipse(innerBrush, 4, 4, 28, 28);

            // "RS" yazısı
            using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var brush = new SolidBrush(ColorWhite);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("RS", font, brush, new RectangleF(0, 0, 36, 36), sf);
        }

        // ────────────────────────────────────────────────────────────
        // Mesaj baloncukları
        // ────────────────────────────────────────────────────────────
        private void AddWelcomeMessage()
        {
            AddBubble("G-Assist aktif. Ekranını analiz edebilir veya direkt soru sorabilirsin. API anahtarını apikey.txt dosyasına ekle.", isUser: false);
        }

        private void AddBubble(string text, bool isUser)
        {
            var outer = new Panel
            {
                AutoSize  = true,
                Width     = flowMessages.Width - 20,
                BackColor = Color.Transparent,
                Padding   = new Padding(isUser ? 40 : 4, 2, isUser ? 4 : 40, 2)
            };

            var lbl = new Label
            {
                Text      = (isUser ? "Sen: " : "🤖 G-Assist: ") + text,
                ForeColor = ColorWhite,
                BackColor = isUser ? ColorUserBubble : ColorAIBubble,
                Font      = new Font("Segoe UI", 9f),
                AutoSize  = true,
                MaximumSize = new Size(320, 0),
                Padding   = new Padding(10, 7, 10, 7)
            };

            // Yuvarlak köşe
            lbl.Paint += (s, e) =>
            {
                var ctl = (Label)s!;
                var path2 = RoundedRect(new Rectangle(0, 0, ctl.Width, ctl.Height), 8);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var b = new SolidBrush(ctl.BackColor);
                e.Graphics.FillPath(b, path2);
                ctl.Region = new Region(path2);
            };

            outer.Controls.Add(lbl);
            flowMessages.Controls.Add(outer);

            // Scroll aşağı
            flowMessages.ScrollControlIntoView(outer);
        }

        private void AddTypingIndicator(out Panel indicator)
        {
            indicator = new Panel
            {
                Width     = flowMessages.Width - 20,
                Height    = 34,
                BackColor = Color.Transparent
            };
            var lbl = new Label
            {
                Text      = "🤖 G-Assist yazıyor...",
                ForeColor = ColorGray,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                AutoSize  = true,
                Location  = new Point(8, 8)
            };
            indicator.Controls.Add(lbl);
            flowMessages.Controls.Add(indicator);
            flowMessages.ScrollControlIntoView(indicator);
        }

        // ────────────────────────────────────────────────────────────
        // Claude API
        // ────────────────────────────────────────────────────────────
        private async Task SendMessage()
        {
            var text = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text) || text == "Bir şey sor veya ekranı analiz ettir...") return;

            AddBubble(text, isUser: true);
            _history.Add(new { role = "user", content = text });
            txtInput.Text = "";

            AddTypingIndicator(out var typing);
            SetStatus("⬤  Düşünüyor...", ColorRed);
            btnSend.Enabled = btnScreen.Enabled = false;

            var reply = await CallClaudeAsync();

            flowMessages.Controls.Remove(typing);
            AddBubble(reply, isUser: false);
            _history.Add(new { role = "assistant", content = reply });

            SetStatus("⬤  Hazır  |  Ctrl+Shift+G ile aç/kapat", ColorGray);
            btnSend.Enabled = btnScreen.Enabled = true;
        }

        private async Task CaptureAndAnalyze()
        {
            this.Hide();
            await Task.Delay(300);

            var bmp = new Bitmap(Screen.PrimaryScreen!.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            using (var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(Point.Empty, Point.Empty, bmp.Size);

            this.Show();

            // Base64'e çevir
            string b64;
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                b64 = Convert.ToBase64String(ms.ToArray());
            }

            AddBubble("[Ekran görüntüsü alındı — analiz ediliyor...]", isUser: true);
            AddTypingIndicator(out var typing);
            SetStatus("⬤  Ekran analiz ediliyor...", ColorRed);
            btnSend.Enabled = btnScreen.Enabled = false;

            var reply = await CallClaudeWithImageAsync(b64);

            flowMessages.Controls.Remove(typing);
            AddBubble(reply, isUser: false);
            _history.Add(new { role = "assistant", content = reply });

            SetStatus("⬤  Hazır  |  Ctrl+Shift+G ile aç/kapat", ColorGray);
            btnSend.Enabled = btnScreen.Enabled = true;
        }

        private async Task<string> CallClaudeAsync()
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "API anahtarı bulunamadı. Lütfen apikey.txt dosyasına anahtarını ekle.";

            try
            {
                var body = new
                {
                    model      = "claude-opus-4-5",
                    max_tokens = 1024,
                    system     = "Sen G-Assist adlı bir oyun asistanısın. Kullanıcıların oyunlarda daha iyi performans göstermesine yardım edersin. Kısa, net ve pratik cevaplar ver. Türkçe konuş.",
                    messages   = _history
                };

                var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                req.Headers.Add("x-api-key", _apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                req.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                var res  = await _http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();
                dynamic? obj = JsonConvert.DeserializeObject(json);
                return obj?.content?[0]?.text?.ToString() ?? "Yanıt alınamadı.";
            }
            catch (Exception ex)
            {
                return $"Hata: {ex.Message}";
            }
        }

        private async Task<string> CallClaudeWithImageAsync(string base64Jpeg)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "API anahtarı bulunamadı. Lütfen apikey.txt dosyasına anahtarını ekle.";

            try
            {
                var imageContent = new
                {
                    type   = "image",
                    source = new { type = "base64", media_type = "image/jpeg", data = base64Jpeg }
                };
                var textContent = new { type = "text", text = "Bu ekran görüntüsünde ne görüyorsun? Bir oyun oynuyor muyum? Eğer oyun oynuyorsam: hangi oyun, mevcut durum, ve pratik ipuçları ver. Eğer oyun değilse ne yaptığımı açıkla." };

                var msg = new { role = "user", content = new object[] { imageContent, textContent } };

                var body = new
                {
                    model      = "claude-opus-4-5",
                    max_tokens = 1024,
                    system     = "Sen G-Assist adlı bir oyun asistanısın. Ekran görüntülerini analiz edip oyuncuya yardımcı olursun. Kısa, net ve pratik cevaplar ver. Türkçe konuş.",
                    messages   = new[] { msg }
                };

                var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                req.Headers.Add("x-api-key", _apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                req.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                var res  = await _http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();
                dynamic? obj = JsonConvert.DeserializeObject(json);
                return obj?.content?[0]?.text?.ToString() ?? "Yanıt alınamadı.";
            }
            catch (Exception ex)
            {
                return $"Hata: {ex.Message}";
            }
        }

        // ────────────────────────────────────────────────────────────
        // Yardımcılar
        // ────────────────────────────────────────────────────────────
        private void LoadApiKey()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, API_KEY_FILE);
            if (File.Exists(path))
                _apiKey = File.ReadAllText(path).Trim();
        }

        private void SetStatus(string text, Color color)
        {
            if (lblStatus.InvokeRequired)
                lblStatus.Invoke(() => { lblStatus.Text = text; lblStatus.ForeColor = color; });
            else
            { lblStatus.Text = text; lblStatus.ForeColor = color; }
        }

        private void PositionBottomRight()
        {
            var screen = Screen.PrimaryScreen!.WorkingArea;
            Left = screen.Right  - Width  - 16;
            Top  = screen.Bottom - Height - 16;
        }

        private void SetPlaceholder(TextBox tb, string ph)
        {
            tb.Text      = ph;
            tb.ForeColor = ColorGray;
            tb.GotFocus  += (s, e) => { if (tb.Text == ph) { tb.Text = ""; tb.ForeColor = ColorWhite; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = ph; tb.ForeColor = ColorGray; } };
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ────────────────────────────────────────────────────────────
        // Drag
        // ────────────────────────────────────────────────────────────
        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; }
        }
        private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_dragging) { Left += e.X - _dragStart.X; Top += e.Y - _dragStart.Y; }
        }

        // ────────────────────────────────────────────────────────────
        // Enter → gönder
        // ────────────────────────────────────────────────────────────
        private async void TxtInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SendMessage();
            }
        }
    }
}
