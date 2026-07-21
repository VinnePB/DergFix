# DergFix - Ferramenta de Reparo do Sistema

DergFix é um utilitário desenvolvido em C# (WPF) que moderniza e facilita o uso do System File Checker (`sfc.exe`) nativo do Windows. Em vez de lidar com a tela preta do terminal, o aplicativo oferece uma interface gráfica limpa, inteligente e em tempo real para a varredura e correção de arquivos corrompidos do sistema.

O projeto nasceu para resolver um problema comum da ferramenta nativa da Microsoft: o envio de dados com bytes nulos (UTF-16) que quebra a leitura de logs de terceiros. O DergFix atua como um "wrapper" que limpa esses caracteres em tempo real e entrega uma experiência visual fluida.

## Destaques da Ferramenta

* **Interface Moderna e Responsiva:** Inicia em modo compacto focando apenas no que importa, com a opção de expandir o terminal para visualização avançada dos logs.
* **Barra de Progresso Interpolada:** O motor do DergFix calcula a estimativa de tempo e faz a barra de progresso deslizar suavemente em valores decimais, sem pulos bruscos.
* **Cronômetro Inteligente:** Projeta o tempo de conclusão da varredura segundo a segundo, identificando automaticamente quando o sistema está processando blocos pesados.
* **Extrator de Logs:** Ao finalizar, o aplicativo varre automaticamente o arquivo `CBS.log` do Windows e extrai apenas os registros dos arquivos que estavam danificados, exibindo um relatório limpo na tela.
* **Totalmente Autônomo:** Compilado como arquivo único (`.exe`), dispensando a necessidade de instalar o ambiente .NET na máquina do usuário.

## Como Usar

Baixe a versão mais recente acessando a página de [Releases](https://github.com/VinnePB/DergFix/releases). 

O aplicativo requer privilégios administrativos para interagir com o núcleo do Windows. Basta executar o `DergFix.exe` (ele já solicitará a elevação automaticamente) e clicar no botão de iniciar a verificação. Aguarde a conclusão e confira o relatório na tela.

## Tecnologias Utilizadas
* C# 10 / .NET 6.0
* Windows Presentation Foundation (WPF)
* Regex para mineração de dados no console
* DispatcherTimer para sincronização de UI

---
Desenvolvido por Vinicius Banques.