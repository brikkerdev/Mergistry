

# MERGISTRY — Архитектурная документация

---

## 1. Архитектурный обзор

### 1.1 Философия

| Принцип | Обоснование |
|---------|-------------|
| **Data-driven** | Вся конфигурация — в ScriptableObject-ах. Изменение баланса без перекомпиляции |
| **Model-View разделение** | Логика игры — чистый C# без MonoBehaviour. View реагирует на изменения модели через события |
| **Composition over Inheritance** | Системы компонуются, а не наследуются. Минимальная иерархия классов |
| **Event-driven** | Системы общаются через типизированные события, не через прямые ссылки |
| **Stateless services, stateful models** | Сервисы (AI, расчёт урона, генерация) не хранят состояние. Всё состояние — в моделях |
| **Fail-fast** | Assertions и валидация на этапе разработки. В релизе — graceful degradation |

### 1.2 Высокоуровневая диаграмма

```
┌─────────────────────────────────────────────────────────┐
│                      UNITY ENGINE                        │
│                                                          │
│  ┌──────────────────────────────────────────────────┐    │
│  │                  PRESENTATION                     │    │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────────────┐  │    │
│  │  │ UI Views │ │ World    │ │ Shader/VFX       │  │    │
│  │  │ (Canvas) │ │ Views    │ │ Controller       │  │    │
│  │  └────▲─────┘ └────▲─────┘ └────────▲─────────┘  │    │
│  └───────┼─────────────┼───────────────┼─────────────┘    │
│          │             │               │                  │
│          │      EVENTS (one-way)       │                  │
│          │             │               │                  │
│  ┌───────┼─────────────┼───────────────┼─────────────┐    │
│  │       │         GAME LOGIC          │             │    │
│  │  ┌────┴─────────────┴───────────────┴──────────┐  │    │
│  │  │              EVENT BUS                       │  │    │
│  │  └────▲────────────▲───────────────▲───────────┘  │    │
│  │       │            │               │              │    │
│  │  ┌────┴────┐  ┌────┴────┐    ┌─────┴──────┐      │    │
│  │  │ Game    │  │ State   │    │ Services   │      │    │
│  │  │ States  │  │ Models  │    │            │      │    │
│  │  │ (FSM)   │  │         │    │ - Combat   │      │    │
│  │  │         │  │ - Run   │    │ - Distill  │      │    │
│  │  │ - Menu  │  │ - Board │    │ - AI       │      │    │
│  │  │ - Dist  │  │ - Grid  │    │ - Damage   │      │    │
│  │  │ - Comb  │  │ - Inv   │    │ - MapGen   │      │    │
│  │  │ - Map   │  │ - Meta  │    │ - LootGen  │      │    │
│  │  │ - Event │  │         │    │ - Save     │      │    │
│  │  └─────────┘  └────┬────┘    └────────────┘      │    │
│  └─────────────────────┼────────────────────────────┘    │
│                        │                                  │
│  ┌─────────────────────┼────────────────────────────┐    │
│  │                 DATA LAYER                        │    │
│  │  ┌──────────────────┴────────────────────────┐    │    │
│  │  │           ScriptableObjects               │    │    │
│  │  │  - PotionDB, EnemyDB, RelicDB             │    │    │
│  │  │  - FloorConfig, BalanceConfig              │    │    │
│  │  │  - AchievementDB                           │    │    │
│  │  └───────────────────────────────────────────┘    │    │
│  │  ┌───────────────────────────────────────────┐    │    │
│  │  │           Persistent Storage               │    │    │
│  │  │  - JSON save files                         │    │    │
│  │  │  - PlayerPrefs (settings)                  │    │    │
│  │  └───────────────────────────────────────────┘    │    │
│  └───────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────┘
```

### 1.3 Зависимости между слоями

```
Presentation → Game Logic → Data Layer

Presentation НИКОГДА не обращается к Data Layer напрямую.
Game Logic НИКОГДА не обращается к Presentation напрямую.
Общение вниз: прямые вызовы / queries.
Общение вверх: события (Event Bus).
```

---

## 2. Структура проекта

### 2.1 Директории

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/                    # Базовые системы, не привязанные к игре
│   │   │   ├── EventBus/
│   │   │   ├── StateMachine/
│   │   │   ├── ObjectPool/
│   │   │   ├── ServiceLocator/
│   │   │   ├── Extensions/
│   │   │   └── Utilities/
│   │   │
│   │   ├── Data/                    # Конфигурация и определения
│   │   │   ├── Definitions/         # ScriptableObject-классы
│   │   │   ├── Configs/             # Баланс, настройки
│   │   │   └── Enums/
│   │   │
│   │   ├── Models/                  # Чистая логика (no MonoBehaviour)
│   │   │   ├── Run/
│   │   │   ├── Distillation/
│   │   │   ├── Combat/
│   │   │   ├── Inventory/
│   │   │   ├── Map/
│   │   │   └── Meta/
│   │   │
│   │   ├── Services/                # Stateless сервисы
│   │   │   ├── CombatService/
│   │   │   ├── DistillationService/
│   │   │   ├── AIService/
│   │   │   ├── DamageService/
│   │   │   ├── MapGeneratorService/
│   │   │   ├── LootService/
│   │   │   ├── SaveService/
│   │   │   └── AudioService/
│   │   │
│   │   ├── GameStates/              # FSM-состояния игры
│   │   │   ├── MenuState/
│   │   │   ├── DistillationState/
│   │   │   ├── CombatState/
│   │   │   ├── MapState/
│   │   │   ├── EventState/
│   │   │   ├── BossState/
│   │   │   └── ResultState/
│   │   │
│   │   ├── Views/                   # MonoBehaviour визуализация
│   │   │   ├── Board/               # Доска перегонки
│   │   │   ├── Grid/                # Боевая сетка
│   │   │   ├── Entities/            # Игрок, враги
│   │   │   ├── Effects/             # VFX, juice
│   │   │   ├── Map/                 # Карта этажа
│   │   │   └── Common/              # Общие view-компоненты
│   │   │
│   │   ├── UI/                      # UI-контроллеры и виджеты
│   │   │   ├── Screens/
│   │   │   ├── Widgets/
│   │   │   ├── Popups/
│   │   │   └── HUD/
│   │   │
│   │   ├── Input/                   # Обработка ввода
│   │   │
│   │   └── Boot/                    # Точка входа, инициализация
│   │
│   ├── Data/                        # ScriptableObject instances
│   │   ├── Potions/
│   │   ├── Enemies/
│   │   ├── Relics/
│   │   ├── Floors/
│   │   ├── Events/
│   │   ├── Achievements/
│   │   └── Balance/
│   │
│   ├── Shaders/
│   │   ├── Includes/                # .cginc файлы
│   │   ├── Environment/
│   │   ├── Entities/
│   │   ├── Effects/
│   │   ├── UI/
│   │   └── PostProcess/
│   │
│   ├── Materials/
│   │   ├── Environment/
│   │   ├── Entities/
│   │   ├── Effects/
│   │   └── UI/
│   │
│   ├── Prefabs/
│   │   ├── Board/
│   │   ├── Grid/
│   │   ├── Entities/
│   │   ├── Effects/
│   │   ├── UI/
│   │   └── Map/
│   │
│   ├── Scenes/
│   │   ├── Boot.unity               # Единственная сцена загрузки
│   │   └── Game.unity               # Основная сцена (всё в ней)
│   │
│   ├── Audio/
│   │   ├── SFX/
│   │   └── Music/
│   │
│   └── Textures/                    # Только LUT и gradient ramps
│       ├── NoiseLUT.png
│       └── GradientRamp.png
│
├── Plugins/                         # Сторонние SDK
└── Editor/                          # Editor-скрипты
    ├── NoiseGenerator/
    ├── BalanceTools/
    └── DataValidation/
```

### 2.2 Сцены

Две сцены:

| Сцена | Назначение |
|-------|------------|
| `Boot.unity` | Точка входа. Содержит `BootstrapRunner` — инициализирует все системы, загружает `Game.unity` аддитивно |
| `Game.unity` | Основная (и единственная рабочая) сцена. Содержит камеру, Canvas, корневые GameObject-ы для views. Никогда не выгружается |

Смена «экранов» (меню, бой, карта) происходит через включение/выключение корневых GameObject-ов и переключение состояния FSM. Не через загрузку сцен.

---

## 3. Инициализация (Boot)

### 3.1 Порядок загрузки

```
Application Start
    │
    ▼
Boot.unity загружается (единственная сцена в Build Settings)
    │
    ▼
BootstrapRunner.Awake()
    │
    ├── 1. Инициализация ServiceLocator
    ├── 2. Регистрация всех сервисов
    ├── 3. Загрузка конфигурации (ScriptableObjects)
    ├── 4. Инициализация SaveService → загрузка мета-прогрессии
    ├── 5. Инициализация AudioService
    ├── 6. Инициализация ShaderGlobalController
    ├── 7. Определение качества графики (fallback)
    ├── 8. Загрузка Game.unity (аддитивно)
    │
    ▼
Game.unity загружена
    │
    ├── 9. Инициализация UI Manager
    ├── 10. Инициализация GameStateMachine
    ├── 11. Инициализация ObjectPool-ов
    ├── 12. Переход в MenuState
    │
    ▼
Splash screen / Main Menu отображается
```

### 3.2 ServiceLocator

Простой паттерн Service Locator без DI-фреймворка (для минимизации зависимостей и размера билда).

**Регистрация**:

| Сервис | Интерфейс | Тип жизни |
|--------|-----------|-----------|
| EventBus | IEventBus | Singleton |
| CombatService | ICombatService | Singleton |
| DistillationService | IDistillationService | Singleton |
| AIService | IAIService | Singleton |
| DamageService | IDamageService | Singleton |
| MapGeneratorService | IMapGeneratorService | Singleton |
| LootService | ILootService | Singleton |
| SaveService | ISaveService | Singleton |
| AudioService | IAudioService | Singleton |
| InputService | IInputService | Singleton |
| ObjectPoolService | IObjectPoolService | Singleton |
| ConfigProvider | IConfigProvider | Singleton |

**Доступ**: `ServiceLocator.Get<ICombatService>()`. Кэшируется в поле при инициализации каждого потребителя.

---

## 4. Event Bus

### 4.1 Архитектура

Типизированная система событий. Каждое событие — структура (`struct`), реализующая маркерный интерфейс `IGameEvent`. Структуры — для избежания аллокаций в куче.

### 4.2 API

```
IEventBus.Publish<T>(T gameEvent)
IEventBus.Subscribe<T>(Action<T> handler)
IEventBus.Unsubscribe<T>(Action<T> handler)
```

### 4.3 Категории событий

**Distillation Events**:

| Событие | Поля | Когда |
|---------|------|-------|
| `DistillationPhaseStarted` | `BoardState board` | Начало фазы перегонки |
| `MergePerformed` | `GridPos from, GridPos to, PotionType result, int level` | Успешный merge двух ингредиентов |
| `InfusionPerformed` | `GridPos ingredient, GridPos brew, int newLevel` | Успешная подпитка варева |
| `ActionSpent` | `int remaining` | Потрачено действие |
| `BrewCollected` | `PotionType type, int level, int slotIndex` | Варево собрано в инвентарь |
| `DistillationPhaseEnded` | — | Фаза перегонки завершена |
| `InvalidAction` | `GridPos from, GridPos to, string reason` | Попытка невалидного действия |

**Combat Events**:

| Событие | Поля | Когда |
|---------|------|-------|
| `CombatStarted` | `CombatSetup setup` | Начало боя |
| `TurnStarted` | `int turnNumber` | Начало хода игрока |
| `PlayerMoved` | `GridPos from, GridPos to` | Игрок переместился |
| `PotionThrown` | `PotionType type, int level, GridPos target, List<GridPos> affectedCells` | Зелье брошено |
| `DamageDealt` | `EntityId target, int amount, DamageType type, bool isKill` | Урон нанесён |
| `EnemyKilled` | `EntityId enemy, EnemyType type, GridPos position` | Враг убит |
| `PushPerformed` | `EntityId target, GridPos from, GridPos to, bool wallSlam, bool pitKill` | Враг толкнут |
| `TurnSkipped` | `int healAmount` | Ход пропущен |
| `EnemyTurnStarted` | — | Начало хода врагов |
| `EnemyActed` | `EntityId enemy, EnemyAction action` | Враг выполнил действие |
| `PlayerDamaged` | `int amount, int remainingHP` | Игрок получил урон |
| `ZoneCreated` | `ZoneType type, GridPos position, int duration` | Зона создана на поле |
| `ZoneExpired` | `ZoneType type, GridPos position` | Зона исчезла |
| `ComboTriggered` | `ComboType type, List<GridPos> affectedCells, int bonusDamage` | Combo сработало |
| `CombatEnded` | `CombatResult result` | Бой завершён (победа/смерть) |
| `CooldownTick` | `int slotIndex, int remaining` | Кулдаун зелья уменьшился |

**Run Events**:

| Событие | Поля | Когда |
|---------|------|-------|
| `RunStarted` | `int seed` | Ран начат |
| `FloorStarted` | `int floorNumber, FloorConfig config` | Новый этаж |
| `NodeSelected` | `MapNodeId nodeId, NodeType type` | Игрок выбрал узел на карте |
| `EventStarted` | `EventType type` | Начало события |
| `EventChoiceMade` | `EventType type, int choiceIndex` | Игрок выбрал вариант в событии |
| `RelicAcquired` | `RelicType type` | Реликвия получена |
| `RelicChoicePresented` | `List<RelicType> options` | Показан выбор реликвий (после элиты) |
| `RunEnded` | `RunResult result` | Ран завершён |

**Meta Events**:

| Событие | Поля | Когда |
|---------|------|-------|
| `AchievementUnlocked` | `AchievementId id, UnlockType reward` | Ачивка выполнена |
| `RecipeUnlocked` | `PotionType recipe` | Рецепт разблокирован |
| `UpgradeUnlocked` | `UpgradeType upgrade` | Постоянное улучшение получено |
| `StatUpdated` | `StatType type, int newValue` | Статистика обновлена |

**UI Events**:

| Событие | Поля | Когда |
|---------|------|-------|
| `PotionSlotTapped` | `int slotIndex` | Тап на зелье в инвентаре |
| `GridCellTapped` | `GridPos position` | Тап на клетку сетки |
| `ButtonPressed` | `ButtonId id` | Нажатие кнопки UI |
| `PauseToggled` | `bool isPaused` | Пауза включена/выключена |
| `BookOpened` | — | Открыта книга рецептов |

### 4.4 Правила подписки

- **View-слой**: подписывается на события модели. Только читает данные из события. Не вызывает сервисы
- **GameState**: подписывается на UI Events и направляет в сервисы. Является посредником
- **Сервисы**: публикуют события, но не подписываются на события других сервисов (во избежание циклов)
- **Модели**: не публикуют и не подписываются. Изменяются сервисами, а сервис публикует событие после изменения

### 4.5 Жизненный цикл подписок

- Подписка: в `OnEnable` (для MonoBehaviour) или при инициализации (для чистых C# классов)
- Отписка: в `OnDisable` или при деинициализации
- **Строгое правило**: каждый `Subscribe` должен иметь парный `Unsubscribe`

---

## 5. Game State Machine (GSM)

### 5.1 Обзор

Центральный конечный автомат, управляющий текущим «экраном» игры. Каждое состояние инкапсулирует логику входа, обновления и выхода.

### 5.2 Диаграмма переходов

```
                    ┌──────────┐
          ┌────────►│  MENU    │◄─────────────┐
          │         └────┬─────┘              │
          │              │ StartRun            │
          │              ▼                     │
          │         ┌──────────┐              │
          │    ┌───►│   MAP    │◄───┐         │
          │    │    └────┬─────┘    │         │
          │    │         │          │         │
          │    │    SelectNode      │         │
          │    │    ┌────┼─────┐    │         │
          │    │    ▼    ▼     ▼    │         │
          │    │ ┌─────┐┌───┐┌────┐│         │
          │    │ │DIST ││EVT││BOSS││         │
          │    │ └──┬──┘└─┬─┘└──┬─┘│         │
          │    │    │     │     │   │         │
          │    │    ▼     │     ▼   │         │
          │    │ ┌─────┐  │  ┌─────┐│         │
          │    │ │COMBT│  │  │COMBT││         │
          │    │ └──┬──┘  │  └──┬──┘│         │
          │    │    │     │     │   │         │
          │    │    ▼     ▼     ▼   │         │
          │    │  Win?  Done?  Win? │         │
          │    │   │      │     │   │         │
          │    └───┘      │     │   │         │
          │    (next node)│     │   │         │
          │               │     │   │         │
          │         ┌─────┘     │   │         │
          │         │     ┌─────┘   │         │
          │         ▼     ▼         │         │
          │    ┌──────────────┐     │         │
          │    │   RESULT     │     │         │
          │    └──────┬───────┘     │         │
          │           │             │         │
          └───────────┘             │         │
          (Retry = new Run)   FloorComplete   │
                                    │         │
                              NextFloor ──────┘
                              (back to MAP)
```

### 5.3 Состояния

| Состояние | Ответственность | Активные View |
|-----------|----------------|---------------|
| **MenuState** | Главное меню. Отображение лаборатории. Обработка кнопок. Доступ к книге и ачивкам | MenuScreen, LabBackground |
| **MapState** | Отображение карты этажа. Обработка выбора узла. Переход к выбранному узлу | MapScreen, MapNodeViews |
| **DistillationState** | Фаза перегонки. Управление доской. Обработка drag & drop. Подсчёт действий. Сбор варев | BoardView, IngredientViews, BrewViews, PotionSlotViews |
| **CombatState** | Пошаговый бой. Обработка ввода. Ходы игрока и врагов. Применение зелий. Проверка победы/смерти | GridView, EntityViews, ZoneViews, EffectViews, HUD |
| **EventState** | Случайное событие. Отображение вариантов. Обработка выбора | EventScreen |
| **BossState** | Расширение CombatState для босс-боёв. Управление фазами босса | Те же, что CombatState, + BossUI |
| **ResultState** | Экран результата рана. Статистика. Обработка разблокировок. Кнопки «Ещё раз» / «Лаборатория» | ResultScreen |

### 5.4 Интерфейс состояния

Каждое состояние реализует контракт:

| Метод | Когда вызывается |
|-------|------------------|
| `Enter(StateContext context)` | При переходе в это состояние. Инициализация, подписка на события, активация view |
| `Update()` | Каждый кадр, пока состояние активно |
| `Exit()` | При выходе из состояния. Отписка, деактивация view, очистка |

**StateContext** — контейнер с данными, необходимыми для перехода (текущий RunModel, выбранный узел, результат боя и т.д.).

### 5.5 Подсостояния CombatState

CombatState содержит **внутренний FSM** (для управления фазами хода):

```
┌─────────────────────────────────────────┐
│              CombatState                │
│                                         │
│  ┌───────────┐    ┌───────────────────┐ │
│  │ PlayerTurn │───►│ EnemyTurn         │ │
│  │            │    │                   │ │
│  │ ┌────────┐ │    │ - Resolve intents │ │
│  │ │WaitMove│ │    │ - Apply damage    │ │
│  │ └───┬────┘ │    │ - Update zones    │ │
│  │     ▼      │    │ - Tick cooldowns  │ │
│  │ ┌────────┐ │    │ - Check win/lose  │ │
│  │ │WaitAct │ │    │ - Show intents    │ │
│  │ └───┬────┘ │    └────────┬──────────┘ │
│  │     ▼      │             │            │
│  │ ┌────────┐ │             │            │
│  │ │Confirm │─┘             │            │
│  │ └────────┘               │            │
│  │     ▲                    │            │
│  │     └────────────────────┘            │
│  │         (next turn)                   │
│  │                                       │
│  │  ┌───────────┐                        │
│  │  │ Animating │ (блокирует ввод,       │
│  │  │           │  проигрывает анимации) │
│  │  └───────────┘                        │
└─────────────────────────────────────────┘
```

| Подсостояние | Описание |
|-------------|----------|
| `WaitingForMove` | Ожидание свайпа движения от игрока. Подсвечены доступные клетки |
| `WaitingForAction` | Ожидание действия (бросок / толчок / пропуск). Подсвечены доступные действия |
| `Confirming` | Игрок видит превью хода, может подтвердить или отменить (если включено подтверждение) |
| `AnimatingPlayerAction` | Блокировка ввода. Проигрывание анимации движения + действия игрока |
| `ResolvingEnemyTurn` | Последовательное разрешение действий врагов с анимациями |
| `AnimatingEnemyActions` | Проигрывание анимаций врагов |
| `CheckingOutcome` | Проверка: все враги мертвы? Игрок мёртв? Если нет — новый ход |

---

## 6. Модели данных (Models)

### 6.1 Принципы

- Чистые C# классы, без MonoBehaviour
- Сериализуемые (для save/load)
- Иммутабельные где возможно (readonly поля, методы возвращают новое состояние)
- Где невозможно иммутабельность — мутация только через сервисы

### 6.2 RunModel

Корневая модель текущего рана. Создаётся при старте, уничтожается при завершении.

```
RunModel
├── seed: int                        # Seed для генерации (воспроизводимость)
├── currentFloor: int                # Текущий этаж (0-2)
├── currentNodeIndex: int            # Текущий узел на карте
├── playerHP: int                    # Текущее HP
├── maxHP: int                       # Максимальное HP
├── inventory: InventoryModel        # Зелья игрока
├── relics: List<RelicType>          # Активные реликвии
├── floorMaps: FloorMapModel[3]      # Карты всех трёх этажей
├── stats: RunStatsModel             # Статистика рана
├── bonusActions: int                # Бонусные действия (от событий/реликвий)
├── turnNumber: int                  # Номер хода (для логирования)
└── isFirstRun: bool                 # Обучающий ран?
```

### 6.3 InventoryModel

```
InventoryModel
├── slots: PotionSlot[4..5]
│   ├── potionType: PotionType?      # null = пустой слот
│   ├── level: int                   # 1-3
│   └── cooldownRemaining: int       # 0 = готово
├── maxSlots: int                    # 4 (или 5 после мета-апгрейда)
└── Methods:
    ├── GetAvailablePotions(): List<PotionSlot>
    ├── HasFreeSlot(): bool
    ├── FindSlotByType(PotionType): int?
    └── GetSlotsOnCooldown(): List<int>
```

### 6.4 BoardModel (доска перегонки)

```
BoardModel
├── width: int                       # 4 (или 5 после мета-апгрейда)
├── height: int                      # 4
├── cells: CellContent[width, height]
│   ├── type: CellType               # Empty, Ingredient, Brew
│   ├── elementType: ElementType?     # Ignis, Aqua, Toxin, Lux, Umbra (если Ingredient)
│   ├── brewType: PotionType?         # Тип варева (если Brew)
│   └── brewLevel: int?              # Уровень варева (если Brew)
├── actionsRemaining: int            # 3 (или 4)
├── maxActions: int
└── Methods:
    ├── GetCell(GridPos): CellContent
    ├── SetCell(GridPos, CellContent): void
    ├── GetNeighbors(GridPos): List<GridPos>
    ├── GetValidMergeTargets(GridPos): List<GridPos>
    ├── GetValidInfusionTargets(GridPos): List<GridPos>
    └── GetAllBrews(): List<(GridPos, PotionType, int)>
```

### 6.5 CombatModel (бой)

```
CombatModel
├── grid: GridModel
│   ├── width: int                   # 5 или 6
│   ├── height: int
│   └── cells: GridCell[width, height]
│       ├── isPassable: bool
│       ├── occupant: EntityId?
│       └── zones: List<ZoneInstance>
│           ├── type: ZoneType
│           └── turnsRemaining: int
│
├── player: PlayerCombatModel
│   ├── position: GridPos
│   ├── hasMoved: bool               # Двигался ли в этот ход
│   ├── hasActed: bool               # Действовал ли в этот ход
│   └── statusEffects: List<StatusEffect>
│
├── enemies: List<EnemyCombatModel>
│   ├── entityId: EntityId
│   ├── definition: EnemyDefinition  # Ссылка на SO
│   ├── position: GridPos
│   ├── currentHP: int
│   ├── maxHP: int
│   ├── armorPoints: int
│   ├── currentIntent: EnemyIntent
│   │   ├── type: IntentType         # Attack, Move, Summon, Shield
│   │   ├── targetCells: List<GridPos>
│   │   ├── damage: int
│   │   └── direction: GridPos?
│   ├── statusEffects: List<StatusEffect>
│   ├── isDead: bool
│   └── deathTurn: int?              # Для некроманта (воскрешение)
│
├── turnNumber: int
├── roomModifier: RoomModifier?      # Модификатор комнаты
├── combatLog: List<CombatLogEntry>  # Лог для replay/debug
│
└── Methods:
    ├── GetEntitiesInArea(List<GridPos>): List<EntityId>
    ├── GetAdjacentEnemies(GridPos): List<EntityId>
    ├── IsPositionValid(GridPos): bool
    ├── GetPathableCells(GridPos, int range): List<GridPos>
    └── GetZonesAt(GridPos): List<ZoneInstance>
```

### 6.6 FloorMapModel (карта этажа)

```
FloorMapModel
├── floorNumber: int
├── nodes: List<MapNode>
│   ├── id: MapNodeId
│   ├── type: NodeType               # Combat, Elite, Event, Boss
│   ├── position: Vector2            # Позиция на карте (для отображения)
│   ├── connections: List<MapNodeId>  # Соединённые узлы (вниз)
│   ├── isVisited: bool
│   ├── isAccessible: bool           # Можно ли выбрать
│   ├── roomModifier: RoomModifier?
│   └── combatSetup: CombatSetup?    # Состав врагов (генерируется при создании)
├── currentNodeId: MapNodeId?
└── bossNodeId: MapNodeId
```

### 6.7 MetaProgressionModel

Сохраняется между ранами. Один экземпляр на весь жизненный цикл приложения.

```
MetaProgressionModel
├── unlockedRecipes: HashSet<PotionType>
├── unlockedRelics: HashSet<RelicType>
├── unlockedUpgrades: HashSet<UpgradeType>
├── achievementProgress: Dictionary<AchievementId, int>  # Текущий прогресс
├── completedAchievements: HashSet<AchievementId>
├── stats: GlobalStatsModel
│   ├── totalRuns: int
│   ├── totalWins: int
│   ├── totalEnemiesKilled: int
│   ├── totalPotionsUsed: int
│   ├── totalMergesPerformed: int
│   ├── totalCombosTriggered: int
│   ├── totalPushes: int
│   ├── totalEventsVisited: int
│   ├── totalRoomsWithModifiers: int
│   ├── totalPoisonDamage: int
│   ├── bestFloorReached: int
│   ├── bestComboInSingleBattle: int
│   └── maxEnemiesHitByOnePotion: int
└── settings: SettingsModel
    ├── sfxVolume: float
    ├── musicVolume: float
    ├── vibrationEnabled: bool
    ├── screenShakeAmount: float
    ├── confirmTurnEnabled: bool
    └── qualityLevel: int
```

### 6.8 Перечисления (Enums)

```
ElementType     { Ignis, Aqua, Toxin, Lux, Umbra }

PotionType      { Flame, Stream, Poison, Radiance, Darkness,     // базовые
                  Steam, Napalm, Flash, Curse, Acid,              // рецептурные
                  Lightning, Abyss, Spore, Miasma, Chaos }

EnemyType       { Skeleton, Spider, MushroomBomb, MagnetGolem,
                  MirrorSlime, ArmoredBeetle, Phantom, Necromancer,
                  BossSpiderQueen, BossIronGolem, BossAlchemist }

ZoneType        { Fire, Water, Poison, Ice }

IntentType      { Attack, Move, Summon, Shield, Pull, Flee, Copy, Detonate }

NodeType        { Combat, Elite, Event, Boss }

EventType       { Fountain, Merchant, Altar, Chest }

RelicType       { Thermos, Lens, Mutagen, Flask, Dice,
                  Prism, Decanter, Spike, Vortex, Monocle }

UpgradeType     { FifthSlot, LargerBoard, FourthAction,
                  StartingPotion, Farsight }

RoomModifier    { Flooded, Burning, Pits, Timer, Dark, Conveyor }

StatusEffect    { Poisoned, Slowed, Blinded, Stunned, Confused,
                  ArmorBroken, Frozen }

DamageType      { Direct, Fire, Poison, Lightning, Ice, Acid, Push, Zone }

CombatResult    { Victory, Defeat }

ComboType       { LightningWater, FirePoison, StreamIce,
                  FireIce, LightningFire, AcidWater, PoisonFire }
```

---

## 7. Сервисы

### 7.1 DistillationService

**Обязанности**: Управление доской перегонки. Валидация действий. Выполнение merge/подпитки. Генерация доски.

| Метод | Входы | Выход | Описание |
|-------|-------|-------|----------|
| `GenerateBoard` | `int floor, MetaProgressionModel meta, int seed` | `BoardModel` | Создать новую доску с учётом этажа и разблокировок |
| `CanMerge` | `BoardModel board, GridPos from, GridPos to` | `MergeValidation` | Проверка валидности merge (return: valid/invalid + причина) |
| `CanInfuse` | `BoardModel board, GridPos ingredient, GridPos brew` | `InfuseValidation` | Проверка валидности подпитки |
| `PerformMerge` | `BoardModel board, GridPos from, GridPos to` | `MergeResult` | Выполнить merge, вернуть изменённый board + результат |
| `PerformInfuse` | `BoardModel board, GridPos ingredient, GridPos brew` | `InfuseResult` | Выполнить подпитку |
| `CollectBrews` | `BoardModel board, InventoryModel inventory` | `CollectionResult` | Собрать все варева в инвентарь |
| `GetRecipe` | `ElementType a, ElementType b` | `PotionType?` | Найти рецепт для пары элементов |
| `GetValidInfusionElements` | `PotionType brewType` | `List<ElementType>` | Какие элементы подходят для подпитки |

**Зависимости**: IConfigProvider (для PotionDB, RecipeDB).

**Публикуемые события**: `MergePerformed`, `InfusionPerformed`, `ActionSpent`, `BrewCollected`, `InvalidAction`.

### 7.2 CombatService

**Обязанности**: Управление боем. Инициализация сетки. Разрешение ходов.

| Метод | Входы | Выход | Описание |
|-------|-------|-------|----------|
| `InitCombat` | `CombatSetup setup, InventoryModel inv, List<RelicType> relics` | `CombatModel` | Создать модель боя |
| `GetValidMoves` | `CombatModel model` | `List<GridPos>` | Клетки, куда может пойти игрок |
| `GetValidTargets` | `CombatModel model, PotionType potion, int level` | `List<GridPos>` | Клетки, куда можно бросить зелье |
| `GetPushTargets` | `CombatModel model` | `List<(EntityId, Direction)>` | Враги, которых можно толкнуть |
| `MovePlayer` | `CombatModel model, GridPos destination` | `MoveResult` | Выполнить движение |
| `ThrowPotion` | `CombatModel model, int slotIndex, GridPos target` | `PotionResult` | Бросить зелье (вычислить урон, зоны, combo) |
| `PushEnemy` | `CombatModel model, EntityId enemy, Direction dir` | `PushResult` | Толкнуть врага |
| `SkipTurn` | `CombatModel model` | `SkipResult` | Пропустить и восстановить HP |
| `ResolveEnemyTurn` | `CombatModel model` | `EnemyTurnResult` | Разрешить все действия врагов |
| `EndTurn` | `CombatModel model` | `TurnEndResult` | Тик зон, кулдаунов, эффектов. Проверка win/lose |
| `CheckCombatEnd` | `CombatModel model` | `CombatResult?` | Проверка: все враги мертвы? Игрок мёртв? |

**Зависимости**: IDamageService, IAIService, IConfigProvider.

**Публикуемые события**: все Combat Events из секции 4.3.

### 7.3 DamageService

**Обязанности**: Вычисление урона. Применение формул. Обработка брони, статус-эффектов, модификаторов.

| Метод | Описание |
|-------|----------|
| `CalculateDamage(PotionType, int level, List<RelicType> relics, RoomModifier? mod, List<ZoneType> targetZones)` | Финальный урон с учётом всех множителей |
| `ApplyDamage(EnemyCombatModel target, int damage, DamageType type)` | Применить урон к врагу (учёт брони) |
| `ApplyDamageToPlayer(PlayerCombatModel player, int damage, DamageType type)` | Применить урон к игроку |
| `CheckCombo(PotionType potion, GridPos target, GridModel grid)` | Проверить, срабатывает ли combo |
| `CalculateComboDamage(ComboType combo, int baseDamage, List<RelicType> relics)` | Урон combo |
| `GetAffectedCells(PotionType, int level, GridPos target, GridModel grid)` | Список клеток AoE |

### 7.4 AIService

**Обязанности**: Определение намерений врагов. Вычисление их ходов.

| Метод | Описание |
|-------|----------|
| `DetermineIntents(CombatModel model)` | Для каждого живого врага определить Intent (тип, цель, урон) |
| `ExecuteIntents(CombatModel model)` | Выполнить все Intent-ы (движение + действия) |

**Архитектура AI**: Паттерн **Strategy**. Каждый тип врага реализует интерфейс `IEnemyBehavior`:

| Метод | Описание |
|-------|----------|
| `DetermineIntent(EnemyCombatModel self, CombatModel context)` | Определить намерение |
| `Execute(EnemyCombatModel self, CombatModel context, EnemyIntent intent)` | Выполнить намерение |

Реализации:

| Класс | Тип врага | Логика |
|-------|-----------|--------|
| `SkeletonBehavior` | Скелет | Если рядом с игроком → атака. Иначе → движение к игроку на 1 клетку (A* pathfinding) |
| `SpiderBehavior` | Паук | Определить линию к игроку (гориз. или верт., ближайшая). Intent = атака по этой линии |
| `MushroomBombBehavior` | Гриб | Декремент таймера. Если 0 → Intent = взрыв 3×3. Иначе → Intent = ожидание |
| `MagnetGolemBehavior` | Голем | Intent = притяжение (сдвиг игрока на 1 клетку к себе). Если рядом → атака |
| `MirrorSlimeBehavior` | Слайм | Скопировать последнее зелье из лога. Intent = движение + бросок копии по игроку |
| `ArmoredBeetleBehavior` | Жук | Аналогично скелету, но с бронёй |
| `PhantomBehavior` | Фантом | Выбрать случайную свободную клетку в радиусе 2 от игрока. Телепорт → атака |
| `NecromancerBehavior` | Некромант | Найти убитого врага (deathTurn != null). Воскресить с 1 HP на случайной клетке. Движение от игрока |

Боссы имеют свои `IBossBehavior` с поддержкой фаз:

| Класс | Логика |
|-------|--------|
| `SpiderQueenBehavior` | Phase check → Phase1: 2 линии + движение. Phase2: призыв + 3 линии + отступление |
| `IronGolemBehavior` | Phase check → Phase1: удар 2×2 + притяжение. Phase2: ломать столбы + ускорение |
| `AlchemistBehavior` | Phase check → Phase1: копия зелья + зона. Phase2: генерация зон из котлов + телепорт |

### 7.5 MapGeneratorService

**Обязанности**: Генерация карты этажа. Расстановка типов узлов. Генерация составов врагов.

| Метод | Описание |
|-------|----------|
| `GenerateFloor(int floorNumber, int seed, MetaProgressionModel meta)` | Создать полную карту этажа |
| `GenerateCombatSetup(int floorNumber, NodeType type, int seed)` | Создать состав врагов для узла |
| `GenerateRoomModifier(int floorNumber, int seed)` | Выбрать модификатор комнаты (или null) |

**Алгоритм генерации карты**: Описан в секции 9.

### 7.6 LootService

**Обязанности**: Генерация наград. Выбор реликвий. Генерация зелий в событиях.

| Метод | Описание |
|-------|----------|
| `GenerateRelicChoices(int count, List<RelicType> owned, MetaProgressionModel meta)` | Выбрать N реликвий из пула (не дублировать имеющиеся) |
| `GenerateEventPotion(int floorNumber, MetaProgressionModel meta)` | Случайное зелье для события |
| `GenerateMerchantStock(int floorNumber, MetaProgressionModel meta)` | 3 зелья для торговца |

### 7.7 SaveService

**Обязанности**: Сохранение и загрузка. Мета-прогрессия. Mid-run save.

| Метод | Описание |
|-------|----------|
| `SaveMeta(MetaProgressionModel)` | Сохранить мета-прогрессию |
| `LoadMeta()` | Загрузить мета-прогрессию (или создать новую) |
| `SaveRun(RunModel)` | Сохранить текущий ран (mid-run) |
| `LoadRun()` | Загрузить сохранённый ран (или null) |
| `DeleteRunSave()` | Удалить mid-run save после завершения рана |
| `SaveSettings(SettingsModel)` | Сохранить настройки |
| `LoadSettings()` | Загрузить настройки |
| `ResetAllProgress()` | Полный сброс (с подтверждением) |

**Формат**:
- Мета-прогрессия: JSON-файл в `Application.persistentDataPath/meta.json`
- Mid-run save: JSON-файл в `Application.persistentDataPath/run.json`
- Настройки: `PlayerPrefs` (маленький объём, быстрый доступ)

**Сериализация**: Unity `JsonUtility` для простых структур. Для сложных (Dictionary, HashSet) — обёртка с массивами.

**Защита от потери данных**:
- Запись в временный файл → атомарное переименование
- Бэкап предыдущей версии (1 поколение)
- Checksum (простой CRC32) для детекции повреждений

### 7.8 AudioService

**Обязанности**: Воспроизведение SFX и музыки. Управление громкостью.

| Метод | Описание |
|-------|----------|
| `PlaySFX(SFXType type, float volumeScale = 1.0)` | Воспроизвести звуковой эффект |
| `PlayMusic(MusicTrack track, float fadeTime = 1.0)` | Переключить музыку с fade |
| `StopMusic(float fadeTime = 0.5)` | Остановить музыку |
| `SetSFXVolume(float volume)` | Громкость SFX (0–1) |
| `SetMusicVolume(float volume)` | Громкость музыки (0–1) |

**Реализация**: Пул `AudioSource`-ов (8 для SFX, 2 для музыки — crossfade). Один `AudioListener` на камере.

**Подписки на события для автоматических звуков**:

| Событие | Звук |
|---------|------|
| `MergePerformed` | Merge SFX (варьируется по типу результата) |
| `InfusionPerformed` | Infuse SFX (rising tone) |
| `PotionThrown` | Throw SFX (зависит от типа зелья) |
| `DamageDealt` | Hit SFX (зависит от DamageType) |
| `EnemyKilled` | Death SFX |
| `ComboTriggered` | Combo SFX (мощный, с эхо) |
| `PlayerDamaged` | Hurt SFX + Rumble |
| `CombatStarted` | Encounter jingle |
| `CombatEnded (Victory)` | Victory jingle |

### 7.9 InputService

**Обязанности**: Абстракция ввода. Трансляция жестов в игровые команды.

**Архитектура**: InputService не знает о текущем контексте игры. Он регистрирует жесты и публикует Input Events. GameState интерпретирует события в зависимости от контекста.

| Жест | Input Event |
|------|-------------|
| Tap (short touch, <0.3s, no movement) | `TapEvent { screenPos, worldPos }` |
| Swipe (touch + drag > 20px + release) | `SwipeEvent { startPos, endPos, direction, screenDelta }` |
| Drag start (touch + hold > 0.15s) | `DragStartEvent { screenPos, worldPos }` |
| Drag update (finger moves while held) | `DragUpdateEvent { screenPos, worldPos, delta }` |
| Drag end (release after drag) | `DragEndEvent { screenPos, worldPos }` |
| Long press (hold > 0.5s, no movement) | `LongPressEvent { screenPos, worldPos }` |

**Обработка в GameState**:

| GameState | Tap | Swipe | Drag |
|-----------|-----|-------|------|
| DistillationState | — | — | DragStart на ингредиенте → начать перетаскивание. DragEnd на цели → merge/подпитка |
| CombatState (WaitMove) | Тап на клетку = движение (если в радиусе) | Свайп от персонажа = движение | — |
| CombatState (WaitAction) | Тап на зелье = выбрать. Тап на клетку = бросить. Тап на себя = пропуск | Свайп от врага = толчок | — |
| MapState | Тап на узел = выбрать | — | — |
| EventState | Тап на вариант = выбрать | — | — |

**Координатные системы**:
- `screenPos`: пиксели экрана
- `worldPos`: координаты мира (Camera.ScreenToWorldPoint)
- `gridPos`: координаты ячейки сетки (вычисляется из worldPos + offset и scale сетки)

---

## 8. View-слой

### 8.1 Принципы

- Каждый View — MonoBehaviour, прикреплённый к GameObject в сцене
- View **не содержит логики**. Только отображение состояния + анимации
- View **подписывается на события** из Event Bus. Реагирует обновлением визуала
- View **не хранит игровое состояние**. Может хранить визуальное состояние (анимации, tweens)
- View общается «вверх» только через UI Events

### 8.2 Иерархия View-объектов в сцене

```
Game (Scene Root)
├── Camera
│   └── PostProcessController
│
├── World
│   ├── Background                   # SH_Background quad
│   │
│   ├── BoardRoot                    # Доска перегонки (вкл/выкл)
│   │   ├── BoardBackground          # SH_BoardBackground
│   │   ├── IngredientViews[16]      # SH_Ingredient (pooled)
│   │   └── BrewViews[3]            # SH_Brew (pooled)
│   │
│   ├── GridRoot                     # Боевая сетка (вкл/выкл)
│   │   ├── FloorTiles[25..36]      # SH_Floor (pooled per grid size)
│   │   ├── WallTiles[~20]          # SH_Wall
│   │   ├── ZoneOverlays[pooled]    # SH_Zone_* (pool: 10)
│   │   ├── PlayerView              # SH_Player
│   │   ├── EnemyViews[pooled]      # SH_Enemy_* (pool: 6)
│   │   └── EffectsRoot
│   │       ├── EffectViews[pooled] # SH_FX_* (pool: 5)
│   │       └── DamageNumbers[pooled]# SH_FX_DamageNumber (pool: 8)
│   │
│   └── MapRoot                      # Карта этажа (вкл/выкл)
│       ├── MapBackground
│       ├── MapNodeViews[pooled]     # Узлы (pool: 12)
│       └── MapPathViews[pooled]     # Пути между узлами (pool: 15)
│
├── Canvas (Screen Space - Overlay)
│   ├── HUD                          # Всегда видимый
│   │   ├── HealthBar               # SH_UI_HealthHeart × 5
│   │   ├── PotionSlots             # SH_PotionSlot × 4-5
│   │   ├── RelicIcons              # Маленькие иконки реликвий
│   │   ├── ActionDots              # Точки действий перегонки
│   │   ├── SkipButton              # Кнопка пропуска
│   │   └── PauseButton
│   │
│   ├── Screens                      # По одному экрану, вкл/выкл
│   │   ├── MenuScreen
│   │   ├── ResultScreen
│   │   ├── EventScreen
│   │   ├── BookScreen
│   │   └── AchievementScreen
│   │
│   ├── Popups                       # Модальные окна
│   │   ├── PausePopup
│   │   ├── RelicChoicePopup
│   │   ├── SlotReplacePopup
│   │   └── ConfirmPopup
│   │
│   └── Overlays                     # Поверх всего
│       ├── TutorialOverlay
│       ├── TransitionOverlay        # Fade in/out
│       └── NotificationOverlay     # Разблокировки
│
└── AudioRoot
    ├── SFXSources[8]               # Пул AudioSource для SFX
    └── MusicSources[2]             # Для crossfade музыки
```

### 8.3 Ключевые View-классы

**BoardView**: Управляет визуализацией доски перегонки.
- Создаёт/переиспользует IngredientView и BrewView из пула
- Обрабатывает DragStart/Update/End для ингредиентов (визуальная часть: деформация, подсветка целей)
- Подписки: `DistillationPhaseStarted` → инициализация. `MergePerformed` → анимация merge. `InfusionPerformed` → анимация подпитки. `DistillationPhaseEnded` → анимация сбора
- Анимации: притяжение (lerp), слияние (shockwave), bounce, сбор в инвентарь (bezier curve)

**GridView**: Управляет визуализацией боевой сетки.
- Создаёт FloorTileView для каждой клетки
- Управляет подсветкой клеток (доступные ходы, опасные зоны, цели бросков)
- Подписки: `CombatStarted` → инициализация. `ZoneCreated/Expired` → управление overlay-ями. `PlayerMoved` → анимация движения

**EntityView (abstract)**: Базовый класс для PlayerView и EnemyView.
- Управляет общими визуальными параметрами: flash, dissolve, squash, opacity
- Tween-анимации: движение (lerp позиции), удар (squash → bounce back), смерть (dissolve)
- MaterialPropertyBlock для per-instance параметров шейдера

**PlayerView extends EntityView**: Визуализация персонажа игрока.
- Дополнительно: обновление `_LastPotionColor`, move trail, idle bob
- Подписки: `PlayerMoved`, `PotionThrown`, `PlayerDamaged`

**EnemyView extends EntityView**: Визуализация одного врага.
- Подписки: `EnemyActed`, `DamageDealt (filtered by entityId)`, `EnemyKilled`
- IntentionIconView: дочерний объект, отображает намерение
- Управление per-enemy параметрами шейдера (через MaterialPropertyBlock)

**PotionSlotView**: Один слот зелья в HUD.
- Подписки: `BrewCollected`, `CooldownTick`, `PotionThrown`
- Обновление MaterialPropertyBlock: `_CooldownNormalized`, `_Selected`, `_Level`

**EffectView**: Визуализация одного эффекта зелья (кратковременный).
- Получает из пула. Настраивает параметры. Запускает анимацию (progress 0→1). Возвращается в пул по завершении
- Отдельные подклассы: `ExplosionEffectView`, `LightningEffectView`, `AcidEffectView`, `HealEffectView`, `SteamEffectView`

### 8.4 Система анимаций

Анимации реализованы через **лёгкую tween-систему** (собственная, без DOTween — для контроля размера билда).

**Компоненты tween-системы**:

| Компонент | Описание |
|-----------|----------|
| `TweenRunner` | MonoBehaviour-синглтон. Ведёт список активных tweens. Обновляет в Update/LateUpdate |
| `Tween` | Структура: target value, duration, ease function, callback |
| `EaseFunctions` | Статический класс с функциями: Linear, EaseInOut, EaseOutElastic, EaseOutBounce, EaseOutBack |

**Типы tweens**:

| Тween | Что анимирует |
|-------|---------------|
| `TweenPosition` | Transform.localPosition |
| `TweenScale` | Transform.localScale |
| `TweenAlpha` | MaterialPropertyBlock float (например, `_Opacity`) |
| `TweenFloat` | Произвольный float → callback |
| `TweenColor` | MaterialPropertyBlock color |
| `TweenSequence` | Цепочка tweens (последовательно) |
| `TweenParallel` | Группа tweens (параллельно) |

**Анимационные последовательности для ключевых моментов**:

| Момент | Последовательность |
|--------|-------------------|
| Merge | `[Parallel: IngredientA lerp → target (0.2s, EaseInOut), IngredientB scale → 0 (0.15s)] → Shockwave (0.1s) → BrewView spawn + bounce (0.3s, EaseOutElastic)` |
| Подпитка | `[Ingredient lerp → brew (0.15s)] → Brew glow flash (0.1s) → Brew fill level up (0.2s, EaseOutBack)` |
| Бросок зелья | `[PotionSlot pulse (0.1s)] → Projectile arc (0.25s, parabolic) → Impact effect (0.4s) + Damage numbers (0.6s, bounce)` |
| Движение игрока | `[Player lerp to target (0.2s, EaseInOut)] — squash & stretch during motion` |
| Смерть врага | `[Flash white (0.05s)] → Dissolve (0.4s) → Particles burst (0.3s) → Particles fly to player (0.4s, bezier)` |
| Combo | `[Slowmo (0.15s)] → Enhanced effect (0.5s) → Combo text fly-in (0.3s, EaseOutBack) → Text fade (0.4s)` |

---

## 9. Генерация контента

### 9.1 Генерация карты этажа

**Алгоритм**:

```
Вход: floorNumber, seed

1. Определить количество рядов: 3-4 (случайно из seed)
2. Определить количество узлов в ряду: 1-3 (случайно, но минимум 2 на карту)
3. Для каждого узла:
   a. Определить тип (см. правила ниже)
   b. Определить позицию для отображения (X: равномерно + jitter, Y: по ряду)
4. Соединить узлы:
   a. Каждый узел в ряду N связан с 1-2 узлами в ряду N+1
   b. Гарантировать: каждый узел достижим хотя бы из одного узла предыдущего ряда
   c. Гарантировать: каждый узел имеет хотя бы один путь к боссу
5. Финальный ряд: один узел Boss
6. Присвоить модификаторы комнат (для Combat-узлов, этаж 2+)
7. Предгенерировать CombatSetup для каждого боевого узла
```

**Правила типов узлов**:

| Правило | Описание |
|---------|----------|
| Ровно 1 Elite на этаж | Размещается в среднем ряду (ряд 2 из 4 или ряд 1-2 из 3) |
| 1-2 Event на этаж | Не в первом ряду (чтобы первый бой был гарантирован). Не два подряд по одному пути |
| Boss всегда последний | Один узел, все пути ведут к нему |
| Остальные — Combat | Заполняют оставшиеся слоты |
| Этаж 1, ряд 1 | Всегда один Combat-узел (для обучающего боя с 1 скелетом) |

### 9.2 Генерация доски перегонки

**Алгоритм**:

```
Вход: floor, unlockedRecipes, boardWidth, boardHeight, seed

1. Определить пул элементов по этажу:
   - Этаж 1: [Ignis, Aqua, Toxin]
   - Этаж 2: + [Lux]
   - Этаж 3: + [Umbra]

2. Определить квоты (примерно равные доли):
   - totalCells = width × height (16 или 20)
   - perElement = totalCells / elementCount
   - Остаток распределить случайно

3. Создать список из totalCells элементов (по квотам), перемешать (Fisher-Yates, seed)

4. Заполнить сетку row by row

5. Валидация:
   - Подсчитать количество валидных пар (соседние ингредиенты, merge которых даёт разблокированный рецепт ИЛИ одинаковые)
   - Если < 3 пар → перегенерировать (новый seed = seed + 1)
   - Максимум 5 попыток, потом принудительно поставить пары
```

### 9.3 Генерация состава врагов

**Алгоритм**:

```
Вход: floorNumber, nodeType, seed, meta

1. Определить пул врагов по этажу (из EnemyDB)
2. Определить бюджет:
   - Combat: {Этаж1: 3-8 HP суммарно, Этаж2: 6-14, Этаж3: 8-16}
   - Elite: бюджет × 1.5
3. Случайно выбирать врагов из пула, пока бюджет не исчерпан:
   - Стоимость врага ≈ HP + specialWeight
   - Не более 3-4 врагов за бой
   - Не более 1 элитного типа (Necromancer)
4. Расставить врагов на сетке:
   - Игрок всегда в клетке (1,1) или (0,0)
   - Враги — минимум 2 клетки от игрока
   - Дальнобойные (паук) — на расстоянии 3+
   - Статичные (голем, гриб) — ближе к центру
5. Разместить объекты модификатора (ямы, столбы и т.д.)
```

### 9.4 Детерминизм

Вся генерация — **детерминированная** через seed. Один seed → одинаковый ран на любом устройстве. Это позволяет:
- Воспроизводить баги
- Daily challenge (одинаковый seed для всех игроков — будущая фича)
- Тестирование баланса

Seed генерируется из `System.DateTime.Now.Ticks` при старте рана и сохраняется в RunModel.

---

## 10. Object Pool

### 10.1 Архитектура

| Компонент | Описание |
|-----------|----------|
| `ObjectPoolService` | Центральный сервис. Хранит пулы по типу prefab-а |
| `PooledObject` | MonoBehaviour-тег на pooled-объектах. Хранит ссылку на родительский пул |
| `Pool<T>` | Типизированный пул. Stack свободных объектов. Фабрика для создания новых |

### 10.2 API

| Метод | Описание |
|-------|----------|
| `Get<T>(prefab)` | Взять объект из пула (или создать). Вызвать `OnGet()` |
| `Return(instance)` | Вернуть в пул. Вызвать `OnReturn()`. Деактивировать |
| `Prewarm<T>(prefab, count)` | Предсоздать N объектов (при инициализации) |

### 10.3 Пулируемые объекты

| Объект | Размер пула | Когда используется |
|--------|-------------|-------------------|
| IngredientView | 20 | Доска перегонки (16 ячеек + запас) |
| BrewView | 4 | Варева на доске (максимум 3 за фазу + 1 запас) |
| FloorTileView | 36 | Сетка (максимум 6×6) |
| EnemyView (per type) | 4 per type | Враги в бою (максимум 4 одного типа) |
| ZoneOverlayView | 12 | Зоны на поле |
| EffectView (per type) | 3 per type | Эффекты зелий |
| DamageNumberView | 8 | Числа урона |
| MapNodeView | 12 | Узлы карты |
| MapPathView | 15 | Пути между узлами |
| ParticleView | 20 | Универсальные частицы (dissolve, лечение) |

### 10.4 Prewarm

При загрузке `Game.unity`:
- Все пулы предсоздаются с минимальным размером
- Цель: избежать runtime-аллокаций во время геймплея
- Prewarm выполняется в корутине (1-2 объекта за кадр) для избежания freeze при загрузке

---

## 11. Управление камерой

### 11.1 Настройки камеры

| Параметр | Значение |
|----------|----------|
| Проекция | Ортографическая |
| Size | Динамический (подстраивается под aspect ratio) |
| Фон | Solid color (#0A0A12) |
| Позиция | (0, 0, -10) |

### 11.2 Адаптация под aspect ratio

Целевое: 9:16 (1080×1920). Поддерживаемый диапазон: 9:18 (high phones) до 3:4 (планшеты).

**Стратегия**: контент рассчитан на 9:16. При более узком экране — letterbox по горизонтали. При более широком — дополнительное пространство по бокам (фон).

Сетка боя и доска перегонки всегда **центрированы** и **масштабированы** так, чтобы помещаться с отступами.

**Вычисление**:
```
targetWorldWidth = gridColumns * cellSize + padding
cameraSize = targetWorldWidth / (2 * aspectRatio)
// Ограничить cameraSize минимумом (чтобы не обрезать по высоте)
```

### 11.3 Screen Shake

Реализация в `ShaderGlobalController`:
- Смещение `_ScreenShake` (Vector2) передаётся во все шейдеры через `Shader.SetGlobalVector`
- Шейдеры добавляют смещение в вершинном шейдере: `v.vertex.xy += _ScreenShake`
- Значение управляется из C#: случайное направление, затухание по AnimationCurve

---

## 12. UI-система

### 12.1 Архитектура

Unity Canvas (Screen Space — Overlay). Один Canvas с несколькими sub-canvas-ами (для оптимизации rebuild-а).

| Sub-Canvas | Содержимое | Rebuild частота |
|------------|------------|-----------------|
| HUD Canvas | HP, зелья, кнопки | Редко (при изменении HP/кулдауна) |
| Screen Canvas | Экраны (меню, результат) | При смене экрана |
| Popup Canvas | Модальные окна | При открытии/закрытии |
| Overlay Canvas | Переходы, нотификации | Редко |

### 12.2 UIManager

MonoBehaviour-синглтон. Управляет показом/скрытием экранов и popup-ов.

| Метод | Описание |
|-------|----------|
| `ShowScreen(ScreenType type)` | Показать экран (скрыть предыдущий) |
| `ShowPopup(PopupType type, PopupData data)` | Показать popup поверх текущего экрана |
| `ClosePopup()` | Закрыть верхний popup |
| `ShowNotification(string text, float duration)` | Показать нотификацию (разблокировка) |
| `PlayTransition(TransitionType type, Action onMidpoint)` | Переход с fade (callback на середине — для смены контента) |

### 12.3 Экраны

Каждый экран — MonoBehaviour со стандартным интерфейсом:

| Метод | Описание |
|-------|----------|
| `Show(ScreenData data)` | Активировать. Заполнить данными. Анимация входа |
| `Hide()` | Анимация выхода. Деактивировать |
| `OnButtonClicked(ButtonId id)` | Обработка кнопок (публикует `ButtonPressed` в Event Bus) |

### 12.4 Переходы между экранами

```
Текущий экран
    │
    ▼ PlayTransition(FadeToBlack)
    │
    ├── Fade to black (0.2s)
    │
    ├── onMidpoint callback:
    │   ├── Hide current screen
    │   ├── Show new screen
    │   ├── Switch world views (board/grid/map)
    │   └── Update HUD
    │
    ├── Fade from black (0.2s)
    │
    ▼
Новый экран
```

---

## 13. Обучение (Tutorial System)

### 13.1 Архитектура

| Компонент | Описание |
|-----------|----------|
| `TutorialController` | MonoBehaviour. Управляет потоком обучения. Проверяет условия перехода между шагами |
| `TutorialStep` | ScriptableObject. Описывает один шаг: условие начала, подсказки, ограничения ввода, условие завершения |
| `TutorialOverlay` | View-компонент. Отображает стрелки, подсветку, затемнение |
| `TutorialData` | ScriptableObject. Последовательность шагов |

### 13.2 Механизм ограничения ввода

Во время обучения TutorialController может ограничить ввод:
- Заблокировать все клетки кроме целевых (затемнение + блокировка tap/drag)
- Подсветить единственную правильную цель (пульсация + стрелка)
- Отключить кнопки, не относящиеся к текущему шагу

Реализация: TutorialController устанавливает `InputFilter` в InputService — маску допустимых зон экрана.

### 13.3 Условия перехода

Каждый TutorialStep определяет `CompletionCondition`:

| Шаг | Условие завершения |
|-----|-------------------|
| 1 (Merge) | Событие `MergePerformed` получено 2 раза |
| 2 (Первый бой) | Событие `CombatEnded(Victory)` |
| 3 (Кулдаун) | Событие `CombatEnded(Victory)` (второй бой) |
| 4 (Подпитка + рецепт) | Событие `InfusionPerformed` + `BookOpened` |
| 5 (Свободная игра) | — (завершает обучение) |

Прогресс обучения сохраняется в MetaProgressionModel: `tutorialCompleted: bool`.

---

## 14. Мета-прогрессия (Achievement System)

### 14.1 Архитектура

| Компонент | Описание |
|-----------|----------|
| `AchievementService` | Сервис. Проверяет условия ачивок. Разблокирует награды |
| `AchievementDefinition` | ScriptableObject. Описание одной ачивки: id, условие, награда |
| `AchievementDB` | ScriptableObject. Список всех ачивок |
| `AchievementTracker` | Подписывается на игровые события и обновляет прогресс в MetaProgressionModel |

### 14.2 Типы условий

| Тип условия | Поля | Пример |
|-------------|------|--------|
| `AccumulateStat` | `StatType stat, int target` | «Используй 30 зелий суммарно» |
| `SingleRunCondition` | `Predicate<RunStatsModel>` | «Дойди до Этажа 2» |
| `CombatCondition` | `Predicate<CombatModel>` | «Убей элитного врага без урона» |
| `CompositeCondition` | `List<Condition>, LogicOp (AND/OR)` | «Открой все остальные рецепты» |

### 14.3 Поток разблокировки

```
Событие (например, EnemyKilled)
    │
    ▼
AchievementTracker.OnEnemyKilled()
    │
    ├── Обновить stats в MetaProgressionModel
    │
    ├── Для каждой незавершённой ачивки:
    │   └── Проверить условие
    │       └── Если выполнено:
    │           ├── Пометить как завершённую
    │           ├── Применить награду (разблокировать рецепт/реликвию/улучшение)
    │           ├── Publish: AchievementUnlocked
    │           └── SaveService.SaveMeta()
    │
    ▼
AchievementUnlocked → UI NotificationOverlay → показать нотификацию
```

---

## 15. Relic System

### 15.1 Архитектура

| Компонент | Описание |
|-----------|----------|
| `RelicDefinition` | ScriptableObject. ID, описание, иконка, эффект |
| `RelicDB` | ScriptableObject. Все реликвии |
| `RelicEffect` | Абстрактный класс. Подклассы реализуют конкретные эффекты |
| `RelicManager` | Сервис. Хранит активные реликвии рана. Применяет эффекты |

### 15.2 Типы эффектов реликвий

| Тип | Как работает | Примеры |
|-----|-------------|---------|
| `PassiveModifier` | Изменяет числовые параметры (множители) | Линза (+1 AoE), Призма (×1.5 combo), Декантер (+1 ход зон) |
| `EventTrigger` | Подписывается на событие, выполняет действие | Термос (при PotionThrown, 25% шанс не начинать cooldown), Фляга (при CombatEnded, +1 HP) |
| `StartOfCombat` | Вызывается при CombatStarted | Вихрь (создать случайную зону), Монокль (показать 2 хода намерений) |
| `ModifyCombat` | Изменяет правила боя | Шип (толчок наносит 2 урона) |
| `ModifyDistillation` | Изменяет правила перегонки | Мутаген (20% шанс бонусной капли), Кубик (+1 действие) |

### 15.3 Применение эффектов

Сервисы проверяют наличие реликвий через `RelicManager`:

```
// В DamageService:
int damage = baseDamage;
if (relicManager.Has(RelicType.Prism) && isCombo) {
    damage = (int)(damage * 1.5f);
}

// В CombatService, при толчке:
int pushDamage = relicManager.Has(RelicType.Spike) ? 2 : 0;
```

---

## 16. Shader Management

### 16.1 ShaderGlobalController

MonoBehaviour на камере. Управляет глобальными uniform-ами и пост-процессингом.

**Update-цикл**:

```
ShaderGlobalController.Update()
│
├── Обновить _GameTime (с учётом паузы и slowmo)
│   _GameTime += Time.deltaTime * _Slowmo (если не пауза)
│
├── Обновить _ScreenShake (decay)
│   _ScreenShake *= shakeCurve.Evaluate(elapsed / duration)
│
├── Обновить _GlobalFlash (decay)
│   _GlobalFlash.a -= Time.deltaTime / flashDuration
│
├── Обновить _Slowmo (decay)
│   Lerp _Slowmo → 1.0
│
├── Обновить _ChromaticAmount (decay)
│
├── Обновить _DamageVignetteAmount (decay)
│
└── Shader.SetGlobalFloat/Vector/Color для каждого параметра
```

### 16.2 MaterialManager

Управляет созданием и кэшированием MaterialPropertyBlock-ов.

| Метод | Описание |
|-------|----------|
| `GetPropertyBlock(Renderer renderer)` | Получить (или создать) MaterialPropertyBlock для renderer-а |
| `SetFloat(Renderer, string property, float value)` | Установить float в property block + применить |
| `SetColor(Renderer, string property, Color value)` | Установить цвет |
| `BatchUpdate(List<Renderer>, string property, float[] values)` | Массовое обновление (для тайлов пола, ингредиентов) |

### 16.3 Quality Controller

| Метод | Описание |
|-------|----------|
| `DetectQuality()` | Автоопределение качества (см. шейдерную документацию) |
| `SetQuality(QualityLevel level)` | Переключение keyword-ов: `QUALITY_HIGH` / `QUALITY_LOW` |
| `MonitorPerformance()` | Корутина: каждые 5 сек проверяет средний FPS. Если <45 → понизить качество |

---

## 17. Потоки данных (Data Flow)

### 17.1 Ход боя (полный поток)

```
1. [CombatState: WaitingForMove]
   │
   ├── InputService → TapEvent(gridPos)
   │
   ├── CombatState получает TapEvent
   │   ├── Проверить: gridPos в validMoves?
   │   ├── Если да: CombatService.MovePlayer(model, gridPos)
   │   │   ├── Обновить model.player.position
   │   │   ├── model.player.hasMoved = true
   │   │   └── EventBus.Publish(PlayerMoved{from, to})
   │   │       └── Views реагируют:
   │   │           ├── PlayerView: tween position
   │   │           ├── GridView: обновить подсветку
   │   │           └── AudioService: play move SFX
   │   └── CombatState → WaitingForAction
   │
2. [CombatState: WaitingForAction]
   │
   ├── Player тапает зелье (UI) → PotionSlotTapped{slotIndex}
   │   ├── CombatState: запомнить selectedSlot
   │   ├── GridView: подсветить validTargets
   │   └── PotionSlotView: выделить слот
   │
   ├── Player тапает клетку → GridCellTapped{gridPos}
   │   ├── CombatState: если selectedSlot != null
   │   │   ├── CombatService.ThrowPotion(model, slotIndex, gridPos)
   │   │   │   ├── DamageService.GetAffectedCells()
   │   │   │   ├── DamageService.CalculateDamage()
   │   │   │   ├── DamageService.CheckCombo()
   │   │   │   ├── Для каждого affected cell:
   │   │   │   │   ├── Если есть враг: DamageService.ApplyDamage()
   │   │   │   │   │   ├── EventBus.Publish(DamageDealt{...})
   │   │   │   │   │   └── Если kill: EventBus.Publish(EnemyKilled{...})
   │   │   │   │   └── Если зелье создаёт зону: модель обновляется
   │   │   │   │       └── EventBus.Publish(ZoneCreated{...})
   │   │   │   ├── Если combo: EventBus.Publish(ComboTriggered{...})
   │   │   │   ├── Обновить cooldown слота
   │   │   │   └── EventBus.Publish(PotionThrown{...})
   │   │   │
   │   │   └── Views реагируют:
   │   │       ├── EffectView: spawn + animate
   │   │       ├── EnemyView: flash, damage number, dissolve (если kill)
   │   │       ├── ZoneOverlayView: spawn
   │   │       ├── PotionSlotView: обновить cooldown
   │   │       ├── ShaderGlobalController: screenshake, flash, slowmo
   │   │       └── AudioService: impact SFX, combo SFX
   │   │
   │   ├── model.player.hasActed = true
   │   └── CombatState → AnimatingPlayerAction → ResolvingEnemyTurn
   │
3. [CombatState: ResolvingEnemyTurn]
   │
   ├── EventBus.Publish(EnemyTurnStarted)
   ├── AIService.ExecuteIntents(model)
   │   ├── Для каждого живого врага:
   │   │   ├── behavior.Execute(enemy, model, intent)
   │   │   │   ├── Движение: обновить position
   │   │   │   ├── Атака: DamageService.ApplyDamageToPlayer()
   │   │   │   │   └── EventBus.Publish(PlayerDamaged{...})
   │   │   │   ├── Спецдействие: зависит от типа
   │   │   │   └── EventBus.Publish(EnemyActed{...})
   │   │   │
   │   │   └── Views реагируют: анимация врага
   │
   ├── CombatService.EndTurn(model)
   │   ├── Тик зон (уменьшить duration, удалить expired)
   │   ├── Тик кулдаунов зелий
   │   ├── Тик статус-эффектов
   │   ├── Применить zone damage (к стоящим на зонах)
   │   ├── Конвейер (если модификатор): сдвинуть все сущности
   │   ├── Горящая комната: добавить новые зоны огня
   │   ├── Таймер: проверить лимит ходов
   │   └── AIService.DetermineIntents(model) → обновить intent-ы
   │       └── Views: обновить IntentionIconView
   │
   ├── CombatService.CheckCombatEnd(model)
   │   ├── Все враги мертвы → EventBus.Publish(CombatEnded{Victory})
   │   ├── Игрок мёртв → EventBus.Publish(CombatEnded{Defeat})
   │   └── Иначе: reset hasMoved/hasActed, increment turnNumber
   │
   └── CombatState → WaitingForMove (новый ход)
       или → победа/смерть → transition
```

### 17.2 Фаза перегонки (полный поток)

```
1. [DistillationState: Enter]
   │
   ├── DistillationService.GenerateBoard(floor, meta, seed)
   │   └── → BoardModel
   │
   ├── EventBus.Publish(DistillationPhaseStarted{board})
   │   └── BoardView: создать IngredientViews, анимация появления
   │
   └── DistillationState: ожидание ввода
   
2. [Игрок перетаскивает ингредиент]
   │
   ├── InputService → DragStartEvent{pos}
   │   └── BoardView: определить IngredientView под пальцем
   │       ├── Увеличить ингредиент (×1.2)
   │       ├── Запросить у DistillationService: GetValidMergeTargets + GetValidInfusionTargets
   │       └── Подсветить валидные соседние клетки
   │
   ├── InputService → DragUpdateEvent{pos}
   │   └── BoardView: перемещать ингредиент за пальцем, деформация
   │       └── Если палец над валидной целью: показать превью результата
   │
   ├── InputService → DragEndEvent{pos}
   │   └── BoardView: определить целевую клетку
   │       ├── Если нет цели или невалидная: bounce back, ничего не тратить
   │       ├── Если цель — ингредиент (merge):
   │       │   ├── DistillationService.PerformMerge(board, from, to)
   │       │   │   ├── Обновить BoardModel (потребить оба, создать brew)
   │       │   │   ├── EventBus.Publish(MergePerformed{...})
   │       │   │   └── EventBus.Publish(ActionSpent{remaining})
   │       │   └── BoardView: анимация merge
   │       │
   │       └── Если цель — варево (подпитка):
   │           ├── DistillationService.PerformInfuse(board, ingredient, brew)
   │           │   ├── Обновить BoardModel (потребить ингредиент, level up brew)
   │           │   ├── EventBus.Publish(InfusionPerformed{...})
   │           │   └── EventBus.Publish(ActionSpent{remaining})
   │           └── BoardView: анимация подпитки
   │
3. [Проверка завершения]
   │
   ├── Если actionsRemaining == 0 ИЛИ нажата кнопка «В бой»:
   │   ├── DistillationService.CollectBrews(board, inventory)
   │   │   ├── Для каждого варева:
   │   │   │   ├── Если зелье такого типа уже есть: обновить уровень (если новый выше)
   │   │   │   ├── Если есть свободный слот: занять
   │   │   │   ├── Если нет слота: показать SlotReplacePopup → ждать выбора
   │   │   │   └── EventBus.Publish(BrewCollected{...})
   │   │   └── BoardView: анимация сбора (варева летят в слоты)
   │   │
   │   ├── EventBus.Publish(DistillationPhaseEnded)
   │   └── DistillationState → transition → CombatState
```

---

## 18. Оптимизация

### 18.1 Render

| Оптимизация | Описание |
|------------|----------|
| GPU Instancing | Все одинаковые сущности (тайлы, ингредиенты, враги одного типа) — один draw call |
| MaterialPropertyBlock | Per-instance параметры без создания копий материалов |
| Static batching | Стены и фон — static (не двигаются) |
| Shader LOD | `QUALITY_HIGH` vs `QUALITY_LOW` (runtime keyword) |
| Bloom at half-res | Blur-пассы на ½ или ¼ разрешения |
| Noise LUT | Текстурная выборка вместо процедурного noise на слабых GPU |

### 18.2 CPU

| Оптимизация | Описание |
|------------|----------|
| Object Pooling | Нет runtime-аллокаций GameObject-ов во время боя |
| Struct Events | Events — struct, не class. Нет GC allocation при публикации |
| Cached references | ServiceLocator.Get<> вызывается один раз при Init, кэшируется в поле |
| LateUpdate batching | MaterialPropertyBlock обновляется пакетно в LateUpdate, не per-object |
| AI caching | Pathfinding результаты кэшируются per-turn (не пересчитываются per-frame) |
| No LINQ in hot paths | Циклы вместо LINQ в Update/combat resolution |
| String interning | Shader property names → Shader.PropertyToID при Init |

### 18.3 Memory

| Оптимизация | Описание |
|------------|----------|
| Object Pool | Переиспользование вместо создания/уничтожения |
| ScriptableObject sharing | Одни и те же SO инстансы, не копии |
| Minimal textures | Только Noise LUT (256×256) + Gradient Ramp (256×4) + UI atlas (512×512) |
| No runtime Texture2D | Все текстуры — предгенерированные, нет runtime-создания |
| List<> pre-allocated | Все List<> в моделях инициализируются с capacity |

### 18.4 Профилирование

| Инструмент | Что мониторить |
|-----------|---------------|
| Unity Profiler | CPU: GC alloc, Update time. GPU: draw calls, shader time |
| Frame Debugger | Количество draw calls, batching efficiency |
| Memory Profiler | Heap size, texture memory, mesh memory |
| Xcode Instruments (iOS) / Android Studio Profiler | Real device performance, thermal throttling |

**Целевые метрики**:

| Метрика | Цель | Критический порог |
|---------|------|-------------------|
| FPS | 60 | <45 → понизить качество |
| Draw calls | <20 | >30 → оптимизировать батчинг |
| GC alloc per frame | 0 B (gameplay) | >1 KB → найти и устранить |
| RAM | <150 MB | >200 MB → утечка |
| APK size | <50 MB | >100 MB → ревизия ассетов |

---

## 19. Save/Load (детальная архитектура)

### 19.1 Формат данных

Два файла:

**meta.json** — мета-прогрессия:
```
{
  "version": 1,
  "checksum": "a1b2c3d4",
  "unlockedRecipes": [0, 1, 2, 5, 7],
  "unlockedRelics": [0, 3],
  "unlockedUpgrades": [],
  "achievementProgress": {"KILL_30_ENEMIES": 17, "USE_FOUNTAIN_3": 1},
  "completedAchievements": ["REACH_FLOOR_2"],
  "stats": {
    "totalRuns": 12,
    "totalWins": 2,
    ...
  },
  "tutorialCompleted": true
}
```

**run.json** — mid-run save (удаляется при завершении рана):
```
{
  "version": 1,
  "checksum": "e5f6g7h8",
  "seed": 123456789,
  "currentFloor": 1,
  "currentNodeIndex": 3,
  "playerHP": 4,
  "maxHP": 5,
  "inventory": {
    "slots": [
      {"type": 0, "level": 2, "cooldown": 0},
      {"type": 5, "level": 1, "cooldown": 1},
      null,
      null
    ]
  },
  "relics": [3],
  "floorMaps": [...],
  "stats": {...},
  "state": "MAP"
}
```

### 19.2 Когда сохранять

| Момент | Что сохраняется |
|--------|----------------|
| Завершение боя (победа) | run.json (обновить позицию, HP, инвентарь) |
| Завершение события | run.json |
| Выбор узла на карте | run.json |
| Завершение рана (победа/смерть) | Удалить run.json. Обновить meta.json |
| Разблокировка ачивки | meta.json |
| Изменение настроек | PlayerPrefs |
| Application.OnApplicationPause(true) | run.json (если mid-run) |
| Application.OnApplicationQuit() | run.json (если mid-run) |

### 19.3 Загрузка при старте

```
Boot:
  1. LoadMeta() → MetaProgressionModel
  2. LoadRun() → RunModel? 
  3. Если RunModel != null:
     └── Показать popup: "Продолжить прерванный ран?" [Да] [Нет]
         ├── Да: восстановить состояние → переход в сохранённый GameState
         └── Нет: DeleteRunSave() → MenuState
  4. Если RunModel == null:
     └── MenuState
```

### 19.4 Миграция версий

Поле `version` в JSON. При загрузке:
```
if (data.version < CURRENT_VERSION) {
    data = MigrationService.Migrate(data, data.version, CURRENT_VERSION);
}
```

Миграции — цепочка: v1→v2, v2→v3 и т.д. Каждая миграция — отдельный метод, добавляющий новые поля с дефолтными значениями.

---

## 20. Обработка ошибок

### 20.1 Стратегия

| Слой | Стратегия |
|------|-----------|
| Data Layer | Валидация при загрузке. Если повреждение — reset с уведомлением |
| Game Logic | Assertions в Debug. В Release — fallback (skip invalid action, log warning) |
| View Layer | Try-catch вокруг анимаций. Если view сломан — skip анимации, продолжить |
| Input | Ignore invalid input (вне допустимых зон, слишком частый) |

### 20.2 Crash Recovery

- Mid-run save при каждом изменении состояния → crash не теряет больше одного действия
- При загрузке повреждённого run.json → удалить, начать с меню
- При загрузке повреждённого meta.json → попытка восстановить из бэкапа. Если бэкап тоже повреждён → reset

### 20.3 Логирование

| Уровень | Когда | В Release |
|---------|-------|-----------|
| `Debug.Log` | Переходы состояний, генерация контента | Отключено |
| `Debug.LogWarning` | Некритичные ошибки (fallback сработал) | Включено |
| `Debug.LogError` | Критичные ошибки (состояние не консистентно) | Включено + Crashlytics (опционально) |

---

## 21. Тестирование

### 21.1 Unit Tests (EditMode)

Чистые модели и сервисы тестируются без Unity runtime:

| Модуль | Примеры тестов |
|--------|----------------|
| BoardModel | Создание, установка ячейки, получение соседей, граничные случаи |
| DistillationService | Валидация merge (одинаковые, разные, невалидные). Подпитка (допустимая, недопустимая, max level). Сбор варев. Генерация доски (гарантия пар) |
| DamageService | Формулы урона (все уровни, все множители). Combo detection. Броня. AoE расчёт |
| CombatService | Валидация движения. Бросок зелья. Толчок. Пропуск. Конец боя |
| AIService | Intent determination для каждого типа врага (все граничные случаи) |
| MapGeneratorService | Валидность карты (все узлы достижимы, ровно 1 элита, все пути ведут к боссу) |
| SaveService | Сериализация/десериализация всех моделей. Миграция. Повреждённые данные |
| RecipeDB | Все пары элементов дают ожидаемые результаты. Нет пропущенных комбинаций |
| AchievementTracker | Каждая ачивка срабатывает при правильных условиях и не срабатывает при неправильных |

### 21.2 Integration Tests (PlayMode)

| Тест | Что проверяет |
|------|---------------|
| Full Run Simulation | Прогон полного рана с автоматическим вводом (random actions). Нет exception-ов, нет infinite loop-ов |
| Tutorial Flow | Прогон обучения: все 5 шагов завершаются корректно |
| Save/Load Round Trip | Сохранить mid-run → загрузить → состояние идентично |
| State Machine Transitions | Все переходы между GameState-ами проходят без ошибок |
| 100 Random Boards | Генерация 100 досок: все проходят валидацию (≥3 пар) |
| 100 Random Maps | Генерация 100 карт: все проходят валидацию |

### 21.3 Баланс-тестирование

| Инструмент | Описание |
|-----------|----------|
| `BalanceSimulator` | Editor-скрипт. Прогоняет 10 000 ранов с оптимальной стратегией AI-игрока. Выводит статистику: win rate, average length, potion usage, death причины |
| `DamageCalculator` | Editor-окно. Ввести зелье + уровень + модификаторы → показать урон по всем типам врагов. Сколько ходов до убийства |
| `BoardAnalyzer` | Editor-окно. Сгенерировать 1000 досок → статистика: среднее количество пар, вероятность lv3, распределение элементов |

### 21.4 Manual QA Checklist

| Область | Чеклист |
|---------|---------|
| Перегонка | Все рецепты работают. Подпитка валидирует элемент. lv3 не превышается. Пустые клетки не ломают drag. Досрочный выход работает |
| Бой | Все зелья наносят правильный урон. Все AoE паттерны верны. Все враги ведут себя по описанию. Намерения совпадают с действиями. Combo срабатывают правильно. Зоны тикают правильно. Кулдауны считаются верно |
| Карта | Все пути проходимы. Элита даёт реликвию. Босс завершает этаж. Модификаторы отображаются |
| События | Все 4 типа работают. Все варианты выбора применяют эффект. HP не превышает max. Зелья попадают в слоты корректно |
| Мета | Все ачивки срабатывают. Разблокировки применяются к следующему рану. Save/load не теряет прогресс |
| UI | Все экраны отображаются. Все кнопки работают. Книга рецептов полная. Пауза работает |
| Устройства | 720×1280, 1080×1920, 1080×2400 (high phone), 1536×2048 (планшет). Нет обрезки, нет overlap |

---

## 22. Билд-пайплайн

### 22.1 Build Settings

| Параметр | Значение |
|----------|----------|
| Target | Android (первый), iOS (затем) |
| Scripting Backend | IL2CPP |
| API Compatibility | .NET Standard 2.1 |
| Minimum API Level | Android 8.0 (API 26) |
| Target Architecture | ARM64 |
| Graphics APIs | Vulkan, OpenGL ES 3.0 (fallback) |
| Managed Stripping Level | Medium |

### 22.2 Build Automation

| Шаг | Инструмент | Описание |
|-----|-----------|----------|
| 1. Pre-build | Editor Script | Валидация ScriptableObject-ов (все ссылки заполнены, нет пустых). Генерация Noise LUT, если устарел |
| 2. Build | Unity Build Pipeline | IL2CPP, ARM64, Release config |
| 3. Post-build | Gradle (Android) | ProGuard/R8. Подпись APK/AAB |
| 4. Test | Firebase Test Lab | Автоматические smoke-тесты на 5 устройствах |
| 5. Deploy | Google Play Console | Internal testing track → Closed testing → Production |

### 22.3 Версионирование

Semantic versioning: `MAJOR.MINOR.PATCH`
- MAJOR: несовместимые изменения save-формата
- MINOR: новый контент (враги, зелья, реликвии)
- PATCH: баланс, фиксы

Bundle Version Code (Android): автоинкремент при каждом билде.

---

## 23. Зависимости (третьи библиотеки)

### Минимальный набор

| Библиотека | Версия | Назначение | Обязательна? |
|-----------|--------|-----------|--------------|
| Unity 6.3 | — | Движок | Да |
| Unity UI | Built-in | Canvas UI | Да |
| Unity Test Framework | Built-in | Unit/Integration тесты | Да (dev only) |
| Newtonsoft JSON | 3.x | Сериализация сложных типов (Dictionary) | Рекомендуется (альтернатива: ручные обёртки для JsonUtility) |

### Опциональные (после MVP)

| Библиотека | Назначение | Когда добавлять |
|-----------|-----------|----------------|
| Firebase Analytics | Аналитика | Beta |
| Firebase Crashlytics | Crash reporting | Beta |
| Unity Ads / AdMob | Монетизация (если решится) | Post-release |

### Намеренно исключённые

| Библиотека | Причина исключения |
|-----------|-------------------|
| DOTween | Собственная tween-система (меньше размер, больше контроля) |
| Zenject/VContainer | Overkill для проекта такого размера. ServiceLocator достаточен |
| TextMeshPro | Минимум текста в игре. Стандартный Unity Text достаточен. SDF-шрифты — вручную через шейдеры |
| Addressables | Нет динамической загрузки контента. Всё в билде |
| UniRx | Усложняет отладку. Event Bus достаточен |

---

## 24. Контрольный список по фазам

### MVP

| Система | Что должно работать |
|---------|-------------------|
| Boot | ServiceLocator + минимальный набор сервисов |
| GSM | MenuState → DistillationState → CombatState → ResultState (линейно) |
| EventBus | Publish/Subscribe работает, основные события |
| Models | RunModel, BoardModel (4×4), CombatModel (5×5), InventoryModel (4 слота) |
| DistillationService | Генерация (3 элемента), merge, подпитка, сбор |
| CombatService | Движение, бросок, пропуск, конец боя |
| DamageService | Базовые формулы (без combo, без модификаторов) |
| AIService | SkeletonBehavior, SpiderBehavior |
| InputService | Tap, Swipe, Drag |
| Views | BoardView, GridView, PlayerView, SkeletonView, SpiderView, PotionSlotView |
| UI | MenuScreen (кнопка Старт), ResultScreen (кнопка Ещё раз), HUD (HP, зелья) |
| Save | Нет (ран короткий, не критично) |
| Шейдеры | Базовые силуэты (SDF без наполнения), SH_Background, SH_Floor (простой), SH_FX_Explosion (базовый) |

### Alpha

| Система | Что добавляется |
|---------|----------------|
| GSM | MapState, EventState, BossState. Полный цикл рана |
| Models | FloorMapModel, MetaProgressionModel (заглушка) |
| Content | Все 5 элементов, 15 зелий, 8 типов врагов, 3 босса |
| MapGeneratorService | Генерация карт, составов врагов |
| LootService | Генерация реликвий, зелий в событиях |
| Events | Все 4 типа событий |
| Relics | 5 из 10 реликвий |
| Room Modifiers | 3 из 6 |
| Zones + Combo | Зоны на поле (огонь, вода, яд). 3 combo |
| Views | Все EnemyViews, BossViews, MapView, EventScreens |
| Шейдеры | Полные SDF с наполнением, зонные шейдеры, bloom |
| Save | SaveService (meta + mid-run) |
| Audio | Заглушки (placeholder SFX) |

### Beta

| Система | Что добавляется |
|---------|----------------|
| Content | Все 10 реликвий, все 6 модификаторов, все 7 combo |
| Meta | AchievementService, все ачивки, разблокировки, улучшения |
| Tutorial | TutorialController, все 5 шагов |
| Book | Книга алхимика |
| UI | Все popup-ы, нотификации, настройки |
| Juice | Полная tween-система. Screenshake, hitstop, combo-эффекты, dissolve |
| Шейдеры | Все FX-шейдеры, все пост-процесс пассы |
| Quality | Fallback-система, автоопределение |
| Balance | Итерации баланса на основе плейтестов |
| Testing | Полный набор unit/integration тестов |

### Release

| Система | Что добавляется |
|---------|----------------|
| Audio | Все SFX + музыка |
| Transitions | Анимации переходов между экранами |
| Polish | Финальные анимации, timing, VFX polish |
| Optimization | Профилирование на целевых устройствах. Устранение bottleneck-ов |
| Build | IL2CPP, ProGuard, подпись, Google Play настройка |
| QA | Полный ручной QA чеклист |

---

*Версия документа: 1.0*
*Движок: Unity 6.3*
*Target: Android (Google Play)*