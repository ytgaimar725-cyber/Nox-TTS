using System;
using System.Drawing;
using System.Speech.Synthesis;
using System.Windows.Forms;

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
            // Hook up Enter key press event
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

            foreach (var voice in synthesizer.GetInstalledVoices())
            {
                cmbVoices.Items.Add(voice.VoiceInfo.Name);
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
            // Trigger on Enter key without adding a newline
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Prevents the beep sound / newline
                ExecuteSpeech();
            }
        }

        private void ExecuteSpeech()
        {
            string textToSpeak = txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(textToSpeak)) return;

            if (cmbVoices.SelectedItem != null)
            {
                synthesizer.SelectVoice(cmbVoices.SelectedItem.ToString());
            }

            // Speak asynchronously so the UI doesn't freeze
            synthesizer.SpeakAsync(textToSpeak);

            // Clear input box instantly for the next message
            txtInput.Clear();
        }
    }
}
