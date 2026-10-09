# Third-party components

EVA Clipper starts a separate portable OBS Studio process with unchanged upstream binaries and separately bundled plugins. OBS Studio and its dependencies retain their respective licenses. The MIT license in this repository applies to the C# EVA Clipper application; the native OBS plugin under `native/` is GPL-2.0-or-later.

## OBS Studio 32.0.4

- Project: https://obsproject.com/
- Release and upstream assets: https://github.com/obsproject/obs-studio/releases/tag/32.0.4
- License: GNU General Public License version 2 or later; see `OBS-LICENSE.txt` in the application archive and the upstream source tree.
- Runtime files from the Windows ZIP are included unchanged under `engine/` (its `bin`, `data` and `obs-plugins` directories, excluding optional `.pdb` debug symbols). Runtime configuration is generated on first launch and is not distributed.
- Full upstream corresponding source: `OBS-Studio-32.0.4-Sources.tar.gz`, provided as a separate asset alongside the application ZIP. This archive includes upstream build scripts and submodule source.

| Upstream asset | SHA-256 |
| --- | --- |
| OBS-Studio-32.0.4-Windows-x64.zip | fd367a7f156319426f1a600720c1288ad0011da168dff41e0f94dbb18cc1dc0f |
| OBS-Studio-32.0.4-Sources.tar.gz | d9e62ea24305fbfe14c69a6422de1525fe84da232df2cc0d7e4601a840bdbba8 |

License notices supplied with the upstream runtime are preserved. Additional dependency notices and source/build instructions are available in the accompanying source archive. OBS Studio is not affiliated with EVA Clipper.

## Source Record 0.4.8

- Project and binary release: https://github.com/exeldro/obs-source-record/releases/tag/0.4.8
- Source archive: https://github.com/exeldro/obs-source-record/archive/refs/tags/0.4.8.tar.gz
- License: GNU General Public License version 2 or later; see `Source-Record-LICENSE.txt` in the application archive, copied from the tagged source.
- The official 64-bit `source-record.dll` and locale files are included unchanged in the private OBS runtime. 32-bit files and debug symbols are excluded.
- The unmodified tagged source archive, including build files, is provided as `source-record-0.4.8-Sources.tar.gz` beside the application ZIP. Upstream CI/build instructions declare the build dependencies.

| Upstream asset | SHA-256 |
| --- | --- |
| source-record-0.4.8-windows.zip | c2e461afc42746bb8f1861f0aaef63c42f5cc436b55da1a21b3b07bc6d014074 |
| source-record-0.4.8-Sources.tar.gz | ad60c3cb88fec5b726fade90eeb872b527070822affc8d7fdd7cfb831c5a539e |

Source Record and its author are not affiliated with EVA Clipper. EVA communicates through OBS WebSocket; no proprietary application is patched.

## Optional FFmpeg tools

`ffmpeg.exe` and `ffprobe.exe` are optional, user-supplied gallery helpers and are not bundled. Their license depends on the build you choose. Project: https://ffmpeg.org/.

## EVA borderless capture plugin

`native/eva-window-capture.c` and its build script produce `eva-window-capture.dll`, linked against the OBS 32.0.4 plugin API. License: GPL-2.0-or-later, as marked in the source; the OBS GPL text is included in the package. The complete plugin source, import definition and build instructions are in this repository and accompany release source. No upstream DLL is patched. Windows 10 capture uses PrintWindow on background worker threads.

## Bebas Neue

The brand font is embedded from `assets/BebasNeue-Regular.ttf`, obtained from https://github.com/google/fonts/tree/main/ofl/bebasneue. License: SIL Open Font License 1.1, included as `BebasNeue-OFL.txt`. All other interface text uses the Windows-provided Segoe UI.
