# Retro Dithering (URP 17, Unity 6)

Три способа получить ретро-картинку с пониженной глубиной цвета.

| Эффект | Где работает | Что делает |
|---|---|---|
| **Retro Dithering** | Постэффект (Renderer Feature + Volume) | Пикселизация, цветокоррекция, дизеринг Байера, квантование или палитра. Стилизованный вариант. |
| **Retro Color Depth** | Постэффект (Renderer Feature + Volume) | Только округление каждого канала до N уровней (по умолчанию 8 = 3 бита), без дизеринга. |
| **Retro Lit** | Шейдер материала | URP Complex Lit + округление итогового цвета (после света, теней и тумана) на каждом объекте. |

## Структура

- `Shaders/RetroDithering.shader` — постэффект Retro Dithering.
- `Shaders/RetroColorDepth.shader` — постэффект Retro Color Depth.
- `Shaders/RetroLit.shader` + `RetroLitForward.hlsl` — шейдер на объектах (`ParfenovGames/Retro Lit`).
- `Shaders/RetroQuantize.hlsl` — общее квантование для Retro Color Depth и Retro Lit.
- `Scripts/` — Renderer Feature и Volume для постэффектов, `RetroLitSettings` для Retro Lit.
- `Editor/` — команды меню и запекание палитры.

## Быстрое подключение

Открой сцену → **Tools → Retro Dithering → Setup Active Scene →**

- **Retro Dithering (Post)** или **Retro Color Depth (Post)** — добавит Renderer Feature во все URP Renderer из Quality Settings,
  создаст глобальный Volume с профилем рядом со сценой и выключит сглаживание на главной камере.
- **Retro Lit (Per Object)** — переключит все материалы сцены на URP Lit / Complex Lit на шейдер Retro Lit
  и добавит объект `Retro Lit Settings`. Остальные шейдеры (Shader Graph и т.п.) пропускаются, список — в консоли.

Сохрани сцену после команды.

**Tools → Retro Dithering → Revert Scene Materials To URP Lit** — вернёт материалы на исходный шейдер.

Внимание: Retro Lit меняет сами ассеты материалов, значит, изменение видно во всех сценах, где они используются.

## Retro Lit: важное

- Свойства те же, что у URP Lit, инспектор тоже от Lit. Материал можно переключать туда-обратно без потерь.
- Глобальные настройки — компонент **Retro Lit Settings** (Color Levels, Dither Strength). Без него — 8 уровней, без дизеринга.
- Работает в Forward, Forward+ и Deferred (как Complex Lit, рисуется в forward).
- Чтобы итоговые цвета точно легли на сетку из 8 уровней:
  - не включай тонмаппинг и цветокоррекцию в постобработке (они меняют цвет после квантования);
  - выключи Bloom;
  - в URP Asset можно выключить HDR — с HDR-буфером значения могут отличаться на 1–2 единицы.
- Прозрачные материалы квантуются до смешивания, поэтому на стыках возможны промежуточные цвета.
- Объект на обычном Lit не квантуется — так делаются исключения (экраны, интерфейс в мире).
- Не включай одновременно постэффект: команда Retro Lit предупредит в консоли, если в сцене активен Volume с ретро-эффектом.

## Палитра (только Retro Dithering)

1. Выдели `Palettes/Palette_Basement.png` (или свою картинку, до 256 уникальных цветов).
2. **Assets → Retro Dithering → Bake Palette LUT**. Рядом появится `Palette_Basement_LUT.asset`.
3. Перетащи его в поле **Palette Lut** в Volume.
