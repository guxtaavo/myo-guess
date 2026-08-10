import paho.mqtt.client as mqtt

MQTT_BROKER = "172.20.25.125"
MQTT_PORT = 1883
MQTT_TOPIC = "myo/gesture/id"


def on_connect(client, userdata, flags, reason_code, properties):
    if reason_code == 0:
        print("Conectado ao broker MQTT")
        print(f"Escutando o tópico: {MQTT_TOPIC}")
        client.subscribe(MQTT_TOPIC, qos=1)
    else:
        print(f"Falha na conexão: {reason_code}")


def on_message(client, userdata, message):
    texto = message.payload.decode("utf-8")

    try:
        gesture_id = int(texto)
        print(f"Gesto recebido — ID: {gesture_id}")

        # Execute ações conforme o ID recebido
        if gesture_id == 1:
            print("Ação do gesto 1")
        elif gesture_id == 2:
            print("Ação do gesto 2")
        elif gesture_id == 3:
            print("Ação do gesto 3")
        else:
            print("Gesto sem ação configurada")

    except ValueError:
        print(f"Mensagem inválida recebida: {texto}")


client = mqtt.Client(
    mqtt.CallbackAPIVersion.VERSION2,
    client_id="leitor-de-gestos",
)

client.on_connect = on_connect
client.on_message = on_message

print(f"Conectando a {MQTT_BROKER}:{MQTT_PORT}...")

try:
    client.connect(MQTT_BROKER, MQTT_PORT, 60)
    client.loop_forever()

except KeyboardInterrupt:
    print("\nLeitor encerrado pelo usuário.")
    client.disconnect()

except OSError as erro:
    print(f"Não foi possível conectar ao broker: {erro}")