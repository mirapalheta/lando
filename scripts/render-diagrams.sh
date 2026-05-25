#!/bin/bash
# Renders every PlantUML and draw.io source under docs/assets/src/ to both
# SVG and PNG, writing the output to docs/assets/.
#
# Run from the repo root:
#   ./scripts/render-diagrams.sh
#
# Requires: plantuml + drawio on PATH
#   brew install plantuml
#   brew install --cask drawio

set -e

SRC_DIR="docs/assets/src"
OUT_DIR="docs/assets"

for src in "${SRC_DIR}"/*.puml; do
        base="$(basename "${src}" .puml)"
        plantuml -tsvg -o "${OUT_DIR}" "${src}"
        echo "${SRC_DIR}/${base}.puml -> ${OUT_DIR}/${base}.svg"
        plantuml -tpng -o "${OUT_DIR}" "${src}"
        echo "${SRC_DIR}/${base}.puml -> ${OUT_DIR}/${base}.png"
done

for src in "${SRC_DIR}"/*.drawio; do
        base="$(basename "${src}" .drawio)"
        drawio -x -f svg -o "${OUT_DIR}/${base}.svg" "${src}"
        drawio -x -f png -o "${OUT_DIR}/${base}.png" "${src}"
done
