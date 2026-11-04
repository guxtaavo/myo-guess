"""Grava os 21 pontos da mão da HuskyLens 2 em CSV por I2C.

Na Raspberry, com o terminal na raiz de myo-guess:
    python3 src/huskylens_landmark_logger.py --csv pontos.csv --label OpenHand

Com o terminal dentro de src/:
    python3 huskylens_landmark_logger.py --csv pontos.csv --label OpenHand

A biblioteca DFRobot_HuskylensV2 pode ficar ao lado de src/. O caminho é
encontrado automaticamente; o script também funciona na estrutura da Unity.

Cada amostra contém as 42 coordenadas X/Y da mão mais próxima do centro.
O arquivo é criado automaticamente. Encerre a coleta com Ctrl+C.
O intervalo padrão é 0,5 segundo entre inícios de leitura (alvo: 2 amostras/s).
"""

import argparse
import csv
import math
import sys
import time
from contextlib import ExitStack
from datetime import datetime, timezone
from pathlib import Path


def find_library_dir(script_path):
    """Aceita tanto a estrutura Unity quanto o script copiado para src/."""
    for parent in Path(script_path).resolve().parents:
        candidate = parent / "DFRobot_HuskylensV2" / "python" / "smbus2"
        if (candidate / "dfrobot_huskylensv2.py").is_file():
            return candidate
    return None


LIBRARY_DIR = find_library_dir(__file__)

# Campos oficiais, na ordem 0–20: punho, polegar, indicador, médio, anelar, mínimo.
POINT_NAMES = (
    "wrist", "thumb_cmc", "thumb_mcp", "thumb_ip", "thumb_tip",
) + tuple(
    f"{finger}_finger_{joint}"
    for finger in ("index", "middle", "ring", "pinky")
    for joint in ("mcp", "pip", "dip", "tip")
)
COORDINATE_FIELDS = tuple(
    f"{point}_{axis}" for point in POINT_NAMES for axis in ("x", "y")
)
CSV_FIELDS = ("timestamp_utc", "elapsed_s", "label") + COORDINATE_FIELDS


def read_coordinates(result):
    """Preserva cada coordenada disponível; campos inválidos ficam vazios."""
    coordinates = {}
    invalid = []
    for field in COORDINATE_FIELDS:
        try:
            value = float(getattr(result, field))
        except (AttributeError, TypeError, ValueError, OverflowError):
            invalid.append(field)
            continue
        if not math.isfinite(value) or value < 0:
            invalid.append(field)
            continue
        coordinates[field] = value
    return coordinates, invalid


def read_sample(camera, algorithm):
    row = {"status": "read_error"}
    try:
        count = camera.getResult(algorithm)
        if count is None:
            raise ValueError("A Husky não retornou uma leitura completa.")
        row["hand_count"] = count
        if count == 0:
            row["status"] = "no_hand"
            return row  # Não reaproveita coordenadas da amostra anterior.
        result = camera.getCachedCenterResult(algorithm)
        if result is None:
            raise ValueError("Resultado da mão indisponível.")
    except (OSError, ValueError, IndexError) as error:
        row["error"] = str(error)
        return row

    # Os pontos também são coletados de gestos que a câmera não aprendeu.
    coordinates, invalid = read_coordinates(result)
    row.update(coordinates)
    row["status"] = "invalid_points" if invalid else "ok"
    if invalid:
        row["error"] = "Coordenadas ausentes/inválidas: " + ", ".join(invalid)
    return row


def positive_interval(value):
    interval = float(value)
    if not math.isfinite(interval) or interval <= 0:
        raise argparse.ArgumentTypeError("O intervalo deve ser positivo e finito.")
    return interval


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "--csv", type=Path,
        help="Arquivo novo de saída (padrão: huskylens_points_DATA_HORA.csv).",
    )
    parser.add_argument("--label", default="", help="Anotação manual do gesto executado.")
    parser.add_argument(
        "--interval", type=positive_interval, default=0.5,
        help="Intervalo alvo entre inícios de leitura, incluindo I²C (padrão: 0.5 = 2/s).",
    )
    args = parser.parse_args()
    output_path = args.csv or Path(
        "huskylens_points_" + datetime.now(timezone.utc).strftime("%Y%m%d_%H%M%S_%f") + ".csv"
    )

    # --help funciona mesmo sem a câmera ou suas dependências instaladas.
    if LIBRARY_DIR is not None:
        sys.path.insert(0, str(LIBRARY_DIR))
    try:
        from dfrobot_huskylensv2 import ALGORITHM_HAND_RECOGNITION, HuskylensV2_I2C
    except ImportError as error:
        if isinstance(error, ModuleNotFoundError) and error.name == "dfrobot_huskylensv2":
            hint = (
                "A biblioteca oficial não foi encontrada. Na raiz do projeto, execute:\n"
                "git clone --depth 1 https://github.com/DFRobot/DFRobot_HuskylensV2.git "
                "DFRobot_HuskylensV2\n"
                "Ela pode ficar ao lado de src/; não precisa mover o script."
            )
        else:
            hint = (
                "Instale as dependências no ambiente Python:\n"
                "python3 -m pip install smbus2 pyserial\n"
                "O pacote correto se chama pyserial, não serial."
            )
        print(f"Dependência indisponível: {error}\n{hint}", file=sys.stderr)
        return 1

    saved_samples = 0
    try:
        with ExitStack() as stack:
            # 'x' impede sobrescrever uma coleta existente.
            csv_file = stack.enter_context(output_path.open("x", newline="", encoding="utf-8"))
            writer = csv.DictWriter(csv_file, fieldnames=CSV_FIELDS)
            writer.writeheader()
            csv_file.flush()

            if LIBRARY_DIR is not None:
                print(f"Biblioteca DFRobot encontrada em: {LIBRARY_DIR}", flush=True)
            print("Inicializando HuskyLens 2 por I²C...", flush=True)
            camera = HuskylensV2_I2C()
            stack.callback(camera.bus.close)
            if not camera.knock():
                raise RuntimeError("A Husky não respondeu. Verifique a alimentação e o I²C.")
            if not camera.switchAlgorithm(ALGORITHM_HAND_RECOGNITION):
                raise RuntimeError("Não foi possível ativar Hand Recognition.")

            print(f"Gravando os 21 pontos em: {output_path.resolve()}", flush=True)
            print(
                f"Intervalo alvo: {args.interval:g} s "
                f"({1 / args.interval:g} amostras por segundo).",
                flush=True,
            )
            print("Use uma mão visível. Ctrl+C para encerrar.", flush=True)
            started = time.monotonic()
            while True:
                before_read = time.monotonic()
                row = read_sample(camera, ALGORITHM_HAND_RECOGNITION)
                received = time.monotonic()
                row.update(
                    timestamp_utc=datetime.now(timezone.utc).isoformat(timespec="milliseconds"),
                    elapsed_s=round(received - started, 6),
                    label=args.label,
                )
                # Os diagnósticos ficam no terminal; o CSV contém só estes campos.
                writer.writerow({field: row.get(field, "") for field in CSV_FIELDS})
                csv_file.flush()
                saved_samples += 1
                print(
                    f"Amostra {saved_samples}: {row['status']} | "
                    f"mãos={row.get('hand_count', '?')}",
                    flush=True,
                )
                if row.get("hand_count", 0) > 1:
                    print("  Registrada a mão mais próxima do centro da imagem.", flush=True)
                if row.get("error"):
                    print(f"  {row['error']}", flush=True)
                # Desconta a leitura, a gravação e a saída no terminal do período.
                # Se esse trabalho exceder o intervalo, começa a próxima leitura
                # imediatamente, sem duplicar pontos para preencher o atraso.
                remaining = args.interval - (time.monotonic() - before_read)
                if remaining > 0:
                    time.sleep(remaining)
    except KeyboardInterrupt:
        print(f"\nColeta encerrada. Amostras gravadas: {saved_samples}.")
    except (OSError, RuntimeError, ValueError) as error:
        print(f"Erro: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
