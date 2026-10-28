# Myo Guess

Myo Guess é uma plataforma experimental desenvolvida em **Unity para o Meta Quest 3**, com foco na **validação dos sensores FBG desenvolvidos para identificação de gestos da mão**. O aplicativo oferece uma interface de coleta de dados e um ambiente de jogo para apoiar os experimentos.

O objetivo da coleta é construir uma base de dados de diferentes pessoas para desenvolver um **algoritmo de machine learning capaz de identificar gestos a partir dos sinais dos sensores FBG e generalizar para outras pessoas**. O **Meta Quest 3 e a HuskyLens 2 terão o papel de fontes adicionais de validação**, acrescentando redundância à avaliação do algoritmo baseado em FBG.

> **Projeto em desenvolvimento.** O foco do trabalho é validar os sensores FBG e o reconhecimento de gestos a partir deles. Atualmente, o aplicativo utiliza o reconhecimento do Quest para desenvolver e verificar o fluxo experimental. **Ainda falta integrar os sensores FBG e a HuskyLens 2**, assim como implementar o fluxo completo de dados e validação do modelo. A presença de scripts e bibliotecas relacionados a sensores e MQTT não significa que essas etapas estejam concluídas.

## Sumário

- [Visão geral e estado atual](#visão-geral-e-estado-atual)
- [Objetivo da pesquisa e papel dos dispositivos](#objetivo-da-pesquisa-e-papel-dos-dispositivos)
- [Como funciona a partida](#como-funciona-a-partida)
- [Gestos utilizados](#gestos-utilizados)
- [Coleta de dados para machine learning](#coleta-de-dados-para-machine-learning)
- [Coleta e formato dos dados](#coleta-e-formato-dos-dados)
- [Tecnologias e dependências](#tecnologias-e-dependências)
- [Preparação do ambiente](#preparação-do-ambiente)
- [Build e instalação no Quest](#build-e-instalação-no-quest)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Arquitetura e scripts principais](#arquitetura-e-scripts-principais)
- [Verificações no Editor](#verificações-no-editor)
- [Problemas comuns](#problemas-comuns)
- [Próximas etapas](#próximas-etapas)
- [Contribuições e assets](#contribuições-e-assets)

## Visão geral e estado atual

O projeto reúne dois fluxos: uma **partida**, voltada a reproduzir gestos dentro de um tempo limite, e uma **coleta de dados**, acessível pelo botão **Treinamento**, destinada a formar a base experimental para o desenvolvimento do algoritmo de identificação de gestos por FBG.

A sequência de poses e descansos organiza a aquisição dos dados. O termo “treinamento” se refere à finalidade dos dados para o modelo de machine learning; a orientação visual serve para padronizar a coleta dos participantes.

A base atual permite desenvolver a interface, as regras e o protocolo de coleta usando o Quest como fonte de reconhecimento disponível. A aquisição dos sinais FBG, o desenvolvimento do modelo e a comparação com as fontes de validação ainda precisam ser integrados ao fluxo completo.

| Recurso | Estado atual |
| --- | --- |
| Menu principal | Navegação para partida e treinamento. |
| Partida | Sorteio de gestos, temporizador, pontuação, vidas e painel de fim de jogo. |
| Reconhecimento pelo Quest | Implementado com as poses do Meta XR Interaction SDK para a mão direita; previsto como fonte adicional de validação do algoritmo FBG. |
| Coleta para machine learning | Protocolo com poses, descanso, ciclos e progresso, acessível como **Treinamento** na interface. |
| Registro em CSV | Coleta local com identificação sequencial e contador persistente; ainda sem sinais FBG. |
| Áudio | Músicas por cena e efeitos de interface, erro e fim de jogo. |
| Sensores FBG | **Foco da pesquisa; integração pendente.** `FbgGestureSource` ainda não fornece leituras. |
| Modelo de machine learning para FBG | **Desenvolvimento pendente.** Objetivo de reconhecer gestos em pessoas além das utilizadas no treinamento do modelo. |
| HuskyLens 2 | **Integração pendente.** Prevista como fonte adicional de validação e redundância. |
| MQTT | Scripts auxiliares de recepção de IDs; sem integração com as regras atuais da partida. |

Os recursos descritos refletem o código e as configurações presentes no repositório. As verificações no Editor complementam, mas não substituem, os testes no headset e, futuramente, com os sensores físicos.

## Objetivo da pesquisa e papel dos dispositivos

O objetivo central é avaliar os sensores FBG desenvolvidos e sua utilização para identificar gestos. Para isso, pretende-se realizar uma coleta ampla com diferentes participantes, desenvolver um modelo de machine learning com os sinais adquiridos e avaliar sua capacidade de reconhecer gestos em outras pessoas.

| Componente | Papel no trabalho |
| --- | --- |
| **Sensores FBG** | Fonte principal de sinais para o algoritmo de identificação de gestos e objeto central da validação experimental. |
| **Modelo de machine learning** | Aprender a relação entre os sinais FBG e os gestos, buscando reconhecimento que generalize para pessoas não utilizadas no treinamento. |
| **Meta Quest 3** | Apresentar o protocolo e o ambiente interativo; fornecer reconhecimento de gestos como fonte adicional de comparação para validar o algoritmo FBG. |
| **HuskyLens 2** | Fornecer uma segunda fonte adicional de reconhecimento para comparação e redundância na validação do algoritmo FBG. |

### Fluxo experimental pretendido

1. Coletar sinais dos sensores FBG durante a execução dos gestos por diferentes participantes, associando-os às etapas e aos gestos solicitados.
2. Organizar a base de dados para desenvolver e treinar o algoritmo de identificação de gestos.
3. Avaliar o modelo com pessoas que não participaram do seu treinamento, verificando a generalização entre participantes.
4. Comparar as identificações do algoritmo FBG com o gesto solicitado e com as leituras do Meta Quest 3 e da HuskyLens 2, usando essas fontes para apoiar a validação experimental.

A redundância pretendida permite confrontar as respostas de diferentes fontes durante a avaliação. O método de sincronização, os critérios de concordância e o tratamento de divergências ainda precisam ser definidos e implementados. Os resultados do Quest e da HuskyLens 2 deverão ser avaliados nesse protocolo, sem assumir que sejam referências infalíveis.

Esse fluxo descreve o objetivo da pesquisa. O estado atual do aplicativo ainda registra apenas o protocolo e o reconhecimento do Quest, sem aquisição FBG, treinamento do modelo ou comparação integrada entre os três dispositivos.

## Como funciona a partida

O jogo oferece um ambiente interativo para apresentar gestos e observar respostas durante os experimentos. A implementação atual avalia as rodadas pelo reconhecimento do Quest; a avaliação do algoritmo baseado nos sensores FBG depende da integração pendente.

1. No menu principal, selecione **Jogar**.
2. Um gesto é sorteado e sua imagem se desloca entre os pontos configurados na cena.
3. Reproduza o gesto com a mão direita, mantendo-a visível para o rastreamento do Quest.
4. O jogo compara a pose com o gesto esperado próximo ao fim da rodada.
5. Um acerto adiciona pontos; um erro desconta uma vida.
6. Ao perder todas as vidas, o painel de fim de jogo mostra a pontuação e o recorde, com opções para jogar novamente ou voltar ao menu.

### Regras configuradas atualmente

| Parâmetro | Valor |
| --- | --- |
| Vidas iniciais | 3 |
| Pontos por acerto | 100 |
| Duração de cada rodada | 5 segundos |
| Antecedência da avaliação | 0,1 segundo antes do fim |
| Intervalo de feedback | 0,75 segundo |

A avaliação ocorre **uma única vez por rodada**, no primeiro frame que alcança o instante configurado: aproximadamente **4,9 segundos** com os valores atuais. Não há pontuação por manter a pose correta durante uma fração acumulada da rodada. Por isso, fazer o gesto apenas no início não garante um acerto; ele precisa estar reconhecido no momento da avaliação.

O recorde é salvo localmente com `PlayerPrefs`, na chave `MyoGuess.HighScore`. Ele não representa um ranking online e pode ser perdido ao limpar os dados do aplicativo.

As regras podem ser ajustadas no componente `GestureRoundController` da cena `Gameplay` pelo Inspector.

## Gestos utilizados

O enum `GestureType` define os identificadores usados pelo código e pelos CSVs:

| Valor | Identificador | Uso |
| --- | --- | --- |
| 0 | `None` | Nenhum gesto reconhecido. Não é um alvo da partida. |
| 1 | `OpenHand` | Mão aberta. |
| 2 | `Pointer` | Apontar. |
| 3 | `Rock` | Pose Rock. |
| 4 | `Tripod` | Pose Tripod. |
| 5 | `ThumbsUp` | Pose de polegar para cima; exibida como **OK** no treinamento atual. |
| 6 | `Rest` | Pose de descanso. |

A partida inclui os seis gestos de `OpenHand` a `Rest`. O treinamento usa os cinco primeiros como alvos e intercala fases de descanso.

O reconhecimento usa os estados de poses configurados nas cenas. Para as poses com reconhecimento de formato, `HeadsetGestureSource` remove em tempo de execução as exigências de orientação da mão, preservando os critérios de formato dos dedos.

Quando mais de uma pose está ativa, o indicador de gesto reconhecido e o CSV usam a prioridade `Tripod → ThumbsUp → Rock → Pointer → OpenHand → Rest`. Já a partida consulta diretamente se a pose do alvo está ativa.

## Coleta de dados para machine learning

A tela chamada **Treinamento** organiza a coleta de dados destinada ao desenvolvimento do modelo de machine learning. O participante alterna descanso e execução de gestos conforme as instruções visuais, permitindo associar as amostras às etapas do protocolo.

A finalidade é reunir dados de diferentes pessoas para que o algoritmo baseado nos sensores FBG consiga identificar gestos em outros participantes. Os ciclos e as instruções servem à organização dessa coleta, cujo foco é a formação da base experimental.

**Na implementação atual**, o CSV registra a sequência apresentada e o resultado do reconhecimento pelo Quest. A inclusão dos sinais FBG e das leituras da HuskyLens 2 está pendente. O treinamento e a avaliação do modelo de machine learning também são etapas a desenvolver; não são executados automaticamente pelo aplicativo.

### Protocolo atual

| Parâmetro | Valor |
| --- | --- |
| Gestos por ciclo | 5 |
| Ciclos | 5 |
| Descanso antes de cada gesto | 3 segundos |
| Execução de cada gesto | 5 segundos |
| Intervalo nominal de amostragem | 0,1 segundo, aproximadamente 10 Hz |
| Total de execuções de gestos | 25 |
| Duração nominal da sequência | 200 segundos, ou 3 minutos e 20 segundos, sem suspensões |

A ordem dos cinco gestos é embaralhada a cada ciclo. Os tempos, ciclos e intervalo de amostragem são configuráveis no componente `TrainingInterfaceController` da cena `Training`.

### Como realizar uma coleta

1. Abra **Treinamento** pelo menu principal.
2. Prepare a mão direita e selecione **Iniciar**. Nesse momento é reservado um novo ID e aberto um CSV.
3. Siga a imagem e as instruções: descanse na fase de descanso e mantenha a pose solicitada na fase de gesto.
4. Acompanhe o tempo, o ciclo e o progresso na interface. Durante a sequência, **Iniciar** e **Voltar** ficam indisponíveis.
5. Ao terminar, a interface informa **CSV SALVO** e o nome do arquivo. O botão **Iniciar** fica oculto, e **Voltar** fica disponível.
6. Para uma nova coleta, volte ao menu e abra **Treinamento** novamente.

O controlador também bloqueia chamadas de reinício após a conclusão, mesmo que sejam feitas fora do botão da interface.

Se o sistema suspender o aplicativo, o treinamento interrompe o avanço das fases e a amostragem, descarrega o buffer e retoma o mesmo arquivo ao retornar. O tempo `tempo_coleta_s` usa o relógio de tempo real desde o início da coleta, portanto pode apresentar um salto após a suspensão.

## Coleta e formato dos dados

### Local dos arquivos

No **Unity Editor**, os arquivos ficam em `training/`, na raiz do projeto. No **Quest**, ficam em `Application.persistentDataPath/training`, dentro da pasta do aplicativo:

```text
Android/data/com.labtel.myoguess/files/
└── training/
    ├── .user_counter
    ├── user_0001.csv
    ├── user_0002.csv
    └── ...
```

Cada coleta iniciada recebe um arquivo próprio, sem subpastas por usuário. O nome usa pelo menos quatro dígitos e continua normalmente após `user_9999.csv`.

O ID identifica a **coleta iniciada como um novo voluntário** no fluxo atual. O aplicativo não cadastra pessoas nem verifica se duas coletas pertencem à mesma pessoa.

Para avaliar o reconhecimento em outras pessoas, será necessário manter a correspondência entre coletas e participantes no protocolo experimental. O nome sequencial do arquivo, sozinho, não garante essa distinção quando uma mesma pessoa faz várias coletas. Essa organização ainda precisa ser definida para separar participantes de treinamento e de avaliação do modelo.

### Numeração e preservação

- O arquivo oculto `.user_counter` mantém o último número reservado entre execuções do aplicativo.
- Arquivos e pastas existentes com IDs `user_*` também são considerados para determinar o próximo número.
- Os CSVs são criados sem sobrescrever arquivos existentes.
- Uma coleta interrompida mantém o ID consumido; remover seu CSV não libera esse número enquanto o contador for preservado.
- Um contador com conteúdo inválido interrompe a abertura da coleta, em vez de reiniciar silenciosamente a numeração.

Preserve `.user_counter` junto aos CSVs ao organizar ou fazer backup das coletas. A numeração é local a cada instalação ou diretório; ela não coordena IDs entre diferentes headsets.

**Versionamento:** a pasta `training/` não está ignorada pelo `.gitignore`. Os arquivos de coleta gerados nessa pasta podem ser incluídos no versionamento; revise quais coletas serão adicionadas em cada commit.

### Colunas do CSV

Os arquivos usam UTF-8 sem BOM, vírgula como separador e ponto decimal, independentemente do idioma do computador ou do Quest.

| Coluna | Descrição |
| --- | --- |
| `timestamp_utc` | Data e hora da amostra em UTC, no formato `yyyy-MM-ddTHH:mm:ss.fffZ`. |
| `tempo_coleta_s` | Segundos de tempo real decorridos desde o início da coleta, com seis casas decimais. |
| `ciclo` | Número do ciclo, começando em 1. |
| `etapa` | Posição do gesto dentro do ciclo, começando em 1. |
| `fase` | Fase amostrada: `Rest` ou `Gesture`. |
| `gesto_esperado` | Gesto solicitado pelo protocolo; durante o descanso, `Rest`. Não confirma, por si só, qual gesto o participante executou. |
| `gesto_reconhecido` | Identificador selecionado pelo reconhecimento atual do Quest; `None` quando não há pose reconhecida. Ainda não é a saída de um modelo FBG. |

Exemplo ilustrativo:

```csv
timestamp_utc,tempo_coleta_s,ciclo,etapa,fase,gesto_esperado,gesto_reconhecido
2026-10-28T12:00:00.000Z,0.000000,1,1,Rest,Rest,Rest
2026-10-28T12:00:03.100Z,3.100000,1,1,Gesture,Pointer,Pointer
2026-10-28T12:00:03.200Z,3.200000,1,1,Gesture,Pointer,None
```

A amostragem depende dos frames reais: o gravador não duplica leituras para compensar frames lentos. O intervalo de 0,1 segundo é nominal, não uma garantia de aquisição com frequência fixa.

O buffer é descarregado aproximadamente a cada segundo, na suspensão e ao fechar a coleta. Encerramentos forçados podem perder as amostras ainda no buffer. Os CSVs atuais **não incluem sinais brutos dos sensores FBG, resultados da HuskyLens 2 ou dados sincronizados desses dispositivos**.

Portanto, os arquivos atuais permitem verificar o protocolo e o reconhecimento do Quest, mas ainda não constituem a base de sinais necessária para treinar o algoritmo de identificação por FBG. O formato de aquisição dos sinais e a associação das fontes deverão ser ampliados durante a integração.

### Copiar os CSVs do Quest

Com o headset conectado por USB e a depuração autorizada, execute no computador:

```bash
adb devices
adb pull /sdcard/Android/data/com.labtel.myoguess/files/training ./training
```

O armazenamento pertence ao aplicativo e não exige a permissão `MANAGE_EXTERNAL_STORAGE`. Alguns gerenciadores de arquivos restringem a navegação em `Android/data`; a cópia por ADB é uma alternativa para o fluxo de desenvolvimento.

Uma atualização com o mesmo identificador e assinatura normalmente preserva os dados. Desinstalar ou limpar os dados do aplicativo pode apagar as coletas. Se houver uma instalação antiga com outro identificador, ela possui uma pasta de dados separada. Arquivos do formato anterior em `files/data_collect` não são migrados nem excluídos automaticamente.

## Tecnologias e dependências

As versões abaixo são as declaradas no repositório, e não uma indicação das versões mais recentes disponíveis.

| Tecnologia | Versão ou configuração |
| --- | --- |
| Unity Editor | `6000.3.27f1` |
| Plataforma principal | Meta Quest 3, build Android ARM64 |
| Meta XR SDK All | `207.0.1` |
| Unity OpenXR Plugin | `1.16.1` |
| Interface | Unity UI, TextMesh Pro e interação do Meta XR SDK |
| NuGetForUnity | Dependência Git declarada em `Packages/manifest.json` |
| MQTTnet | `4.3.7.1207`, declarado em `Assets/packages.config` |
| MicronOptics.Hyperion.Communication | `2.0.0`, declarado em `Assets/packages.config`; integração FBG pendente |
| Android mínimo / alvo | API 32 / API 34 em Player Settings |
| Identificador Android | `com.labtel.myoguess` |

O script auxiliar `gesture_subscriber.py` depende de `paho-mqtt`. Python não é necessário para executar o fluxo atual de partida e treinamento.

## Preparação do ambiente

1. Clone o repositório e abra sua pasta pelo **Unity Hub**.
2. Instale o **Unity 6000.3.27f1** com **Android Build Support**, incluindo Android SDK, NDK e OpenJDK.
3. Abra o projeto e aguarde a importação dos assets e a resolução dos pacotes.
4. Confirme a restauração das dependências NuGet declaradas em `Assets/packages.config`. Se aparecerem erros de referências a `MQTTnet` ou `MicronOptics`, verifique a restauração pelo NuGetForUnity.
5. Abra `Assets/Scenes/MainMenu.unity` como ponto de entrada.
6. Para testar o reconhecimento, utilize o Quest com rastreamento das mãos ativo. Executar apenas o Editor sem uma fonte de poses não reproduz as condições de uso no headset.

Não é necessário configurar um broker MQTT, conectar sensores FBG ou conectar a HuskyLens 2 para usar o reconhecimento atual pelo Quest.

## Build e instalação no Quest

1. Conecte o Quest ao computador por USB, habilite o modo de desenvolvedor e autorize a depuração no headset.
2. No Unity, abra **File → Build Profiles**, selecione Android e ative a plataforma, se necessário.
3. Confira a lista de cenas do build, nesta ordem:
   - `Assets/Scenes/MainMenu.unity`
   - `Assets/Scenes/Gameplay.unity`
   - `Assets/Scenes/Training.unity`
4. Confira o identificador `com.labtel.myoguess` e a arquitetura ARM64 nas configurações do Player.
5. Gere o APK com **Build** ou instale diretamente com **Build And Run**.
6. Abra o aplicativo no Quest e teste navegação, rastreamento das mãos, partida e uma coleta completa.

Para instalar um APK já gerado por ADB, substitua o caminho abaixo pelo caminho real do arquivo:

```bash
adb install -r /caminho/para/myo-guess.apk
```

A opção `-r` solicita atualização da instalação existente. Para atualizar preservando os dados, mantenha o mesmo identificador e assinatura do APK instalado.

## Estrutura do projeto

```text
Assets/
├── Audio/                    # Músicas e efeitos
├── Fonts/                    # Fontes e atlas TextMesh Pro
├── Images/                   # Imagens dos gestos, ícones e logo
├── Plugins/Android/          # Manifesto e configuração Android
├── Resources/                # Configurações carregadas em execução
├── Samples/                  # Exemplos importados dos SDKs
├── Scenes/
│   ├── MainMenu.unity        # Entrada e navegação
│   ├── Gameplay.unity        # Partida
│   ├── Training.unity        # Protocolo de coleta para a base de dados
│   └── PoseExamples.unity    # Cena auxiliar; fora do build principal
└── Scripts/
    ├── Editor/               # Ferramentas e verificações do Editor
    └── ...                   # Regras, reconhecimento, áudio e gravação
Packages/                     # Dependências do Unity Package Manager
ProjectSettings/              # Configurações do projeto e lista de cenas
training/                     # Gerada ao coletar pelo Editor
```

As cenas de exemplos dos SDKs e a cena auxiliar `PoseExamples` não fazem parte das três cenas habilitadas no build principal.

## Arquitetura e scripts principais

### Partida e reconhecimento

| Script | Responsabilidade |
| --- | --- |
| `MainMenuController.cs` | Abre as cenas de partida e treinamento. |
| `GestureType.cs` | Define os gestos e os modos de entrada. |
| `GestureSource.cs` | Contrato comum para disponibilidade, suporte a gestos e consulta de poses. |
| `HeadsetGestureSource.cs` | Consulta os reconhecedores do Meta SDK e atualiza o indicador de gesto reconhecido. |
| `FbgGestureSource.cs` | Ponto de extensão para FBG; atualmente retorna indisponível em todas as consultas. |
| `GameplaySession.cs` | Regras de tempo, pontos, vidas e avaliação, independentes da interface e dos sensores. |
| `GestureRoundController.cs` | Conecta entrada, sorteio, animação, HUD, regras e recorde. |
| `GameOverPanel.cs` | Exibe o resultado e os comandos de reinício e retorno ao menu. |
| `GestureLogger.cs` | Registra eventos de detecção de poses para diagnóstico. |

Selecionar o modo FBG no Inspector ainda não habilita uma fonte funcional: o controlador impede o início da partida quando essa fonte está indisponível.

### Coleta de dados e áudio

| Script ou asset | Responsabilidade |
| --- | --- |
| `TrainingInterfaceController.cs` | Organiza ciclos e fases, atualiza a interface e controla a coleta. |
| `TrainingCsvWriter.cs` | Reserva IDs, cria arquivos e serializa as amostras em CSV. |
| `GameAudio.cs` | Inicializa o áudio automaticamente, persiste entre cenas e reproduz músicas e efeitos. |
| `GameAudioSettings.cs` | Define os campos de configuração de músicas, efeitos e volumes. |
| `Assets/Resources/GameAudioSettings.asset` | Contém a configuração de áudio utilizada em execução. |

### MQTT e integração futura

`GestureSubscriber.cs` recebe IDs inteiros de gestos por MQTT, coloca as mensagens em uma fila e as processa na thread principal do Unity. O script Python `gesture_subscriber.py` é um leitor auxiliar que imprime os IDs recebidos.

Esses scripts usam o tópico `myo/gesture/id` e possuem um endereço de broker de desenvolvimento configurado no código ou Inspector. Esse endereço precisa ser ajustado para o ambiente em que os exemplos forem utilizados.

O subscriber C# não faz parte das três cenas atuais do build. Também não há, no fluxo atual, uma conversão conectada desses IDs para `FbgGestureSource` ou uma integração com a HuskyLens 2. O protocolo final e o mapeamento de entradas externas ainda precisam ser definidos; os IDs dos exemplos MQTT não devem ser tratados como um contrato já implementado para os sensores.

## Verificações no Editor

Fora do Play Mode, use os comandos do menu **Myo Guess** e acompanhe os resultados no Console:

| Comando | O que verifica |
| --- | --- |
| **Validar regras da partida** | Instante da avaliação, pontuação única, perda de vidas, fim de jogo e reinício. |
| **Validar coleta do treinamento** | Formato dos CSVs, numeração, contador persistente, preservação de arquivos, configuração de poses e ciclo de gravação. |
| **Validar áudio** | Configurações, músicas por cena, efeitos, importação dos arquivos, volumes e listeners. |

A validação de coleta utiliza uma pasta temporária própria em `Logs/TrainingCollectionChecks`, que é removida pela verificação. Ela não precisa usar os arquivos reais dos participantes.

O menu **Myo Guess → Configurar gameplay** é uma ferramenta de edição, e não uma verificação: ele configura componentes e referências na cena `Assets/Scenes/Gameplay.unity`. Use-o apenas quando precisar reconstruir essa configuração e revise as alterações antes de salvá-las no Git.

Depois de alterar reconhecimento, cenas ou armazenamento, faça também um teste no Quest: conclua uma partida, realize uma coleta, copie o CSV e confira seus dados. A existência dos comandos de validação não implica que a versão aberta no Editor ou o APK atual já tenham sido aprovados.

## Problemas comuns

| Sintoma | O que conferir |
| --- | --- |
| A mão ou o gesto não é reconhecido | Rastreamento das mãos ativo, mão direita visível, iluminação e referências das poses na cena. |
| A partida não inicia no modo FBG | Esse modo ainda não está implementado. Use a entrada do Quest para o fluxo atual. |
| O gesto aparece antes do fim, mas a rodada conta como erro | A partida consulta a pose uma única vez, perto de 4,9 s; mantenha o alvo reconhecido nesse instante. |
| O treinamento não permite repetir ao concluir | Comportamento esperado: volte ao menu e abra o treinamento para iniciar outra coleta. |
| Aparece “ERRO NA COLETA” | Verifique o Console ou logs do dispositivo, o armazenamento e a integridade de `.user_counter`. Preserve os dados antes de corrigir arquivos manualmente. |
| Os CSVs não aparecem no gerenciador de arquivos | Confira a pasta do aplicativo e utilize `adb pull` com a depuração USB autorizada. |
| Há erros de compilação relacionados a bibliotecas | Confira a versão do Unity, os pacotes do Package Manager e a restauração do NuGetForUnity. |
| O áudio não toca | Confira `GameAudioSettings.asset`, os clips atribuídos e os listeners usando **Validar áudio**. |

## Próximas etapas

- **Integrar os sensores FBG:** implementar a aquisição dos sinais que formarão a entrada principal do algoritmo de identificação de gestos.
- **Ampliar a coleta com diferentes participantes:** organizar os sinais, as sessões e os gestos solicitados para construir a base de dados de machine learning.
- **Desenvolver e treinar o modelo baseado em FBG:** implementar o processamento dos sinais e a classificação dos gestos.
- **Avaliar a generalização para outras pessoas:** distinguir os participantes utilizados no treinamento daqueles utilizados na avaliação.
- **Integrar a HuskyLens 2 e as comparações com o Quest:** usar suas leituras como fontes adicionais de validação e redundância para o algoritmo FBG.
- Definir a sincronização das fontes, o protocolo de comunicação, os IDs e o papel do MQTT no fluxo experimental.
- Definir os critérios de avaliação e analisar reconhecimento, latência, concordância entre fontes e divergências para validar os sensores FBG desenvolvidos.
- Refinar a interface e o protocolo de coleta a partir dos experimentos.

Esses itens representam trabalho pendente e não funcionalidades disponíveis nesta versão.

## Contribuições e assets

Ao modificar o projeto, mantenha os arquivos `.meta` junto aos assets, utilize a versão de Unity indicada e execute as verificações relacionadas à alteração. Descreva quais testes foram feitos no Editor e quais foram feitos no Quest.

O repositório inclui SDKs, exemplos, fontes e arquivos de áudio de terceiros. Consulte as licenças que acompanham cada recurso antes de redistribuí-lo. A licença dos efeitos Kenney está em `Assets/Audio/SFX/Kenney-License.txt`. Não há uma licença geral do projeto definida na raiz do repositório no estado atual.
