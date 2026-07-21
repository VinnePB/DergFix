using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DergFix
{
    public partial class MainWindow : Window
    {
        private DateTime _tempoInicio;
        private double _ultimaPorcentagem = 0;
        private DateTime _ultimaAtualizacaoTempo;
        private bool _verificacaoConcluida = false;
        private readonly string _caminhoLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Logs\CBS\CBS.log");

        // Timer para atualizar o relógio em tempo real
        private DispatcherTimer _timerUI;
        private DateTime _momentoConclusaoPrevisto;
        private bool _varreduraAtiva = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Configura o relógio para bater a cada 1 segundo
            _timerUI = new DispatcherTimer();
            _timerUI.Interval = TimeSpan.FromSeconds(1);
            _timerUI.Tick += TimerUI_Tick;
        }

        private void TimerUI_Tick(object? sender, EventArgs e)
        {
            if (!_varreduraAtiva || _ultimaPorcentagem == 0 || _ultimaPorcentagem >= 100) return;

            var tempoTotalEstimado = (_momentoConclusaoPrevisto - _tempoInicio).TotalMilliseconds;
            var tempoDecorrido = (DateTime.Now - _tempoInicio).TotalMilliseconds;

            // SUAVIZAÇÃO DA BARRA AZUL EM TEMPO REAL
            if (tempoTotalEstimado > 0)
            {
                double progressoTempoReal = (tempoDecorrido / tempoTotalEstimado) * 100.0;
                progressoTempoReal = Math.Clamp(progressoTempoReal, _ultimaPorcentagem, _ultimaPorcentagem + 0.99);
                progressVarredura.Value = progressoTempoReal;
            }

            var tempoRestante = _momentoConclusaoPrevisto - DateTime.Now;

            if (tempoRestante.TotalSeconds > 0)
            {
                lblEstimativa.Text = $"Tempo Restante: {tempoRestante.Minutes:D2}m {tempoRestante.Seconds:D2}s";
            }
            else
            {
                // Se o tempo esgotou (o arquivo é mais pesado que a nossa estimativa calculou)
                if ((DateTime.Now - _ultimaAtualizacaoTempo).TotalSeconds > 5)
                {
                    lblEstimativa.Text = "Lendo blocos pesados do sistema...";
                }
            }
        }

        private void Log(string mensagem)
        {
            Dispatcher.Invoke(() =>
            {
                txtConsole.AppendText($"{DateTime.Now:HH:mm:ss} - {mensagem}\n");
                txtConsole.ScrollToEnd();
                ProcessarProgresso(mensagem);
            });
        }

        private void ProcessarProgresso(string linha)
        {
            var match = Regex.Match(linha, @"(\d+)\s*%", RegexOptions.IgnoreCase);
            
            if (match.Success && double.TryParse(match.Groups[1].Value, out double porcentagem))
            {
                porcentagem = Math.Clamp(porcentagem, 0, 100);

                Dispatcher.Invoke(() =>
                {
                    progressVarredura.IsIndeterminate = false;
                    progressVarredura.Value = porcentagem;
                    lblPorcentagem.Text = $"{porcentagem}%";

                    if (porcentagem > 0 && _tempoInicio != default)
                    {
                        if (porcentagem != _ultimaPorcentagem)
                        {
                            _ultimaPorcentagem = porcentagem;
                            _ultimaAtualizacaoTempo = DateTime.Now;
                            
                            var tempoDecorrido = DateTime.Now - _tempoInicio;
                            double progressoDecimal = porcentagem / 100.0;
                            double tempoTotalEstimado = tempoDecorrido.TotalMilliseconds / progressoDecimal;
                            
                            // Calcula em qual momento exato do futuro o processo deve acabar
                            _momentoConclusaoPrevisto = _tempoInicio.AddMilliseconds(tempoTotalEstimado);
                            
                            // Força o relógio a atualizar o texto na hora
                            TimerUI_Tick(null, EventArgs.Empty);
                        }
                    }
                });
            }
        }

        private async void btnDisparar_Click(object sender, RoutedEventArgs e)
        {
            if (_verificacaoConcluida)
            {
                Application.Current.Shutdown();
                return;
            }

            txtConsole.Clear();
            btnDisparar.IsEnabled = false;
            btnDisparar.Content = "Aguarde...";
            btnAbrirLog.Visibility = Visibility.Collapsed;
            
            progressVarredura.Value = 0;
            progressVarredura.IsIndeterminate = true;
            _ultimaPorcentagem = 0;
            _ultimaAtualizacaoTempo = DateTime.Now;
            
            lblPorcentagem.Text = "0%";
            lblEstimativa.Text = "Iniciando varredura profunda...";
            
            _tempoInicio = DateTime.Now;
            _varreduraAtiva = true;
            _timerUI.Start(); // Liga o motor do relógio

            Log("=== INICIANDO VERIFICAÇÃO DE INTEGRIDADE ===");

            await ExecutorComponent.ExecutarComandoAsync("sfc.exe", "/scannow", Log);

            _varreduraAtiva = false;
            _timerUI.Stop(); // Desliga o relógio
            progressVarredura.IsIndeterminate = false;
            progressVarredura.Value = 100;
            lblPorcentagem.Text = "100%";

            Log("=== VARREDURA FINALIZADA ===");
            Log("[Processando] Analisando registros de reparo do sistema...");
            
            await AnalisarArquivosDanificadosAsync();

            lblEstimativa.Text = "Concluído!";
            
            _verificacaoConcluida = true;
            btnDisparar.Content = "Sair";
            btnDisparar.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333"));
            btnDisparar.IsEnabled = true;
            btnAbrirLog.Visibility = Visibility.Visible;
        }

        private void btnOcultarTerminal_Click(object sender, RoutedEventArgs e)
        {
            if (btnOcultarTerminal.IsChecked == true)
            {
                boxTerminal.Visibility = Visibility.Collapsed;
                btnOcultarTerminal.Content = "Mostrar Terminal";
                this.SizeToContent = SizeToContent.Height; // Encolhe na medida exata
            }
            else
            {
                boxTerminal.Visibility = Visibility.Visible;
                btnOcultarTerminal.Content = "Ocultar Terminal";
                this.SizeToContent = SizeToContent.Manual; // Desliga o modo automático
                this.Height = 580; // Volta pro tamanho original
            }
        }

        private async Task AnalisarArquivosDanificadosAsync()
        {
            if (!File.Exists(_caminhoLog))
            {
                Log("[Aviso] Arquivo CBS.log não foi localizado para extração de detalhes.");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    var resumoReparos = new StringBuilder();
                    int detectados = 0;

                    using (var fs = new FileStream(_caminhoLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string? linha;
                        while ((linha = sr.ReadLine()) != null)
                        {
                            if (linha.Contains("[SR]") && (linha.Contains("Repairing") || linha.Contains("Repaired") || linha.Contains("corrupt")))
                            {
                                int idxCsi = linha.IndexOf("[SR]");
                                string linhaLimpa = linha.Substring(idxCsi);
                                resumoReparos.AppendLine($"   -> {linhaLimpa}");
                                detectados++;
                            }
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (detectados > 0)
                        {
                            txtConsole.AppendText($"\n=== ARQUIVOS DANIFICADOS ENCONTRADOS ({detectados}) ===\n");
                            txtConsole.AppendText(resumoReparos.ToString());
                            txtConsole.AppendText("====================================================\n\n");
                        }
                        else
                        {
                            txtConsole.AppendText("\n[Info] Nenhum arquivo corrompido pendente de reparo foi registrado nesta sessão.\n\n");
                        }
                        txtConsole.ScrollToEnd();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => Log($"[Falha ao ler logs]: {ex.Message}"));
                }
            });
        }

        private void btnAbrirLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pastaLog = Path.GetDirectoryName(_caminhoLog) ?? @"C:\Windows\Logs\CBS";
                Process.Start(new ProcessStartInfo
                {
                    FileName = pastaLog,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível abrir a pasta de logs: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}