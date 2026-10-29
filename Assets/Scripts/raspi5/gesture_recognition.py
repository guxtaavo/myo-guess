"""Publica os IDs da HuskyLens 2 no broker MQTT do computador."""

import sys
import threading
import time
from pathlib import Path

import paho.mqtt.client as mqtt

# A biblioteca DFRobot deve estar na raiz do checkout na Raspberry.
PROJECT_DIR = Path(__file__).resolve().parents[3]
sys.path.insert(
    0,
    str(PROJECT_DIR / "DFRobot_HuskylensV2" / "python" / "smbus2"),
)

from dfrobot_huskylensv2 import (
    ALGORITHM_HAND_RECOGNITION,
    HuskylensV2_I2C,
)

MQTT_BROKER = "10.42.0.147"
MQTT_PORT = 1883
MQTT_TOPIC = "myo/gesture/huskylens2/id"

NO_GESTURE = -1
READ_INTERVAL = 0.5  # Pausa entre leituras; a leitura também leva tempo.

connected = threading.Event()


def on_connect(client, userdata, flags, reason_code, properties):
    if reason_code.is_failure:
        connected.clear()
        print(f"Conexão recusada: {reason_code}", flush=True)
        return

    connected.set()
    print("Conectado ao broker MQTT.", flush=True)


def on_disconnect(client, userdata, disconnect_flags, reason_code, properties):
    connected.clear()
    print(f"MQTT desconectado: {reason_code}", flush=True)


def main():
    client = mqtt.Client(
        callback_api_version=mqtt.CallbackAPIVersion.VERSION2,
        client_id="myo-guess-raspberry",
        protocol=mqtt.MQTTv311,
    )
    client.on_connect = on_connect
    client.on_disconnect = on_disconnect
    client.reconnect_delay_set(min_delay=1, max_delay=10)
    client.max_queued_messages_set(20)
    client.will_set(
        MQTT_TOPIC,
        payload=str(NO_GESTURE),
        qos=1,
        retain=False,
    )

    try:
        print("Inicializando HuskyLens...", flush=True)
        huskylens = HuskylensV2_I2C()

        if not huskylens.knock():
            raise RuntimeError("HuskyLens não respondeu. Verifique o I²C.")
        if not huskylens.switchAlgorithm(ALGORITHM_HAND_RECOGNITION):
            raise RuntimeError("Não foi possível ativar o algoritmo de mãos.")

        print(
            f"Conectando ao broker {MQTT_BROKER}:{MQTT_PORT}...",
            flush=True,
        )
        client.connect(MQTT_BROKER, MQTT_PORT, 60)
        client.loop_start()

        if not connected.wait(timeout=10):
            raise RuntimeError("O broker não confirmou a conexão.")

        print("Reconhecimento iniciado. Ctrl+C para encerrar.", flush=True)

        while True:
            gesture_id = NO_GESTURE
            reading_failed = False

            try:
                count = huskylens.getResult(ALGORITHM_HAND_RECOGNITION)

                if count is None:
                    reading_failed = True
                elif huskylens.available(ALGORITHM_HAND_RECOGNITION):
                    result = huskylens.getCachedCenterResult(
                        ALGORITHM_HAND_RECOGNITION
                    )
                    if result is not None:
                        gesture_id = int(result.ID)

            except OSError as error:
                reading_failed = True
                print(f"Erro de comunicação I²C: {error}", flush=True)

            if reading_failed:
                print(
                    "Leitura inválida; enviando -1 e tentando novamente.",
                    flush=True,
                )

            # Publica em todas as iterações, mesmo que o ID não mude.
            if connected.is_set():
                message = client.publish(
                    MQTT_TOPIC,
                    payload=str(gesture_id),
                    qos=1,
                    retain=False,
                )
                if message.rc == mqtt.MQTT_ERR_SUCCESS:
                    print(f"Envio MQTT enfileirado: {gesture_id}", flush=True)
                else:
                    print(
                        f"Falha ao enfileirar envio MQTT: {message.rc}",
                        flush=True,
                    )

            time.sleep(READ_INTERVAL)

    except KeyboardInterrupt:
        print("\nEncerrando...", flush=True)
    except Exception as error:
        print(f"Erro: {error}", flush=True)
        return 1
    finally:
        if connected.is_set():
            try:
                message = client.publish(
                    MQTT_TOPIC,
                    payload=str(NO_GESTURE),
                    qos=1,
                    retain=False,
                )
                message.wait_for_publish(timeout=2)
            except Exception:
                pass
        client.disconnect()
        client.loop_stop()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
