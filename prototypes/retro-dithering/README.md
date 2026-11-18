# Retro Dithering (URP, Unity 6)

Полноэкранный эффект: пониженное разрешение, цветокоррекция, упорядоченный дизеринг (Bayer) и квантование цвета.
Значения по умолчанию подобраны под тёмный тёплый хоррор-стиль, 8 уровней на канал (3 бита).

## Структура

- `Shaders/RetroDithering.shader` — сам шейдер (`Hidden/ParfenovGames/RetroDithering`).
- `Scripts/RetroDitheringRendererFeature.cs` — Renderer Feature и проход через Render Graph.
- `Scripts/RetroDitheringVolume.cs` — настройки в Volume.
- `Editor/RetroDitheringLutBaker.cs` — запекание палитры в 3D LUT.
- `Palettes/Palette_Basement.png` — пример палитры на 24 цвета.

## Подключение

**Быстро:** открой сцену → **Tools → Retro Dithering → Setup Active Scene** → сохрани сцену.
Команда добавит Renderer Feature во все URP Renderer из Quality Settings, создаст в сцене глобальный Volume
с профилем `<Сцена>_RetroDithering.asset` рядом со сценой и выключит сглаживание на главной камере.

**Вручную:**

1. Открой ассет URP Renderer (например, `Assets/Settings/PC_Renderer`) → **Add Renderer Feature → Retro Dithering**.
2. На Global Volume: **Add Override → Post-processing Custom → Retro Dithering**, включи **Intensity** и поставь 1.
3. Для чистого вида на камере выключи сглаживание (Anti-aliasing: None), в Volume выключи Bloom.

## Палитра (необязательно)

1. Выдели `Palettes/Palette_Basement.png` (или свою картинку, где каждый уникальный цвет — элемент палитры, до 256 цветов).
2. **Assets → Retro Dithering → Bake Palette LUT**. Рядом появится `Palette_Basement_LUT.asset`.
3. Перетащи его в поле **Palette Lut** в Volume. Пустое поле — режим «N уровней на канал».

## Параметры

| Параметр | Что делает |
|---|---|
| Pixel Size | Размер «ретро-пикселя» в экранных пикселях |
| Exposure / Contrast / Saturation / Black Level / Tint | Цветокоррекция до квантования |
| Color Levels | Уровней на канал без палитры (8 = как в референсе) |
| Dither Strength | 0 — просто постеризация, 1 — классический дизеринг |
| Bayer Matrix | 2×2 / 4×4 / 8×8 |
| Palette Dither Spread | Амплитуда дизеринга в режиме палитры |
| Grain / Vignette | Зерно и виньетка, тоже проходят через дизеринг |

## Заметки

- Точка встраивания по умолчанию — After Rendering Post Processing: квантуется уже тонмапленный кадр.
- Если эффекта не видно, проверь, что Renderer Feature добавлен именно в тот Renderer, который использует камера.
