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

        // Dark Theme Color Palette
        private readonly Color DarkBg = Color.FromArgb(30, 30, 30);
        private readonly Color PanelBg = Color.FromArgb(45, 45, 48);
        private readonly Color TextColor = Color.FromArgb(220, 220, 220);
        private readonly Color AccentColor = Color.FromArgb(0, 122, 204);

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public MainForm()
        {
            this.Text = "Nox TTS - Virtual Cable Bridge";
            this.Size = new Size(460, 375);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = DarkBg;
            this.ForeColor = TextColor;

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

            // UI Layout & Styling
            Label lblText = new Label() 
            { 
                Text = "Type message and press Enter:", 
                Left = 20, Top = 20, Width = 400, ForeColor = TextColor 
            };
            
            txtInput = new TextBox() 
            { 
                Left = 20, Top = 45, Width = 400, Height = 60, 
                Multiline = true, BackColor = PanelBg, ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle
            };
            txtInput.KeyDown += TxtInput_KeyDown;

            Label lblVoice = new Label() { Text = "Voice:", Left = 20, Top = 115, Width = 190, ForeColor = TextColor };
            cmbVoices = new ComboBox() 
            { 
                Left = 20, Top = 135, Width = 190, 
                DropDownStyle = ComboBoxStyle.DropDownList, BackColor = PanelBg, ForeColor = TextColor
            };

            Label lblDevice = new Label() { Text = "Output Device (Cable):", Left = 230, Top = 115, Width = 190, ForeColor = TextColor };
            cmbDevices = new ComboBox() 
            { 
                Left = 230, Top = 135, Width = 190, 
                DropDownStyle = ComboBoxStyle.DropDownList, BackColor = PanelBg, ForeColor = TextColor
            };

            Label lblVolume = new Label() { Text = "Volume:", Left = 20, Top = 175, Width = 60, ForeColor = TextColor };
            trackVolume = new TrackBar()
            {
                Left = 80, Top = 170, Width = 300, Height = 45,
                Minimum = 0, Maximum = 100, Value = 100,
                TickFrequency = 10
            };
            lblVolumeValue = new Label() { Text = "100%", Left = 385, Top = 175, Width = 40, ForeColor = TextColor };
            
            trackVolume.Scroll += (s, e) =>
            {
                lblVolumeValue.Text = trackVolume.Value + "%";
            };

            // Load Installed System Voices
            foreach (var voice in synthesizer.GetInstalledVoices())
            {
                if (voice.Enabled) cmbVoices.Items.Add(voice.VoiceInfo.Name);
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

            btnSpeak = new Button() 
            { 
                Text = "Speak to Cable", 
                Left = 20, Top = 230, Width = 400, Height = 35,
                BackColor = AccentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnSpeak.FlatAppearance.BorderSize = 0;
            btnSpeak.Click += (s, e) => ExecuteSpeech();

            this.Controls.Add(lblText);
            this.Controls.Add(txtInput);
            this.Controls.Add(lblVoice);
            this.Controls.Add(cmbVoices);
            this.Controls.Add(lblDevice);
            this.Controls.Add(cmbDevices);
            this.Controls.Add(lblVolume);
            this.Controls.Add(trackVolume);
            this.Controls.Add(lblVolumeValue);
            this.Controls.Add(btnSpeak);
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
            float volumeLevel = trackVolume.Value / 100f; // Convert 0-100 to 0.0-1.0 float

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
                    // Wrap with VolumeSampleProvider to adjust volume cleanly
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
                MessageBox.Show("Error: " + ex.Message);
            }

            txtInput.Clear();
        }
    }
}
