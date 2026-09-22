using System;
using System.Drawing;
using System.Speech.Synthesis;
using System.Windows.Forms;
using System.IO;

namespace NoxTTS
{
    public partial class MainForm : Form
    {
        private SpeechSynthesizer synthesizer;
        private TextBox txtInput;
        private ComboBox cmbVoices;
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
            this.Size = new Size(460, 260);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = DarkBg;
            this.ForeColor = TextColor;

            // Load icon.png if present
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
                Left = 20, 
                Top = 20, 
                Width = 400, 
                ForeColor = TextColor 
            };
            
            txtInput = new TextBox() 
            { 
                Left = 20, 
                Top = 45, 
                Width = 400, 
                Height = 60, 
                Multiline = true,
                BackColor = PanelBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtInput.KeyDown += TxtInput_KeyDown;
            
            cmbVoices = new ComboBox() 
            { 
                Left = 20, 
                Top = 120, 
                Width = 230, 
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = PanelBg,
                ForeColor = TextColor
            };

            // Dynamically scan and load all installed Windows system voices (like Microsoft Andrew)
            foreach (var voice in synthesizer.GetInstalledVoices())
            {
                if (voice.Enabled)
                {
                    cmbVoices.Items.Add(voice.VoiceInfo.Name);
                }
            }

            if (cmbVoices.Items.Count == 0)
            {
                cmbVoices.Items.Add("Default System Voice");
            }

            cmbVoices.SelectedIndex = 0;

            btnSpeak = new Button() 
            { 
                Text = "Speak", 
                Left = 260, 
                Top = 119, 
                Width = 160, 
                Height = 30,
                BackColor = AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSpeak.FlatAppearance.BorderSize = 0;
            btnSpeak.Click += (s, e) => ExecuteSpeech();

            this.Controls.Add(lblText);
            this.Controls.Add(txtInput);
            this.Controls.Add(cmbVoices);
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

            try
            {
                if (selectedVoice != "Default System Voice")
                {
                    synthesizer.SelectVoice(selectedVoice);
                }
            }
            catch 
            {
                // Fallback silently if selection fails
            }

            synthesizer.Rate = 0;
            synthesizer.SpeakAsync(textToSpeak);
            txtInput.Clear();
        }
    }
}
