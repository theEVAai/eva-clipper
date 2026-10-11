# EVA Clipper

<!-- impeccable:product-schema 1 -->

## Platform and purpose

Native Windows desktop utility for local gameplay recording and instant replay. A compact panel at the right edge keeps recording, replay, microphone and recent moments within reach of global shortcuts.

## Capabilities

- Ordinary recording and adjustable instant replay, independently or simultaneously.
- A moment containing separate videos of the monitor, game, Discord, browser and an optional selected window.
- Automatic window discovery with explicit manual selection available. Overlapping windows can be captured; minimized windows are reported as unavailable. The browser captures the displayed tab; Discord must already display the desired stream.
- Six audio buses in multi-source mode: system, microphone, game, Discord, browser and extra application. Single-video mode also supports one mixed or two separate tracks.
- Main video up to 4K, 30/60/120 FPS, H.264 or HEVC; separate lower-cost quality for additional windows.
- Local gallery, configurable destination, optional storage/age retention and protection of complete moments.

## Interface direction

Use a 440px native right-side panel in the EVA colors verified at theeva.ai: cream, ink and cobalt blue, with EVA's heart and Bebas Neue brand lettering. Segoe UI remains the readable Russian interface face. Recent moments, replay saving, ordinary recording and source availability lead the home screen. The five settings tabs and Apply action remain fixed while the contents scroll. The wheel only scrolls; sliders and selectors change through explicit mouse or keyboard interaction. No game filters, photo mode, highlights or unrelated statistics screen.

## Behavior and limits

Backend confirmation determines recording state. The panel remains visible in desktop captures. Alt+X reveals or hides it at a fixed window position; repeated toggles reverse the reveal while preserving its current position and velocity. One opaque panel image is displayed during motion, then live controls return when opening settles. Motion typically settles in about 200ms, depending on its current state and frame delivery, without an exact duration or FPS guarantee. Disabling motion switches directly between visible and hidden states.

One portable OBS process and the bundled Source Record plugin encode all sources. Each active video needs its own encoded buffer, so replay duration and bitrate still determine memory use. Unavailable sources do not start an encoder until a usable window appears. Gallery work has bounded concurrency and pagination.

Borderless windows are enabled by default. On Windows 10 builds below 20348 the bundled EVA capture plugin uses PrintWindow for Discord/browser/manual windows, with one worker and a reused bitmap per source. The automatic game source uses OBS Game Capture. Newer Windows uses OBS WGC's borderless support. The option can be disabled for windows that require WGC; yellow outlines can then return. PrintWindow compatibility and actual frame delivery depend on the target application. Minimized windows are unavailable, and a window requiring a protected capture path is not circumvented.

Videos share a save command; encoder startup and keyframes can differ. A moment manifest records source names and approximate editing offsets, without promising frame-accurate synchronization. Automatic deletion is off by default, covers only the local library, and protects the newest and pinned moments. These protections may keep storage above the requested limit.

## Distribution

Publish only source, product documentation and clean runtime packages. User settings, library indexes, window titles, device identifiers, recordings, logs, screenshots and runtime credentials are local data and must not enter the public repository or release.
