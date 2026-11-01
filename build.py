"""Rebuild against your installed game's Managed directory; requires .NET SDK."""
import argparse, pathlib, re, shutil, subprocess, tempfile
p=argparse.ArgumentParser()
p.add_argument('--managed-dir', required=True, type=pathlib.Path)
p.add_argument('--dotnet', default=shutil.which('dotnet') or 'dotnet')
a=p.parse_args()
root=pathlib.Path(__file__).resolve().parent
managed=a.managed_dir.resolve()
for name in ['Game.dll','0Harmony.dll','mscorlib.dll','Unity.TextMeshPro.dll']:
 if not (managed/name).is_file():p.error('Missing '+str(managed/name))
sdks=subprocess.check_output([a.dotnet,'--list-sdks'],text=True)
items=re.findall(r'^([^ ]+) \[(.+)\]$',sdks,re.M)
if not items:p.error('Install .NET SDK first')
version,base=items[-1]
csc=pathlib.Path(base)/version/'Roslyn'/'bincore'/'csc.dll'
output=root/'Libraries'/'AnimalsWearHats.dll'
output.parent.mkdir(exist_ok=True)
args=['-nostdlib+','-target:library','-langversion:9.0','-optimize+',f'-out:"{output}"']
args += [f'-r:"{x}"' for x in sorted(managed.glob('*.dll'))]
args += [f'"{x}"' for x in sorted((root/'Code').glob('*.cs'))]
with tempfile.TemporaryDirectory() as folder:
 rsp=pathlib.Path(folder)/'mod.rsp';rsp.write_text('\n'.join(args),encoding='utf-8')
 subprocess.run([a.dotnet,str(csc),'@'+str(rsp)],check=True)
print('Built:',output)

