using Mergistry.Core;
using Mergistry.Data;
using Mergistry.Events;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.Screens;
using UnityEngine;

namespace Mergistry.GameStates
{
    public class EventState : IGameState
    {
        private readonly EventScreenView  _screen;
        private readonly GameStateMachine _fsm;
        private readonly FadeView         _fadeView;
        private readonly RunModel         _runModel;
        private readonly InventoryModel   _inventory;
        private readonly IRelicService    _relicService;
        private readonly ILootService     _lootService;

        private MapState        _mapState;
        private EventDefinition _currentEvent;

        public EventState(
            EventScreenView  screen,
            GameStateMachine fsm,
            FadeView         fadeView,
            RunModel         runModel,
            InventoryModel   inventory,
            IRelicService    relicService,
            ILootService     lootService)
        {
            _screen       = screen;
            _fsm          = fsm;
            _fadeView     = fadeView;
            _runModel     = runModel;
            _inventory    = inventory;
            _relicService = relicService;
            _lootService  = lootService;
        }

        public void SetMapState(MapState mapState) => _mapState = mapState;

        // ── IGameState ────────────────────────────────────────────────────────

        public void Enter()
        {
            _currentEvent = EventDatabase.GetRandom();
            _screen.Show(_currentEvent);
            _screen.OnChoiceClicked   += OnChoiceClicked;
            _screen.OnContinueClicked += OnContinue;
            _fadeView?.FadeIn(0.2f, null);
            Debug.Log($"[EventState] Enter — {_currentEvent.Title}");
        }

        public void Exit()
        {
            _screen.OnChoiceClicked   -= OnChoiceClicked;
            _screen.OnContinueClicked -= OnContinue;
            _screen.Hide();
        }

        public void Tick() { }

        // ── Private ───────────────────────────────────────────────────────────

        private void OnChoiceClicked(int index)
        {
            if (index < 0 || index >= _currentEvent.Choices.Count) return;

            var choice = _currentEvent.Choices[index];
            string result = ApplyOutcome(choice);

            EventBus.Publish(new EventChoiceMadeEvent
            {
                EventType   = _currentEvent.Type,
                OutcomeType = choice.OutcomeType
            });

            _screen.ShowResult(result);
            Debug.Log($"[EventState] Choice: {choice.Label} → {choice.OutcomeType}");
        }

        private string ApplyOutcome(EventChoice choice)
        {
            switch (choice.OutcomeType)
            {
                case "fountain_drink":
                    if (Random.value <= 0.7f)
                    {
                        _runModel.PersistentHP = _runModel.MaxHP;
                        return "Вода исцеляет вас полностью!\nHP восстановлены.";
                    }
                    else
                    {
                        _runModel.MaxHP = Mathf.Max(1, _runModel.MaxHP - 1);
                        if (_runModel.PersistentHP > _runModel.MaxHP)
                            _runModel.PersistentHP = _runModel.MaxHP;
                        return "Вода оказалась проклятой...\nМакс. HP уменьшено на 1.";
                    }

                case "random_potion":
                {
                    var type  = _lootService.GetRandomPotionType();
                    int level = choice.OutcomeValue;
                    bool added = _inventory.TryAdd(type, level);
                    var name = PotionDatabase.GetName(type);
                    return added
                        ? $"Получено: {name} lv{level}!"
                        : $"Инвентарь полон! {name} lv{level} потеряно.";
                }

                case "buy_potion":
                {
                    int cost = choice.OutcomeValue;
                    if (_runModel.PersistentHP <= cost)
                        return "Недостаточно HP для покупки!";

                    _runModel.PersistentHP -= cost;
                    var type  = _lootService.GetRandomPotionType();
                    int level = cost + 1; // cost 1 → lv2, cost 2 → lv3
                    bool added = _inventory.TryAdd(type, level);
                    var name = PotionDatabase.GetName(type);
                    return added
                        ? $"Куплено: {name} lv{level}!\n(−{cost} HP)"
                        : $"Инвентарь полон! {name} lv{level} потеряно.\n(−{cost} HP)";
                }

                case "sacrifice_potion":
                {
                    // Find first non-empty slot
                    int slotIdx = -1;
                    for (int i = 0; i < InventoryModel.SlotCount; i++)
                    {
                        var slot = _inventory.GetSlot(i);
                        if (!slot.IsEmpty) { slotIdx = i; break; }
                    }

                    if (slotIdx < 0)
                        return "У вас нет зелий для жертвы!";

                    _inventory.Replace(slotIdx, PotionType.None, 0);
                    var relics = _relicService.GetRandomRelicChoices(1);
                    if (relics.Count > 0)
                    {
                        _relicService.AcquireRelic(relics[0]);
                        var relicName = RelicDatabase.Get(relics[0]).Name;
                        return $"Жертва принята!\nПолучена реликвия: {relicName}";
                    }
                    return "Жертва принята, но все реликвии уже найдены.";
                }

                case "heal":
                {
                    int amount = choice.OutcomeValue;
                    _runModel.PersistentHP = Mathf.Min(_runModel.PersistentHP + amount, _runModel.MaxHP);
                    return $"HP восстановлены на {amount}!\nТекущее HP: {_runModel.PersistentHP}/{_runModel.MaxHP}";
                }

                case "gain_max_hp":
                {
                    int amount = choice.OutcomeValue;
                    _runModel.MaxHP        += amount;
                    _runModel.PersistentHP += amount;
                    return $"Макс. HP увеличено на {amount}!\nТекущее HP: {_runModel.PersistentHP}/{_runModel.MaxHP}";
                }

                case "nothing":
                    return "Вы уходите дальше.";

                default:
                    return "...";
            }
        }

        private void OnContinue()
        {
            _fadeView?.FadeOut(0.2f, () => _fsm.ChangeState(_mapState));
        }
    }
}
