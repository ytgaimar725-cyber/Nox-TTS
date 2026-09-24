using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NoxTTS
{
    public partial class MainForm : Form
    {
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
            this.Size = new Size(460, 410);
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
                        IntPtr hIcon = bmp.GetHicon();
                        this.Icon = Icon.FromHandle(hIcon);
                    }
                }
            }
            catch { }

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

            LoadSapiVoices();

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

        private void LoadSapiVoices()
        {
            try
            {
                // Expose OneCore Natural voices (like Andrew) to standard SAPI enumeration layout
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens"))
                {
                    if (baseKey != null)
                    {
                        using (var targetKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Speech\Voices\Tokens"))
                        {
                            foreach (string subKeyName in baseKey.GetSubKeyNames())
                            {
                                if (subKeyName.Contains("Andrew", StringComparison.OrdinalIgnoreCase) || subKeyName.Contains("Natural", StringComparison.OrdinalIgnoreCase))
                                {
                                    using (var sourceSubKey = baseKey.OpenSubKey(subKeyName))
                                    {
                                        if (sourceSubKey != null && targetKey.OpenSubKey(subKeyName) == null)
                                        {
                                            // Safely mirror the registry node so SAPI can target it natively
                                            using (var destSubKey = targetKey.CreateSubKey(subKeyName))
                                            {
                                                foreach (string valueName in sourceSubKey.GetValueNames())
                                                {
                                                    destSubKey.SetValue(valueName, sourceSubKey.GetValue(valueName));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                Type? sapiType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (sapiType != null)
                {
                    dynamic? sapiVoice = Activator.CreateInstance(sapiType);
                    if (sapiVoice != null)
                    {
                        foreach (var token in sapiVoice.GetVoices())
                        {
                            string desc = token.GetDescription();
                            if (!cmbVoices.Items.Contains(desc))
                            {
                                cmbVoices.Items.Add(desc);
                            }
                        }
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

            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "nox_temp_speech.wav");
                if (File.Exists(tempFile)) File.Delete(tempFile);

                Type? sapiType = Type.GetTypeFromProgID("SAPI.SpVoice");
                Type? fileStreamType = Type.GetTypeFromProgID("SAPI.SpFileStream");

                if (sapiType != null && fileStreamType != null)
                {
                    dynamic? voice = Activator.CreateInstance(sapiType);
                    dynamic? fileStream = Activator.CreateInstance(fileStreamType);

                    if (voice != null && fileStream != null)
                    {
                        fileStream.Open(tempFile, 3, false);
                        voice.AudioOutputStream = fileStream;

                        foreach (var token in voice.GetVoices())
                        {
                            string desc = token.GetDescription();
                            if (desc.Equals(selectedVoice, StringComparison.OrdinalIgnoreCase))
                            {
                                voice.Voice = token;
                                break;
                            }
                        }

                        voice.Speak(textToSpeak);
                        fileStream.Close();
                    }
                }

                if (File.Exists(tempFile))
                {
                    using (var audioFile = new AudioFileReader(tempFile))
                    {
                        var volumeProvider = new VolumeSampleProvider(audioFile.ToSampleProvider())
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
                    try { File.Delete(tempFile); } catch { }
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
