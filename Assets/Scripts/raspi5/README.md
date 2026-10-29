# Raspberry Pi 5 — HuskyLens 2

`gesture_recognition.py` lê a HuskyLens 2 por I²C e publica IDs inteiros no tópico
`myo/gesture/huskylens2/id`. Publica mesmo quando o gesto se repete, com pausa de
0,5 segundo entre leituras. O tempo de leitura também entra no intervalo real.
O valor `-1` indica ausência de gesto ou falha de leitura. Os IDs nativos da
HuskyLens precisam ser mapeados para os gestos do aplicativo Unity.

Na Raspberry, coloque a biblioteca oficial
[DFRobot_HuskylensV2](https://github.com/DFRobot/DFRobot_HuskylensV2)
na raiz deste checkout. O arquivo esperado é
`DFRobot_HuskylensV2/python/smbus2/dfrobot_huskylensv2.py`.
Habilite a interface I²C da Raspberry e configure a câmera para comunicação I²C.

Na raiz do projeto na Raspberry:

```bash
python3 -m venv .venv
source .venv/bin/activate
python -m pip install -r Assets/Scripts/raspi5/requirements.txt
python Assets/Scripts/raspi5/gesture_recognition.py
```

Antes de executar, inicie o broker no computador e confira `MQTT_BROKER` no
script. O endereço atual é `10.42.0.147`, porta `1883`; ajuste se o IP mudar.
Esta pasta contém o código para versionar; ambientes virtuais ficam fora do Git.
