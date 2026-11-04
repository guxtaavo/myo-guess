"""Visualiza e compara os 21 pontos gravados pelo coletor da HuskyLens 2.

    python Assets/Scripts/raspi5/huskylens_landmark_plotter.py pontos.csv
    python Assets/Scripts/raspi5/huskylens_landmark_plotter.py aberta.csv apontar.csv

Requer matplotlib. Os controles permitem escolher duas amostras, sobrepor as
mãos e alinhar o punho/escala. --save imagem.png exporta sem abrir uma janela.
"""

import argparse
import csv
import math
import sys
from pathlib import Path
from textwrap import shorten


# Mesma ordem do coletor, sem depender da biblioteca da câmera.
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
HAND_CHAINS = (
    (0, 1, 2, 3, 4), (5, 6, 7, 8), (9, 10, 11, 12),
    (13, 14, 15, 16), (17, 18, 19, 20), (0, 5, 9, 13, 17, 0),
)


def read_csv(path):
    """Mantém as linhas sem detecção e representa pontos indisponíveis por None."""
    samples = []
    with path.open(newline="", encoding="utf-8-sig") as handle:
        reader = csv.DictReader(handle)
        headers = reader.fieldnames or []
        missing = [name for name in COORDINATE_FIELDS if name not in headers]
        if missing:
            raise ValueError(f"{path}: faltam colunas de coordenadas: {', '.join(missing)}")
        if len(headers) != len(set(headers)):
            raise ValueError(f"{path}: há nomes de colunas repetidos.")
        for row_number, row in enumerate(reader, start=2):
            if None in row:
                raise ValueError(f"{path}, linha {row_number}: há valores além do cabeçalho.")
            points = []
            for name in POINT_NAMES:
                try:
                    point = (float(row[name + "_x"]), float(row[name + "_y"]))
                    if not all(math.isfinite(value) and value >= 0 for value in point):
                        point = None
                except (TypeError, ValueError, OverflowError):
                    point = None
                points.append(point)
            samples.append({
                "points": points,
                "timestamp_utc": row.get("timestamp_utc") or "",
                "elapsed_s": row.get("elapsed_s") or "",
                "label": row.get("label") or "Sem rótulo",
            })
    if not samples:
        raise ValueError(f"{path}: o arquivo não tem amostras.")
    return samples


def display_points(sample, normalize):
    points = sample["points"]
    if not normalize:
        return points, ""
    wrist, middle_base = points[0], points[9]
    if wrist is None or middle_base is None:
        return [None] * 21, "Punho ou base do dedo médio ausente para alinhar a mão."
    scale = math.dist(wrist, middle_base)
    if scale < 1e-6:
        return [None] * 21, "Punho e base do dedo médio coincidem; escala indisponível."
    return [
        None if point is None else (
            (point[0] - wrist[0]) / scale,
            (point[1] - wrist[1]) / scale,
        )
        for point in points
    ], ""


def plot_bounds(datasets, normalize):
    """Limites fixos e iguais para comparar sem distorcer as proporções."""
    xmin = ymin = math.inf
    xmax = ymax = -math.inf
    for samples in datasets:
        for sample in samples:
            points, _ = display_points(sample, normalize)
            for point in points:
                if point is not None:
                    xmin, xmax = min(xmin, point[0]), max(xmax, point[0])
                    ymin, ymax = min(ymin, point[1]), max(ymax, point[1])
    if not math.isfinite(xmin):
        return (-1, 1, -1, 1)
    span = max(xmax - xmin, ymax - ymin, 1.0) * 1.2
    xcenter, ycenter = (xmin + xmax) / 2, (ymin + ymax) / 2
    return (xcenter - span/2, xcenter + span/2, ycenter - span/2, ycenter + span/2)


class LandmarkViewer:
    def __init__(self, plt, datasets, paths, indices, normalize=False,
                 overlay=False, show_indices=True, interactive=True):
        self.datasets, self.paths, self.indices = datasets, paths, list(indices)
        self.normalize, self.overlay = normalize, overlay
        self.show_indices = show_indices
        self.bounds = {mode: plot_bounds(datasets, mode) for mode in (False, True)}
        self.figure, axes = plt.subplots(1, 2, figsize=(12, 8 if interactive else 7))
        self.axes = list(axes)
        self.bottom = 0.34 if interactive else 0.14
        self.figure.subplots_adjust(left=0.08, right=0.96, bottom=self.bottom,
                                    top=0.77, wspace=0.25)
        self.positions = [axis.get_position().frozen() for axis in self.axes]
        self.figure.suptitle("Comparação de gestos — HuskyLens 2", y=0.97, fontsize=16)
        self.summaries = [self.figure.text(0.08, y, "", fontsize=10) for y in (0.90, 0.86)]
        self.figure.text(
            0.08, 0.035,
            "Índices: 0 punho · 1–4 polegar · 5–8 indicador · 9–12 médio · "
            "13–16 anelar · 17–20 mínimo",
            fontsize=9,
        )
        self.sliders = []
        self.checks = None
        if interactive:
            self.add_controls()
        self.redraw()

    def add_controls(self):
        from matplotlib.widgets import CheckButtons, Slider

        for side, y in enumerate((0.235, 0.155)):
            count = len(self.datasets[side])
            control_axis = self.figure.add_axes((0.16, y, 0.48, 0.03))
            slider = Slider(
                control_axis, f"Amostra {'AB'[side]}", 1, max(2, count),
                valinit=self.indices[side] + 1, valstep=1, valfmt="%d",
            )
            if count == 1:
                slider.set_active(False)
                control_axis.set_alpha(0.45)
            slider.on_changed(lambda value, side=side: self.select_sample(side, value))
            self.sliders.append(slider)
        options_axis = self.figure.add_axes((0.75, 0.12, 0.22, 0.17))
        self.checks = CheckButtons(
            options_axis, ("Alinhar punho / escala", "Sobrepor", "Índices dos pontos"),
            (self.normalize, self.overlay, self.show_indices),
        )
        self.checks.on_clicked(self.change_options)

    def select_sample(self, side, value):
        self.indices[side] = max(0, min(int(value) - 1, len(self.datasets[side]) - 1))
        self.redraw()

    def change_options(self, _label):
        self.normalize, self.overlay, self.show_indices = self.checks.get_status()
        self.redraw()

    def configure_axis(self, axis):
        xmin, xmax, ymin, ymax = self.bounds[self.normalize]
        axis.set_xlim(xmin, xmax)
        axis.set_ylim(ymax, ymin)  # Y cresce para baixo, como na imagem da câmera.
        axis.set_aspect("equal", adjustable="box")
        unit = "unidades da palma" if self.normalize else "px"
        axis.set_xlabel(f"X ({unit})")
        axis.set_ylabel(f"Y ({unit})")
        axis.grid(True, alpha=0.2)

    def draw_hand(self, axis, points, side):
        color = ("tab:blue", "tab:orange")[side]
        style = "-" if side == 0 else "--"
        for chain in HAND_CHAINS:
            for start, end in zip(chain, chain[1:]):
                if points[start] is not None and points[end] is not None:
                    axis.plot(
                        (points[start][0], points[end][0]),
                        (points[start][1], points[end][1]),
                        color=color, linestyle=style, linewidth=1.8,
                    )
        valid = [(index, point) for index, point in enumerate(points) if point is not None]
        if valid:
            axis.scatter(
                [point[0] for _, point in valid], [point[1] for _, point in valid],
                color=color, marker="o" if side == 0 else "^", s=30,
                label=f"Gesto {'AB'[side]}", zorder=3,
            )
        if self.show_indices:
            for index, point in valid:
                label = f"{'AB'[side]}:{index}" if self.overlay else str(index)
                axis.annotate(label, point, xytext=(4, 5 if side == 0 else -10),
                              textcoords="offset points", fontsize=8)

    def redraw(self):
        for side, axis in enumerate(self.axes):
            axis.clear()
            axis.set_visible(not self.overlay or side == 0)
            axis.set_position(self.positions[side])
        if self.overlay:
            self.axes[0].set_position((0.08, self.bottom, 0.88, 0.77 - self.bottom))
        notes = []
        for side, samples in enumerate(self.datasets):
            index = self.indices[side]
            sample = samples[index]
            points, note = display_points(sample, self.normalize)
            count = sum(point is not None for point in points)
            filename = shorten(self.paths[side].name, width=32, placeholder="…")
            label = shorten(sample["label"], width=24, placeholder="…")
            summary = (f"{'AB'[side]}: {filename} | amostra {index + 1}/{len(samples)} | "
                       f"{label} | t={sample['elapsed_s'] or '?'} s")
            if sample["timestamp_utc"]:
                summary += f" | {sample['timestamp_utc']}"
            self.summaries[side].set_text(summary)
            axis = self.axes[0] if self.overlay else self.axes[side]
            self.configure_axis(axis)
            self.draw_hand(axis, points, side)
            if not self.overlay:
                axis.set_title(f"Gesto {'AB'[side]} · {count}/21 pontos")
                if note or count == 0:
                    axis.text(0.5, 0.5, note or "Sem pontos nesta amostra",
                              transform=axis.transAxes, ha="center", va="center", wrap=True)
            elif note or count == 0:
                notes.append(f"{'AB'[side]}: {note or 'Sem pontos nesta amostra'}")
        if self.overlay:
            self.axes[0].set_title("Gestos A e B sobrepostos")
            handles, labels = self.axes[0].get_legend_handles_labels()
            if handles:
                self.axes[0].legend(handles, labels)
            if notes:
                self.axes[0].text(0.5, 0.03, "\n".join(notes), transform=self.axes[0].transAxes,
                                  ha="center", va="bottom", fontsize=9, wrap=True)
        self.figure.canvas.draw_idle()


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("csv_a", type=Path, help="CSV da primeira coleta.")
    parser.add_argument("csv_b", type=Path, nargs="?", help="Segunda coleta; padrão: mesmo CSV.")
    parser.add_argument("--sample-a", type=int, default=1, help="Amostra A, contando de 1.")
    parser.add_argument("--sample-b", type=int, help="Amostra B; padrão: primeira de B ou última de A.")
    parser.add_argument("--normalize", action="store_true", help="Alinha punho e escala da palma.")
    parser.add_argument("--overlay", action="store_true", help="Sobrepõe as duas mãos.")
    parser.add_argument("--no-indices", action="store_true", help="Oculta índices de 0 a 20.")
    parser.add_argument("--save", type=Path, help="Salva PNG, SVG ou PDF sem abrir a janela.")
    args = parser.parse_args()
    try:
        data_a = read_csv(args.csv_a)
        data_b = read_csv(args.csv_b) if args.csv_b else data_a
        sample_b = args.sample_b if args.sample_b is not None else (1 if args.csv_b else len(data_b))
        for name, index, samples in (("A", args.sample_a, data_a), ("B", sample_b, data_b)):
            if not 1 <= index <= len(samples):
                raise ValueError(f"Amostra {name} deve estar entre 1 e {len(samples)}.")
        if args.save and args.save.exists():
            raise ValueError(f"O arquivo {args.save} já existe; escolha outro nome.")
        import matplotlib
        if args.save:
            matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        if not args.save and plt.get_backend().lower() in (
            "agg", "cairo", "pdf", "pgf", "ps", "svg", "template",
        ):
            raise ValueError(
                "O Matplotlib deste ambiente não consegue abrir janelas. "
                "Use --save comparacao.png ou execute com suporte gráfico Tk/Qt."
            )
        viewer = LandmarkViewer(
            plt, (data_a, data_b), (args.csv_a, args.csv_b or args.csv_a),
            (args.sample_a - 1, sample_b - 1), normalize=args.normalize,
            overlay=args.overlay, show_indices=not args.no_indices, interactive=not args.save,
        )
        if args.save:
            viewer.figure.savefig(args.save, dpi=160)
            plt.close(viewer.figure)
            print(f"Comparação salva em: {args.save.resolve()}")
        else:
            plt.show()
    except ImportError:
        print("Instale a dependência com: python -m pip install matplotlib", file=sys.stderr)
        return 1
    except (OSError, ValueError, csv.Error) as error:
        print(f"Erro: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
