"""Create a runtime ZIP without game DLLs, sources or development caches."""
import pathlib
import subprocess
import sys
import zipfile

root = pathlib.Path(__file__).resolve().parents[1]
subprocess.run([sys.executable, str(root / 'Tools/validate_package.py')], check=True)
target = pathlib.Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else root.parent / 'outputs/pets-wear-hats-0.2.1-prototype.zip'
target.parent.mkdir(parents=True, exist_ok=True)
names = ['ModInfo.json', 'DogFit.json', 'CatFit.json', 'PigFit.json', 'ModCover.jpg', 'README.md', 'README_RU.md', 'VALIDATION.md']
files = [root / name for name in names]
files += sorted((root / 'Graphics').rglob('*.png'))
files += sorted((root / 'Translations').glob('*.csv'))
files += [root / 'Libraries/AnimalsWearHats.dll']
with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED) as archive:
    for file in files:
        archive.write(file, 'AnimalsWearHats/' + file.relative_to(root).as_posix())
print(f'{target}: {len(files)} runtime files')
