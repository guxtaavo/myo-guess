"""Mostra os 21 pontos gravados por huskylens_landmark_logger.py.

No notebook, dentro de Assets/Scripts/raspi5/ (precisa de matplotlib):
    python3 src/huskylens_landmark_plotter.py data/mao_aberta_001.csv
    python3 src/huskylens_landmark_plotter.py data/mao_aberta_001.csv --save mao.png --amostra 10

Painéis:
    esquerda  pontos crus, em pixels da câmera
    direita   pontos normalizados: punho na origem e tamanho da palma = 1,
              então a posição e a distância da mão à câmera deixam de importar
    embaixo   ID do gesto ao longo do tempo

Teclas: ← → trocam de amostra, espaço reproduz/pausa, r liga/desliga a rotação
(gira a mão para o dedo médio apontar para cima).
"""

import argparse
import csv
import math
import sys
from pathlib import Path

# Mesma ordem do coletor (0 a 20).
POINTS = [
    "wrist",
    "thumb_cmc", "thumb_mcp", "thumb_ip", "thumb_tip",
    "index_finger_mcp", "index_finger_pip", "index_finger_dip", "index_finger_tip",
    "middle_finger_mcp", "middle_finger_pip", "middle_finger_dip", "middle_finger_tip",
    "ring_finger_mcp", "ring_finger_pip", "ring_finger_dip", "ring_finger_tip",
    "pinky_finger_mcp", "pinky_finger_pip", "pinky_finger_dip", "pinky_finger_tip",
]
WRIST, MIDDLE_MCP = 0, 9

# Linhas do esqueleto, uma cor por dedo; a última fecha a palma.
FINGERS = [
    ((0, 1, 2, 3, 4), "tab:red"),
    ((0, 5, 6, 7, 8), "tab:orange"),
    ((9, 10, 11, 12), "tab:green"),
    ((13, 14, 15, 16), "tab:blue"),
    ((0, 17, 18, 19, 20), "tab:purple"),
    ((5, 9, 13, 17), "gray"),
]


def read_csv(path):
    times, ids, hands = [], [], []
    with open(path, newline="") as file:
        for row in csv.DictReader(file):
            times.append(float(row["time_s"]))
            ids.append(int(row["gesture_id"]))
            hands.append([(float(row[f"{p}_x"]), float(row[f"{p}_y"])) for p in POINTS])
    if not hands:
        sys.exit(f"{path} não tem amostras.")
    return times, ids, hands


def normalize(hand, rotate):
    """Punho em (0, 0), distância punho→base do médio = 1, opcionalmente girada."""
    wx, wy = hand[WRIST]
    mx, my = hand[MIDDLE_MCP]
    size = math.hypot(mx - wx, my - wy) or 1.0

    # Ângulo que leva o vetor punho→médio para "cima" (y negativo, como na imagem).
    angle = -math.pi / 2 - math.atan2(my - wy, mx - wx) if rotate else 0.0
    cos, sin = math.cos(angle), math.sin(angle)

    result = []
    for x, y in hand:
        x, y = (x - wx) / size, (y - wy) / size
        result.append((x * cos - y * sin, x * sin + y * cos))
    return result


def limits(hands, margin=0.1):
    xs = [x for hand in hands for x, _ in hand]
    ys = [y for hand in hands for _, y in hand]
    span = max(max(xs) - min(xs), max(ys) - min(ys)) * (1 + 2 * margin)
    cx, cy = (max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2
    return (cx - span / 2, cx + span / 2), (cy + span / 2, cy - span / 2)  # y para baixo


def draw_hand(ax, hand):
    for chain, color in FINGERS:
        ax.plot([hand[i][0] for i in chain], [hand[i][1] for i in chain],
                "-o", color=color, markersize=4, linewidth=2)


def main():
    parser = argparse.ArgumentParser(description="Visualiza o CSV da HuskyLens 2.")
    parser.add_argument("csv", type=Path)
    parser.add_argument("--amostra", type=int, default=1, help="amostra inicial, contando de 1")
    parser.add_argument("--rotacao", action="store_true", help="começa com a rotação ligada")
    parser.add_argument("--save", type=Path, help="salva a imagem em vez de abrir a janela")
    args = parser.parse_args()

    times, ids, hands = read_csv(args.csv)

    import matplotlib
    if args.save:
        matplotlib.use("Agg")  # funciona sem tela, por exemplo via SSH
    import matplotlib.pyplot as plt
    from matplotlib.widgets import Slider

    state = {"i": min(max(args.amostra, 1), len(hands)) - 1,
             "rotate": args.rotacao, "playing": False}

    fig = plt.figure(figsize=(12, 8))
    grid = fig.add_gridspec(2, 2, height_ratios=(3, 1), bottom=0.15, top=0.92, hspace=0.3)
    ax_raw, ax_norm = fig.add_subplot(grid[0, 0]), fig.add_subplot(grid[0, 1])
    ax_ids = fig.add_subplot(grid[1, :])

    raw_lim = limits(hands)

    ax_ids.step(times, ids, where="post", color="black", linewidth=1)
    ax_ids.set_xlabel("tempo (s)")
    ax_ids.set_ylabel("gesture_id")
    ax_ids.yaxis.get_major_locator().set_params(integer=True)
    ax_ids.grid(alpha=0.3)
    cursor = ax_ids.axvline(times[0], color="tab:red")

    slider_ax = fig.add_axes((0.15, 0.04, 0.7, 0.03))
    slider = Slider(slider_ax, "Amostra", 1, len(hands), valinit=state["i"] + 1,
                    valstep=1, valfmt="%d")

    def redraw():
        i = state["i"]
        normalized_all = [normalize(h, state["rotate"]) for h in hands]

        ax_raw.clear()
        draw_hand(ax_raw, hands[i])
        ax_raw.set_xlim(raw_lim[0]); ax_raw.set_ylim(raw_lim[1])
        ax_raw.set_aspect("equal")
        ax_raw.set_title("Crua (pixels)")
        ax_raw.grid(alpha=0.3)

        ax_norm.clear()
        draw_hand(ax_norm, normalized_all[i])
        norm_lim = limits(normalized_all)
        ax_norm.set_xlim(norm_lim[0]); ax_norm.set_ylim(norm_lim[1])
        ax_norm.set_aspect("equal")
        ax_norm.set_title("Normalizada" + (" + rotação" if state["rotate"] else "")
                          + " (palma = 1)")
        ax_norm.grid(alpha=0.3)

        cursor.set_xdata([times[i], times[i]])
        fig.suptitle(f"{args.csv.name} — amostra {i + 1}/{len(hands)} · "
                     f"t = {times[i]:.2f} s · ID {ids[i]}")
        fig.canvas.draw_idle()

    def go_to(i):
        state["i"] = i % len(hands)
        slider.set_val(state["i"] + 1)  # chama redraw pelo on_changed

    slider.on_changed(lambda value: (state.update(i=int(value) - 1), redraw()))

    timer = fig.canvas.new_timer(interval=100)
    timer.add_callback(lambda: go_to(state["i"] + 1))

    def on_key(event):
        if event.key == "right":
            go_to(state["i"] + 1)
        elif event.key == "left":
            go_to(state["i"] - 1)
        elif event.key == "r":
            state["rotate"] = not state["rotate"]
            redraw()
        elif event.key == " ":
            state["playing"] = not state["playing"]
            timer.start() if state["playing"] else timer.stop()

    fig.canvas.mpl_connect("key_press_event", on_key)
    redraw()

    if args.save:
        fig.savefig(args.save, dpi=150)
        print(f"Imagem salva em {args.save.resolve()}")
    else:
        plt.show()


if __name__ == "__main__":
    main()
