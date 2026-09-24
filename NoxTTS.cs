using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NoxTTS
{
    public partial class MainForm : Form
    {
        private SpeechSynthesizer synthesizer;
        private TextBox txtInput;
        private ComboBox cmbVoices;
        private ComboBox cmbDevices;
        private TrackBar trackVolume;
        private Label lblVolumeValue;
        private Button btnSpeak;
        
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        // Midnight Palette with crisp white framing elements
        private readonly Color BgColor = Color.FromArgb(14, 14, 17);
        private readonly Color PanelColor = Color.FromArgb(22, 22, 27);
        private readonly Color OutlineWhite = Color.FromArgb(210, 210, 220); // White border outline
        private readonly Color TextPrimary = Color.FromArgb(250, 250, 255);
        private readonly Color TextMuted = Color.FromArgb(135, 135, 150);
        private readonly Color AccentCyan = Color.FromArgb(0, 229, 255);
        private readonly Color AccentHover = Color.FromArgb(50, 240, 255);

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public MainForm()
        {
            this.Text = "NoxTTS";
            this.Size = new Size(460, 410);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgColor;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.None;
            
            // Smooth Rounded Corners
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 16, 16));

            // Setup App Icon securely from file
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

            // --- White Outline Border Panel (Wrapper) ---
            Panel pnlBorder = new Panel()
            {
                Left = 1, Top = 1, Width = 458, Height = 408,
                BackColor = BgColor,
                Enabled = false
            };
            // Paint subtle outer white border via custom border logic or panel wrapper styling
            this.Paint += (s, e) => {
                using (Pen whitePen = new Pen(OutlineWhite, 1.5f))
                {
                    e.Graphics.DrawRectangle(whitePen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // --- Custom Modern Title Bar ---
            Panel pnlTitleBar = new Panel() { Left = 2, Top = 2, Width = 456, Height = 36, BackColor = BgColor };
            pnlTitleBar.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };

            Label lblTitle = new Label() { 
                Text = "NOX • VOICE BRIDGE", 
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), 
                Left = 18, Top = 10, Width = 250, Height = 20, 
                ForeColor = AccentCyan 
            };
            lblTitle.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };

            Button btnClose = new Button() { 
                Text = "×", 
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                Left = 416, Top = 6, Width = 26, Height = 24, 
                FlatStyle = FlatStyle.Flat, 
                ForeColor = TextMuted, 
                BackColor = BgColor,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Application.Exit();
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.FromArgb(230, 50, 50); };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = TextMuted; btnClose.BackColor = BgColor; };

            pnlTitleBar.Controls.Add(lblTitle);
            pnlTitleBar.Controls.Add(btnClose);

            // --- Main Content Inputs & Selection Slots ---

            txtInput = new TextBox() 
            { 
                Left = 20, Top = 48, Width = 420, Height = 75, 
                Multiline = true, 
                BackColor = PanelColor, 
                ForeColor = TextPrimary, 
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };
            txtInput.KeyDown += TxtInput_KeyDown;

            Label lblVoice = new Label() 
            { 
                Text = "Voice Model Slot", 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Left = 20, Top = 140, Width = 196, 
                ForeColor = TextMuted 
            };
            
            cmbVoices = new ComboBox() 
            { 
                Left = 20, Top = 160, Width = 200, Height = 28, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = PanelColor, 
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F)
            };

            Label lblDevice = new Label() 
            { 
                Text = "Virtual Audio Cable Slot", 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Left = 240, Top = 140, Width = 196, 
                ForeColor = TextMuted 
            };
            
            cmbDevices = new ComboBox() 
            { 
                Left = 240, Top = 160, Width = 200, Height = 28, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = PanelColor, 
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F)
            };

            Label lblVolume = new Label() 
            { 
                Text = "Volume Output", 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Left = 20, Top = 205, Width = 90, 
                ForeColor = TextMuted 
            };
            
            trackVolume = new TrackBar()
            {
                Left = 110, Top = 198, Width = 290, Height = 35,
                Minimum = 0, Maximum = 100, Value = 100,
                TickFrequency = 10,
                BackColor = BgColor
            };
            
            lblVolumeValue = new Label() 
            { 
                Text = "100%", 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Left = 405, Top = 205, Width = 40, 
                ForeColor = AccentCyan 
            };

            trackVolume.Scroll += (s, e) => {
                lblVolumeValue.Text = trackVolume.Value + "%";
            };

            // Comprehensive Voice Discovery (Scans standard SAPI5 + OneCore downloaded tokens)
            LoadAllSystemVoices();

            // Load Wave Output Devices & auto-select VB-Cable Input
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

            // Sleek Interactive Action Button
            btnSpeak = new Button() 
            { 
                Text = "BROADCAST TO CABLE", 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Left = 20, Top = 268, Width = 420, Height = 42,
                BackColor = AccentCyan, 
                ForeColor = Color.FromArgb(14, 14, 17), 
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSpeak.FlatAppearance.BorderSize = 0;
            btnSpeak.MouseEnter += (s, e) => btnSpeak.BackColor = AccentHover;
            btnSpeak.MouseLeave += (s, e) => btnSpeak.BackColor = AccentCyan;
            btnSpeak.Click += (s, e) => ExecuteSpeech();

            Label lblHint = new Label()
            {
                Text = "Tip: Press Enter in the text box to broadcast immediately.",
                Font = new Font("Segoe UI", 8.25F, FontStyle.Italic),
                Left = 20, Top = 330, Width = 420,
                ForeColor = Color.FromArgb(110, 110, 125),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Add Controls to Form
            this.Controls.Add(pnlTitleBar);
            this.Controls.Add(txtInput);
            this.Controls.Add(lblVoice);
            this.Controls.Add(cmbVoices);
            this.Controls.Add(lblDevice);
            this.Controls.Add(cmbDevices);
            this.Controls.Add(lblVolume);
            this.Controls.Add(trackVolume);
            this.Controls.Add(lblVolumeValue);
            this.Controls.Add(btnSpeak);
            this.Controls.Add(lblHint);
        }

        private void LoadAllSystemVoices()
        {
            // Standard SAPI5 Voices via SpeechSynthesizer
            try
            {
                foreach (var voice in synthesizer.GetInstalledVoices())
                {
                    if (voice.Enabled)
                    {
                        string name = voice.VoiceInfo.Name;
                        if (!cmbVoices.Items.Contains(name))
                        {
                            cmbVoices.Items.Add(name);
                        }
                    }
                }
            }
            catch { }

            // Deep Registry Scan for downloaded Windows OneCore / Mobile Voice Packs
            try
            {
                using (RegistryKey? baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens"))
                {
                    if (baseKey != null)
                    {
                        foreach (string subKeyName in baseKey.GetSubKeyNames())
                        {
                            using (RegistryKey? tokenKey = baseKey.OpenSubKey(subKeyName))
                            {
                                object? displayName = tokenKey?.GetValue("") ?? tokenKey?.GetValue("ıcı");
                                if (displayName != null)
                                {
                                    string cleanName = displayName.ToString()!;
                                    if (cleanName.Contains("Token")) cleanName = subKeyName;
                                    
                                    if (!cmbVoices.Items.Contains(cleanName))
                                    {
                                        cmbVoices.Items.Add(cleanName);
                                    }
                                }
                                else
                                {
                                    if (!cmbVoices.Items.Contains(subKeyName))
                                    {
                                        cmbVoices.Items.Add(subKeyName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            if (cmbVoices.Items.Count == 0)
            {
                cmbVoices.Items.Add("Default System Voice");
            }
            cmbVoices.SelectedIndex = 0;
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ExecuteSpeech();
            }
        }

        private void ExecuteSpeech()
        {
            string textToSpeak = txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(textToSpeak)) return;

            string selectedVoice = cmbVoices.SelectedItem?.ToString() ?? "";
            int selectedDeviceIndex = cmbDevices.SelectedIndex;
            float volumeLevel = trackVolume.Value / 100f;

            try
            {
                if (!string.IsNullOrEmpty(selectedVoice) && selectedVoice != "Default System Voice")
                {
                    try { synthesizer.SelectVoice(selectedVoice); } catch { }
                }

                MemoryStream stream = new MemoryStream();
                synthesizer.SetOutputToAudioStream(stream, new System.Speech.AudioFormat.SpeechAudioFormatInfo(16000, System.Speech.AudioFormat.AudioBitsPerSample.Sixteen, System.Speech.AudioFormat.AudioChannel.Mono));
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
                        waveOut.Play();
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(50);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "NoxTTS Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            txtInput.Clear();
        }
    }
}
