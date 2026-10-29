import asyncio
import logging

from amqtt.broker import Broker


async def main():
    broker = Broker({
        "listeners": {
            "default": {
                "type": "tcp",
                "bind": "0.0.0.0:1883",
            },
        },
        "plugins": {
            "amqtt.plugins.authentication.AnonymousAuthPlugin": {
                "allow_anonymous": True,
            },
        },
    })

    await broker.start()
    print("✅ Broker MQTT iniciado na porta 1883.")

    try:
        await asyncio.Event().wait()
    finally:
        await broker.shutdown()
        print("🛑 Broker MQTT encerrado.")


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass