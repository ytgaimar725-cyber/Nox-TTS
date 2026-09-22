using System;
using System.Drawing;
using System.IO;
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
        private Label lblVolumeValue;
        private Button btnSpeak;

        // Modern Midnight Neon Palette
        private readonly Color BgColor = Color.FromArgb(18, 18, 20);          // Deep dark base
        private readonly Color PanelColor = Color.FromArgb(30, 30, 35);       // Soft surface container
        private readonly Color BorderColor = Color.FromArgb(50, 50, 60);     // Subtle borders
        private readonly Color TextPrimary = Color.FromArgb(240, 240, 245);  // Crisp white/silver
        private readonly Color TextMuted = Color.FromArgb(160, 160, 175);    // Soft labels
        private readonly Color AccentCyan = Color.FromArgb(0, 229, 255);     // Neon Cyan Accent
        private readonly Color AccentHover = Color.FromArgb(30, 240, 255);   // Lighter cyan for hover

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
            this.Size = new Size(480, 395);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgColor;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

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

            // --- UI Components ---
            
            Label lblHeader = new Label() 
            { 
                Text = "TEXT TO SPEECH BRIDGE", 
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Left = 24, Top = 20, Width = 400, Height = 18, 
                ForeColor = AccentCyan 
            };

            txtInput = new TextBox() 
            { 
                Left = 24, Top = 45, Width = 416, Height = 70, 
                Multiline = true, 
                BackColor = PanelColor, 
                ForeColor = TextPrimary, 
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
            };
            txtInput.KeyDown += TxtInput_KeyDown;

            Label lblVoice = new Label() 
            { 
                Text = "Voice Model", 
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Left = 24, Top = 130, Width = 196, 
                ForeColor = TextMuted 
            };
            
            cmbVoices = new ComboBox() 
            { 
                Left = 24, Top = 152, Width = 196, Height = 25, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = PanelColor, 
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F)
            };

            Label lblDevice = new Label() 
            { 
                Text = "Virtual Audio Output", 
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Left = 244, Top = 130, Width = 196, 
                ForeColor = TextMuted 
            };
            
            cmbDevices = new ComboBox() 
            { 
                Left = 244, Top = 152, Width = 196, Height = 25, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = PanelColor, 
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F)
            };

            Label lblVolume = new Label() 
            { 
                Text = "Volume", 
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Left = 24, Top = 198, Width = 60, 
                ForeColor = TextMuted 
            };
            
            trackVolume = new TrackBar()
            {
                Left = 80, Top = 192, Width = 330, Height = 40,
                Minimum = 0, Maximum = 100, Value = 100,
                TickFrequency = 10,
                BackColor = BgColor
            };
            
            lblVolumeValue = new Label() 
            { 
                Text = "100%", 
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Left = 412, Top = 198, Width = 40, 
                ForeColor = AccentCyan 
            };

            trackVolume.Scroll += (s, e) =>
            {
                lblVolumeValue.Text = trackVolume.Value + "%";
            };

            // Load Installed System Voices (Filtered strictly for Male voices with clean fallback)
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

            // Modern Flat Accent Button with Interactive Hover Effect
            btnSpeak = new Button() 
            { 
                Text = "BROADCAST TO CABLE", 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Left = 24, Top = 255, Width = 416, Height = 42,
                BackColor = AccentCyan, 
                ForeColor = Color.FromArgb(18, 18, 20), 
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSpeak.FlatAppearance.BorderSize = 0;
            btnSpeak.MouseEnter += (s, e) => btnSpeak.BackColor = AccentHover;
            btnSpeak.MouseLeave += (s, e) => btnSpeak.BackColor = AccentCyan;
            btnSpeak.Click += (s, e) => ExecuteSpeech();

            Label lblHint = new Label()
            {
                Text = "Tip: Press Enter in the text box to speak instantly.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Left = 24, Top = 310, Width = 416,
                ForeColor = Color.FromArgb(110, 110, 125),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Add all controls to form
            this.Controls.Add(lblHeader);
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
