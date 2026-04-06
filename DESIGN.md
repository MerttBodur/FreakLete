# Design System: FreakLete

This document defines the visual and interaction system for FreakLete based on the current repository implementation (colors, styles, controls, and page patterns).

Primary sources used:
- `Resources/Styles/Colors.xaml`
- `Resources/Styles/Styles.xaml`
- `Xaml/*.xaml` screens
- `Xaml/Controls/*.xaml` shared components

## 1. Visual Theme and Atmosphere
FreakLete uses a dark, performance-first visual language designed for athletes, not productivity users. The interface feels like a training environment: focused, contrasty, and metric-driven. The core atmosphere is "night gym dashboard" with layered deep-purple surfaces and vivid violet accents.

The most distinctive visual move is the contrast between:
- deep base surfaces (`#100D1A`, `#171321`, `#1D1828`)
- sharp but controlled violet accents (`#8B5CF6`, `#A78BFA`)
- light text (`#F7F7FB`) and muted metadata tones (`#B3B2C5`, `#8A889B`)

Many containers use gentle linear gradients (`SurfaceRaised -> Surface`) to avoid flat cards and keep the UI premium without heavy shadows. The result is modern and athletic, but still calm enough for long-form logging sessions.

Key characteristics:
- Dark-first interface with violet identity
- OpenSans-only typography (no serif voice)
- Rounded card-heavy UI with soft 18-26px radii
- Border-led depth (`SurfaceBorder`) instead of aggressive shadows
- Dashboard rhythm: hero card -> metrics -> tools -> actions
- Consistent spacing and touch target discipline (44px+ minimum)

## 2. Color Palette and Roles
### Primary
- `Accent` `#8B5CF6`: primary action, CTA backgrounds, active emphasis
- `AccentGlow` `#A78BFA`: highlighted numbers, active labels, bright accents
- `Primary` `#8B5CF6`: equivalent to Accent in token usage

### Core Surfaces
- `Background` `#100D1A`: default page canvas
- `Surface` `#171321`: base card surface
- `SurfaceRaised` `#1D1828`: elevated surface for cards and input shells
- `SurfaceStrong` `#251F33`: stronger secondary button background
- `TopBar` `#161125`: navigation/top chrome tone

### Border and Structural
- `SurfaceBorder` `#342D46`: default stroke, separators, card boundaries
- `BorderSubtle` `#2A2437`: quieter structural line
- `OverlayScrim` `#B3100D1A`: overlays and modal dim states

### Text
- `TextPrimary` `#F7F7FB`: primary readable text
- `TextSecondary` `#B3B2C5`: secondary descriptions and helper text
- `TextMuted` `#8A889B`: tertiary labels and low-priority metadata

### Semantic
- `Success` `#22C55E`, `SuccessSoft` `#0D2818`
- `Warning` `#F59E0B`, `WarningSoft` `#2A1F06`
- `Danger` `#DC2626`, `DangerSoft` `#3A1623`
- `Error` `#EF4444`
- `Info` `#3B82F6`, `InfoSoft` `#0D1B2A`

### Legacy/Compatibility Tokens
The dictionary still includes generic grayscale and legacy tokens (`Gray100-950`, `Magenta`, `MidnightBlue`, `OffBlack`) for compatibility. New UI work should prefer `Background/Surface/Accent/Text*` tokens above.

### Gradient System
FreakLete relies on subtle gradients for depth:
- Standard card gradient: `SurfaceRaised (#1D1828) -> Surface (#171321)`
- Accent hero gradient: `AccentSoft (#2F2346) -> Surface (#171321)`
- Bottom action bars: vertical `SurfaceRaised -> Surface`

## 3. Typography Rules
### Font Family
- Primary UI font: `OpenSansSemibold` (headings, labels, action text)
- Secondary/body font: `OpenSansRegular` (descriptive text, helper text)
- No serif and no monospace design language in current UI system

### Current Hierarchy (from styles and page usage)
| Role | Font | Size | Weight | Typical line-height | Notes |
|---|---|---:|---|---|---|
| Hero headline | OpenSansSemibold | 34px (`Headline`) | Semibold | default | Login/Register feature hero |
| Section title | OpenSansSemibold | 24px (`SubHeadline`) | Semibold | default | Profile and section anchors |
| Header title | OpenSansSemibold | 21px (`HeaderTitle`) | Semibold | default | Top header bar |
| Card title | OpenSansSemibold | 18-20px | Semibold | default | Home/Workout/FreakAI cards |
| Standard label | OpenSansSemibold | 15px | Semibold | default | Default Label style and inputs |
| Muted body | OpenSansRegular | 14px (`BodyMuted`) | Regular | default | Supporting content |
| Caption/meta | OpenSansRegular | 11-13px | Regular | default | Card metadata and hints |
| Eyebrow | OpenSansSemibold | 12px (`Eyebrow`) | Semibold | default | Uppercase, 1.2 spacing |
| Micro nav label | OpenSansSemibold | 10px | Semibold | default | Bottom nav tab labels |

Typography principles:
- Semibold carries product tone and hierarchy.
- Regular is used for helper and explanatory text.
- Keep clear contrast by pairing semibold headers with muted regular body text.
- Avoid dense text blocks; break copy into short dashboard-readable chunks.

## 4. Component Stylings
### Buttons
#### Primary CTA
- Background: `Accent` `#8B5CF6`
- Text: `TextPrimary` / white
- Radius: `18` (or `14` in compact bars)
- Padding: around `18,14` (global), `16-20,12-14` (contextual)
- Font: `OpenSansSemibold`, 14-15px

#### Secondary Button
- Style key: `SecondaryButton`
- Background: `SurfaceStrong` `#251F33`
- Text: `TextPrimary`
- Radius: `18`
- Used for alternate actions, browse buttons, low-risk actions

#### Semantic Destructive
- Background often `DangerSoft`
- Text inherits button text contrast
- Used for irreversible account actions

### Cards and Containers
- Default card style: `CardBorder`
  - Background: `Surface`
  - Stroke: `SurfaceBorder`
  - Radius: `24`
  - Padding: `18`
- Accent card style: `AccentCardBorder`
  - Background: `AccentSoft`
  - Stroke: `Primary`
  - Radius: `24`
- Compact card variants: round 18-20 for tiles and summary blocks

### Inputs and Forms
- `Entry`: `SurfaceRaised`, `TextPrimary`, `OpenSansSemibold`, 15px, 52px min height
- `Editor`: `SurfaceRaised`, `OpenSansRegular`, 14px
- Placeholder: `TextMuted`
- Disabled states use gray fallback tokens
- Focus follows platform behavior; keep visible contrast against dark surface

### Navigation
#### Top Header
- Minimal bar with optional back button
- `HeaderTitle` style token
- Transparent controls over dark background

#### Bottom Navigation
- Floating rounded container
- Background: `Surface`
- Stroke: `SurfaceBorder`
- Radius: `26`
- One notable shadow use (`Radius 20`, `Opacity 0.35`, `Offset 0,4`)
- Active tab rendered as pill emphasis in code-behind

### Chart and Data Components
- `ExerciseComparisonChart` uses gradient card shell + compact badges
- Positive KPI colors: `Success`, `AccentGlow`
- Legend and supporting labels stay in `TextSecondary`
- Empty states use `TextMuted`

### Motion and Feedback
- Current system uses minimal motion; interaction feedback mainly by:
  - color shift
  - border/pill state
  - navigation transitions
  - inline status labels and toasts

## 5. Layout Principles
### Spacing System
Base rhythm: 4px / 8px scaling with common values:
- 8, 10, 12, 14, 16, 18, 20, 24, 26, 32

Common page structure:
- Outer page padding: `20-24` horizontal
- Section spacing: `18-20`
- In-card stack spacing: `8-12`
- Action row gaps: `8-12`

### Grid and Container Patterns
- Primary shell: `Grid RowDefinitions="*,Auto"` (content + bottom nav)
- Content usually in one vertical scroll column
- Metric zones use 2 or 3-column grids
- Horizontal card rails for quick workouts and quick actions

### Border Radius Scale
- Small interactive: 10-14
- Standard controls: 18
- Standard cards: 18-24
- Bottom nav and large shells: 26+

### Whitespace Philosophy
- Dashboard-first: chunk information into scannable cards
- Prefer clear section boundaries over long continuous forms
- Keep top area focused on identity + immediate action

## 6. Depth and Elevation
| Level | Treatment | Use |
|---|---|---|
| Level 0 | Flat `Background` canvas | Page base |
| Level 1 | 1px `SurfaceBorder` + solid surface | Standard cards and forms |
| Level 2 | Gradient surface (`SurfaceRaised -> Surface`) | Hero and emphasized cards |
| Level 3 | Accent gradient (`AccentSoft -> Surface`) | Branded highlight blocks |
| Level 4 | Soft drop shadow (limited) | Bottom nav floating container |

Depth philosophy:
- Prefer borders and tonal layering over strong drop shadows.
- Use gradients to separate hierarchy without visual noise.
- Keep shadow usage rare to preserve crisp performance-oriented UI.

## 7. Do and Do Not
### Do
- Use design tokens from `Colors.xaml`; avoid ad-hoc hex values.
- Keep dark-first visual hierarchy across new screens.
- Use `OpenSansSemibold` for hierarchy and action labels.
- Use `CardBorder` / `SecondaryButton` styles for consistency.
- Maintain rounded, soft geometry (18-24 radius baseline).
- Keep touch targets >= 44x44.

### Do Not
- Do not introduce cool blue themes that conflict with violet identity.
- Do not use serif display typography in this app style.
- Do not rely on heavy shadows as primary depth.
- Do not mix too many accent colors in one section.
- Do not ship screens with mixed legacy light tokens unless intentional.
- Do not bypass tokenized semantic colors for warnings/errors/success.

## 8. Responsive Behavior
FreakLete is phone-first, with tablet adaptation through flexible layouts.

Suggested breakpoints for future consistency:
- Compact phone: `<360dp`
- Standard phone: `360-430dp`
- Large phone / small tablet: `431-767dp`
- Tablet: `>=768dp`

Behavior guidelines:
- Keep single-column flow as default.
- Promote metric rows from stacked -> 2/3 column when width allows.
- Preserve 20-24 horizontal gutters on phones; increase to 28-36 on tablets.
- Keep bottom nav touch targets and spacing stable across sizes.
- For tablet, widen cards and avoid over-stretched text lines (max-width containers).

## 9. Agent Prompt Guide
### Quick token reference
- Page background: `Background #100D1A`
- Main card surface: `Surface #171321`
- Elevated card tone: `SurfaceRaised #1D1828`
- Primary action: `Accent #8B5CF6`
- Highlight text: `AccentGlow #A78BFA`
- Main text: `TextPrimary #F7F7FB`
- Secondary text: `TextSecondary #B3B2C5`
- Card border: `SurfaceBorder #342D46`

### Example component prompts
- "Create a dark dashboard card using `CardBorder` style, with title at 18px `OpenSansSemibold`, body at 14px `OpenSansRegular`, and gradient background `SurfaceRaised -> Surface`."
- "Design a primary CTA row with two buttons: left as secondary (`SurfaceStrong`), right as primary (`Accent`), both 14px semibold text and 14-18 radius."
- "Build a profile summary block on `Background` with three metric cards (18 radius), accent-glow numeric values, and secondary metadata labels."
- "Create a quick-workout horizontal rail with 180px cards, image header, subtle overlay scrim, and compact metadata text."
- "Design a settings list card stack using `CardBorder`, icon + title + subtitle + chevron, with disabled rows at 0.5 opacity and `AccentSoft` coming-soon badge."

### Iteration workflow
1. Reference exact token names, not approximate colors.
2. Specify heading/body font family explicitly.
3. Define radius and spacing values numerically.
4. Keep one visual goal per iteration (hierarchy, density, or contrast).
5. Verify legibility and touch target size after every visual update.

