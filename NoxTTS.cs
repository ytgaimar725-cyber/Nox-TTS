```csharp
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NoxTTS
{
    public class MainForm : Form
    {
        private SpeechSynthesizer synthesizer;
        private ModernTextBox txtInput;
        private ComboBox cmbVoices;
        private ComboBox cmbDevices;
        private ModernSlider trackVolume;
        private Label lblVolumeValue;
        private RoundedButton btnSpeak;
        private Label lblStatus;

        // Modern Midnight Neon Palette
        private readonly Color BgColor = Color.FromArgb(14, 14, 16);          // Deep dark base
        private readonly Color PanelColor = Color.FromArgb(24, 24, 28);      // Soft surface container
        private readonly Color BorderColor = Color.FromArgb(42, 42, 48);     // Subtle borders
        private readonly Color TextPrimary = Color.FromArgb(240, 240, 245);  // Crisp white/silver
        private readonly Color TextMuted = Color.FromArgb(138, 138, 146);    // Soft labels
        private readonly Color AccentCyan = Color.FromArgb(0, 229, 255);    // Neon Cyan Accent
        private readonly Color AccentHover = Color.FromArgb(51, 234, 255);   // Lighter cyan for hover
        private readonly Color DangerRed = Color.FromArgb(255, 85, 85);      // Error status

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public MainForm()
        {
            this.Text = "NoxTTS — Voice Bridge";
            this.Size = new Size(480, 460);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgColor;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.DoubleBuffered = true; // Prevent UI flicker

            // Load custom app icon if present
            try
            {
                if (File.Exists("icon.png"))
                {
                    using (var bmp = new Bitmap("icon.png"))
                    {
                        this.Icon = Icon.FromHandle(bmp.GetHicon());
                    }
                }
            }
            catch { }

            synthesizer = new SpeechSynthesizer();
            InitializeUI();
            LoadVoicesAndDevices();
        }

        private void InitializeUI()
        {
            // --- Header ---
            var lblHeader = new Label()
            {
                Text = "TEXT TO SPEECH BRIDGE",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Left = 24, Top = 20, Width = 400, Height = 20,
                ForeColor = AccentCyan,
                BackColor = Color.Transparent
            };

            // --- Input Text Box (Wrapped in a custom Panel) ---
            var pnlInput = new RoundedPanel()
            {
                Left = 24, Top = 50, Width = 416, Height = 90,
                BackColor = PanelColor,
                BorderColor = BorderColor,
                BorderRadius = 8
            };
            
            txtInput = new ModernTextBox()
            {
                BorderStyle = BorderStyle.None,
                BackColor = PanelColor,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
                Multiline = true,
                Left = 10, Top = 10, Width = 396, Height = 70
            };
            txtInput.KeyDown += TxtInput_KeyDown;
            pnlInput.Controls.Add(txtInput);

            // --- Voice & Device Dropdowns ---
            var lblVoice = new Label()
            {
                Text = "VOICE MODEL",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                Left = 24, Top = 160, Width = 196,
                ForeColor = TextMuted,
                BackColor = Color.Transparent
            };

            cmbVoices = new ComboBox()
            {
                Left = 24, Top = 180, Width = 196, Height = 25,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = PanelColor,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F)
            };
            cmbVoices.DrawItem += Cmb_DrawItem;
            cmbVoices.SelectedIndexChanged += Cmb_DrawItem; // Force redraw on select

            var lblDevice = new Label()
            {
                Text = "VIRTUAL AUDIO OUTPUT",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                Left = 244, Top = 160, Width = 196,
                ForeColor = TextMuted,
                BackColor = Color.Transparent
            };

            cmbDevices = new ComboBox()
            {
                Left = 244, Top = 180, Width = 196, Height = 25,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = PanelColor,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F)
            };
            cmbDevices.DrawItem += Cmb_DrawItem;
            cmbDevices.SelectedIndexChanged += Cmb_DrawItem;

            // --- Volume Slider ---
            var lblVolume = new Label()
            {
                Text = "VOLUME",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                Left = 24, Top = 230, Width = 60,
                ForeColor = TextMuted,
                BackColor = Color.Transparent
            };

            trackVolume = new ModernSlider()
            {
                Left = 24, Top = 250, Width = 370, Height = 20,
                BackColor = BgColor,
                AccentColor = AccentCyan,
                MaxValue = 100,
                Value = 100
            };
            trackVolume.ValueChanged += (s, e) => lblVolumeValue.Text = trackVolume.Value + "%";

            lblVolumeValue = new Label()
            {
                Text = "100%",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Left = 400, Top = 250, Width = 40, Height = 20,
                ForeColor = AccentCyan,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // --- Broadcast Button ---
            btnSpeak = new RoundedButton()
            {
                Text = "BROADCAST TO CABLE",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Left = 24, Top = 300, Width = 416, Height = 42,
                BackColor = AccentCyan,
                ForeColor = BgColor,
                BorderRadius = 8,
                FlatStyle = FlatStyle.Flat
            };
            btnSpeak.FlatAppearance.BorderSize = 0;
            btnSpeak.MouseEnter += (s, e) => btnSpeak.BackColor = AccentHover;
            btnSpeak.MouseLeave += (s, e) => btnSpeak.BackColor = AccentCyan;
            btnSpeak.Click += async (s, e) => await ExecuteSpeechAsync();

            // --- Status Label ---
            lblStatus = new Label()
            {
                Text = "Ready.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Left = 24, Top = 360, Width = 416,
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // --- Add Controls ---
            this.Controls.Add(lblHeader);
            this.Controls.Add(pnlInput);
            this.Controls.Add(lblVoice);
            this.Controls.Add(cmbVoices);
            this.Controls.Add(lblDevice);
            this.Controls.Add(cmbDevices);
            this.Controls.Add(lblVolume);
            this.Controls.Add(trackVolume);
            this.Controls.Add(lblVolumeValue);
            this.Controls.Add(btnSpeak);
            this.Controls.Add(lblStatus);
        }

        private void Cmb_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Custom drawing for ComboBox items to match dark theme
            if (e.Index < 0) return;
            ComboBox cmb = sender as ComboBox;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bgColor = isSelected ? AccentCyan : PanelColor;
            Color fgColor = isSelected ? BgColor : TextPrimary;

            using (SolidBrush brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            
            if ((e.State & DrawItemState.ComboBoxEdit) != DrawItemState.ComboBoxEdit)
            {
                using (Pen pen = new Pen(BorderColor, 1))
                {
                    e.Graphics.DrawLine(pen, 0, e.Bounds.Top, cmb.Width, e.Bounds.Top);
                }
            }

            using (SolidBrush brush = new SolidBrush(fgColor))
            {
                e.Graphics.DrawString(cmb.Items[e.Index].ToString(), e.Font, brush, e.Bounds.X + 5, e.Bounds.Y + 3);
            }
            e.DrawFocusRectangle();
        }

        private void LoadVoicesAndDevices()
        {
            // Load Installed System Voices (Male preference with fallback)
            foreach (var voice in synthesizer.GetInstalledVoices())
            {
                if (voice.Enabled && voice.VoiceInfo.Gender == VoiceGender.Male)
                {
                    cmbVoices.Items.Add(voice.VoiceInfo.Name);
                }
            }

            if (cmbVoices.Items.Count == 0)
            {
                foreach (var voice in synthesizer.GetInstalledVoices())
                {
                    if (voice.Enabled) cmbVoices.Items.Add(voice.VoiceInfo.Name);
                }
            }

            if (cmbVoices.Items.Count == 0) cmbVoices.Items.Add("Default System Voice");
            cmbVoices.SelectedIndex = 0;

            // Load Wave Output Devices and auto-select VB-Cable Input
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                cmbDevices.Items.Add(caps.ProductName);
                if (caps.ProductName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase))
                {
                    cmbDevices.SelectedIndex = i;
                }
            }
            if (cmbDevices.SelectedIndex == -1 && cmbDevices.Items.Count > 0) cmbDevices.SelectedIndex = 0;
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true; // Prevents new line on Enter
                _ = ExecuteSpeechAsync();
            }
        }

        private async Task ExecuteSpeechAsync()
        {
            string textToSpeak = txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(textToSpeak)) return;

            string selectedVoice = cmbVoices.SelectedItem?.ToString() ?? "";
            int selectedDeviceIndex = cmbDevices.SelectedIndex;
            float volumeLevel = trackVolume.Value / 100f;

            // UI State: Speaking
            btnSpeak.Enabled = false;
            btnSpeak.BackColor = BorderColor;
            lblStatus.Text = "Broadcasting...";
            lblStatus.ForeColor = AccentCyan;

            try
            {
                await Task.Run(() =>
                {
                    if (!string.IsNullOrEmpty(selectedVoice) && selectedVoice != "Default System Voice")
                    {
                        synthesizer.SelectVoice(selectedVoice);
                    }

                    using (var stream = new MemoryStream())
                    {
                        var formatInfo = new System.Speech.AudioFormat.SpeechAudioFormatInfo(16000, System.Speech.AudioFormat.AudioBitsPerSample.Sixteen, System.Speech.AudioFormat.AudioChannel.Mono);
                        synthesizer.SetOutputToAudioStream(stream, formatInfo);
                        synthesizer.Speak(textToSpeak);
                        stream.Position = 0;

                        using (var reader = new RawSourceWaveStream(stream, new WaveFormat(16000, 16, 1)))
                        {
                            var volumeProvider = new VolumeSampleProvider(reader.ToSampleProvider())
                            {
                                Volume = volumeLevel
                            };

                            using (var waveOut = new WaveOutEvent())
                            {
                                waveOut.DeviceNumber = selectedDeviceIndex;
                                waveOut.Init(volumeProvider);
                                
                                var tcs = new TaskCompletionSource<bool>();
                                waveOut.PlaybackStopped += (s, e) => tcs.TrySetResult(true);
                                
                                waveOut.Play();
                                tcs.Task.Wait(); // Non-blocking wait inside background thread
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate {
                    lblStatus.Text = "Error: " + ex.Message;
                    lblStatus.ForeColor = DangerRed;
                });
            }
            finally
            {
                // UI State: Ready
                txtInput.Clear();
                btnSpeak.Enabled = true;
                btnSpeak.BackColor = AccentCyan;
                if (lblStatus.ForeColor != DangerRed)
                {
                    lblStatus.Text = "Ready.";
                    lblStatus.ForeColor = TextMuted;
                }
            }
        }
    }

    // --- CUSTOM MODERN CONTROLS ---

    public class RoundedPanel : Panel
    {
        public int BorderRadius { get; set; } = 8;
        public Color BorderColor { get; set; } = Color.FromArgb(42, 42, 48);

        public RoundedPanel()
        {
            DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            
            using (GraphicsPath path = GetRoundedPath(rect, BorderRadius))
            using (Pen pen = new Pen(BorderColor, 1))
            {
                this.Region = new Region(path);
                e.Graphics.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedButton : Button
    {
        public int BorderRadius { get; set; } = 8;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, BorderRadius, BorderRadius, 180, 90);
                path.AddArc(rect.Right - BorderRadius, rect.Y, BorderRadius, BorderRadius, 270, 90);
                path.AddArc(rect.Right - BorderRadius, rect.Bottom - BorderRadius, BorderRadius, BorderRadius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - BorderRadius, BorderRadius, BorderRadius, 90, 90);
                path.CloseFigure();

                this.Region = new Region(path);
                using (SolidBrush brush = new SolidBrush(BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public class ModernSlider : Control
    {
        public int MinValue { get; set; } = 0;
        public int MaxValue { get; set; } = 100;
        public Color AccentColor { get; set; } = Color.Cyan;
        
        private int _value = 100;
        public int Value
        {
            get { return _value; }
            set 
            { 
                _value = Math.Max(MinValue, Math.Min(MaxValue, value)); 
                Invalidate(); 
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;
        private bool isDragging = false;

        public ModernSlider()
        {
            DoubleBuffered = true;
            this.Height = 20;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int trackHeight = 4;
            int trackY = (Height - trackHeight) / 2;
            Rectangle trackRect = new Rectangle(0, trackY, Width, trackHeight);
            float pct = (float)(_value - MinValue) / (MaxValue - MinValue);
            
            // Background Track
            using (GraphicsPath path = GetRoundedPath(trackRect, 2))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(42, 42, 48)))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Active Track
            Rectangle valRect = new Rectangle(0, trackY, (int)(Width * pct), trackHeight);
            if (valRect.Width > 0)
            {
                using (GraphicsPath path = GetRoundedPath(valRect, 2))
                using (SolidBrush brush = new SolidBrush(AccentColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            // Thumb
            int thumbX = (int)(Width * pct);
            Rectangle thumbRect = new Rectangle(thumbX - 6, 2, 12, Height - 4);
            using (SolidBrush b = new SolidBrush(Color.White))
            {
                e.Graphics.FillEllipse(b, thumbRect);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            isDragging = true;
            UpdateValue(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDragging) UpdateValue(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isDragging = false;
        }

        private void UpdateValue(int x)
        {
            float pct = (float)x / Width;
            int newVal = (int)(MinValue + (MaxValue - MinValue) * pct);
            Value = newVal;
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Subclassed TextBox to fix selection colors natively without breaking default behavior
    public class ModernTextBox : TextBox
    {
        public ModernTextBox()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }
}
```
