"""Render original vector art; requires PyMuPDF. No game graphics are bundled."""
import pathlib
import fitz

root = pathlib.Path(__file__).resolve().parents[1]
def raster(svg, path, size, alpha=True):
    source = fitz.open(stream=svg.encode(), filetype='svg')
    document = fitz.open(stream=source.convert_to_pdf(), filetype='pdf')
    page = document[0]
    page.get_pixmap(matrix=fitz.Matrix(size / page.rect.width, size / page.rect.height),
                    alpha=alpha).save(str(path))
    document.close()
    source.close()

destination = root / 'Graphics/Beings/Hats/AnimalsWearHats/Dog'
destination.mkdir(parents=True, exist_ok=True)
for direction in 'DLUR':
    source = root / 'Artwork' / f'TopHat_{direction}.svg'
    text = source.read_text()
    # The alternate walking frame is explicitly supplied, even though this first hat is rigid.
    for pose in ['', '_1', '_H']:
        rendered = text
        if pose == '_H':
            start = rendered.index('<defs>')
            rendered = rendered[:start] + '<g transform="translate(128 128) rotate(60) scale(.82) translate(-128 -128)">' + rendered[start:]
            rendered = rendered.replace('</svg>', '</g></svg>')
        target = destination / f'TopHat{pose}_{direction}.png'
        raster(rendered, target, 256)
raster((root / 'Artwork/ModCover.svg').read_text(), root / 'ModCover.jpg', 1024, False)
print('Rendered twelve directional/state sprites and mod cover.')
