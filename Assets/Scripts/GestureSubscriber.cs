using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GestureIdEvent : UnityEvent<int>
{
}

public class GestureSubscriber : MonoBehaviour
{
    [Header("MQTT")]
    [SerializeField] private string brokerIp = "172.20.25.125";
    [SerializeField] private int brokerPort = 1883;
    [SerializeField] private string topic = "myo/gesture/id";

    [Header("Último gesto")]
    [SerializeField] private int lastGestureId;

    public GestureIdEvent onGestureReceived;

    private IMqttClient mqttClient;
    private CancellationTokenSource cancellation;

    // Guarda mensagens recebidas até a thread principal da Unity processá-las.
    private readonly ConcurrentQueue<int> receivedGestures =
        new ConcurrentQueue<int>();

    private async void Start()
    {
        cancellation = new CancellationTokenSource();

        MqttFactory factory = new MqttFactory();
        mqttClient = factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += message =>
        {
            string payload =
                message.ApplicationMessage.ConvertPayloadToString();

            if (int.TryParse(payload, out int gestureId))
            {
                receivedGestures.Enqueue(gestureId);
            }

            return Task.CompletedTask;
        };

        MqttClientOptions options = new MqttClientOptionsBuilder()
            .WithClientId("unity-myo-guess")
            .WithTcpServer(brokerIp, brokerPort)
            .WithCleanSession()
            .Build();

        try
        {
            Debug.Log($"Conectando ao MQTT em {brokerIp}:{brokerPort}...");

            await mqttClient.ConnectAsync(
                options,
                cancellation.Token
            );

            MqttClientSubscribeOptions subscribeOptions =
                factory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(filter =>
                    {
                        filter
                            .WithTopic(topic)
                            .WithQualityOfServiceLevel(
                                MqttQualityOfServiceLevel.AtLeastOnce
                            );
                    })
                    .Build();

            await mqttClient.SubscribeAsync(
                subscribeOptions,
                cancellation.Token
            );

            Debug.Log($"MQTT conectado. Escutando: {topic}");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Erro ao conectar ao MQTT: {exception.Message}"
            );
        }
    }

    private void Update()
    {
        // Update roda na thread principal da Unity.
        while (receivedGestures.TryDequeue(out int gestureId))
        {
            lastGestureId = gestureId;

            Debug.Log($"Gesture ID recebido: {gestureId}");

            HandleGesture(gestureId);
            onGestureReceived?.Invoke(gestureId);
        }
    }

    private void HandleGesture(int gestureId)
    {
        switch (gestureId)
        {
            case 1:
                Debug.Log("Executando ação do gesto 1");
                break;

            case 2:
                Debug.Log("Executando ação do gesto 2");
                break;

            case 3:
                Debug.Log("Executando ação do gesto 3");
                break;

            default:
                Debug.Log($"Gesto {gestureId} sem ação configurada");
                break;
        }
    }

    private async void OnDestroy()
    {
        cancellation?.Cancel();

        if (mqttClient != null && mqttClient.IsConnected)
        {
            try
            {
                await mqttClient.DisconnectAsync();
            }
            catch (Exception)
            {
                // O aplicativo já pode estar encerrando.
            }
        }

        cancellation?.Dispose();
    }
}