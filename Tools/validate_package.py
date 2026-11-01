"""Validate asset completeness, translations and build output before packaging."""
import csv
import json
import pathlib
import struct

root = pathlib.Path(__file__).resolve().parents[1]
info = json.loads((root / 'ModInfo.json').read_text())
assert info['Id'] == 'AnimalsWearHats' and info['ModVersion'] == '0.1.1'
assert (root / 'Libraries/AnimalsWearHats.dll').is_file(), 'Build DLL first'
expected = {
    f'TopHat{pose}_{facing}.png'
    for pose in ['', '_1', '_H'] for facing in 'DLUR'
}
folder = root / 'Graphics/Beings/Hats/AnimalsWearHats/Dog'
assert {f.name for f in folder.glob('*.png')} == expected
for name in expected:
    data = (folder / name).read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', name
    assert struct.unpack('>II', data[16:24]) == (256, 256), name
    assert data[25] == 6, f'{name}: RGBA required'
keys = None
for language in ['English', 'Russian']:
    with (root / f'Translations/{language}.csv').open(newline='', encoding='utf-8') as f:
        rows = list(csv.DictReader(f))
    current = {row['key'] for row in rows}
    assert len(current) == len(rows) == 6
    assert all(row['text'] and None not in row for row in rows)
    assert keys is None or keys == current
    keys = current
fit = json.loads((root / 'DogFit.json').read_text())
assert 0.05 <= fit['Scale'] <= 2
for key in ['Down', 'Left', 'Up', 'Right', 'LyingDown']:
    assert set(fit[key]) == {'x', 'y'} and all(abs(v) <= 2 for v in fit[key].values())
assert (root / 'ModCover.jpg').is_file()
print('PASS: 12 RGBA sprites, 6 matching translation keys, fitting config, metadata, DLL and cover.')
