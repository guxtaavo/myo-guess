"""Grava os 21 pontos da mão da HuskyLens 2 em um CSV.

Na Raspberry, na raiz de myo-guess:
    .venv/bin/python src/huskylens_landmark_logger.py              # nome automático
    .venv/bin/python src/huskylens_landmark_logger.py teste.csv    # data/teste.csv

Os caminhos relativos ficam em data/, independentemente da pasta
do terminal. Arquivos existentes não são sobrescritos. Encerre com Ctrl+C.
"""

import argparse
import csv
import sys
import time
from datetime import datetime
from pathlib import Path

# Biblioteca oficial: https://github.com/DFRobot/DFRobot_HuskylensV2
# Fica ao lado de src/, em myo-guess/DFRobot_HuskylensV2.
PROJECT_DIR = Path(__file__).resolve().parent.parent
sys.path.append(str(PROJECT_DIR / "DFRobot_HuskylensV2" / "python" / "smbus2"))
DATA_DIR = PROJECT_DIR / "data"

STARTUP_DELAY = 5    # segundos para a câmera terminar de trocar o algoritmo
INTERVAL = 0.1       # segundos entre leituras

# Os 21 pontos, na ordem da câmera (0 a 20).
POINTS = [
    "wrist",
    "thumb_cmc", "thumb_mcp", "thumb_ip", "thumb_tip",
    "index_finger_mcp", "index_finger_pip", "index_finger_dip", "index_finger_tip",
    "middle_finger_mcp", "middle_finger_pip", "middle_finger_dip", "middle_finger_tip",
    "ring_finger_mcp", "ring_finger_pip", "ring_finger_dip", "ring_finger_tip",
    "pinky_finger_mcp", "pinky_finger_pip", "pinky_finger_dip", "pinky_finger_tip",
]

# Cabeçalho: tempo, ID do gesto e depois wrist_x, wrist_y, thumb_cmc_x, ...
HEADER = ["time_s", "gesture_id"]
for point in POINTS:
    HEADER += [f"{point}_x", f"{point}_y"]

def output_path(name=None):
    path = Path(name) if name else Path(f"huskylens_{datetime.now():%Y%m%d_%H%M%S_%f}.csv")
    return path if path.is_absolute() else DATA_DIR / path


def main():
    parser = argparse.ArgumentParser(description="Grava os pontos da HuskyLens em data/.")
    parser.add_argument("csv", nargs="?", help="nome do CSV; padrão: nome com data e hora")
    args = parser.parse_args()
    csv_path = output_path(args.csv)
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    try:
        file = csv_path.open("x", newline="")
    except FileExistsError:
        parser.error(f"{csv_path} já existe. Escolha outro nome para a coleta.")

    with file:
        from dfrobot_huskylensv2 import HuskylensV2_I2C, ALGORITHM_HAND_RECOGNITION

        huskylens = HuskylensV2_I2C()
        huskylens.knock()
        huskylens.switchAlgorithm(ALGORITHM_HAND_RECOGNITION)

        print(f"Aguardando {STARTUP_DELAY} s para a câmera carregar o algoritmo...")
        time.sleep(STARTUP_DELAY)

        writer = csv.writer(file)
        writer.writerow(HEADER)
        print(f"Gravando em {csv_path.resolve()}. Ctrl+C para parar.")

        start = time.monotonic()
        samples = 0
        try:
            while True:
                huskylens.getResult(ALGORITHM_HAND_RECOGNITION)

                if huskylens.available(ALGORITHM_HAND_RECOGNITION):
                    hand = huskylens.getCachedCenterResult(ALGORITHM_HAND_RECOGNITION)

                    row = [round(time.monotonic() - start, 3), hand.ID]
                    for point in POINTS:
                        row += [getattr(hand, f"{point}_x"), getattr(hand, f"{point}_y")]

                    writer.writerow(row)
                    file.flush()  # garante que a linha já está no arquivo
                    samples += 1
                    print(f"Amostra {samples} | ID {hand.ID} | punho ({hand.wrist_x}, {hand.wrist_y})")

                time.sleep(INTERVAL)
        except KeyboardInterrupt:
            print(f"\nFim. {samples} amostras gravadas em {csv_path}.")


if __name__ == "__main__":
    main()
