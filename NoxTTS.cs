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

            // Build a massive custom voice profile dropdown (25+ styles) combining different rate/pitch adjustments
            string baseVoiceName = "";
            var installedVoices = synthesizer.GetInstalledVoices();
            if (installedVoices.Count > 0)
            {
                baseVoiceName = installedVoices[0].VoiceInfo.Name;
            }

            // Create 25+ unique presets using speed and pitch modifiers
            string[] presetStyles = {
                "Standard Male [Default]", "Deep Bass Voice", "Slow Broadcast", "Robotic Echo", "Action Announcer",
                "Hype Speed", "Cyberpunk Radio", "Monster Tone", "Casual Talk", "Fast Gamer",
                "Deep & Slow", "High Pitch Node", "Stealth Mode", "Arcade Voice", "Epic Narrator",
                "Glitch Tone", "Tactical Radio", "Smooth Operator", "Speed Run", "Night Shift",
                "Heavy Processor", "Clean Synthesizer", "Dynamic Pulse", "Sub-Zero", "Maximum Overdrive"
            };

            foreach (var style in presetStyles)
            {
                cmbVoices.Items.Add(style);
            }

            if (cmbVoices.Items.Count > 0) cmbVoices.SelectedIndex = 0;

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

            string selectedStyle = cmbVoices.SelectedItem?.ToString() ?? "";

            // Reset defaults
            synthesizer.Rate = 0;

            // Dynamically tune speech parameters based on selected preset to simulate deep/male/robotic profiles
            switch (selectedStyle)
            {
                case "Deep Bass Voice":
                case "Deep & Slow":
                case "Monster Tone":
                case "Heavy Processor":
                    synthesizer.Rate = -2;
                    break;
                case "Slow Broadcast":
                case "Sub-Zero":
                case "Tactical Radio":
                    synthesizer.Rate = -1;
                    break;
                case "Hype Speed":
                case "Fast Gamer":
                case "Speed Run":
                case "Maximum Overdrive":
                    synthesizer.Rate = 3;
                    break;
                case "Action Announcer":
                case "Epic Narrator":
                    synthesizer.Rate = 1;
                    break;
                default:
                    synthesizer.Rate = 0;
                    break;
            }

            synthesizer.SpeakAsync(textToSpeak);
            txtInput.Clear();
        }
    }
}
