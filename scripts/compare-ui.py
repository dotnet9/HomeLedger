"""Create same-viewport comparison evidence; pixel error is not a fidelity score."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageStat

parser = argparse.ArgumentParser()
parser.add_argument("--actual", type=Path, required=True)
parser.add_argument("--prototype", type=Path, required=True)
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
pages = {
    "member-ledger": "member-0-zh-CN.png",
    "member-deposits": "member-1-zh-CN.png",
    "member-report": "member-2-zh-CN.png",
    "admin-overview": "admin-0-zh-CN.png",
    "admin-details": "admin-1-zh-CN.png",
    "admin-members": "admin-2-zh-CN.png",
    "admin-backup": "admin-3-zh-CN.png",
}
measurements = []
for page, actual_name in pages.items():
    actual = Image.open(args.actual / actual_name).convert("RGB")
    prototype = Image.open(args.prototype / ("standalone-" + page + ".png")).convert("RGB")
    if actual.size != (1280, 840) or prototype.size != actual.size:
        raise ValueError(f"Viewport mismatch: {page}: {actual.size}, {prototype.size}")
    comparison = Image.new("RGB", (2560, 872), "#eeeeee")
    draw = ImageDraw.Draw(comparison)
    draw.text((12, 9), "PROTOTYPE - 1280 x 840", fill="black")
    draw.text((1292, 9), "ACTUAL AVALONIA - 1280 x 840", fill="black")
    comparison.paste(prototype, (0, 32))
    comparison.paste(actual, (1280, 32))
    comparison.save(args.output / (page + ".png"))
    difference = ImageChops.difference(prototype, actual)
    difference.save(args.output / (page + "-difference.png"))
    measurements.append({"page": page, "viewport": [1280, 840],
                         "mean_absolute_channel_error_0_to_255": round(sum(ImageStat.Stat(difference).mean) / 3, 3),
                         "limitation": "Different demo data, browser/native controls and window chrome; not a fidelity percentage."})
(args.output / "measurements.json").write_text(json.dumps(measurements, indent=2), encoding="utf-8")
print(f"Created {len(measurements)} same-viewport pairs and absolute pixel difference images.")
