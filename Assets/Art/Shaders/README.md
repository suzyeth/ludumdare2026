# Prism Filter — Setup

## Asset wiring

1. Create a material from `PrismFilter.shader` → name it `M_PrismFilter`.
2. Open the **URP Renderer asset** (`Assets/Settings/Renderer2D.asset`).
3. Add a **Full Screen Pass Renderer Feature**. Configure:
   - Pass Material: `M_PrismFilter`
   - Injection Point: **Before Rendering Post Processing** (so Pixel Perfect upscale happens *after* the channel strip — keeps the effect pixel-exact at 640×360).
   - Requirements: Color
4. In your scene bootstrap, assign `M_PrismFilter` to `FilterManager.fullScreenMaterial`.

## Runtime

`FilterManager.SetFilter(FilterColor)` pushes `_FilterMode` into the material
(0=none, 1=red strip, 2=green, 3=blue). The shader renders the base pass,
Pixel Perfect Camera then upscales the result to 1920×1080.

## Verifying

- Draw three overlapping colored squares (`#E53935`, `#43A047`, `#1E88E5`).
- Press 1: red square vanishes → anything behind it (painted in red) now readable.
- Press 2 / 3: same for green / blue.
- Press 0: all three visible.
