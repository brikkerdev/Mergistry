# Release — Итерации

> **Цель фазы**: готовность к публикации в Google Play.
> Звук, финальный полиш, оптимизация, сборка.
>
> **Предусловие**: Beta завершена — полная игра,
> сбалансированная, с juice.
>
> **Длительность**: 3–4 недели (5 итераций × 3–4 дня)

---

## R1 — Звук

**Цель**: все игровые действия озвучены. Музыка играет.

### Фичи

1. **SFX для всех событий**
   - Генерация через sfxr / jsfxr (синтетические звуки)
   - Категории:
     - Merge: «буль» + rising tone
     - Подпитка: шипение + chime
     - Бросок зелья: whoosh (per element color)
     - Попадание: short noise burst (per damage type)
     - Убийство: sharp decay + sparkle
     - Combo: layered impact + reverb
     - Движение: soft step
     - Толчок: thud
     - Получение урона: crunch + low rumble
     - UI: click, whoosh, page flip
     - Событие: chime (фонтан), coins (торговец),
       dark tone (алтарь), chest open (сундук)
   - `AudioService`: подписка на события → воспроизведение SFX
   - Пул из 8 AudioSource для SFX

2. **Музыка**
   - 4 ambient loop-а (60–90 сек каждый):
     - Этаж 1: спокойный, загадочный (low pad + drip)
     - Этаж 2: напряжённый (rhythmic pulse + bass)
     - Этаж 3: тревожный (dissonant pad + heartbeat)
     - Босс: интенсивный (percussion + tension)
   - Crossfade между треками (1 сек) при смене этажа / входе в босса
   - 2 AudioSource для crossfade
   - Музыка приглушается при открытии popup-ов (duck to 30%)

3. **Haptic (вибрация)**
   - Лёгкая вибрация: попадание, merge
   - Средняя: combo, убийство
   - Сильная: получение урона, смерть
   - Настройка: вкл/выкл в Settings
   - `Handheld.Vibrate()` с разной длительностью
     (или Unity Input System Haptics при наличии)

### Приёмочный тест (Play Mode)

```
1. Merge: слышен «буль» при слиянии
2. Бросок Пламени: whoosh → impact
3. Убийство: sparkle звук + dissolve
4. Combo: усиленный impact + reverb
5. Получение урона: crunch + вибрация (если включена)
6. Музыка этажа 1: спокойный фон
7. Переход на этаж 2: плавный crossfade к напряжённому треку
8. Вход в босс-бой: интенсивный трек
9. Громкость SFX и музыки управляется из настроек
10. Мьют при сворачивании приложения
    (AudioListener.pause = true при OnApplicationPause)
```

---

## R2 — Переходы и UI-полиш

**Цель**: плавные переходы между экранами.
Нотификации разблокировок. Финальный UI-полиш.

### Фичи

1. **Анимации переходов**
   - `TransitionOverlay`: fullscreen quad с alpha-анимацией
   - Типы:
     - FadeToBlack: alpha 0→1 (0.2s) → callback → 1→0 (0.2s)
     - SlideLeft: текущий экран уезжает влево, новый —
       въезжает справа (0.3s)
     - CrossDissolve: noise-dissolve текущего экрана,
       одновременно появление нового (0.4s)
   - Использование:
     - Menu → Distillation: FadeToBlack
     - Distillation → Combat: SlideLeft
     - Combat → Map: FadeToBlack
     - Map → Event: FadeToBlack
     - Floor → Floor: CrossDissolve (dramatic)

2. **Нотификации разблокировок**
   - `NotificationOverlay`: плашка сверху экрана,
     slide-in → hold 2s → slide-out
   - Типы: «Рецепт разблокирован!» (с иконкой зелья),
     «Реликвия разблокирована!», «Улучшение получено!»,
     «Ачивка выполнена!»
   - Очередь: если несколько нотификаций — показывать
     последовательно (stagger 0.5s)

3. **UI-полиш**
   - Все кнопки: SH_UI_Glow (пульсирующее свечение
     для главных кнопок, статичное для вторичных)
   - Кнопка «Старт»: усиленный glow-pulse
   - Кнопки в popup-ах: hover-feedback (scale ×1.05 при нажатии)
   - Лаборатория (фон меню): анимированный шейдер
     (котёл с дымом + пузырьки + мерцающие ингредиенты)
   - Result screen: анимированный подсчёт статистики
     (числа «набегают» с counting animation)
   - HP hearts: SH_UI_HealthHeart (SDF-сердечко, пульс при потере)

### Приёмочный тест (Play Mode)

```
1. Меню → «Старт»: плавный fade to black → доска появляется
2. Перегонка → бой: экран «уезжает» влево,
   бой «въезжает» справа
3. Бой → карта: fade
4. Этаж 1 → этаж 2: noise-dissolve (красиво)
5. Разблокировка рецепта → плашка сверху «Рецепт: Молния!»,
   slide-in, задержка, slide-out
6. Две разблокировки одновременно → показываются по очереди
7. Кнопка «Старт» пульсирует свечением
8. Result screen: числа «набегают» (0 → 7 врагов убито, 1 sec)
9. Фон меню: анимированный котёл с дымом
10. HP hearts — SDF-сердечки, пульсируют при потере HP
```

---

## R3 — Оптимизация

**Цель**: стабильные 60 FPS на минимальном устройстве.
Размер APK < 50 MB.

### Фичи

1. **Профилирование и фиксация**
   - Тест на минимальном устройстве (или эмуляция:
     Android 8.0, 2 GB RAM, Mali-T720)
   - Unity Profiler: найти CPU bottleneck-и
     (GC alloc, expensive Update)
   - Frame Debugger: проверить draw call count
     (цель: ≤20 per frame)
   - Исправления:
     - Struct events вместо class (если ещё не сделано)
     - Shader.PropertyToID кэширование
     - List<> pre-allocation
     - Удаление LINQ из hot paths
     - Object pool verification (нет runtime Instantiate
       во время геймплея)

2. **Шейдерные fallback-и**
   - Проверить все шейдеры в режиме `QUALITY_LOW`
   - Noise LUT: проверить что выборка текстуры используется
   - Bloom: 2 pass вместо 4 на LOW
   - Без хроматической аберрации на LOW
   - Без каустики в воде на LOW
   - Визуальная проверка: всё читаемо и приемлемо на LOW

3. **Размер билда**
   - Managed Stripping Level: Medium → High
     (проверить что ничего не порезано лишнего)
   - Проверить размер текстур (LUT 256×256, Ramp 256×4,
     UI atlas 512×512 — должно быть < 1 MB total)
   - Аудио: сжатие (Vorbis, quality 50% для SFX,
     70% для музыки)
   - Нет лишних ассетов в билде (Editor-only файлы исключены)
   - Целевой APK: < 50 MB (AAB может быть меньше)

### Приёмочный тест (Play Mode)

```
1. На минимальном устройстве:
    - Загрузка < 3 сек
    - FPS ≥ 55 стабильно (все сцены)
    - Пиковый момент (combo + 3 врага + зоны): FPS ≥ 45
    - RAM < 200 MB
2. Unity Profiler: 0 B GC alloc per frame во время геймплея
3. Frame Debugger: ≤ 20 draw calls per frame
4. Quality LOW: визуально приемлемо, все объекты читаемы,
   bloom есть, анимации плавные
5. APK size: < 50 MB
6. Батарея: < 10% за 30 минут игры
7. Нет thermal throttling за 15 минут непрерывной игры
```

---

## R4 — Сборка и публикация

**Цель**: APK/AAB собран, подписан, загружен в Google Play Console.

### Фичи

1. **Настройка билда**
   - IL2CPP backend, ARM64
   - Keystore: создать и сохранить (secure backup!)
   - Подпись AAB
   - `ApplicationIdentifier`: `com.studio.alchemistsdescent`
   - Version: 1.0.0, versionCode: 1
   - Min API: 26 (Android 8.0), Target API: 34 (Android 14)
   - Иконка: адаптивная (foreground SDF-рендер → PNG export,
     background solid dark)
   - Splash screen: минимальный (логотип 1 сек)

2. **Google Play Console**
   - Создать приложение
   - Описание: короткое (80 символов) и полное (4000 символов)
   - Скриншоты: 5+ (из игры, 1080×1920)
   - Feature graphic: 1024×500
   - Категория: Casual / Roguelike
   - Content rating: заполнить опросник (IARC)
   - Privacy policy: простая страница (нет сбора данных)
   - Internal testing track: загрузить AAB

3. **Crash reporting (опционально)**
   - Firebase Crashlytics: интеграция
     (или Unity Cloud Diagnostics — бесплатно)
   - Проверить: crash при startup, crash при save/load,
     ANR detection

### Приёмочный тест (Play Mode)

```
1. AAB собирается без ошибок
2. Установка на устройство из AAB (bundletool): работает
3. Splash → меню за < 3 сек
4. Полный ран проходится без crash-ей
5. Сворачивание / разворачивание: состояние сохранено
6. Уведомления не мешают (overlay notification → игра на паузе)
7. Google Play Console: AAB загружен,
   проходит pre-launch тесты
8. Internal testing: ссылка работает,
   3 тестера установили и прошли ран
```

---

## R5 — Финальное QA и релиз

**Цель**: все баги исправлены. Приложение опубликовано.

### Фичи

1. **Полный QA-прогон**
   - Чеклист из архитектурной документации (секция 21.4)
   - Все экраны, все переходы
   - Все зелья, все враги, все боссы
   - Все события, все реликвии
   - Все ачивки (проверить что срабатывают)
   - Все модификаторы комнат
   - Все combo
   - Save/load: mid-run + мета
   - Обучение: полный прогон + пропуск
   - 3 устройства: low-end, mid-range, flagship

2. **Bug fixing**
   - Критические: crashes, data loss, infinite loops
   - Major: gameplay-breaking (невозможно пройти,
     невалидные состояния)
   - Minor: визуальные глитчи, неточные описания
   - Won't fix: косметические, не влияющие на геймплей
   - Приоритет: Critical → Major → Minor

3. **Публикация**
   - Closed testing (если нужно): 20–50 тестеров,
     1 неделя, сбор фидбэка
   - Production release: поэтапный rollout (10% → 50% → 100%)
   - Мониторинг: crash rate < 1%, ANR rate < 0.5%,
     rating ≥ 4.0
   - Hotfix-процесс: fast-track для critical bugs
     (fix → build → upload → review → release, < 48 часов)

### Приёмочный тест (Play Mode)

```
1. Полный прогон чеклиста (все пункты ✓)
2. 0 Critical bugs, 0 Major bugs
3. 10 полных ранов на 3 устройствах без crash-ей
4. Crash rate в Firebase / Cloud Diagnostics: 0% за тест-период
5. 3 внешних тестера (closed testing):
    - Все прошли обучение
    - Все прошли минимум 5 ранов
    - Средняя оценка ≥ 4/5
6. Production release: rollout 10%,
   мониторинг 24 часа, нет всплеска crash-ов
7. Rollout 100%
```

---

## Чеклист релиза

- [ ] Все SFX озвучены (merge, бросок, попадание, смерть,
      combo, UI, события)
- [ ] Музыка: 4 трека, crossfade, duck
- [ ] Вибрация: лёгкая / средняя / сильная, настройка в Settings
- [ ] Переходы: fade, slide, dissolve — все плавные
- [ ] Нотификации разблокировок работают
- [ ] UI-полиш: glow кнопки, анимированный фон меню,
      counting на результате
- [ ] 60 FPS на mid-range, 50+ на low-end
- [ ] 0 B GC alloc per frame
- [ ] ≤ 20 draw calls
- [ ] APK < 50 MB
- [ ] Quality fallback: LOW визуально приемлем
- [ ] AAB подписан, загружен в Google Play Console
- [ ] Описание, скриншоты, feature graphic, категория,
      content rating, privacy policy
- [ ] Crashlytics / Cloud Diagnostics подключены
- [ ] 0 Critical, 0 Major bugs
- [ ] Closed testing пройден (3+ тестера, ≥ 4/5)
- [ ] Production rollout: 10% → 50% → 100%
- [ ] Мониторинг: crash rate < 1%, ANR < 0.5%

---

## Суммарный таймлайн всех итераций

| Фаза | Итераций | Длительность | Накопительно |
|------|----------|-------------|--------------|
| **MVP** (M1–M6) | 6 | 4–5 недель | 1 месяц |
| **Alpha** (A1–A8) | 8 | 5–6 недель | 2.5 месяца |
| **Beta** (B1–B7) | 7 | 4–5 недель | 3.5 месяца |
| **Release** (R1–R5) | 5 | 3–4 недели | 4.5 месяца |
| **Итого** | **26 итераций** | **16–20 недель** | **~4.5 месяца** |
