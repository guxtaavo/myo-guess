# HuskyLens 2: coletar na Raspberry e visualizar no notebook

Este guia parte da instalação do zero e termina com a comparação de dois
CSVs. Execute os passos na ordem indicada e no equipamento identificado em
cada seção.

## 1. Equipamentos e ambientes

| Atividade | Equipamento | Venv | Arquivo de requisitos |
| --- | --- | --- | --- |
| Coletar pontos e publicar IDs | Raspberry ligada à Husky por I²C | `~/Documents/myo-guess/.venv` | `~/Documents/myo-guess/requirements.txt`, copiado deste diretório |
| Visualizar CSVs | Notebook Linux com este repositório | `Assets/Scripts/raspi5/.plotter.venv` | `Assets/Scripts/raspi5/requirements-plotter.txt` |
| Executar o broker | Notebook | Ambiente próprio do broker | [Guia do broker](../broker/README.md) |

Cada venv pertence ao equipamento em que foi criada. Instalar pacotes no
notebook não os instala na Raspberry. A coleta em CSV funciona sem broker MQTT.

Os exemplos do notebook usam Ubuntu 24.04 ou posterior, com Python 3.11 ou
posterior e o checkout em `~/Documents/GitHub/myo-guess`. A Raspberry usa
Raspberry Pi OS de 64 bits, o endereço `raspi5labtel@10.42.0.1` e os arquivos em
`~/Documents/myo-guess`. Substitua o usuário e o IP pelos definidos na sua placa.

## 2. Preparar a Raspberry

### 2.1 Instalar o sistema e conectar por SSH

1. Grave o Raspberry Pi OS de 64 bits usando o
   [Raspberry Pi Imager](https://www.raspberrypi.com/software/).
2. Na personalização, defina usuário, senha e rede, e habilite SSH.
3. Inicie a Raspberry, conecte o notebook à mesma rede e anote o IP da placa.
4. **No terminal local do notebook**, conecte por SSH:

```bash
ssh raspi5labtel@10.42.0.1
```

Os próximos comandos da seção 2 são executados nessa sessão remota. O prompt
identifica o usuário da Raspberry. Abrir uma pasta por SFTP no gerenciador de
arquivos permite transferir arquivos; isso não transforma o terminal local em
uma sessão SSH.

### 2.2 Instalar ferramentas e habilitar I²C

**Na Raspberry**, instale as ferramentas do sistema:

```bash
sudo apt update
sudo apt install -y git python3 python3-venv python3-pip i2c-tools
sudo usermod -aG i2c "$USER"
sudo raspi-config
```

No `raspi-config`, abra **Interface Options → I2C**, habilite a interface e
finalize a configuração. Reinicie para aplicar a configuração e a participação
no grupo `i2c`:

```bash
sudo reboot
```

A reinicialização encerra o SSH. **No notebook**, conecte novamente:

```bash
ssh raspi5labtel@10.42.0.1
```

### 2.3 Conectar a HuskyLens 2

1. Prepare a alimentação e o conector Gravity I²C/UART conforme o
   [guia de hardware DFRobot](https://wiki.dfrobot.com/sen0638/).
2. Conecte SDA ao GPIO 2/pino físico 3, SCL ao GPIO 3/pino físico 5 e o terra
   comum ao GND/pino físico 6 da Raspberry. A alimentação da câmera deve seguir
   a montagem indicada pela DFRobot.
3. Nas configurações de comunicação da HuskyLens, selecione **I²C**.
4. Abra **Hand Recognition** na câmera. Os scripts também selecionam esse
   algoritmo na inicialização. A obtenção dos pontos não exige ensinar gestos.
5. **Na Raspberry**, consulte o barramento:

```bash
ls -l /dev/i2c-1
i2cdetect -y 1
```

A biblioteca usa o barramento `/dev/i2c-1` e o endereço `0x50`. Consulte também a
[configuração I²C da Raspberry Pi](https://www.raspberrypi.com/documentation/computers/configuration.html#enable-or-disable-i2c).

## 3. Copiar os scripts do notebook para a Raspberry

**Na sessão SSH da Raspberry**, crie a pasta de trabalho:

```bash
mkdir -p ~/Documents/myo-guess/src
```

**Em outro terminal, local do notebook**, copie os scripts e o arquivo de requisitos:

```bash
cd ~/Documents/GitHub/myo-guess
scp Assets/Scripts/raspi5/gesture_recognition.py \
    Assets/Scripts/raspi5/huskylens_landmark_logger.py \
    raspi5labtel@10.42.0.1:Documents/myo-guess/src/
scp Assets/Scripts/raspi5/requirements.txt \
    raspi5labtel@10.42.0.1:Documents/myo-guess/requirements.txt
```

Na Raspberry, `requirements.txt` fica na raiz de `myo-guess`, ao lado de `src`
e `.venv`, pois descreve as dependências do componente executado nessa placa.

O visualizador permanecerá no notebook. Copie os scripts e seus requisitos;
os ambientes virtuais são criados separadamente em cada equipamento.

## 4. Instalar as dependências da Raspberry

**Na sessão SSH da Raspberry**, baixe a biblioteca oficial na raiz da pasta de
trabalho e crie a venv:

```bash
cd ~/Documents/myo-guess
git clone --depth 1 https://github.com/DFRobot/DFRobot_HuskylensV2.git
python3 -m venv .venv
```

Instale nessa venv todos os pacotes do arquivo copiado do projeto:

```bash
.venv/bin/python -m pip install -r requirements.txt
```

O comando usa diretamente o Python da venv. O `requirements.txt` contém:

| Pacote instalado | Uso |
| --- | --- |
| `paho-mqtt==2.1.0` | Publicação de IDs pelo script MQTT. |
| `smbus2==0.6.1` | Comunicação I²C com a câmera. |
| `pyserial==3.5` | Fornece o módulo `serial`, importado pela biblioteca DFRobot. |

**`pyserial` é o nome do pacote; `serial` é o nome do módulo no código.**
A biblioteca oficial faz `import serial`. A classe I²C comunica por `smbus2`;
a classe UART utiliza `serial.Serial`. O pacote PyPI chamado `serial` é outro
projeto e não faz parte dos requisitos deste coletor. Referências:
[pySerial](https://pyserial.readthedocs.io/en/latest/pyserial.html#installation) e
[biblioteca DFRobot](https://github.com/DFRobot/DFRobot_HuskylensV2/tree/master/python/smbus2).

Confira os componentes instalados antes da primeira coleta:

```bash
.venv/bin/python -m pip check
.venv/bin/python -c 'import smbus2, serial; from paho.mqtt import client; print("Comunicação instalada; pySerial:", serial.VERSION)'
ls DFRobot_HuskylensV2/python/smbus2/dfrobot_huskylensv2.py
```

A estrutura preparada é:

```text
~/Documents/myo-guess/
├── .venv/
├── DFRobot_HuskylensV2/
│   └── python/smbus2/dfrobot_huskylensv2.py
├── requirements.txt
└── src/
    ├── gesture_recognition.py
    └── huskylens_landmark_logger.py
```

## 5. Coletar os 21 pontos em CSV

**Na Raspberry**, crie a pasta de dados e inicie uma coleta de mão aberta:

```bash
cd ~/Documents/myo-guess
mkdir -p data
.venv/bin/python src/huskylens_landmark_logger.py --csv data/mao_aberta_001.csv --label OpenHand
```

Mantenha uma mão no campo de visão da câmera. O terminal mostra o número de cada
amostra, o estado da leitura e a quantidade de mãos. Encerre com **Ctrl+C**;
o CSV já terá sido atualizado a cada amostra.

Faça uma segunda coleta com o gesto de apontar:

```bash
.venv/bin/python src/huskylens_landmark_logger.py --csv data/apontar_001.csv --label Pointer
```

O argumento `--label` é uma anotação manual aplicada à coleta inteira. Ele não
significa que o script reconheceu o gesto. Use um arquivo novo para cada coleta,
por exemplo `mao_aberta_002.csv` na próxima repetição.

O intervalo alvo é **0,5 segundo entre inícios de leitura**, buscando duas
amostras por segundo. O programa desconta o tempo de leitura e gravação antes
da espera. A taxa real é limitada pelo tempo de comunicação; os dados não são
duplicados para preencher atrasos.

### Formato do CSV

Cada linha contém **45 colunas**:

| Colunas | Conteúdo |
| --- | --- |
| `timestamp_utc` | Data e hora UTC de recebimento na Raspberry. |
| `elapsed_s` | Segundos desde o início da coleta até o recebimento. |
| `label` | Rótulo informado na execução. |
| Demais 42 colunas | X/Y dos 21 pontos: punho e quatro pontos de cada dedo. |

Os nomes seguem a biblioteca, como `wrist_x`, `wrist_y`, `index_finger_tip_x` e
`index_finger_tip_y`. São coordenadas 2D da imagem. Ausência da mão e falhas
produzem campos de coordenadas vazios, preservando o horário e o rótulo.
Leituras parciais preservam os pontos disponíveis. Os estados `ok`, `no_hand`,
`read_error` e `invalid_points` aparecem no terminal. Com mais de uma mão
visível, o coletor usa a mais próxima do centro da imagem.

## 6. Copiar os CSVs para o notebook

**Em um terminal local do notebook**, copie os dois arquivos produzidos:

```bash
cd ~/Documents/GitHub/myo-guess
mkdir -p data/huskylens
scp raspi5labtel@10.42.0.1:Documents/myo-guess/data/mao_aberta_001.csv data/huskylens/
scp raspi5labtel@10.42.0.1:Documents/myo-guess/data/apontar_001.csv data/huskylens/
```

Os próximos passos usam essas cópias locais, sem conexão com a câmera.

## 7. Instalar o visualizador no notebook

**No notebook Linux**, instale o suporte a venv e a janelas Tk do Python:

```bash
sudo apt update
sudo apt install -y python3 python3-venv python3-pip python3-tk
cd ~/Documents/GitHub/myo-guess
python3 -m venv Assets/Scripts/raspi5/.plotter.venv
```

Instale os pacotes do visualizador em seu próprio ambiente:

```bash
Assets/Scripts/raspi5/.plotter.venv/bin/python -m pip install -r Assets/Scripts/raspi5/requirements-plotter.txt
Assets/Scripts/raspi5/.plotter.venv/bin/python -m pip check
```

O arquivo `requirements-plotter.txt` declara Matplotlib e requer Python 3.11 ou
posterior. Essa venv é independente da venv da Raspberry e da venv do broker.

## 8. Abrir e comparar os gestos no notebook

Abra o CSV de mão aberta informando seu caminho após o nome do script:

```bash
Assets/Scripts/raspi5/.plotter.venv/bin/python Assets/Scripts/raspi5/huskylens_landmark_plotter.py data/huskylens/mao_aberta_001.csv
```

O CSV é um argumento obrigatório. Com um arquivo, o programa permite comparar
duas amostras da mesma coleta. Para comparar as duas coletas:

```bash
Assets/Scripts/raspi5/.plotter.venv/bin/python Assets/Scripts/raspi5/huskylens_landmark_plotter.py data/huskylens/mao_aberta_001.csv data/huskylens/apontar_001.csv
```

| Controle | Ação |
| --- | --- |
| **Amostra A / Amostra B** | Seleciona a linha de cada coleta; a contagem começa em 1. |
| **Sobrepor** | Desenha as duas mãos no mesmo gráfico. |
| **Alinhar punho / escala** | Coloca o punho na origem e usa a distância punho–base do médio como unidade. |
| **Índices dos pontos** | Mostra os números de 0 a 20, com a correspondência dos dedos no rodapé. |

Os gráficos têm os mesmos limites e proporções. Y cresce para baixo, como na
imagem. A normalização preserva a orientação; não corrige perspectiva. Uma
amostra sem punho ou base do médio não pode ser normalizada. Linhas vazias
continuam na sequência, e pontos ausentes não são interpolados.

Para salvar uma comparação sem abrir janela:

```bash
Assets/Scripts/raspi5/.plotter.venv/bin/python Assets/Scripts/raspi5/huskylens_landmark_plotter.py data/huskylens/mao_aberta_001.csv data/huskylens/apontar_001.csv --normalize --overlay --save data/huskylens/comparacao_001.png
```

Use `--sample-a 2 --sample-b 3` para escolher amostras na exportação e
`--no-indices` para ocultar os números. A exportação aceita PNG, SVG e PDF;
cada exportação deve receber um nome de arquivo novo.

## 9. Publicar IDs por MQTT na Raspberry

Este fluxo publica os IDs aprendidos pela HuskyLens. Use a preparação da
Raspberry das seções 2 a 4.

1. **No notebook**, instale e inicie o broker pelo
   [guia do broker](../broker/README.md).
2. Anote o IP do notebook acessível pela Raspberry, conforme a etapa de rede
   daquele guia.
3. **Na Raspberry**, abra `src/gesture_recognition.py` e configure `MQTT_BROKER`
   com esse IP. Mantenha a porta `1883` e o tópico `myo/gesture/huskylens2/id`.
4. No modo **Hand Recognition** da Husky, aprenda os gestos cujos IDs serão utilizados.
5. Na sessão SSH, inicie a publicação:

```bash
cd ~/Documents/myo-guess
.venv/bin/python src/gesture_recognition.py
```

O script publica IDs inteiros, inclusive quando se repetem. `-1` indica ausência
de gesto ou falha de leitura; `0` é o ID nativo de gesto não aprendido. Nesse
publicador, a pausa de 0,5 segundo acontece depois da leitura, diferentemente
da frequência alvo do coletor de pontos. Encerre com **Ctrl+C**. O mapeamento
desses IDs para os gestos do aplicativo Unity é uma integração pendente.
