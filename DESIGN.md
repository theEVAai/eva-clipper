---
name: "EVA Clipper 3.1"
description: "A compact native Windows recording panel in EVA cream, ink and cobalt."
colors:
  action: "#1a3dcc"
  action-hover: "#2450e8"
  action-pressed: "#0f2490"
  action-text: "#ffffff"
  canvas: "#0d0d0d"
  surface: "#1f1f1e"
  surface-hover: "#2d2d2b"
  surface-pressed: "#3b3b37"
  cream: "#d8cdb8"
  muted: "#a7a49a"
  masthead-hover: "#c9bda6"
  toggle-off: "#464540"
  disabled-surface: "#272727"
  disabled-text: "#919191"
  recording: "#d14848"
  recording-hover: "#b83333"
  warning: "#ffaf93"
  heart-fill: "#ede8df"
  heart-hatch: "#828282"
typography:
  brand:
    fontFamily: "Bebas Neue"
    fontSize: "29.3333px"
    fontWeight: 400
  title:
    fontFamily: "Segoe UI"
    fontSize: "17.3333px"
    fontWeight: 700
  section:
    fontFamily: "Segoe UI"
    fontSize: "16px"
    fontWeight: 700
  subsection:
    fontFamily: "Segoe UI"
    fontSize: "14.6667px"
    fontWeight: 700
  label:
    fontFamily: "Segoe UI"
    fontSize: "13.3333px"
    fontWeight: 700
  body:
    fontFamily: "Segoe UI"
    fontSize: "13.3333px"
    fontWeight: 400
  caption:
    fontFamily: "Segoe UI"
    fontSize: "12px"
    fontWeight: 400
  micro:
    fontFamily: "Segoe UI"
    fontSize: "11.3333px"
    fontWeight: 400
rounded:
  control: "0px"
  switch: "11px"
  slider-thumb: "6px"
spacing:
  page-inset: "20px"
  tab-inset: "12px"
  tab-gap: "2px"
  navigation-inset: "12px"
  helper-after: "18px"
  thumbnail-gap: "12px"
components:
  panel:
    backgroundColor: "{colors.canvas}"
    width: "440px"
  masthead:
    backgroundColor: "{colors.cream}"
    textColor: "{colors.canvas}"
    height: "64px"
  home-footer:
    backgroundColor: "{colors.canvas}"
    height: "80px"
  settings-footer:
    backgroundColor: "{colors.canvas}"
    height: "132px"
  button-primary:
    backgroundColor: "{colors.action}"
    textColor: "{colors.action-text}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    width: "384px"
    height: "42px"
  button-save:
    backgroundColor: "{colors.action}"
    textColor: "{colors.action-text}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    width: "384px"
    height: "48px"
  button-primary-hover:
    backgroundColor: "{colors.action-hover}"
    textColor: "{colors.action-text}"
  button-primary-active:
    backgroundColor: "{colors.action-pressed}"
    textColor: "{colors.action-text}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.cream}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    width: "384px"
    height: "38px"
  button-secondary-hover:
    backgroundColor: "{colors.surface-hover}"
    textColor: "{colors.cream}"
  button-secondary-active:
    backgroundColor: "{colors.surface-pressed}"
    textColor: "{colors.cream}"
  button-disabled:
    backgroundColor: "{colors.disabled-surface}"
    textColor: "{colors.disabled-text}"
  button-recording:
    backgroundColor: "{colors.recording}"
    textColor: "{colors.action-text}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    width: "384px"
    height: "46px"
  button-recording-hover:
    backgroundColor: "{colors.recording-hover}"
    textColor: "{colors.action-text}"
  navigation-row:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.cream}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "0px 0px 0px 12px"
    width: "186px"
    height: "40px"
  settings-tab:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.cream}"
    typography: "{typography.caption}"
    rounded: "{rounded.control}"
    width: "72px"
    height: "32px"
  settings-tab-selected:
    backgroundColor: "{colors.action}"
    textColor: "{colors.action-text}"
  text-field:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.cream}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    width: "288px"
  selector:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.cream}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    width: "384px"
  slider:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.cream}"
    width: "384px"
    height: "28px"
  slider-value:
    textColor: "{colors.cream}"
    typography: "{typography.body}"
    width: "125px"
  toggle:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.cream}"
    typography: "{typography.body}"
    width: "384px"
    height: "42px"
  eva-heart:
    backgroundColor: "{colors.cream}"
    width: "30px"
    height: "30px"
  gallery-thumbnail:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.control}"
    width: "120px"
    height: "68px"
---

# Design System: EVA Clipper 3.1

## Overview

**Creative North Star: "EVA recording console"**

A narrow native utility carries EVA's warm cream identity above an ink workspace. Cobalt gives actions and selection a clear visual weight; compact media and source rows keep recording work legible without decorative card stacks.

The brand is concentrated in the heart and condensed EVA CLIPPER wordmark. Russian controls use Segoe UI, explicit labels and familiar Windows keyboard behavior. Flat rectangular actions coexist with round slider thumbs and pill switches because those shapes communicate different operations.

This is the implemented 3.1 system extracted from `src/Panel.cs`, `src/Controls.cs` and `src/Overlay.cs`, with the embedded font confirmed in `scripts/build.ps1`. The palette and display family match the public [EVA site](https://theeva.ai), read on 2026-10-09. Sampled native captures are `.impeccable/review-v31/home.png`, `video-720.png` and `video-bottom-720.png`; the available review set also contains all five settings tabs at 440 × 1000. The harness renders production controls with synthetic engine and device state. It demonstrates interface appearance, not capture compatibility, successful recording or exact synchronization.

The 3.1.1 repaint and motion behavior is implemented in `src/Controls.cs`, `src/Panel.cs` and `src/PanelMotion.cs`. The current production-control fixture matrix is `.impeccable/review-v311/{home.png,Источники.png,Видео.png,Аудио.png,Файлы.png,Клавиши.png,video-720.png,video-bottom-720.png}`, at 440px wide and 1000px or 720px high with synthetic engine and device state. `tests/PanelChecks.cs` covers translated and clipped toggle paint, removal of old labels, continuous reversals, duplicate targets and irregular frame timing. Fixture appearance and callback timings do not establish product FPS or direct live hover behavior; Computer Use hover capture remains pending.

**Key Characteristics:**

- Cream masthead, ink canvas, warm text and cobalt actions.
- A fixed-width right-edge panel with persistent settings navigation and Apply footer.
- Embedded Bebas Neue branding with a Segoe UI interface.
- Owner-drawn selectors, slim sliders and switches with visible keyboard focus.
- A reversible reveal at a fixed window position and settings that scroll without wheel edits.

## Colors

Warm neutrals carry the interface; a single cobalt family identifies actions and selection. The frontmatter is the normative palette.

### Primary

- **EVA Cobalt** (`action`): save, Apply, selected tabs, checked switches and the enabled microphone button.
- **Bright Cobalt** (`action-hover`): primary hover and the filled section of the slider rail.
- **Pressed Cobalt** (`action-pressed`): primary mouse-down state.
- **Action White** (`action-text`): content on cobalt and recording controls, selected dropdown rows and checked switch thumbs.

### Neutral

- **EVA Ink** (`canvas`): body, tabs and footer; also masthead lettering and icons.
- **Control Charcoal** (`surface`), **Hover Charcoal** (`surface-hover`) and **Pressed Charcoal** (`surface-pressed`): resting, hover and mouse-down fills. Hover Charcoal also forms the unfilled slider rail.
- **EVA Cream** (`cream`): masthead, ordinary content, slider values and thumbs.
- **Warm Gray** (`muted`): help, metadata, inactive source detail and footer status.
- **Masthead Shade** (`masthead-hover`): header button hover.
- **Switch Charcoal** (`toggle-off`): unchecked switch track.
- **Disabled Charcoal** (`disabled-surface`) and **Disabled Gray** (`disabled-text`): unavailable button fill and content.

### Status and identity details

**Recording Red** (`recording`) and its hover shade (`recording-hover`) identify the confirmed stop-recording action. **Warning Peach** (`warning`) is used for errors, shortcut conflicts and source warnings. These are status assignments, not additional brand accents.

The heart has a **Pale Cream** fill (`heart-fill`), **Gray Hatching** (`heart-hatch`) and an EVA Cobalt outline. Preserve its exact geometry and striped treatment; the masthead and heart interior deliberately use different creams.

**The Brand and State Rule.** Cream and the heart identify EVA; cobalt marks primary actions, selection and enabled settings. Recording red identifies the stop action after engine confirmation; warning text identifies current errors or unavailable sources.

## Typography

**Display Font:** embedded Bebas Neue Regular, loaded from the `EvaBrand.ttf` assembly resource; its source is `assets/BebasNeue-Regular.ttf`.

**Body Font:** Segoe UI, regular and bold.

The frontmatter expresses font sizes as CSS pixels at 96 DPI for portable token tooling. Native sizes use points: brand (22pt), page title (13pt), section (12pt), subsection (11pt), label and body (10pt), caption (9pt), and micro text (8.5pt). These are unit conversions, not a separate web type scale. The application defines no tracking or line-height scale.

The condensed wordmark is the only display treatment. Bold page and section headings organize tasks; bold labels pair with regular values. Captions support controls without competing with them. Resource metadata uses the smallest role. Interface copy stays in ordinary sentence case, with units on values and keyboard shortcuts beside actions.

**The Two Faces Rule.** Use Bebas Neue for the EVA CLIPPER wordmark only. Use Segoe UI for Russian headings, controls, values and help text.

## Layout

The borderless, topmost form occupies the primary monitor's working-area right edge. Its fixed width is defined by `panel`; the form follows the working-area height and declares a minimum height of 580px. Most content starts at `page-inset` and spans 384px. The remaining space accommodates the right gutter and native vertical scrollbar.

The masthead is fixed above the body. Home and gallery use `home-footer`; settings use `settings-footer` so Apply, status and resource metadata remain available. The settings strip is 56px high with a 12px top inset. Its tabs stay in this order: Источники, Видео, Аудио, Файлы, Клавиши. Their widths are 96, 72, 72, 72 and 84px respectively, separated by `tab-gap`.

Labels sit above selectors. Slider rows put the label at the left and a 125px value column at the right. Helpers are measured for wrapped Russian text and followed by `helper-after`. Toggle rows advance by 50px. Conditional settings rebuilds preserve scroll and named-control focus where the implementation requests it.

The home media strip fits three 120 × 68px thumbnails with `thumbnail-gap`. Gallery rows use 120 × 76px previews, metadata, an open action and a protection switch; one page holds up to eight moments. Source names and right-aligned detail form compact rows, not cards.

Native captures at a 720px work-area height show the video settings scrolling while navigation and Apply remain fixed. The implementation uses `AutoScaleMode.None`; there are no authored responsive breakpoints or claims of verified arbitrary-DPI adaptation.

**The Fixed Chrome Rule.** Keep the masthead, five settings tabs and Apply action outside the scrolling body. Short work areas change the visible content height, not the panel width or navigation order.

The reveal keeps the HWND at its final on-screen position. `SlidePreview` prepares one opaque layered image at the start of a transition and exposes a region from the right edge while the live form is hidden. Reversals reuse that image instead of repainting child controls. Once motion settles, the preview is disposed and live controls return if the target is visible.

`PanelMotion` advances an analytic critically damped spring (`Omega = 48`) using elapsed time. Repeated toggles change only the target, preserving continuous position and velocity. Convergence is typically about 200ms, but depends on the current state and delivered frames; there is no universal exact duration or FPS guarantee. `CompositionClock` waits on `DwmFlush` off the UI thread and posts at most one queued frame callback. Turning off “Плавное открытие панели” switches directly between visible and hidden states.

## Elevation & Depth

There are no authored shadows. Depth comes from the cream masthead, ink canvas and slightly lighter control fills. Selector popups, text-field borders, scrollbar rendering and focus cues retain Windows behavior; they do not establish separate EVA shadow or material tokens.

**The Flat Surface Rule.** Separate controls through fill, spacing and typography. The native system has no authored shadow, glass blur or decorative card stack.

## Shapes

Buttons, tabs, selector faces and media frames are square-cornered. Switch tracks are pills, and slider thumbs are circles; this functional vocabulary must not become a universal square-corner rule.

The slider uses a rounded 4px rail and a 12px thumb inside a 28px-high control. A switch track is 42 × 22px with a 16px thumb and a 3px inset. The heart occupies a 30px square in the home masthead and scales its original 32-unit path. Diagonal hatching is clipped inside the heart before the cobalt outline is painted.

Header and navigation icons are drawn geometry. The action-icon stroke is 1.8px with round line ends; selector chevrons use 1.5px. Preserve the native icon vocabulary and accessible names for icon-only actions.

## Components

### Buttons

Flat rectangular actions carry clear labels and visible native focus cues. Apply uses `button-primary`; Save uses the taller, bold `button-save`. Secondary actions use charcoal with cream text. Hover and pressed fills are defined in the frontmatter. Disabled buttons remain visibly subdued. Their native painter fills only the control rectangle with `FillRectangle`, rather than clearing the drawing surface.

The record button becomes red only when the engine reports recording; its label includes elapsed time. It has a dedicated red hover fill and retains the shared secondary pressed fill. Replay and ordinary recording remain separate controls. Save is disabled unless the engine is ready, replay is running and the application is not busy.

The home microphone control is a labelled button: cobalt and white when enabled, charcoal and muted text when disabled. It is distinct from the pill switches inside settings.

### Inputs / Fields

The folder path is a borderless native text field beside a browse button. Shortcut capture uses a read-only native field with a single border and white text. Both retain their native editing or key-capture behavior.

**EvaSelect** is a non-editable native ComboBox with an owner-drawn charcoal face, cream text and drawn chevron. The declaration allocates a 32px slot; the native font determines the actual face height. Popup rows use a 23px item height, cobalt selection and white selected text. A 620px dropdown accommodates long window or device names. Unavailable saved values stay represented with explicit wording instead of silently selecting another source.

**EvaSlider** draws its rail and thumb. Click or drag sets a value; arrows move one step and Home/End select the limits. It exposes the native Slider accessibility role and current value. Focus is drawn inside the control. Numeric labels include the relevant unit.

**EvaToggle** retains CheckBox semantics while drawing the label, pill track and thumb. Cobalt with a white thumb means checked; charcoal with a cream thumb means unchecked. Text remains part of the click target. Focus uses a visible inset rectangle. Its background is opaque EVA Ink (`BackColor`, `UseVisualStyleBackColor = false`). Each repaint fills only its clipped control bounds; `TextRenderer` preserves graphics clipping and translation, so labels do not inherit a stale parent tile or escape the control during a translated paint.

**The Wheel Safety Rule.** The wheel scrolls the settings body without changing a value, even over a selector popup or slider. Changing a parameter requires an explicit click, drag or keyboard action.

The wheel filter includes child controls and the selector's separate popup window. A wheel gesture closes an open dropdown and scrolls the body. Distance follows Windows wheel settings; horizontal-wheel messages are consumed without changing a parameter. These controls have no authored hover animation.

### Navigation and fixed chrome

Home shows the heart and wordmark. Secondary pages show a back action and Segoe UI page title. Close hides the panel; the settings gear and other icon-only buttons have accessible names.

Five tabs stay outside the scrolling body. Selection is cobalt; idle tabs share the ink canvas with a charcoal hover. Apply stays in the fixed footer. Navigation rows use left-aligned text, `navigation-inset` and a drawn right chevron.

### Media, source rows and feedback

Home thumbnails are local captures fitted within their frames; a small duration stamp can overlay the corner. The empty state says saved moments will appear here and that the save shortcut requires replay to be enabled. It remains valid before the engine is ready.

Source rows pair a stable name with current availability or buffering detail. Normal detail uses muted text; active buffering uses cream; current warnings use Warning Peach. The fixed footer separates operational status from RAM, FPS and codec metadata.

Capture-method wording preserves the limits in `PRODUCT.md`. “Захват окон без жёлтой рамки” controls the capture engine. Its visual switch does not establish universal border removal or compatibility, and the shared save action does not establish frame-accurate synchronization.

The sidecar's component samples are self-contained HTML/CSS translations for design inspection. Native C# controls remain authoritative for rendering, keyboard interaction, wheel routing, accessibility and engine-driven state. Generated tonal ramps are display aids, not extra implemented application colors.

## Do's and Don'ts

### Do:

- **Do** preserve the cream masthead, EVA heart, ink canvas and cobalt action hierarchy.
- **Do** keep the wordmark in the embedded Bebas Neue face and Russian controls in Segoe UI.
- **Do** retain the 440px panel and fixed settings tabs, Apply action and status area.
- **Do** make parameter edits explicit and preserve visible keyboard focus.
- **Do** show recording, replay, source availability and disabled actions from confirmed engine state.
- **Do** use real local media in the gallery and keep private captures out of public design assets.

### Don't:

- **Don't** replace the EVA heart with a text glyph or redraw action icons as font characters.
- **Don't** apply rectangular control geometry to switch tracks or slider thumbs.
- **Don't** add ornamental shadows, glass layers or card stacks to this utility.
- **Don't** turn a wheel gesture into a selector or slider edit.
- **Don't** imply a successful save while replay is off or before the engine confirms completion.
- **Don't** treat a UI screenshot as proof of borderless capture compatibility or frame-accurate synchronization.
