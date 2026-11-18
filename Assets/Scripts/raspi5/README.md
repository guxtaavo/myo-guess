# HuskyLens 2: coletar na Raspberry e visualizar no notebook

Este guia descreve a instalação, a coleta dos 21 pontos em CSV na Raspberry,
a transferência dos arquivos e a visualização interativa no notebook com
Python. A publicação de IDs por MQTT é um fluxo separado, descrito ao final.
Execute os comandos no equipamento indicado em cada seção.

## 1. Equipamentos e ambientes

| Atividade | Equipamento | Venv | Arquivo de requisitos |
| --- | --- | --- | --- |
| Coletar pontos e publicar IDs | Raspberry ligada à Husky por I²C | `~/Documents/myo-guess/.venv` | `~/Documents/myo-guess/requirements.txt`, copiado deste diretório |
| Visualizar CSVs no plotter interativo | Notebook | `~/Documents/GitHub/myo-guess/Assets/Scripts/raspi5/.venv` | `Assets/Scripts/raspi5/requirements-plotter.txt` |
| Executar o broker | Notebook | Ambiente próprio do broker | [Guia do broker](../broker/README.md) |

Cada venv pertence ao equipamento em que foi criada. Instalar pacotes no
notebook não os instala na Raspberry. A coleta em CSV funciona sem broker MQTT.

Os exemplos do notebook usam Ubuntu 24.04 ou posterior, com Python 3.11 ou
posterior e o checkout em `~/Documents/GitHub/myo-guess`. A Raspberry usa
Raspberry Pi OS de 64 bits, o endereço `raspi5labtel@10.42.0.1` e os arquivos em
`~/Documents/myo-guess`. Substitua o usuário e o IP pelos definidos na sua placa.

No repositório do notebook, `Assets/Scripts/raspi5/` corresponde à pasta
`~/Documents/myo-guess/` da Raspberry: ambas contêm `src/`, `data/` e
`requirements.txt`. As venvs e a biblioteca de hardware pertencem a cada máquina.

```text
Assets/Scripts/raspi5/         # Notebook
├── README.md
├── requirements.txt          # Dependências da Raspberry
├── requirements-plotter.txt  # Dependências do notebook
├── .venv/                    # Ambiente local; fora do Git
├── data/                     # CSVs locais; fora do Git
└── src/
    ├── gesture_recognition.py
    ├── huskylens_landmark_logger.py
    └── huskylens_landmark_plotter.py
```

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
scp Assets/Scripts/raspi5/src/gesture_recognition.py \
    Assets/Scripts/raspi5/src/huskylens_landmark_logger.py \
    Assets/Scripts/raspi5/src/huskylens_landmark_plotter.py \
    raspi5labtel@10.42.0.1:Documents/myo-guess/src/
scp Assets/Scripts/raspi5/requirements.txt \
    raspi5labtel@10.42.0.1:Documents/myo-guess/requirements.txt
```

Na Raspberry, `requirements.txt` fica na raiz de `myo-guess`, ao lado de `src`
e `.venv`, pois descreve as dependências do componente executado nessa placa.

Copie apenas os scripts e seus requisitos; a venv é criada na própria Raspberry.

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
├── data/          # CSVs gerados pelo coletor
└── src/
    ├── gesture_recognition.py
    ├── huskylens_landmark_logger.py
    └── huskylens_landmark_plotter.py
```

## 5. Coletar os 21 pontos em CSV

**Na Raspberry**, inicie uma coleta:

```bash
cd ~/Documents/myo-guess
source .venv/bin/activate
python3 src/huskylens_landmark_logger.py mao_aberta_001.csv
```

O script cria a pasta `~/Documents/myo-guess/data/` automaticamente e
salva nela os CSVs, mesmo quando iniciado de dentro de `src/`. No exemplo, o
arquivo será `data/mao_aberta_001.csv`. Sem argumento, o nome é
`huskylens_DATA_HORA_MICROSSEGUNDOS.csv`. Nomes relativos são resolvidos dentro
da pasta de coletas; caminhos absolutos permitem outro destino. Arquivos já
existentes são recusados para preservar a coleta anterior.

Para deixar o nome automático, execute sem o argumento do CSV:

```bash
python3 src/huskylens_landmark_logger.py
```

Se o terminal já estiver em `~/Documents/myo-guess/src/`, use
`python3 huskylens_landmark_logger.py mao_aberta_001.csv`. O destino continua
sendo `~/Documents/myo-guess/data/`.

O script ativa **Hand Recognition**, espera **5 segundos** para a câmera
terminar de trocar o algoritmo e só então começa a gravar. Depois de cada
leitura e gravação, espera 0,1 s antes da próxima leitura; o tempo de comunicação
também conta no intervalo real. Grava uma linha **somente quando há uma mão visível**.
Com mais de uma mão, usa a mais próxima do centro da imagem. Encerre com
**Ctrl+C**; cada linha é salva no arquivo assim que é lida.

### Formato do CSV

Cada linha contém **44 colunas**:

| Colunas | Conteúdo |
| --- | --- |
| `time_s` | Segundos desde o início da gravação (depois da espera de 5 s). |
| `gesture_id` | ID do gesto informado pela câmera; `0` é gesto não aprendido. |
| Demais 42 colunas | X/Y dos 21 pontos, na ordem da câmera: punho, polegar, indicador, médio, anelar e mínimo. |

Os nomes seguem a biblioteca, como `wrist_x`, `wrist_y`, `index_finger_tip_x` e
`index_finger_tip_y`. São coordenadas 2D em pixels da imagem.
O campo `gesture_id` identifica o gesto informado pela câmera. Use o nome do
arquivo para identificar cada coleta ou participante.

## 6. Sincronizar e visualizar os CSVs no notebook

Encerre a coleta na Raspberry com **Ctrl+C** antes de sincronizar. Os dois
equipamentos usam `data/`: na Raspberry em `~/Documents/myo-guess/data/` e no
notebook em `~/Documents/GitHub/myo-guess/Assets/Scripts/raspi5/data/`.
Os CSVs ficam fora do Git; a pasta local mantém os dados entre sessões.

**Em um terminal local do notebook**, instale as dependências uma vez:

No Ubuntu, instale `python3-venv`, `python3-pip` e `python3-tk` se ainda não
estiverem disponíveis. O Tk permite abrir a janela interativa do Matplotlib:

```bash
sudo apt install -y python3-venv python3-pip python3-tk
```

```bash
cd ~/Documents/GitHub/myo-guess/Assets/Scripts/raspi5
python3 -m venv .venv  # somente se a venv do notebook ainda não existir
.venv/bin/python -m pip install -r requirements-plotter.txt
source .venv/bin/activate
```

### Transferir uma nova coleta

**No terminal local do notebook**, dentro de `Assets/Scripts/raspi5/`, copie
o CSV desejado. Substitua `mao_aberta_001.csv` pelo nome gerado na Raspberry:

```bash
mkdir -p data
scp -p raspi5labtel@10.42.0.1:Documents/myo-guess/data/mao_aberta_001.csv data/
```

O comando solicita a senha SSH, se necessário, e copia apenas o arquivo
indicado. Também é possível copiar por SFTP ou pedir ao Codex para buscar as
novas coletas pela conexão autenticada disponível. A atualização é feita sob
demanda. Depois da cópia, renomear um CSV no notebook não altera o original na
Raspberry; use o nome local ao abrir o plotter.

### Abrir o plotter com Python

Em cada novo terminal do notebook, entre na pasta do componente e ative sua
venv. O arquivo CSV é um argumento obrigatório:

```bash
cd ~/Documents/GitHub/myo-guess/Assets/Scripts/raspi5
source .venv/bin/activate
python3 src/huskylens_landmark_plotter.py data/mao_aberta_001.csv
```

O plotter abre um CSV por vez. Para salvar uma imagem em vez de abrir a janela:

```bash
python3 src/huskylens_landmark_plotter.py data/mao_aberta_001.csv --save mao_aberta.png
```

O primeiro argumento é o script Python; o segundo é o caminho do CSV, relativo
à pasta atual do terminal. Se o terminal já estiver em `src/`, use
`python3 huskylens_landmark_plotter.py ../data/mao_aberta_001.csv`. O `..` sobe
de `src/` para a pasta que contém `data/`.

Sem `--save`, abre uma janela interativa: arraste a barra **Amostra**, use
**←/→** para navegar, **Espaço** para reproduzir/pausar e **R** para alternar a
rotação. Execute no notebook, em um terminal com acesso à sessão gráfica
(inclusive o terminal integrado do Codex). Se estiver no prompt
`raspi5labtel@pi`, abra outro terminal local ou use `exit` para sair do SSH.
Depois de sincronizados, os CSVs e o plotter funcionam com a Raspberry desconectada.

O painel esquerdo mostra os pontos em pixels. O direito coloca o punho na
origem e divide as coordenadas pela distância entre o punho e a base do dedo
médio; a tecla **R** também alinha esse dedo para cima. O painel inferior mostra
o `gesture_id` ao longo do tempo. `--amostra 10` seleciona a amostra inicial e
`--rotacao` inicia com o alinhamento ligado. O plotter lê o CSV de 44 colunas
descrito na seção 5: `time_s`, `gesture_id` e as coordenadas X/Y dos 21 pontos.

## 7. Publicar IDs por MQTT na Raspberry

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
de gesto ou falha de leitura; `0` é o ID nativo de gesto não aprendido. O
publicador espera 0,5 segundo depois de cada leitura e publicação.
Encerre com **Ctrl+C**. O mapeamento
desses IDs para os gestos do aplicativo Unity é uma integração pendente.
