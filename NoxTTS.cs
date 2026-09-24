using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
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
        private TrackBar trackVolume;
        private TrackBar trackRate;
        private Label lblVolumeValue;
        private Label lblRateValue;
        private Button btnSpeak;
        
        // Navigation / Tabs state
        private Label lblTabMain, lblTabSettings;
        private Panel pnlMainTab, pnlSettingsTab;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        // Modern Roblox Studio dark theme palette matching the reference style
        private readonly Color WindowBg = Color.FromArgb(20, 20, 24);
        private readonly Color CardBg = Color.FromArgb(28, 28, 35);
        private readonly Color BorderColor = Color.FromArgb(45, 45, 58);
        private readonly Color TextPrimary = Color.FromArgb(240, 240, 245);
        private readonly Color TextMuted = Color.FromArgb(140, 140, 155);
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
            this.Size = new Size(480, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = WindowBg;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.None;
            
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 12, 12));

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

            // Custom border frame paint
            this.Paint += (s, e) => {
                using (Pen borderPen = new Pen(BorderColor, 1.5f))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // --- Title Bar ---
            Panel pnlTitleBar = new Panel() { Left = 2, Top = 2, Width = 476, Height = 40, BackColor = WindowBg };
            pnlTitleBar.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };

            Label lblTitle = new Label() { 
                Text = "NoxTTS", 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), 
                Left = 16, Top = 10, Width = 100, Height = 22, 
                ForeColor = TextPrimary 
            };
            lblTitle.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };

            // Tabs matching the reference image layout
            lblTabMain = CreateTabLabel("Console", 110, true, () => SwitchTab(true));
            lblTabSettings = CreateTabLabel("Config", 185, false, () => SwitchTab(false));

            Button btnClose = new Button() { 
                Text = "×", 
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                Left = 434, Top = 6, Width = 28, Height = 26, 
                FlatStyle = FlatStyle.Flat, 
                ForeColor = TextMuted, 
                BackColor = WindowBg,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Application.Exit();
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.FromArgb(230, 50, 50); };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = TextMuted; btnClose.BackColor = WindowBg; };

            pnlTitleBar.Controls.Add(lblTitle);
            pnlTitleBar.Controls.Add(lblTabMain);
            pnlTitleBar.Controls.Add(lblTabSettings);
            pnlTitleBar.Controls.Add(btnClose);

            // --- Tab 1: Main Console Panel ---
            pnlMainTab = new Panel() { Left = 16, Top = 50, Width = 448, Height = 350, BackColor = WindowBg };
            
            txtInput = new TextBox() 
            { 
                Left = 0, Top = 5, Width = 448, Height = 105, 
                Multiline = true, 
                BackColor = CardBg, 
                ForeColor = TextPrimary, 
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
            };
            txtInput.KeyDown += TxtInput_KeyDown;

            // Wrap textbox in a border card panel for the UI aesthetic
            Panel pnlTextBoxCard = new Panel() { Left = 0, Top = 5, Width = 448, Height = 110, BackColor = BorderColor };
            txtInput.Left = 1; txtInput.Top = 1; txtInput.Width = 446; txtInput.Height = 108;
            pnlTextBoxCard.Controls.Add(txtInput);

            // Voice Selector Card
            Label lblVoice = new Label() { Text = "Voice Model Slot", Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), Left = 0, Top = 126, Width = 210, ForeColor = TextMuted };
            cmbVoices = CreateStyledComboBox(0, 146, 215);

            // Device Selector Card
            Label lblDevice = new Label() { Text = "Virtual Audio Cable Slot", Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), Left = 233, Top = 126, Width = 215, ForeColor = TextMuted };
            cmbDevices = CreateStyledComboBox(233, 146, 215);

            btnSpeak = new Button() 
            { 
                Text = "BROADCAST TO CABLE", 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Left = 0, Top = 195, Width = 448, Height = 44,
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
                Left = 0, Top = 250, Width = 448,
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlMainTab.Controls.Add(pnlTextBoxCard);
            pnlMainTab.Controls.Add(lblVoice);
            pnlMainTab.Controls.Add(cmbVoices);
            pnlMainTab.Controls.Add(lblDevice);
            pnlMainTab.Controls.Add(cmbDevices);
            pnlMainTab.Controls.Add(btnSpeak);
            pnlMainTab.Controls.Add(lblHint);

            // --- Tab 2: Settings / Config Panel ---
            pnlSettingsTab = new Panel() { Left = 16, Top = 50, Width = 448, Height = 350, BackColor = WindowBg, Visible = false };

            // Volume Control Setting Card
            Panel pnlVolCard = CreateSettingCard(0, 10, "Output Volume", "Adjusts the master volume piped into the virtual cable.");
            trackVolume = new TrackBar() { Left = 15, Top = 42, Width = 345, Height = 30, Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, BackColor = CardBg };
            lblVolumeValue = new Label() { Text = "100%", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Left = 375, Top = 45, Width = 55, ForeColor = AccentCyan, TextAlign = ContentAlignment.MiddleRight };
            trackVolume.Scroll += (s, e) => { lblVolumeValue.Text = trackVolume.Value + "%"; };
            pnlVolCard.Controls.Add(trackVolume);
            pnlVolCard.Controls.Add(lblVolumeValue);

            // Speed Control Setting Card
            Panel pnlSpeedCard = CreateSettingCard(0, 110, "Speech Rate / Speed", "Controls how fast or slow the synthesis engine speaks.");
            trackRate = new TrackBar() { Left = 15, Top = 42, Width = 345, Height = 30, Minimum = -10, Maximum = 10, Value = 0, TickFrequency = 2, BackColor = CardBg };
            lblRateValue = new Label() { Text = "0", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Left = 375, Top = 45, Width = 55, ForeColor = AccentCyan, TextAlign = ContentAlignment.MiddleRight };
            trackRate.Scroll += (s, e) => { lblRateValue.Text = trackRate.Value.ToString(); };
            pnlSpeedCard.Controls.Add(trackRate);
            pnlSpeedCard.Controls.Add(lblRateValue);

            pnlSettingsTab.Controls.Add(pnlVolCard);
            pnlSettingsTab.Controls.Add(pnlSpeedCard);

            // Populate system components
            LoadVoicesAndDevices();

            this.Controls.Add(pnlTitleBar);
            this.Controls.Add(pnlMainTab);
            this.Controls.Add(pnlSettingsTab);
        }

        private Label CreateTabLabel(string text, int leftPos, bool active, Action onClick)
        {
            var lbl = new Label()
            {
                Text = text,
                Font = new Font("Segoe UI", 9F, active ? FontStyle.Bold : FontStyle.Regular),
                Left = leftPos, Top = 12, Width = 65, Height = 22,
                ForeColor = active ? TextPrimary : TextMuted,
                Cursor = Cursors.Hand
            };
            lbl.Click += (s, e) => onClick();
            return lbl;
        }

        private void SwitchTab(bool isMainActive)
        {
            lblTabMain.Font = new Font("Segoe UI", 9F, isMainActive ? FontStyle.Bold : FontStyle.Regular);
            lblTabMain.ForeColor = isMainActive ? TextPrimary : TextMuted;

            lblTabSettings.Font = new Font("Segoe UI", 9F, !isMainActive ? FontStyle.Bold : FontStyle.Regular);
            lblTabSettings.ForeColor = !isMainActive ? TextPrimary : TextMuted;

            pnlMainTab.Visible = isMainActive;
            pnlSettingsTab.Visible = !isMainActive;
        }

        private ComboBox CreateStyledComboBox(int left, int top, int width)
        {
            return new ComboBox()
            {
                Left = left, Top = top, Width = width, Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CardBg,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F)
            };
        }

        private Panel CreateSettingCard(int left, int top, string title, string description)
        {
            var panel = new Panel() { Left = left, Top = top, Width = 448, Height = 88, BackColor = CardBg };
            
            var lblTitle = new Label() { Text = title, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Left = 15, Top = 10, Width = 300, Height = 18, ForeColor = TextPrimary };
            var lblDesc = new Label() { Text = description, Font = new Font("Segoe UI", 8F, FontStyle.Regular), Left = 15, Top = 26, Width = 415, Height = 18, ForeColor = TextMuted };
            
            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblDesc);
            return panel;
        }

        private void LoadVoicesAndDevices()
        {
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

            try
            {
                synthesizer.Rate = speedRate;

                if (!string.IsNullOrEmpty(selectedVoice) && selectedVoice != "Default System Voice")
                {
                    synthesizer.SelectVoice(selectedVoice);
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
