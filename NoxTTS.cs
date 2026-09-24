using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Speech.Synthesis;
using System.Windows.Forms;
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
        private ComboBox cmbPitch;
        private TrackBar trackVolume;
        private TrackBar trackRate;
        private Label lblVolumeValue;
        private Label lblRateValue;
        private Button btnSpeak;
        
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        private readonly Color BgColor = Color.FromArgb(14, 14, 17);
        private readonly Color PanelColor = Color.FromArgb(22, 22, 27);
        private readonly Color OutlineWhite = Color.FromArgb(210, 210, 220);
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
            this.Size = new Size(460, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgColor;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.None;
            
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 16, 16));

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

            this.Paint += (s, e) => {
                using (Pen whitePen = new Pen(OutlineWhite, 1.5f))
                {
                    e.Graphics.DrawRectangle(whitePen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

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

            txtInput = new TextBox() 
            { 
                Left = 20, Top = 48, Width = 420, Height = 65, 
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
                Left = 20, Top = 122, Width = 196, 
                ForeColor = TextMuted 
            };
            
            cmbVoices = new ComboBox() 
            { 
                Left = 20, Top = 140, Width = 200, Height = 28, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = PanelColor, 
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F)
            };

            Label lblDevice = new Label() 
            { 
                Text = "Virtual Audio Cable Slot", 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Left = 240, Top = 122, Width = 196, 
                ForeColor = TextMuted 
            };
            
            cmbDevices = new ComboBox() 
            { 
                Left = 240, Top = 140, Width = 200, Height = 28, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = PanelColor, 
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F)
            };

            // Volume Control
            Label lblVolume = new Label() { Text = "Volume", Font = new Font("Segoe UI", 8.5F), Left = 20, Top = 183, Width = 70, ForeColor = TextMuted };
            trackVolume = new TrackBar() { Left = 95, Top = 178, Width = 305, Height = 30, Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, BackColor = BgColor };
            lblVolumeValue = new Label() { Text = "100%", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Left = 405, Top = 183, Width = 40, ForeColor = AccentCyan };
            trackVolume.Scroll += (s, e) => { lblVolumeValue.Text = trackVolume.Value + "%"; };

            // Speed (Rate) Control (-10 to 10)
            Label lblRate = new Label() { Text = "Speed", Font = new Font("Segoe UI", 8.5F), Left = 20, Top = 222, Width = 70, ForeColor = TextMuted };
            trackRate = new TrackBar() { Left = 95, Top = 217, Width = 305, Height = 30, Minimum = -10, Maximum = 10, Value = 0, TickFrequency = 2, BackColor = BgColor };
            lblRateValue = new Label() { Text = "0", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Left = 405, Top = 222, Width = 40, ForeColor = AccentCyan };
            trackRate.Scroll += (s, e) => { lblRateValue.Text = trackRate.Value.ToString(); };

            // Pitch Control Selector
            Label lblPitch = new Label() { Text = "Voice Pitch", Font = new Font("Segoe UI", 8.5F), Left = 20, Top = 262, Width = 90, ForeColor = TextMuted };
            cmbPitch = new ComboBox()
            {
                Left = 115, Top = 258, Width = 325, Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = PanelColor,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F)
            };
            cmbPitch.Items.AddRange(new string[] { "Default", "Extra Low", "Low", "Medium", "High", "Extra High" });
            cmbPitch.SelectedIndex = 0;

            // Load exact system voices like your working version
            try
            {
                foreach (var voice in synthesizer.GetInstalledVoices())
                {
                    if (voice.Enabled)
                    {
                        cmbVoices.Items.Add(voice.VoiceInfo.Name);
                    }
                }
            }
            catch { }

            if (cmbVoices.Items.Count > 0)
            {
                cmbVoices.SelectedIndex = 0;
                // Auto-select Andrew if present
                for (int i = 0; i < cmbVoices.Items.Count; i++)
                {
                    if (cmbVoices.Items[i].ToString()!.Contains("Andrew", StringComparison.OrdinalIgnoreCase))
                    {
                        cmbVoices.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                cmbVoices.Items.Add("Default System Voice");
                cmbVoices.SelectedIndex = 0;
            }

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

            btnSpeak = new Button() 
            { 
                Text = "BROADCAST TO CABLE", 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Left = 20, Top = 318, Width = 420, Height = 42,
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
                Left = 20, Top = 380, Width = 420,
                ForeColor = Color.FromArgb(110, 110, 125),
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.Add(pnlTitleBar);
            this.Controls.Add(txtInput);
            this.Controls.Add(lblVoice);
            this.Controls.Add(cmbVoices);
            this.Controls.Add(lblDevice);
            this.Controls.Add(cmbDevices);
            this.Controls.Add(lblVolume);
            this.Controls.Add(trackVolume);
            this.Controls.Add(lblVolumeValue);
            this.Controls.Add(lblRate);
            this.Controls.Add(trackRate);
            this.Controls.Add(lblRateValue);
            this.Controls.Add(lblPitch);
            this.Controls.Add(cmbPitch);
            this.Controls.Add(btnSpeak);
            this.Controls.Add(lblHint);
        }

        private void TxtInput_KeyDown(object? sender, KeyEventArgs e)
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
            int speedRate = trackRate.Value;
            string pitchSelection = cmbPitch.SelectedItem?.ToString() ?? "Default";

            try
            {
                synthesizer.Rate = speedRate;

                if (!string.IsNullOrEmpty(selectedVoice) && selectedVoice != "Default System Voice")
                {
                    // Directly pass the exact token name just like your working version
                    synthesizer.SelectVoice(selectedVoice);
                }

                MemoryStream stream = new MemoryStream();
                synthesizer.SetOutputToAudioStream(stream, new System.Speech.AudioFormat.SpeechAudioFormatInfo(16000, System.Speech.AudioFormat.AudioBitsPerSample.Sixteen, System.Speech.AudioFormat.AudioChannel.Mono));
                
                if (pitchSelection == "Default")
                {
                    synthesizer.Speak(textToSpeak);
                }
                else
                {
                    string ssmlPitchValue = pitchSelection.ToLower().Replace("extra ", "x-");
                    PromptBuilder builder = new PromptBuilder();
                    builder.AppendSsmlMarkup($"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\"><prosody pitch=\"{ssmlPitchValue}\">{SecurityElement.Escape(textToSpeak)}</prosody></speak>");
                    synthesizer.Speak(builder);
                }

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
