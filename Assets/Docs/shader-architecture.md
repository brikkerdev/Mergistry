

# MERGISTRY — Техническая документация шейдерной архитектуры

---

## 1. Обзор архитектуры

### Рендер-пайплайн

**Built-in Render Pipeline**, 2D-проект. Камера — ортографическая. Все игровые объекты — плоские quad-меши (MeshRenderer) или спрайты (SpriteRenderer), расположенные на плоскости XY с разделением по Z для сортировки.

### Принцип «Zero Textures»

Все визуальные объекты рисуются процедурно во фрагментном шейдере. Исключения:

| Исключение | Причина |
|-----------|---------|
| Noise LUT (256×256, tileable, RGBA) | Производительность: выборка текстуры дешевле процедурного noise на слабых GPU |
| Gradient ramp (256×1) | Цветовые переходы для эффектов огня, яда и т.д. |
| UI-иконки (атлас 512×512) | Unity UI Toolkit требует текстуры для иконок |

Эти текстуры генерируются **один раз** при сборке проекта (Editor-скрипт) и включаются в билд как assets.

### Типы шейдеров

Все шейдеры — **Unlit** (без освещения). Используется структура Vert/Frag (не Surface Shader). Причины:

- 2D-игра, нет источников света
- Полный контроль над выходным цветом
- Минимальная стоимость фрагмента
- Прозрачность управляется вручную через alpha blending

### Язык

CG/HLSL внутри `.shader`-файлов Unity. Совместимость с OpenGL ES 3.0 (Android) и Metal (iOS).

### Система координат шейдеров

Все SDF-функции работают в **нормализованном UV-пространстве** объекта: `(0,0)` — нижний левый угол quad-меша, `(1,1)` — верхний правый. Центр объекта: `(0.5, 0.5)`. Aspect ratio компенсируется через uniform `_AspectRatio`.

---

## 2. Rendering Order (порядок отрисовки)

Сортировка по Z-позиции камеры (ортографическая, TransparencySortMode.Default). Слои от дальнего к ближнему:

| Sorting Layer | Order | Содержимое |
|--------------|-------|------------|
| Background | 0 | Фон подземелья (один полноэкранный quad) |
| Floor | 10 | Тайлы пола (сетка quad-ов) |
| Zones | 20 | Зоны эффектов на поле (огонь, вода, яд, лёд) |
| Entities | 30 | Враги, игрок, интерактивные объекты |
| Effects | 40 | Эффекты зелий, частицы, вспышки |
| UI_World | 50 | Намерения врагов, числа урона, индикаторы |
| Overlay | 60 | Полноэкранные эффекты (flash, damage vignette) |
| UI | — | Unity UI Canvas (отдельный Camera + Canvas) |
| PostProcess | — | OnRenderImage (bloom, vignette, хром. аберрация) |

### Блендинг

Все шейдеры кроме Background и Floor используют **alpha blending**:

```
Blend SrcAlpha OneMinusSrcAlpha
```

Исключение — **аддитивные эффекты** (glow, вспышки, bloom contribution):

```
Blend SrcAlpha One
```

### Z-Write

Отключён для всех шейдеров (`ZWrite Off`). Сортировка — только через Sorting Layer/Order.

---

## 3. Shared Library (общие функции)

Все шейдеры включают общий cginc-файл, содержащий переиспользуемые функции. Файл: `AlchemistSDF.cginc`.

### 3.1 SDF-примитивы

| Функция | Входы | Выход | Описание |
|---------|-------|-------|----------|
| `sdCircle` | `float2 p, float r` | `float` | Signed distance до круга радиуса r с центром в origin |
| `sdBox` | `float2 p, float2 b` | `float` | Signed distance до прямоугольника размера b |
| `sdRoundedBox` | `float2 p, float2 b, float r` | `float` | Прямоугольник со скруглёнными углами |
| `sdTriangle` | `float2 p, float2 a, float2 b, float2 c` | `float` | Произвольный треугольник |
| `sdEquilateralTriangle` | `float2 p, float size` | `float` | Равносторонний треугольник |
| `sdHexagon` | `float2 p, float r` | `float` | Правильный шестиугольник |
| `sdStar` | `float2 p, float r, int n, float m` | `float` | Звезда с n лучами |
| `sdSegment` | `float2 p, float2 a, float2 b` | `float` | Отрезок от a до b |
| `sdArc` | `float2 p, float2 sc, float ra, float rb` | `float` | Дуга |
| `sdRing` | `float2 p, float r, float w` | `float` | Кольцо радиуса r, толщины w |

### 3.2 SDF-операции

| Функция | Описание |
|---------|----------|
| `opUnion(d1, d2)` | Объединение: `min(d1, d2)` |
| `opSubtract(d1, d2)` | Вычитание: `max(-d1, d2)` |
| `opIntersect(d1, d2)` | Пересечение: `max(d1, d2)` |
| `opSmoothUnion(d1, d2, k)` | Плавное объединение (metaball-эффект), k — радиус сглаживания |
| `opRound(d, r)` | Скругление: `d - r` |
| `opAnnular(d, w)` | Превращение в контур толщины w |
| `opRepeat(p, spacing)` | Повторение пространства (тайлинг) |
| `opRotate(p, angle)` | Вращение UV на angle радиан |
| `opScale(p, s)` | Масштабирование |

### 3.3 Noise-функции

Все noise-функции имеют два варианта: **процедурный** (вычислительный) и **текстурный** (выборка из LUT). Переключение через define: `#define USE_NOISE_LUT`.

| Функция | Описание | Стоимость (процедурный) | Стоимость (LUT) |
|---------|----------|------------------------|-----------------|
| `valueNoise(p)` | Value noise 2D | Средняя | Низкая |
| `gradientNoise(p)` | Perlin-подобный gradient noise | Средняя | Низкая |
| `simplexNoise(p)` | Simplex noise 2D | Средняя | Низкая |
| `fbm(p, octaves)` | Fractal Brownian Motion (до 4 октав) | Высокая | Средняя |
| `voronoi(p)` | Voronoi diagram (возвращает distance + cell ID) | Высокая | Средняя |
| `turbulence(p, octaves)` | `abs(fbm)` — турбулентный шум | Высокая | Средняя |
| `domainWarp(p, strength)` | Искажение UV через noise | Высокая | Средняя |

### 3.4 Утилиты

| Функция | Описание |
|---------|----------|
| `fill(d, smoothness)` | SDF → alpha (плавный край, anti-aliasing). `smoothstep(-smoothness, smoothness, -d)` |
| `stroke(d, width, smoothness)` | SDF → контур толщины width |
| `glow(d, intensity, falloff)` | SDF → свечение (аддитивное). `intensity / pow(max(d, 0.001), falloff)` |
| `hsvToRgb(h, s, v)` | Конвертация HSV → RGB |
| `rgbToHsv(rgb)` | Конвертация RGB → HSV |
| `remap(value, inMin, inMax, outMin, outMax)` | Переназначение диапазона |
| `pulse(t, frequency, sharpness)` | Пульсация: `pow(sin(t * frequency) * 0.5 + 0.5, sharpness)` |
| `hash21(p)` | Хеш float2 → float (для детерминированного рандома) |
| `hash22(p)` | Хеш float2 → float2 |

### 3.5 Общие uniform-переменные

Передаются во все шейдеры через `Shader.SetGlobalFloat/Vector/Color`:

| Uniform | Тип | Описание |
|---------|-----|----------|
| `_GameTime` | `float` | Время с начала рана (не `_Time`, чтобы контролировать паузу) |
| `_ScreenShake` | `float2` | Текущее смещение screenshake (применяется в вершинном шейдере) |
| `_GlobalFlash` | `float4` | Цвет + интенсивность полноэкранной вспышки |
| `_Slowmo` | `float` | Множитель скорости анимации (1.0 = норма, 0.05 = hitstop) |

---

## 4. Шейдеры: Среда подземелья

### 4.1 SH_Background

**Назначение**: Полноэкранный фон. Один quad на весь экран.

| Параметр | Тип | Описание | Диапазон |
|----------|-----|----------|----------|
| `_ColorTop` | Color | Цвет верха | #0A0A12 |
| `_ColorBottom` | Color | Цвет низа | #1A1A2E |
| `_NoiseIntensity` | Float | Интенсивность шума | 0.02–0.05 |
| `_NoiseScale` | Float | Масштаб шума | 3.0–5.0 |
| `_NoiseSpeed` | Float | Скорость анимации шума | 0.1–0.3 |

**Алгоритм фрагмента**:
1. Вертикальный градиент `_ColorTop → _ColorBottom` по UV.y
2. Наложение `valueNoise(UV * _NoiseScale + _GameTime * _NoiseSpeed)` × `_NoiseIntensity` как аддитивный слой
3. Виньетка: затемнение от центра к краям (встроенная, не пост-процесс)

**Стоимость**: Минимальная (1 noise sample + gradient).

### 4.2 SH_Floor

**Назначение**: Тайл пола (одна клетка сетки). Каждая клетка — отдельный quad с этим материалом.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_CellID` | Vector2 | Координата клетки на сетке (для seed voronoi) |
| `_SeamColor` | Color | Цвет швов между камнями |
| `_SeamGlow` | Float | Интенсивность свечения швов (0–1) |
| `_StoneColor` | Color | Базовый цвет камня |
| `_Highlight` | Float | Подсветка клетки (0 = нет, 1 = полная) |
| `_HighlightColor` | Color | Цвет подсветки (красный для опасности, синий для доступности) |
| `_ZoneOverlay` | Float | Тип зоны (0=нет, 1=огонь, 2=вода, 3=яд, 4=лёд) — используется для тонирования |

**Алгоритм фрагмента**:
1. `voronoi(UV * 3.0 + _CellID)` → distance и cellID
2. Камень: `_StoneColor` + лёгкий `valueNoise` для вариации
3. Швы: `smoothstep` по distance → `_SeamColor` × `_SeamGlow`
4. Подсветка: overlay blend `_HighlightColor` × `_Highlight` с пульсацией (`pulse(_GameTime, 3.0, 1.0)`)
5. Тонирование зоны: если `_ZoneOverlay > 0`, мягкий color overlay соответствующего цвета

**GPU instancing**: Включён. Все тайлы пола — один draw call с per-instance properties (`_CellID`, `_Highlight`, `_HighlightColor`, `_ZoneOverlay`).

**Стоимость**: Средняя (voronoi + valueNoise).

### 4.3 SH_Wall

**Назначение**: Декоративные стены по краям сетки.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_WallColor` | Color | Базовый цвет (#252540) |
| `_BrickScale` | Float | Масштаб кирпичной кладки |
| `_MossAmount` | Float | Количество «мха» (noise-based green tint) |

**Алгоритм фрагмента**:
1. Кирпичная кладка: `opRepeat` + `sdBox` для паттерна кирпичей (ряды со смещением)
2. Цвет кирпича: `_WallColor` + `valueNoise` × 0.05 (вариация)
3. Мох: `fbm(UV * 5)` × `_MossAmount` → зелёный тинт в нижней части (UV.y < 0.3)
4. Затемнение к верху (вертикальный gradient alpha)

**Стоимость**: Средняя (repeated SDF + fbm).

---

## 5. Шейдеры: Доска перегонки

### 5.1 SH_Ingredient

**Назначение**: Ингредиент (капля элемента) на доске перегонки.

Один шейдер с параметрами, определяющими тип элемента. Каждый ингредиент — quad ~80×80px.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_ElementType` | Int | 0=Ignis, 1=Aqua, 2=Toxin, 3=Lux, 4=Umbra |
| `_ColorPrimary` | Color | Основной цвет элемента |
| `_ColorSecondary` | Color | Вторичный цвет (для gradient) |
| `_DragOffset` | Vector2 | Смещение при перетаскивании (мировые координаты) |
| `_DragStretch` | Float | Степень деформации при drag (0–1) |
| `_Selected` | Float | Выбран ли (0–1), для подсветки |
| `_Dissolve` | Float | Прогресс растворения при потреблении (0–1) |
| `_IdlePhase` | Float | Начальная фаза idle-анимации (рандомизируется при создании) |
| `_ScalePulse` | Float | Амплитуда пульсации масштаба (0 = покой, 0.2 = пульсация) |

**Алгоритм фрагмента (общий)**:
1. UV трансформация: центрирование → применение drag-деформации (stretch в направлении `_DragOffset`)
2. Idle-покачивание: смещение UV на `sin(_GameTime * 2.0 + _IdlePhase) * 0.02`
3. Базовая форма: `sdCircle(UV, 0.35)` — все ингредиенты основаны на круге
4. Заполнение: зависит от `_ElementType` (см. ниже)
5. Glow: `glow(baseSDF, 0.3, 1.5)` × `_ColorPrimary`
6. Selected overlay: если `_Selected > 0` — яркий edge glow
7. Dissolve: `noise(UV * 8) < _Dissolve` → clip (discard fragment)

**Заполнение по типу элемента**:

| _ElementType | Внутренний паттерн |
|-------------|-------------------|
| 0 (Ignis) | Форма: `sdEquilateralTriangle` внутри круга (маска). Заполнение: `fbm(UV * 4 + _GameTime * 2, 3)` × color ramp (orange → gold → white). Мерцание: `turbulence` модулирует brightness |
| 1 (Aqua) | Форма: круг. Заполнение: горизонтальная волна `sin(UV.x * 8 + _GameTime * 3) * 0.05` смещает UV.y. Цвет: gradient top→bottom (white → light blue). Полоски волн: `step(sin(UV.y * 12 + _GameTime), 0.8)` |
| 2 (Toxin) | Форма: `sdCircle` с Perlin-distorted boundary (`domainWarp`). Пузырьки: 3-5 маленьких `sdCircle` с позициями, модулированными `sin(_GameTime * speed_i + phase_i)` по вертикали (всплывание). Цвет: acid green → dark green gradient |
| 3 (Lux) | Форма: `sdStar(UV, 0.3, 4, 0.5)`. Лучи: 4 `sdSegment` от центра наружу, длина пульсирует. Заполнение: solid bright yellow. Glow усилен (×3). Bloom contribution высокий |
| 4 (Umbra) | Форма: круг. Заполнение: спиральный UV warp (`opRotate(UV - 0.5, _GameTime * 1.5)`) → `gradientNoise`. Цвет: purple → magenta → dark. Anti-glow: `glow` с отрицательным вкладом (затемняет окружение) |

**GPU instancing**: Включён. Per-instance: `_ElementType`, `_ColorPrimary`, `_ColorSecondary`, `_DragOffset`, `_DragStretch`, `_Selected`, `_Dissolve`, `_IdlePhase`.

**Стоимость**: Средняя–Высокая (зависит от типа; Ignis/Umbra дороже за счёт fbm/warp). Оптимизация: octaves fbm = 2 на слабых GPU (define `LOW_QUALITY`).

### 5.2 SH_Brew

**Назначение**: Варево (промежуточный объект на доске). Визуально отличается от ингредиента — колба вместо капли.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_PotionColor` | Color | Цвет жидкости (определяется типом зелья) |
| `_Level` | Int | 1, 2 или 3 |
| `_FillHeight` | Float | Высота заполнения жидкостью (0.33, 0.66, 1.0 — привязана к уровню, анимируется при подпитке) |
| `_BubbleSpeed` | Float | Скорость пузырьков |
| `_GlowIntensity` | Float | Интенсивность свечения (увеличивается с уровнем) |
| `_SpawnBounce` | Float | Прогресс bounce-анимации при создании (1→0, ease-out-elastic) |
| `_CollectProgress` | Float | Прогресс анимации сбора в инвентарь (0→1) |

**Алгоритм фрагмента**:
1. **Силуэт колбы**: составной SDF:
    - Тело: `sdRoundedBox(UV - offset, float2(0.25, 0.35), 0.08)` — нижняя часть
    - Горлышко: `sdRoundedBox(UV - offset, float2(0.08, 0.1), 0.02)` — верхняя часть
    - Объединение: `opSmoothUnion(body, neck, 0.04)`
    - Пробка: `sdRoundedBox(UV - corkOffset, float2(0.1, 0.04), 0.02)` — отдельный SDF, другой цвет (коричневый)

2. **Жидкость**: маска = SDF колбы ∩ полуплоскость `UV.y < _FillHeight`
    - Верхняя граница жидкости: `_FillHeight + sin(UV.x * 12 + _GameTime * 4) * 0.015` (волна)
    - Цвет: `_PotionColor` с вертикальным gradient (светлее наверху)

3. **Пузырьки**: внутри маски жидкости, 3-4 шт:
    - Позиция каждого: `float2(hash(i) * width, fmod(_GameTime * _BubbleSpeed + hash(i+7), _FillHeight))`
    - Форма: `sdCircle(UV - bubblePos, 0.015)`
    - Alpha: затухает при приближении к поверхности

4. **Стекло колбы**: stroke SDF силуэта, цвет: белёсый (#FFFFFF, alpha 0.3). Specular highlight: яркая полоса слева (fake light reflection)

5. **Glow**: вокруг колбы: `glow(flaskSDF, _GlowIntensity * (0.5 + _Level * 0.3), 1.5)` × `_PotionColor`

6. **Индикация уровня**: под колбой — точки. 1-3 маленьких `sdCircle`, заполненных цветом зелья. Количество = `_Level`

7. **Bounce при создании**: `_SpawnBounce` модулирует scale UV (elastic ease: overshoot → settle)

**Стоимость**: Средняя (составной SDF, wave, пузырьки — всё аналитическое, без noise).

### 5.3 SH_BoardBackground

**Назначение**: Фон доски перегонки (за ингредиентами и варевами).

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_GridColor` | Color | Цвет линий сетки (#2A2A45, alpha 0.5) |
| `_CellSize` | Float | Размер ячейки в UV |
| `_EmptyCellHighlight` | Float4[16] | Массив: подсветка каждой из 16 ячеек (0=нет, 1=валидная цель, -1=невалидная) |

**Алгоритм фрагмента**:
1. Сетка: `step(fmod(UV.x, _CellSize), lineWidth)` + `step(fmod(UV.y, _CellSize), lineWidth)` → тонкие линии
2. Фон ячеек: тёмный с лёгким noise
3. Подсветка: per-cell overlay на основе `_EmptyCellHighlight` (зелёный для валидных, красный для невалидных, пульсация)

**Передача массива**: `Material.SetFloatArray` (16 значений). Обновляется каждый кадр при drag.

**Стоимость**: Низкая.

---

## 6. Шейдеры: Сущности (Игрок и Враги)

### 6.0 Общая архитектура шейдеров сущностей

Все сущности используют **один мастер-шейдер** `SH_Entity` с define-ами для каждого типа. Причина: общие механики (flash при ударе, dissolve при смерти, намерение, idle-анимация).

Альтернативный подход: **отдельные шейдеры** для каждого типа с общим include. Рекомендуется второй — лучше читаемость, проще отладка. Общие функции — в `EntityCommon.cginc`.

### EntityCommon.cginc

Общие uniform-ы и функции для всех шейдеров сущностей:

| Uniform | Тип | Описание |
|---------|-----|----------|
| `_FlashAmount` | Float | Белая вспышка при получении урона (0–1, быстрый decay) |
| `_DissolveProgress` | Float | Прогресс dissolve при смерти (0–1) |
| `_DissolveEdgeWidth` | Float | Ширина светящегося края dissolve |
| `_DissolveEdgeColor` | Color | Цвет края dissolve (обычно жёлто-белый) |
| `_HitDirection` | Vector2 | Направление knockback (для squash-деформации) |
| `_HitSquash` | Float | Интенсивность squash (0–1, decay) |
| `_Opacity` | Float | Общая непрозрачность (для fade in/out) |

Общие функции:

| Функция | Описание |
|---------|----------|
| `applyFlash(color, amount)` | Lerp к белому: `lerp(color, white, amount)` |
| `applyDissolve(uv, progress, edgeWidth)` | Noise threshold mask → discard + emission edge |
| `applySquash(uv, direction, amount)` | Деформация UV: сжатие в направлении удара, расширение перпендикулярно |
| `idleBob(uv, time, phase, amplitude)` | Покачивание: вертикальное смещение UV |

### 6.1 SH_Player

**Назначение**: Персонаж игрока.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_BodyColor` | Color | Цвет плаща (по умолчанию нейтральный серо-голубой) |
| `_LastPotionColor` | Color | Цвет последнего использованного зелья (для flow noise) |
| `_MoveDirection` | Vector2 | Направление текущего движения (для squash & stretch) |
| `_MoveProgress` | Float | Прогресс перемещения (0–1) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Голова**: `sdCircle(UV - headOffset, 0.15)`. Два глаза: `sdCircle` × 2 (белые точки). Idle: лёгкое покачивание головы
2. **Плащ (тело)**: `sdEquilateralTriangle(UV - bodyOffset, 0.35)`, перевёрнутый (вершина вниз). Нижний край: `domainWarp(UV.x, sin)` для волнистости
3. **Flow noise заполнение**: внутри плаща: `fbm(UV * 3 + _GameTime * float2(0.5, 0.3), 2)`. Цвет: `lerp(_BodyColor, _LastPotionColor, noiseValue)`. При отсутствии бросков — `_BodyColor` = серый
4. **Объединение**: `opUnion(head, cloak)` для финального SDF
5. **Glow**: мягкий glow цвета `_LastPotionColor` вокруг персонажа

**Анимация движения**: при `_MoveProgress > 0`:
- Squash & stretch в направлении `_MoveDirection`
- Плащ trailing: нижний край плаща отстаёт (UV.y offset на нижних пикселях)

**Стоимость**: Средняя (fbm 2 октавы + составной SDF).

### 6.2 SH_Skeleton

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_EyePulsePhase` | Float | Фаза мерцания глаз |
| `_IntentionIcon` | Int | Тип намерения (0=idle, 1=move, 2=attack) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Тело**: `sdRoundedBox(UV, float2(0.15, 0.25), 0.03)` — вертикальный прямоугольник
2. **Голова**: `sdCircle(UV - float2(0, 0.25), 0.12)`
3. **Рёбра**: 3 горизонтальных `sdSegment` внутри тела (декоративные, тонкие)
4. **Глаза**: 2 × `sdCircle(UV - eyePos, 0.025)`. Alpha: `pulse(_GameTime, 2.0 + _EyePulsePhase, 2.0)`. Цвет: жёлто-белый
5. **Цвет тела**: `_StoneColor` (#F5E6C8) + `valueNoise(UV * 10) * 0.05` для вариации
6. **Намерение**: `_IntentionIcon` определяет маленький символ над головой (⚔️ = скрещённые `sdSegment`)
7. **Idle**: покачивание через `idleBob`

**Стоимость**: Низкая (аналитические SDF, без noise кроме вариации).

### 6.3 SH_Spider

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_LegPhaseOffset` | Float[8] | Фазы анимации каждой из 8 ног |
| `_AttackLineDir` | Int | Направление атаки (0=нет, 1=horizontal, 2=vertical) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Тело**: `sdEllipse(UV, float2(0.2, 0.14))` — горизонтальный овал
2. **Глаза**: кластер из 4 красных точек (2×2), маленькие `sdCircle`
3. **Ноги (×8)**: по 4 с каждой стороны. Каждая нога — `sdSegment(bodyEdge, tipPos)`.
    - `tipPos.x = bodyEdge.x ± legLength * cos(angle_i)`
    - `tipPos.y = bodyEdge.y + sin(_GameTime * 3.0 + _LegPhaseOffset[i]) * 0.03`
    - Угол каждой ноги: равномерно распределён (от 20° до 70° от горизонтали)
4. **Цвет**: тёмно-серый (#424242). Ноги: чуть светлее (#5E5E5E)
5. **Паутинная нить** (при атаке): если `_AttackLineDir > 0`, отрисовка тонкой линии от тела в направлении атаки (SDF линия, пульсирующая alpha, белёсый цвет)

**Стоимость**: Средняя (8 SDF-сегментов + sin анимация).

### 6.4 SH_MushroomBomb

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Timer` | Int | Оставшиеся ходы до взрыва (3, 2, 1) |
| `_TimerNormalized` | Float | 0 = свежий, 1 = вот-вот взорвётся |
| `_PulseSpeed` | Float | Скорость пульсации (увеличивается с таймером) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Шляпка**: `sdCircle(UV - float2(0, 0.1), 0.22)` ∩ полуплоскость `UV.y > 0` → полукруг
2. **Ножка**: `sdRoundedBox(UV - float2(0, -0.15), float2(0.07, 0.15), 0.02)`
3. **Пятнышки**: 3-4 маленьких `sdCircle` на шляпке, позиции фиксированные (hash-based). Цвет: чуть светлее тела
4. **Цвет тела**: `lerp(yellow, red, _TimerNormalized)` — цветовой ramp жёлтый→оранжевый→красный
5. **Пульсация**: `scale = 1.0 + sin(_GameTime * _PulseSpeed) * 0.05 * _TimerNormalized`. UV масштабируется. `_PulseSpeed = lerp(3.0, 15.0, _TimerNormalized)`
6. **Число таймера**: цифра `_Timer` над головой. SDF-рисование цифры (7-segment display из `sdSegment` ×7)
7. **Glow при близком взрыве**: если `_TimerNormalized > 0.66` — усиленный glow красного цвета

**Стоимость**: Низкая (аналитические SDF, lerp, sin).

### 6.5 SH_MagnetGolem

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_PullDirection` | Vector2 | Нормализованное направление к игроку (для волн притяжения) |
| `_PullActive` | Float | Активно ли притяжение (0–1, для анимации волн) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Тело**: `sdRoundedBox(UV, float2(0.28, 0.28), 0.06)` — большой квадрат (крупнее остальных врагов)
2. **Текстура тела**: `voronoi(UV * 5)` → distance → metallic pattern. Цвет: серый (#78909C) с highlight на границах ячеек
3. **Глаза**: 2 × `sdCircle`, цвет: оранжевый, glow
4. **Волны притяжения** (когда `_PullActive > 0`): 3 концентрических кольца (`sdRing`), движущихся **от голема к краям** (но визуально — как будто тянет к себе):
    - Позиция каждого кольца: `radius = fmod(_GameTime * 1.5 + i * 0.33, 1.0) * maxRadius`
    - Alpha: убывает с radius
    - Цвет: оранжевый (#FF6B1A), аддитивный blend
    - Кольца ориентированы в `_PullDirection` (не полные круги, а дуги ~120°)
5. **Idle**: тело не покачивается (массивный), только пульсация glow глаз

**Стоимость**: Средняя (voronoi + 3 ring SDF).

### 6.6 SH_MirrorSlime

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_BlobPositions` | Vector4[2] | Позиции 4 blob-ов (xy + xy) в UV-пространстве |
| `_CopiedPotionColor` | Color | Цвет скопированного зелья (или нейтральный) |
| `_HueShiftSpeed` | Float | Скорость переливания HSV |
| `_ChromaticAmount` | Float | Интенсивность хроматической аберрации (0 в покое, 1 при копировании) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Метаболлы**: 3-4 `sdCircle` с позициями из `_BlobPositions`. Анимация позиций: медленное перемещение (синусоиды с разными фазами и частотами, driven C#-скриптом)
2. **Smooth union**: `opSmoothUnion` всех blob-ов с k=0.15 → аморфная форма
3. **Цвет**: если `_CopiedPotionColor` задан — этот цвет. Иначе — HSV hue shift: `hsvToRgb(fmod(_GameTime * _HueShiftSpeed, 1.0), 0.7, 0.9)`
4. **Хроматическая аберрация** (при копировании): сдвинуть UV на `±_ChromaticAmount * 0.01` для R, G, B каналов раздельно → три чуть разных выборки → собрать в RGB
5. **Glow**: переливающийся, цвет привязан к текущему hue

**Стоимость**: Средняя (4 SDF + smooth union + возможная хром. аберрация).

### 6.7 SH_ArmoredBeetle

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_ArmorPoints` | Int | Текущие очки брони (0, 1, 2) |
| `_ShieldFlash` | Float | Вспышка щита при блокировке урона (0–1) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Тело**: `sdHexagon(UV, 0.25)` — шестиугольник
2. **Цвет тела**: тёмно-серый (#37474F) + specular highlight: `pow(max(dot(normalize(UV - 0.5), float2(-0.5, 0.7)), 0), 8) * 0.3` (fake directional light)
3. **Броня-индикатор**: `_ArmorPoints` маленьких ромбов (`sdBox` повёрнутый на 45°) над головой. Заполненные = яркие, потерянные = тусклые/отсутствуют
4. **Shield flash**: при `_ShieldFlash > 0` — белая полупрозрачная оболочка вокруг тела (stroke SDF, alpha = `_ShieldFlash`)
5. **Глаза**: 2 красных точки
6. **Legs hint**: 6 коротких `sdSegment` от граней шестиугольника (декоративные, статичные)

**Стоимость**: Низкая (hexagon SDF + specular = аналитические).

### 6.8 SH_Phantom

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_VisibilityFlicker` | Float | Текущая непрозрачность (sin oscillation, 0.3–0.8) |
| `_TrailPositions` | Vector4[2] | Предыдущие позиции (для trail-эффекта): xy и xy |
| `_TrailAlphas` | Vector2 | Alpha каждого trail-элемента |
| `_TeleportFlash` | Float | Вспышка при телепортации (0–1) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Trail**: для каждой из 2 предыдущих позиций — `sdCircle` со смещённым центром и убывающей alpha. Цвет: тот же, но более блёклый
2. **Основное тело**: `sdCircle(UV, 0.18)`. Цвет: белёсый (#E0E0E0) с голубым отливом. Alpha: `_VisibilityFlicker`
3. **Flicker**: `_VisibilityFlicker = lerp(0.3, 0.8, sin(_GameTime * 5.0 + hash) * 0.5 + 0.5)` — driven C#-скриптом
4. **Телепорт-вспышка**: при `_TeleportFlash > 0` — ring shockwave (расширяющееся кольцо) от центра, белый, аддитивный, быстрый decay
5. **Глаза**: 2 маленькие точки, постоянная яркость (контраст с мерцающим телом)

**Стоимость**: Низкая (несколько circle SDF + alpha модуляция).

### 6.9 SH_Necromancer

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_VortexSpeed` | Float | Скорость вихря вокруг тела |
| `_VortexIntensity` | Float | Интенсивность вихря (увеличивается при воскрешении) |
| `_RuneScroll` | Float | Скорость прокрутки рун |
| `_ResurrectFlash` | Float | Вспышка при воскрешении (0–1) |
| Общие Entity uniforms | — | — |

**Алгоритм фрагмента**:
1. **Капюшон**: `sdEquilateralTriangle(UV - float2(0, 0.2), 0.2)` — треугольник вершиной вверх
2. **Тело**: `sdRoundedBox(UV - float2(0, -0.1), float2(0.13, 0.2), 0.03)`
3. **Объединение**: `opSmoothUnion(hood, body, 0.05)`
4. **Цвет**: фиолетовый (#6A1B9A)
5. **Руны**: внутри тела — паттерн «рун». Реализация: `step(sin(UV.y * 20 + _GameTime * _RuneScroll), 0.9) * step(sin(UV.x * 15), 0.85)` → сетка горизонтальных и вертикальных штрихов, прокручивающихся. Цвет: светло-фиолетовый, alpha 0.3
6. **Вихрь**: за пределами тела (radius > body SDF). `opRotate(UV - center, _GameTime * _VortexSpeed)` → `gradientNoise(rotatedUV * 4)` → цветовой overlay (фиолетовый, аддитивный). Маска: annular zone вокруг тела
7. **Resurrect flash**: при `_ResurrectFlash > 0` — вихрь усиливается, glow ×3, вспышка зелёного цвета (символ жизни)

**Стоимость**: Средняя (noise для вихря + прокрутка рун).

### 6.10 SH_Boss_SpiderQueen, SH_Boss_IronGolem, SH_Boss_AlchemistRenegade

Боссы — увеличенные версии с более детальными SDF-конструкциями. Занимают 2×2 клетки (quad в 4 раза больше обычного врага). Уникальные параметры:

| Босс | Ключевые отличия от базовых врагов |
|------|------------------------------------|
| Королева пауков | 12 ног (вместо 8). Корона: `sdStar` на голове. Более детальные глаза (кластер 6 вместо 4). Абдомен: второй овал (задняя часть). Цвет: тёмно-бордовый (#4A0404) |
| Железный голем | Тело: `sdRoundedBox` × 3 (торс + 2 руки). Voronoi текстура крупнее. Гвозди/заклёпки: массив `sdCircle` по контуру. Цвет: тёмно-стальной (#263238). Броня-индикатор: 6 ромбов |
| Алхимик-Отступник | Человеческие пропорции. Капюшон: треугольник. Плащ: как у игрока, но с другим flow noise (хаотичный, мультицветный). Посох: `sdSegment` + `sdCircle` (навершие с glow). Котлы рядом: отдельные объекты со своим шейдером (SH_Cauldron) |

Каждый босс имеет **2 варианта параметров** (фаза 1 и фаза 2), переключаемые через uniform `_Phase` (0 или 1). Визуальные изменения при смене фазы: morph-анимация (lerp параметров за 0.5 сек).

**Стоимость**: Высокая (сложные составные SDF). Допустимо: боссы — единичные объекты, нет батчинга.

---

## 7. Шейдеры: Зелья (инвентарь)

### 7.1 SH_PotionSlot

**Назначение**: Зелье в слоте инвентаря (UI-элемент, но рендерится шейдером).

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_PotionColor` | Color | Цвет зелья |
| `_PotionType` | Int | Тип зелья (для декоративного символа внутри колбы) |
| `_Level` | Int | 1–3 |
| `_CooldownNormalized` | Float | 0 = готово, 1 = полный кулдаун |
| `_CooldownTurns` | Int | Число ходов до готовности (для отображения цифры) |
| `_Selected` | Float | Выбрано ли для броска (0–1) |
| `_Empty` | Float | Пустой слот (1 = пустой) |

**Алгоритм фрагмента**:
1. **Если пустой**: только контур колбы (`stroke(flaskSDF, 0.01, 0.005)`), цвет: тусклый серый
2. **Колба**: такая же конструкция, как `SH_Brew` (тело + горлышко + пробка), но меньше и стилизованнее
3. **Жидкость**: заполнение 100%. Волна на поверхности. Цвет: `_PotionColor`
4. **Кулдаун-оверлей**: если `_CooldownNormalized > 0`:
    - Тёмный overlay на колбе (`lerp(color, darkColor, 0.6)`)
    - «Иней»: `valueNoise(UV * 15) * _CooldownNormalized * 0.3` → белёсый overlay
    - Цифра `_CooldownTurns` по центру (7-segment SDF)
    - Замок: маленький `sdRoundedBox` + `sdCircle` (иконка замка) над колбой
5. **Selected**: если `_Selected > 0`:
    - Яркий контур (stroke, цвет: белый, пульсация)
    - Scale up ×1.1 (UV масштаб)
    - Glow ×2
6. **Точки уровня**: под колбой — `_Level` маленьких заполненных кругов
7. **Декоративный символ**: внутри жидкости — полупрозрачный символ зелья (упрощённый SDF: огонь = треугольник, молния = зигзаг, кислота = капля и т.д.)

**Стоимость**: Низкая–Средняя.

---

## 8. Шейдеры: Зоны на поле

Зоны — полупрозрачные overlays на тайлах пола. Каждый тип зоны — отдельный шейдер (или один шейдер с define).

### 8.1 SH_Zone_Fire

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Intensity` | Float | Интенсивность (1.0 при создании, fade out к исчезновению) |
| `_LifetimeNormalized` | Float | 0 = свежая, 1 = вот-вот исчезнет |

**Алгоритм фрагмента**:
1. Базовая форма: заполняет всю клетку (UV 0→1)
2. `fbm(UV * float2(3, 6) + float2(0, -_GameTime * 2), 3)` — шум, вытянутый вертикально, движущийся вверх (как пламя)
3. Color ramp: `noiseValue` → текстурная выборка из gradient ramp (чёрный → красный → оранжевый → жёлтый → белый)
4. Alpha: `noiseValue * _Intensity * smoothstep(0, 0.1, UV.y)` — затухает к нижнему краю
5. Blend: аддитивный (`Blend SrcAlpha One`)
6. Edge: по периметру клетки — мягкий fade (`smoothstep` от краёв)

**Стоимость**: Средняя (fbm 3 октавы). Но на слабых GPU: 2 октавы (define `LOW_QUALITY`).

### 8.2 SH_Zone_Water

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Intensity` | Float | Интенсивность |
| `_WaveSpeed` | Float | Скорость волн |

**Алгоритм фрагмента**:
1. Базовый цвет: голубой (#4FC3F7), alpha 0.3
2. Волны: `sin(UV.x * 10 + _GameTime * _WaveSpeed) * sin(UV.y * 8 + _GameTime * _WaveSpeed * 0.7)` → модуляция alpha и brightness
3. Каустика (опционально, дорого): `voronoi(UV * 5 + _GameTime * 0.5)` → distance → bright caustic lines. Fallback: просто волны
4. Edge fade
5. Blend: alpha blending

**Стоимость**: Низкая (sin + sin) или Средняя (с каустикой).

### 8.3 SH_Zone_Poison

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Intensity` | Float | Интенсивность |
| `_BubblePhase` | Float | Фаза пузырьков |

**Алгоритм фрагмента**:
1. Базовый цвет: зелёный (#76FF03), alpha 0.25
2. Warp: `domainWarp(UV, 0.05, _GameTime * 0.5)` → искажённая граница
3. Пузырьки: 5-7 маленьких `sdCircle` со всплывающей анимацией (`fmod(_GameTime * speed + phase, 1.0)` для Y). Яркие, alpha 0.6
4. Edge fade
5. Blend: alpha blending

**Стоимость**: Средняя (domainWarp + пузырьки).

### 8.4 SH_Zone_Ice

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Intensity` | Float | Интенсивность |
| `_CrackProgress` | Float | Прогресс формирования трещин (0→1, анимируется при создании) |

**Алгоритм фрагмента**:
1. Базовый цвет: бело-голубой (#E0F7FA), alpha 0.4
2. Трещины: `voronoi(UV * 6)` → edge distance → `step(edgeDist, 0.05 * _CrackProgress)` → тёмные линии трещин
3. Блеск: `pow(voronoi_cellDist, 3) * 0.3` → specular highlights на «гранях» льда
4. Crack propagation animation: `_CrackProgress` увеличивается от центра к краям (radial mask × `_CrackProgress`)
5. Edge fade

**Стоимость**: Средняя (voronoi).

---

## 9. Шейдеры: Визуальные эффекты зелий

Эффекты применения зелий — кратковременные overlay-объекты, отрисовывающиеся на слое Effects.

### 9.1 SH_FX_Explosion

**Назначение**: Универсальный эффект взрыва/удара. Используется для огня, вспышки, напалма.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Color1` | Color | Внутренний цвет (яркий, центр) |
| `_Color2` | Color | Внешний цвет (тёмный, край) |
| `_Progress` | Float | Прогресс анимации (0→1, ~0.4 сек) |
| `_NoiseScale` | Float | Масштаб noise для формы |
| `_Shape` | Int | 0=круг, 1=крест, 2=линия |
| `_AoeSize` | Vector2 | Размер AoE в клетках (для масштабирования quad-а) |

**Алгоритм фрагмента**:
1. Форма: зависит от `_Shape`:
    - Круг: `sdCircle(UV, radius)`
    - Крест: `min(sdBox(UV, float2(wide, narrow)), sdBox(UV, float2(narrow, wide)))`
    - Линия: `sdBox(UV, float2(wide, narrow))`
2. Radius/size анимируется: `progress < 0.3` → expand (0→max), `progress > 0.3` → fade
3. Edge: `fbm(UV * _NoiseScale + _GameTime, 2)` → domain warp на SDF boundary (рваный край)
4. Цвет: `lerp(_Color1, _Color2, smoothstep(0, boundary, sdf))` — цветовой gradient от центра к краю
5. Alpha: `(1.0 - _Progress) * fill(sdf)` — общий fade out
6. Blend: аддитивный

**Стоимость**: Средняя (fbm 2 октавы + SDF). Кратковременный (0.4 сек) — пиковая нагрузка, но допустимая.

### 9.2 SH_FX_Lightning

**Назначение**: Эффект молнии (зелье Молния).

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_StartPos` | Vector2 | Начальная точка (UV или world → UV) |
| `_EndPos` | Vector2 | Конечная точка |
| `_Progress` | Float | 0→1, ~0.3 сек |
| `_BranchCount` | Int | Число ответвлений |
| `_JaggedAmount` | Float | Амплитуда ломаности |
| `_Thickness` | Float | Толщина основной линии |
| `_Color` | Color | Цвет (ярко-белый/голубой) |

**Алгоритм фрагмента**:
1. **Основная линия**: параметрическая кривая от `_StartPos` к `_EndPos`:
    - 8 сегментов. Для каждого вертекса: perpendicular offset = `hash21(segmentIndex + floor(_GameTime * 60)) * _JaggedAmount`
    - SDF до полилинии: для каждого UV-пиксела → найти ближайший сегмент → distance
    - Offset пересчитывается каждый кадр → молния «дрожит»
2. **Ответвления**: 2–3 sub-линии, отходящие от случайных точек основной, более тонкие, короткие
3. **Fill + Glow**: `fill(lineSDF, 0.003)` (тонкая яркая линия) + `glow(lineSDF, 1.5, 1.2)` (широкое свечение)
4. **Цвет**: основная линия — почти белая. Glow — голубой
5. **Alpha**: `1.0 - _Progress` (fade out). Вспышка в начале: `_Progress < 0.05` → fullscreen flash contribution
6. **Blend**: аддитивный

**Стоимость**: Средняя–Высокая (полилиния distance — цикл в шейдере). Оптимизация: максимум 8 сегментов, развёрнутый цикл (unrolled loop).

### 9.3 SH_FX_Acid

**Назначение**: Эффект кислоты (зелье Кислота).

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_BlobCount` | Int | Число blob-ов (5–7) |
| `_BlobPositions` | Vector4[4] | Позиции blob-ов (xy пары, packed) |
| `_BlobRadii` | Float[8] | Радиусы (анимируются, расширяются) |
| `_Progress` | Float | 0→1 |
| `_Color` | Color | Зелёный |

**Алгоритм фрагмента**:
1. Для каждого blob-а: `sdCircle(UV - blobPos[i], blobRadii[i])`
2. `opSmoothUnion` всех blob-ов (k=0.1) → метабольная форма, капли сливаются
3. Анимация: blob-ы расширяются и сливаются (radii увеличиваются, позиции расходятся)
4. Цвет: `_Color` + бледная кромка (emission edge на SDF)
5. Пузырьки: внутри формы, всплывают
6. Alpha: fade out к концу `_Progress`

**Стоимость**: Средняя (N smooth union — развёрнутый цикл).

### 9.4 SH_FX_Heal

**Назначение**: Эффект лечения (зелье Спора, пропуск хода).

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Progress` | Float | 0→1 |
| `_Color` | Color | Зелёный для Споры, белый для пропуска |

**Алгоритм фрагмента**:
1. Восходящие частицы: 8–12 маленьких `sdCircle` с позициями:
    - X: `hash(i) * width`
    - Y: `fmod(_GameTime * speed + hash(i + 5), height)` — зацикленное всплывание
2. Каждая частица: glow + trail (2 ghost-а ниже с убывающей alpha)
3. Цвет: `_Color` с вариациями яркости per-particle
4. Blend: аддитивный

**Стоимость**: Низкая (12 circle SDF).

### 9.5 SH_FX_Steam

**Назначение**: Эффект пара (зелье Пар, combo Огонь+Лёд).

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Progress` | Float | 0→1 (длительный, ~1 сек) |
| `_Density` | Float | Плотность тумана |

**Алгоритм фрагмента**:
1. `fbm(UV * 3 + float2(_GameTime * 0.3, _GameTime * 0.5), 3)` → cloud-like noise
2. Цвет: белёсый (#FFFFFF), alpha: `noise * _Density * (1.0 - _Progress)`
3. Expansion: UV масштабируется с `_Progress` (облако расширяется)
4. Мягкие края: radial gradient mask
5. Blend: alpha blending (не аддитивный — пар непрозрачный)

**Стоимость**: Средняя (fbm 3 октавы).

### 9.6 SH_FX_Shockwave

**Назначение**: Ring shockwave при merge, combo, сильных ударах.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Progress` | Float | 0→1, ~0.3 сек |
| `_Color` | Color | Цвет кольца |
| `_MaxRadius` | Float | Максимальный радиус |
| `_Thickness` | Float | Толщина кольца |

**Алгоритм фрагмента**:
1. `radius = _Progress * _MaxRadius`
2. `sdRing(UV - 0.5, radius, _Thickness * (1.0 - _Progress))` — кольцо расширяется, толщина уменьшается
3. Alpha: `(1.0 - _Progress) * fill(ringSDF)`
4. Цвет: `_Color`, аддитивный blend
5. Distortion (опционально): кольцо слегка искажает UV объектов за ним (реализуется через GrabPass — дорого; альтернатива: просто визуальный overlay)

**Стоимость**: Низкая (1 ring SDF).

### 9.7 SH_FX_DamageNumber

**Назначение**: Числа урона, всплывающие над врагами.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Number` | Int | Число (1–99) |
| `_Color` | Color | Цвет (белый = обычный урон, красный = критический, зелёный = лечение) |
| `_Progress` | Float | 0→1 (0.6 сек, bounce ease) |
| `_IsCrit` | Float | 1 = увеличенный размер + screenshake |

**Алгоритм фрагмента**:
1. Разбиение числа на цифры: единицы и десятки
2. Каждая цифра: 7-segment display из `sdSegment` × 7 (определённые сегменты включены/выключены по таблице)
3. Позиция: bounce вверх: `y = bounceEase(_Progress) * maxHeight`
4. Alpha: `1.0` до 70% прогресса, затем fade out
5. Scale: если `_IsCrit` — ×1.5 + лёгкая пульсация
6. Outline: тонкий тёмный контур (stroke SDF) для читаемости на любом фоне

**Стоимость**: Низкая (7 segment SDF).

---

## 10. Шейдеры: Combo-эффекты

Combo-эффекты — это **усиленные версии** базовых эффектов + уникальные визуальные элементы. Реализуются как **overlay-слой поверх базового эффекта**.

### 10.1 SH_FX_ComboOverlay

**Назначение**: Общий overlay для всех combo. Появляется поверх конкретного combo-эффекта.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Progress` | Float | 0→1 |
| `_ComboColor` | Color | Доминирующий цвет combo |

**Алгоритм фрагмента**:
1. Расходящиеся лучи: `atan2(UV.y - 0.5, UV.x - 0.5)` → angular pattern → `step(sin(angle * 12 + _GameTime * 10), 0.7)` → rotating rays
2. Радиальный gradient: ярче к центру
3. Alpha: `(1.0 - _Progress)`, аддитивный blend
4. Текст «COMBO»: отдельный объект с SDF-text (или fallback: UI Text поверх)

### 10.2 Специфичные combo (параметры базовых FX-шейдеров)

| Combo | Визуальная модификация |
|-------|----------------------|
| Молния + Вода | SH_FX_Lightning с увеличенным `_BranchCount` (×2) + все клетки воды подсвечиваются flash (SH_Zone_Water `_Intensity` spike) |
| Огонь + Яд | SH_FX_Explosion с увеличенным `_AoeSize` + SH_Zone_Poison расширяется (spawn новых quad-ов зон) |
| Поток + Лёд | SH_Zone_Ice `_CrackProgress` быстрая анимация на всех клетках воды + blue flash |
| Огонь + Лёд | SH_FX_Steam с увеличенной `_Density` + SH_FX_Shockwave (thermal shock) |
| Молния + Огонь | SH_FX_Explosion (огненный) + SH_FX_Lightning (наложение) — оба одновременно |

---

## 11. Шейдеры: Dissolve (смерть)

### 11.1 SH_Dissolve

Не отдельный шейдер, а **функция в EntityCommon.cginc**, вызываемая во всех шейдерах сущностей.

**Механизм**: noise threshold mask.

| Параметр | Описание |
|----------|----------|
| `_DissolveProgress` | 0 = целый, 1 = полностью растворён |
| `_DissolveEdgeWidth` | Ширина светящегося края (0.05) |
| `_DissolveEdgeColor` | Цвет края (жёлто-белый по умолчанию) |

**Алгоритм**:
1. `noiseValue = valueNoise(UV * 8 + objectID)` — noise per-pixel (objectID для уникальности паттерна)
2. `clip(noiseValue - _DissolveProgress)` — пиксели с noise < progress → discard
3. Edge emission: `edgeMask = smoothstep(_DissolveProgress - _DissolveEdgeWidth, _DissolveProgress, noiseValue)` → multiply by `_DissolveEdgeColor` → add to output

**Результат**: объект «сгорает» с яркой каймой, от краёв к центру (или хаотично, в зависимости от noise).

---

## 12. Шейдеры: UI

### 12.1 SH_UI_Glow

**Назначение**: Кнопки и интерактивные UI-элементы с мягким свечением.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_ButtonColor` | Color | Цвет кнопки |
| `_GlowColor` | Color | Цвет свечения |
| `_GlowPulse` | Float | Амплитуда пульсации (0 = статика) |
| `_Pressed` | Float | Нажата ли (0–1, для scale-анимации) |
| `_CornerRadius` | Float | Радиус скругления углов |

**Алгоритм фрагмента**:
1. `sdRoundedBox(UV - 0.5, size, _CornerRadius)` → заполнение `_ButtonColor`
2. Glow: `glow(sdf, intensity + pulse, falloff)` × `_GlowColor`, аддитивный
3. Pressed: уменьшение scale (UV × 0.95), затемнение цвета
4. Highlight: тонкая белая полоса по верхнему краю (specular fake)

### 12.2 SH_UI_HealthHeart

**Назначение**: Сердечко HP.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Filled` | Float | 0 = пустое (контур), 1 = заполненное |
| `_DamagePulse` | Float | Пульсация при потере HP |

**Алгоритм фрагмента**:
1. Форма сердца: `sdHeart(UV)` — два `sdCircle` (верхние доли) + `sdTriangle` (нижняя часть), `opSmoothUnion`
2. Заполнение: если `_Filled > 0.5` → красный (#FF1744), иначе → тёмно-серый контур (stroke)
3. Damage pulse: при `_DamagePulse > 0` → scale throb + red flash

### 12.3 SH_UI_IntentionIcon

**Назначение**: Иконки намерений врагов (⚔️ 🛡 📢 🚶).

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_IconType` | Int | 0=attack, 1=shield, 2=summon, 3=move |
| `_PulseSpeed` | Float | Скорость пульсации |
| `_Color` | Color | Цвет иконки (красный для атаки, жёлтый для щита и т.д.) |

**Алгоритм фрагмента по типам**:
- Attack (⚔️): два `sdSegment` крест-накрест + два маленьких `sdTriangle` (навершия)
- Shield (🛡): `sdRoundedBox` с закруглённым верхом + крест внутри
- Summon (📢): `sdTriangle` (рупор) + 3 дуги (`sdArc`) — звуковые волны
- Move (🚶): стрелка — `sdTriangle` + `sdBox` (стержень)

**Стоимость**: Минимальная.

---

## 13. Шейдеры: Пост-процессинг

Пост-процессинг реализуется через **скрипт на камере** с `OnRenderImage(RenderTexture src, RenderTexture dst)` и `Graphics.Blit(src, dst, material)`. Каждый эффект — отдельный шейдер-pass.

### Порядок пассов

```
Rendered Scene
    │
    ▼
[Pass 1: Bloom — Extract]  → brightTexture
    │
    ▼
[Pass 2: Bloom — Blur H]   → blurH (из brightTexture)
    │
    ▼
[Pass 3: Bloom — Blur V]   → blurV (из blurH)
    │
    ▼
[Pass 4: Bloom — Combine]  → scene + blurV
    │
    ▼
[Pass 5: Vignette + ChromaticAberration + Flash]  → final
    │
    ▼
Screen
```

### 13.1 SH_PP_BloomExtract

**Pass 1**: Извлечение ярких пикселей.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_Threshold` | Float | Порог яркости (0.8) |
| `_SoftKnee` | Float | Мягкость перехода (0.5) |

**Алгоритм**:
1. Sample `_MainTex`
2. `brightness = max(r, max(g, b))`
3. `contribution = smoothstep(_Threshold - _SoftKnee, _Threshold + _SoftKnee, brightness)`
4. Output: `color * contribution`

### 13.2 SH_PP_BloomBlur

**Pass 2 и 3**: Dual-pass Kawase blur (дешевле Gaussian, хорошо для мобильных).

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_TexelSize` | Vector2 | `1.0 / textureResolution` |
| `_BlurOffset` | Float | Offset для Kawase (1.0 для первого pass, 2.0 для второго) |

**Алгоритм (Kawase downsample)**:
1. Sample 5 точек: центр + 4 угла (со смещением `_BlurOffset * _TexelSize`)
2. Среднее арифметическое

Для более сильного blur — можно добавить 3-й и 4-й pass с увеличивающимся `_BlurOffset` и уменьшенным render target (½ или ¼ разрешения).

### 13.3 SH_PP_BloomCombine

**Pass 4**: Комбинирование оригинала и blur.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_BloomTex` | Sampler2D | Текстура после blur |
| `_BloomIntensity` | Float | Интенсивность bloom (1.5) |

**Алгоритм**:
1. `original = tex2D(_MainTex, uv)`
2. `bloom = tex2D(_BloomTex, uv) * _BloomIntensity`
3. Output: `original + bloom` (аддитивное, но clamp to 1)

### 13.4 SH_PP_Final

**Pass 5**: Виньетка + хроматическая аберрация + flash.

| Параметр | Тип | Описание |
|----------|-----|----------|
| `_VignetteIntensity` | Float | 0.3 |
| `_VignetteSmoothness` | Float | 0.5 |
| `_ChromaticAmount` | Float | 0 в покое, 0.005–0.01 при эффекте. Driven скриптом |
| `_FlashColor` | Color | Цвет полноэкранной вспышки (white или red). Alpha = интенсивность |
| `_DamageVignetteAmount` | Float | Красная виньетка при получении урона (0–0.5) |

**Алгоритм**:
1. **Хроматическая аберрация** (если `_ChromaticAmount > 0`):
    - `r = tex2D(_MainTex, uv + float2(_ChromaticAmount, 0)).r`
    - `g = tex2D(_MainTex, uv).g`
    - `b = tex2D(_MainTex, uv - float2(_ChromaticAmount, 0)).b`
    - Иначе: обычная выборка

2. **Виньетка**:
    - `dist = distance(uv, float2(0.5, 0.5))`
    - `vignette = smoothstep(0.5, 0.5 - _VignetteSmoothness, dist) * _VignetteIntensity`
    - `color *= (1.0 - vignette)`

3. **Damage vignette** (если `_DamageVignetteAmount > 0`):
    - Та же формула, но с красным цветом: `color = lerp(color, red, damageVignette * _DamageVignetteAmount)`

4. **Flash**:
    - `color = lerp(color, _FlashColor.rgb, _FlashColor.a)`

**Стоимость**: Низкая (texture samples + math).

---

## 14. Управление параметрами из C#

### 14.1 Архитектура ShaderController

Один центральный MonoBehaviour `ShaderGlobalController`, прикреплённый к камере:

**Обязанности**:
- Обновление `_GameTime` (с учётом паузы и slowmo)
- Обновление `_ScreenShake` (передача в vertex shader offset)
- Обновление `_GlobalFlash` (decay)
- Обновление `_Slowmo`

**API**:
- `SetScreenShake(float magnitude, float duration, AnimationCurve curve)`
- `SetFlash(Color color, float duration)`
- `SetSlowmo(float timeScale, float duration)`
- `SetChromaticAberration(float amount, float duration)`
- `SetDamageVignette(float amount, float duration)`

Все функции запускают корутину с decay по кривой.

### 14.2 MaterialPropertyBlock

Для per-instance параметров сущностей и тайлов пола — использовать `MaterialPropertyBlock`, не создавая копии материалов. Это критично для GPU instancing.

```
Renderer.SetPropertyBlock(propertyBlock)  // per-object override
```

### 14.3 Массовое обновление параметров

Ингредиенты на доске (до 16 штук) и тайлы пола (до 36 штук) обновляются через **batch-set**:
- Собрать все MaterialPropertyBlock в массив
- Обновить за один проход в LateUpdate
- GPU instancing обеспечит один draw call per material

---

## 15. Noise LUT (текстурная оптимизация)

### 15.1 Генерация

Editor-скрипт генерирует текстуру 256×256 RGBA:
- R: Value noise (octave 1)
- G: Value noise (octave 2, другой seed)
- B: Gradient noise
- A: Voronoi distance

Параметры: tileable (seamless edges), 256×256 достаточно для мобильных.

### 15.2 Использование

Во всех шейдерах с define `USE_NOISE_LUT`:

```
// Вместо:
float n = valueNoise(p);

// Используется:
float n = tex2D(_NoiseLUT, frac(p * scale)).r;
```

**Экономия**: ~50% фрагментной стоимости для шейдеров с noise. На слабых GPU (Mali-T, Adreno 3xx) — обязательно.

### 15.3 Gradient Ramp

Текстура 256×1 RGBA. Несколько рамп упакованы по вертикали:
- Row 0: Fire ramp (чёрный → красный → оранжевый → жёлтый → белый)
- Row 1: Poison ramp (чёрный → тёмно-зелёный → кислотный → жёлто-зелёный)
- Row 2: Ice ramp (тёмно-синий → голубой → белый)
- Row 3: Generic energy ramp (чёрный → цвет элемента → белый)

Доступ: `tex2D(_GradientRamp, float2(noiseValue, rampIndex / 4.0))`

---

## 16. Fallback-стратегия

### Уровни качества

Два define-а, управляемых из C# через `Shader.EnableKeyword`:

| Keyword | Значение |
|---------|----------|
| `QUALITY_HIGH` | Процедурный noise, fbm 3-4 октавы, voronoi, каустика, хром. аберрация |
| `QUALITY_LOW` | Noise из LUT, fbm 2 октавы, без voronoi (simplified patterns), без каустики, без хром. аберрации |

### Автоопределение

При первом запуске:
1. Проверить `SystemInfo.graphicsShaderLevel` (≥35 = HIGH, <35 = LOW)
2. Проверить `SystemInfo.graphicsDeviceType` (Vulkan/Metal = HIGH, OpenGL ES 2.0 = LOW)
3. Первые 30 секунд мониторить FPS. Если <50 стабильно → переключить на LOW
4. Сохранить в PlayerPrefs

### Конкретные fallback-ы

| Шейдер | HIGH | LOW |
|--------|------|-----|
| SH_Floor | Voronoi + valueNoise | Простая сетка (step function), без noise |
| SH_Ingredient (Ignis) | fbm 3 октавы | Gradient + valueNoise из LUT |
| SH_Ingredient (Umbra) | Spiral UV warp + gradientNoise | Spiral UV warp + solid color gradient |
| SH_Zone_Fire | fbm 3 октавы | 2 октавы из LUT |
| SH_Zone_Water | Волны + каустика (voronoi) | Только волны (sin+sin) |
| SH_FX_Lightning | 8 сегментов, random per frame | 5 сегментов, random per 2 frames |
| SH_PP_BloomBlur | 4 passes (¼ → ½ → full) | 2 passes (½ → full) |
| SH_PP_Final | Chrom. aberration + vignette + flash | Vignette + flash (без хром. аберрации) |
| Все Entity шейдеры | Noise-based fill | Solid color + gradient fill |

---

## 17. Полный реестр шейдеров

| # | Имя файла | Тип | Blend | Instancing | Стоимость | Приоритет (MVP/Alpha/Beta) |
|---|-----------|-----|-------|------------|-----------|---|
| 1 | SH_Background.shader | Fullscreen quad | Opaque | Нет | Низкая | MVP |
| 2 | SH_Floor.shader | Per-tile quad | Opaque | Да (36 инстансов) | Средняя | MVP |
| 3 | SH_Wall.shader | Edge quads | Opaque | Да | Средняя | Alpha |
| 4 | SH_Ingredient.shader | Per-ingredient quad | Alpha | Да (16 инстансов) | Средняя–Высокая | MVP |
| 5 | SH_Brew.shader | Per-brew quad | Alpha | Да (до 3) | Средняя | MVP |
| 6 | SH_BoardBackground.shader | Board quad | Opaque | Нет | Низкая | MVP |
| 7 | SH_Player.shader | Player quad | Alpha | Нет | Средняя | MVP |
| 8 | SH_Skeleton.shader | Enemy quad | Alpha | Да | Низкая | MVP |
| 9 | SH_Spider.shader | Enemy quad | Alpha | Да | Средняя | MVP |
| 10 | SH_MushroomBomb.shader | Enemy quad | Alpha | Да | Низкая | Alpha |
| 11 | SH_MagnetGolem.shader | Enemy quad | Alpha | Да | Средняя | Alpha |
| 12 | SH_MirrorSlime.shader | Enemy quad | Alpha | Да | Средняя | Alpha |
| 13 | SH_ArmoredBeetle.shader | Enemy quad | Alpha | Да | Низкая | Alpha |
| 14 | SH_Phantom.shader | Enemy quad | Alpha | Да | Низкая | Alpha |
| 15 | SH_Necromancer.shader | Enemy quad | Alpha | Да | Средняя | Alpha |
| 16 | SH_Boss_SpiderQueen.shader | Boss quad (2x) | Alpha | Нет | Высокая | Alpha |
| 17 | SH_Boss_IronGolem.shader | Boss quad (2x) | Alpha | Нет | Высокая | Alpha |
| 18 | SH_Boss_AlchemistRenegade.shader | Boss quad (2x) | Alpha | Нет | Высокая | Alpha |
| 19 | SH_PotionSlot.shader | UI quad | Alpha | Да (4–5) | Средняя | MVP |
| 20 | SH_Zone_Fire.shader | Per-cell overlay | Additive | Да | Средняя | Alpha |
| 21 | SH_Zone_Water.shader | Per-cell overlay | Alpha | Да | Низкая–Средняя | Alpha |
| 22 | SH_Zone_Poison.shader | Per-cell overlay | Alpha | Да | Средняя | Alpha |
| 23 | SH_Zone_Ice.shader | Per-cell overlay | Alpha | Да | Средняя | Alpha |
| 24 | SH_FX_Explosion.shader | Temporary quad | Additive | Нет | Средняя | MVP |
| 25 | SH_FX_Lightning.shader | Temporary quad | Additive | Нет | Средняя–Высокая | Alpha |
| 26 | SH_FX_Acid.shader | Temporary quad | Additive | Нет | Средняя | Alpha |
| 27 | SH_FX_Heal.shader | Temporary quad | Additive | Нет | Низкая | Beta |
| 28 | SH_FX_Steam.shader | Temporary quad | Alpha | Нет | Средняя | Beta |
| 29 | SH_FX_Shockwave.shader | Temporary quad | Additive | Нет | Низкая | Beta |
| 30 | SH_FX_DamageNumber.shader | Temporary quad | Alpha | Нет | Низкая | MVP |
| 31 | SH_FX_ComboOverlay.shader | Temporary fullscreen | Additive | Нет | Низкая | Beta |
| 32 | SH_UI_Glow.shader | UI element | Alpha | Нет | Низкая | Alpha |
| 33 | SH_UI_HealthHeart.shader | UI element | Alpha | Да (5) | Низкая | MVP |
| 34 | SH_UI_IntentionIcon.shader | World UI element | Alpha | Да | Минимальная | MVP |
| 35 | SH_PP_BloomExtract.shader | Post-process | — | — | Низкая | Alpha |
| 36 | SH_PP_BloomBlur.shader | Post-process | — | — | Средняя | Alpha |
| 37 | SH_PP_BloomCombine.shader | Post-process | — | — | Низкая | Alpha |
| 38 | SH_PP_Final.shader | Post-process | — | — | Низкая | Alpha |
| — | AlchemistSDF.cginc | Include | — | — | — | MVP |
| — | EntityCommon.cginc | Include | — | — | — | MVP |
| — | NoiseLib.cginc | Include | — | — | — | MVP |

**Итого**: 38 шейдеров + 3 include-файла.

### Draw calls (оценка per frame)

| Сцена | Объекты | Draw calls (с instancing) |
|-------|---------|--------------------------|
| Бой (5×5, 3 врага) | Background(1) + Floor(25) + Zones(0-5) + Player(1) + Enemies(3) + Intentions(3) + PotionSlots(4) + HP(5) = 47 объектов | ~10–12 |
| Перегонка (4×4, 12 ингредиентов, 2 варева) | Background(1) + Board(1) + Ingredients(12) + Brews(2) + PotionSlots(4) = 20 объектов | ~6–8 |
| Бой + эффект + зоны (пиковый) | Всё выше + 3 зоны + 1 эффект + 3 числа урона = 54 объектов | ~14–16 |

С учётом пост-процессинга (4 passes): **пиковый — 20 draw calls**. Для мобильных — комфортный бюджет.

---

*Версия документа: 1.0*
*Pipeline: Built-in Render Pipeline*
*Target: OpenGL ES 3.0 (Android), Metal (iOS)*
*Min shader model: 3.0*