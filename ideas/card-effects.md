# Идея: эффекты для карточных игр (шейдеры + «живые» карты)

- **Тип:** шейдер / VFX (+ небольшая система анимации карт)
- **Поток:** A (хит) + B (тренд: карточные рогалики и деккбилдеры, симуляторы магазина карт)
- **Источник:** Balatro, TCG Card Shop Simulator, Slay the Spire 2, CloverPit (см. `research/games.csv`)
- **Статус:** **в работе — первый ассет опубликован**, проверка спроса пройдена

## Наш ассет

[Prismatic Card Shader](https://assetstore.unity.com/packages/vfx/shaders/prismatic-card-shader-394894):
- цена $19,99, Shader Graph, URP + HDRP, исходная версия Unity 6000.4;
- последний релиз 5 окт. 2026, 10 добавлений в избранное, рейтинга пока нет;
- **продажи: 21 за ~2 месяца** (≈ $420 брутто, ≈ $290 доля паблишера после 70/30).

> **Калибровка:** у нас 21 продажа на 10 «избранных». Это одна точка, но для грубой оценки конкурентов можно считать продажи ≈ 1–2× избранного. Перепроверять по мере роста.

## Конкуренты (данные на 2026-11-17)

Почти ни у кого нет рейтинга («not enough ratings»), поэтому основной индикатор интереса — избранное.

| Ассет | Цена | Избранное | Последний релиз | Пайплайны / Unity | Что внутри |
|---|---|---|---|---|---|
| [TCG Card Game Effects](https://assetstore.unity.com/packages/vfx/tcg-card-game-effects-277550) | $35 | 152 | апр. 2026 | Built-in + URP, 2021.3+ | 50 эффектов (частицы и шейдеры), не голография |
| [Card TCG Holographic Shaders Pack](https://assetstore.unity.com/packages/vfx/shaders/card-tcg-holographic-shaders-pack-330762) | $15 | 77 | ? | ? | Пак голографических шейдеров |
| [3D Holographic NFT Card Template](https://assetstore.unity.com/packages/vfx/shaders/3d-holographic-nft-card-template-210416) | $30 | 75 | фев. 2022 | 2020.3 | **Устарел** |
| [Holographic TCG Card](https://assetstore.unity.com/packages/vfx/shaders/holographic-tcg-card-315161) | $9,99 | 59 | мар. 2025 | URP, 6000.0 | Один голо-материал, параллакс |
| [Hologram Shader URP](https://assetstore.unity.com/packages/vfx/shaders/hologram-shader-urp-262977) | $9 | 53 | ноя. 2024 | URP, 6000.0 | Голография + «3D внутри 2D» (стенсил) |
| [Card Visual Effects Suite](https://assetstore.unity.com/packages/vfx/shaders/card-visual-effects-suite-311449) | $15 | ~50 | 2025 | 6000.0 | Foil, holo, negative + скрипты наклона/hover + фон. **Использует название игры в описании** |
| [Holographic Card Shader / Trading Card…](https://assetstore.unity.com/packages/vfx/holographic-card-shader-trading-card-playing-card-shiny-nft-250094) | $8,99 | 34 | ноя. 2025 | URP, 6000.2 | Голо-шейдер |
| [Frame/Outline Shader (UI Friendly)](https://assetstore.unity.com/packages/slug/312353) | $15 | 33 | ? | все пайплайны | Рамки и подсветка, UI/спрайты |
| [Card Effects Vol. 01](https://assetstore.unity.com/packages/vfx/shaders/card-effects-vol-01-309803) | $6,99 | 20 | фев. 2025 | все пайплайны, 2022.3 | Набор эффектов |
| [TCG Holographic Shader](https://assetstore.unity.com/packages/vfx/shaders/tcg-holographic-shader-346880) | $18 | 18 | янв. 2026 | URP, 6000.0 | Голо-шейдер |
| [Card Depth Parallax Shader (URP)](https://assetstore.unity.com/packages/package/330107) | $25 | 11 | сен. 2025 | URP, 6000.2 | Параллакс-глубина |
| [Hologram Card Shader](https://assetstore.unity.com/packages/vfx/shaders/hologram-card-shader-333448) | $4,99 | 4 | янв. 2026 | 2022.3 | Голо-шейдер |
| CraftCards (релиз март 2026) | ? | ? | 2026 | Canvas UI | Фреймворк карт: физика drag без дрожания, arc-раскладка руки, foil/rainbow/glitch UI-шейдеры с поддержкой масок |
| [Card Tilter](https://assetstore.unity.com/packages/tools/particles-effects/card-tilter-120727) | ? | ? | июл. 2018 | 5.6 | **Устарел** |

## Выводы

1. **Ниша фрагментирована, явного лидера нет.** Десяток мелких ассетов без отзывов, цены $5–35. Наша цена $19,99 в верхней половине — и продажи это выдерживают.
2. **Отзывы = главное конкурентное преимущество.** Ни у кого в нише нет рейтинга, поэтому первые 3–5 отзывов сразу выделят наш ассет. Нужен мягкий запрос отзыва (в README/демо-сцене, ответ в поддержке).
3. **Исходная версия Unity 6000.4 сужает рынок.** Конкуренты собраны на 6000.0 LTS или 2022.3. Пере-экспорт пакета из 6000.0 LTS (и, если возможно, 2022.3) откроет ассет тем, кто сидит на LTS.
4. **Нет Built-in.** Built-in поддерживают только TCG Card Game Effects, Frame/Outline и Card Effects Vol. 01. Для шейдера это вторично, но для UI-ориентированных покупателей важно.
5. **UI Canvas — больное место.** Автор CraftCards прямо называет проблемы: дрожание при drag, шейдеры, которые игнорируют маски UI. Поддержка Canvas + масок — заметное преимущество.
6. **«Ощущение карты» почти никто не продаёт вместе с шейдером.** Только Card Visual Effects Suite совмещает шейдеры с наклоном/hover. Это пространство для следующего продукта.
7. **Размер пакета 170,6 МБ** крупный для шейдера (у конкурентов 1–40 МБ). Стоит проверить, что раздувает пакет (текстуры демо?), и сжать.

## Следующие шаги

- [ ] Обновление Prismatic Card Shader: убрать название игры из тегов и описания, добавить родовые ключевые слова (foil, polychrome, holo, trading card, deckbuilder, roguelike card game, tilt, CRT).
- [ ] Пере-экспортировать из Unity 6000.0 LTS, проверить совместимость с Canvas UI и масками.
- [ ] Проверить, что раздувает размер пакета.
- [ ] Попросить первых покупателей оставить отзыв.
- [ ] Следующий продукт линейки: **Card Juice Kit** — наклон за курсором, hover, drag со «весом», flip, shake, раскладка руки по дуге + анимированные фоны для карточных игр. Совместимость с Prismatic Card Shader, затем бандл.

## Скоринг (1–5)

| Критерий | Вес | Балл |
|---|---|---|
| Спрос | 30% | 4 (подтверждён продажами) |
| Разрыв с конкурентами | 25% | 4 (нет лидера, нет отзывов) |
| Ценовой потенциал | 15% | 3 ($10–35) |
| Трудоёмкость (обратная) | 15% | 4 |
| Долгосрочность | 10% | 4 (жанр живёт с 2017, свежие хиты в 2025–2026) |
| Портфолио | 5% | 3 |
| **Итог** | | **3,8** |

## Решение

Развивать линейку: обновить текущий ассет, затем делать Card Juice Kit и бандл.
