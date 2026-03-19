using System;
using System.Drawing;              // Pra trabalhar com cores, fontes e posições
using System.Net.Http;             // Pra fazer requisição HTTP na internet
using System.Text.Json;            // Pra ler JSON que a API retorna
using System.Threading.Tasks;      // Pra usar async/await e não travar o programa
using System.Windows.Forms;        // Pra criar janela e controles do Windows Forms
using System.Runtime.InteropServices; // Pra usar funções do Windows, tipo hotkeys

namespace WeatherWidget
{
    public partial class TempWidget : Form
    {
        private Label tempLabel;        // Label que vai mostrar a temperatura
        private readonly HttpClient client = new HttpClient(); // Cliente HTTP pra chamar a API
        private Timer timer;            // Timer pra atualizar a temperatura de 30 em 30 min

        // Constante pro hotkey global (F10)
        private const int HOTKEY_ID = 9000;

        // Função do Windows pra registrar atalho global
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // Função do Windows pra desregistrar atalho global
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public TempWidget()
        {
            InitializeComponent();        // Inicializa componentes do Form padrão
            InitializeCustomComponents(); // Configura nosso widget customizado

            // Registrar hotkey global F10
            RegisterHotKey(this.Handle, HOTKEY_ID, 0, (uint)Keys.F10);
        }

        // Aqui o Windows avisa quando uma mensagem chega (tipo clique ou hotkey)
        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312; // Código da mensagem de hotkey
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleVisibility(); // Se apertou F10, mostra/esconde o widget
            }
            base.WndProc(ref m); // Sempre chamar o WndProc original
        }

        // Quando fechar o programa
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Desregistrar hotkey pra não deixar bagunça no Windows
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            base.OnFormClosing(e);
        }

        // Função que pega a temperatura na API
        private async Task UpdateTemperatureAsync()
        {
            try
            {
                // URL da API com latitude/longitude da sua cidade
                string url = "https://api.open-meteo.com/v1/forecast?latitude=-7.952&longitude=-37.172&current_weather=true";

                // Chama a API e pega a resposta como string
                var response = await client.GetStringAsync(url);

                // Lê o JSON da API
                using JsonDocument doc = JsonDocument.Parse(response);

                // Pega o valor da temperatura atual
                var temp = doc.RootElement.GetProperty("current_weather").GetProperty("temperature").GetDecimal();

                // Exibe no label com 1 casa decimal e vírgula (29,3 °C)
                tempLabel.Text = temp.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " °C";
            }
            catch
            {
                // Se der erro, mostrar zero grau
                tempLabel.Text = "0 °C";
            }
        }

        // Configura o widget e seus componentes
        private void InitializeCustomComponents()
        {
            // Form sem borda, transparente e sempre na frente
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black; // Preto some
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(1820, 10); // Posição no canto da tela
            this.Size = new Size(100, 50);      // Tamanho do widget

            // Criar o label que mostra a temperatura
            tempLabel = new Label();
            tempLabel.Font = new Font("Trebuchet MS", 16, FontStyle.Bold); // Fonte bonita
            tempLabel.ForeColor = Color.White;    // Texto branco
            tempLabel.BackColor = Color.Transparent; // Fundo transparente
            tempLabel.AutoSize = true;            // Ajusta tamanho pro texto
            this.Controls.Add(tempLabel);         // Coloca na tela

            // Atualiza temperatura ao iniciar
            _ = UpdateTemperatureAsync();

            // Timer que atualiza a cada 30 minutos
            timer = new Timer();
            timer.Interval = 1800000; // 30 min em milissegundos
            timer.Tick += async (s, e) => await UpdateTemperatureAsync(); // Quando bater tempo
            timer.Start(); // Começa o timer
        }

        // Função que mostra ou esconde o widget
        private void ToggleVisibility()
        {
            if (this.Visible)
            {
                // Se tá visível, esconde e pausa o timer
                this.Hide();
                timer.Stop();
            }
            else
            {
                // Se tá escondido, mostra e reinicia o timer
                this.Show();
                _ = UpdateTemperatureAsync(); // Atualiza imediatamente
                timer.Start();
            }
        }
    }
}