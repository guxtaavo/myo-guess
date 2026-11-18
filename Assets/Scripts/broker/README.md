# Broker MQTT no notebook

O `broker.py` recebe as conexões MQTT da Raspberry e dos clientes de teste.
Este guia instala o ambiente do broker do zero e verifica publicação e recepção.
A gravação dos 21 pontos em CSV funciona sem esse serviço.

## 1. Preparar o notebook

Os comandos deste guia são executados **no terminal local do notebook Linux**,
com o checkout em `~/Documents/GitHub/myo-guess`.

O broker usa `amqtt==0.11.3` e `paho-mqtt==2.1.0`, declarados no
[requirements.txt](requirements.txt). A série aMQTT 0.11 documenta suporte a
Python 3.10–3.13. Este procedimento usa Python 3.12 em uma venv própria, separado
do Python do sistema e dos ambientes de coleta/visualização.

Instale as ferramentas de download:

```bash
sudo apt update
sudo apt install -y curl ca-certificates
```

Instale o gerenciador uv pelo [instalador oficial](https://docs.astral.sh/uv/getting-started/installation/)
e baixe o Python 3.12:

```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
"$HOME/.local/bin/uv" python install 3.12
```

O uv é usado aqui para disponibilizar a versão do Python do broker. As
bibliotecas do projeto serão instaladas com pip a partir do arquivo de requisitos.

## 2. Criar a venv do broker e instalar os requisitos

Entre na raiz do checkout e crie o ambiente com Python 3.12 e pip:

```bash
cd ~/Documents/GitHub/myo-guess
"$HOME/.local/bin/uv" venv --seed --python 3.12 Assets/Scripts/broker/.venv
```

Instale todos os pacotes listados no arquivo do broker:

```bash
Assets/Scripts/broker/.venv/bin/python -m pip install -r Assets/Scripts/broker/requirements.txt
Assets/Scripts/broker/.venv/bin/python -m pip check
Assets/Scripts/broker/.venv/bin/python --version
```

O último comando identifica o Python 3.12 da venv. Os comandos seguintes usam
esse executável diretamente; a ativação de outra venv não altera o ambiente
selecionado. A venv da Raspberry não é utilizada neste procedimento.

## 3. Preparar a rede

1. Conecte notebook e Raspberry à mesma rede local.
2. No notebook, identifique o endereço usado para alcançar a Raspberry. O exemplo
   usa a Raspberry em `10.42.0.1`; substitua pelo IP definido na sua instalação:

```bash
ip route get 10.42.0.1
```

Anote o endereço depois de `src` na resposta. Esse é o IP do notebook a informar
em `MQTT_BROKER` no publicador da Raspberry. A porta é `1883`.

O broker escuta em `0.0.0.0:1883`, isto é, nas interfaces IPv4 do notebook.
Configure as regras do firewall da rede para permitir TCP 1883 entre os
participantes do experimento. O serviço usa conexão sem senha e sem TLS e deve
ser utilizado na rede local de laboratório.

## 4. Iniciar o broker

**No notebook**, na raiz do projeto:

```bash
cd ~/Documents/GitHub/myo-guess
Assets/Scripts/broker/.venv/bin/python Assets/Scripts/broker/broker.py
```

A mensagem de inicialização informa a porta 1883. Mantenha esse terminal aberto
durante os testes. Encerre o servidor com **Ctrl+C**.

## 5. Testar publicação e recepção

Com o broker aberto no primeiro terminal, abra um **segundo terminal local do
notebook** e inicie a recepção do tópico da HuskyLens:

```bash
cd ~/Documents/GitHub/myo-guess
Assets/Scripts/broker/.venv/bin/python -c 'import paho.mqtt.subscribe as s; s.callback(lambda c,u,m: print(m.topic, m.payload.decode(), flush=True), "myo/gesture/huskylens2/id", hostname="127.0.0.1")'
```

Abra um **terceiro terminal local do notebook** e publique um ID de teste:

```bash
cd ~/Documents/GitHub/myo-guess
Assets/Scripts/broker/.venv/bin/python -c 'from paho.mqtt.publish import single; single("myo/gesture/huskylens2/id", payload="3", qos=1, hostname="127.0.0.1")'
```

No segundo terminal, a mensagem recebida é:

```text
myo/gesture/huskylens2/id 3
```

Use **Ctrl+C** para encerrar o receptor. O broker do primeiro terminal pode
continuar aberto para receber a Raspberry.

## 6. Conectar a Raspberry e os clientes

- **Raspberry:** use o IP do notebook anotado na seção 3, porta `1883` e tópico
  `myo/gesture/huskylens2/id`. A instalação do publicador e a configuração de
  `MQTT_BROKER` estão no [guia da Raspberry](../raspi5/README.md#7-publicar-ids-por-mqtt-na-raspberry).
- **Cliente no próprio notebook:** use `127.0.0.1:1883`.
- **Cliente no Quest:** use o IP do notebook acessível pela rede do headset.

O tópico do publicador da HuskyLens é `myo/gesture/huskylens2/id`. Os exemplos
`GestureSubscriber.cs` e `gesture_subscriber.py` usam originalmente
`myo/gesture/id`; para receber a Husky, configure no cliente o IP do notebook e o
mesmo tópico do publicador. Essa configuração de transporte não implementa o
mapeamento dos IDs para as regras do jogo. O subscriber C# ainda não está
conectado às cenas principais.

Referências: [compatibilidade do aMQTT](https://amqtt.readthedocs.io/en/v0.11.3/#python-version-compatibility)
e [instalação de Python com uv](https://docs.astral.sh/uv/guides/install-python/).
