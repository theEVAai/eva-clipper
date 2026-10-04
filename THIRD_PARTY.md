# Third-party components

EVA Clipper starts a separate, unmodified portable OBS Studio process. OBS Studio and its dependencies retain their respective licenses. The MIT license in this repository applies only to the EVA Clipper source code.

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

## Optional FFmpeg tools

`ffmpeg.exe` and `ffprobe.exe` are optional, user-supplied gallery helpers and are not bundled. Their license depends on the build you choose. Project: https://ffmpeg.org/.
