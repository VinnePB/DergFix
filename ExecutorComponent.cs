using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DergFix
{
    public class ExecutorComponent
    {
        public static async Task ExecutarComandoAsync(string programa, string argumentos, Action<string> callbackMensagem)
        {
            await Task.Run(() =>
            {
                var configuracao = new ProcessStartInfo
                {
                    FileName = programa,
                    Arguments = argumentos,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var processo = new Process { StartInfo = configuracao })
                {
                    processo.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data)) callbackMensagem($"[ERRO] {e.Data}");
                    };

                    try
                    {
                        processo.Start();
                        processo.BeginErrorReadLine();

                        var reader = processo.StandardOutput;
                        var sb = new StringBuilder();
                        int charRead;
                        
                        while ((charRead = reader.Read()) >= 0)
                        {
                            char c = (char)charRead;
                            
                            if (c == '\0') continue; // O pulo do gato: ignora os bytes nulos que causam o espaçamento
                            
                            if (c == '\r' || c == '\n')
                            {
                                if (sb.Length > 0)
                                {
                                    callbackMensagem(sb.ToString());
                                    sb.Clear();
                                }
                            }
                            else
                            {
                                sb.Append(c);
                            }
                        }

                        if (sb.Length > 0) callbackMensagem(sb.ToString());

                        processo.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        callbackMensagem($"[FALHA AO INICIAR]: {ex.Message}");
                    }
                }
            });
        }
    }
}