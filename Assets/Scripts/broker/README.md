# Broker MQTT Python no computador

`broker.py` inicia um servidor aMQTT independente da Unity. Os clientes Python
(como o leitor da Raspberry) usam paho-mqtt para conectar a esse servidor.
Não é necessário iniciar Mosquitto ou outro broker na mesma porta.

## Preparar o ambiente

Na raiz do projeto, com Python 3.10 a 3.13:

```bash
python3 -m venv Assets/Scripts/broker/.venv
Assets/Scripts/broker/.venv/bin/python -m pip install -r Assets/Scripts/broker/requirements.txt
```

Se a pasta for movida, recrie a venv no novo caminho para atualizar os scripts
de ativação. Não copie a venv para outros computadores: recrie e instale os requisitos.

## Iniciar

Na raiz do projeto:

```bash
Assets/Scripts/broker/.venv/bin/python Assets/Scripts/broker/broker.py
```

Ou ative a venv antes de executar:

```bash
source Assets/Scripts/broker/.venv/bin/activate
python Assets/Scripts/broker/broker.py
```

Deixe o terminal aberto. Ctrl+C encerra o servidor.
No VS Code, selecione `Assets/Scripts/broker/.venv/bin/python` como interpretador.

## Conexões

O servidor escuta em `0.0.0.0:1883`, aceitando conexões por todas as interfaces IPv4.
Essa configuração não usa senha nem TLS; utilize na rede local de laboratório.

- Na Raspberry, use o IP do computador acessível por ela e a porta `1883`.
- No Quest, use o IP do computador acessível pela rede do headset.
- No Play Mode no próprio computador, pode usar `127.0.0.1:1883`.
- Para receber os IDs da HuskyLens, assine `myo/gesture/huskylens2/id`.

Para identificar o IP do computador na conexão com a Raspberry:

```bash
ip route get 10.42.0.1
```

O endereço depois de `src` é o IP a usar, se `10.42.0.1` ainda for o IP da Raspberry.
Se houver firewall, permita TCP 1883 para a rede local utilizada.

## Teste local com Paho

Com o broker ativo, em outro terminal na raiz do projeto:

```bash
Assets/Scripts/broker/.venv/bin/python -c 'import paho.mqtt.subscribe as s; s.callback(lambda c,u,m: print(m.topic, m.payload.decode(), flush=True), "myo/gesture/huskylens2/id", hostname="127.0.0.1")'
```

Em um terceiro terminal, envie um ID:

```bash
Assets/Scripts/broker/.venv/bin/python -c 'from paho.mqtt.publish import single; single("myo/gesture/huskylens2/id", payload="3", qos=1, hostname="127.0.0.1")'
```

O receptor deve mostrar `myo/gesture/huskylens2/id 3`.
