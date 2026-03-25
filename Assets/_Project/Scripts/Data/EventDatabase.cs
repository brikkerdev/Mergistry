using System.Collections.Generic;

namespace Mergistry.Data
{
    [System.Serializable]
    public struct EventChoice
    {
        public string Label;
        public string OutcomeDescription;
        /// <summary>Outcome type string: "heal_full", "lose_max_hp", "random_potion",
        /// "buy_potion", "sacrifice_potion", "gain_relic", "gain_hp", "gain_action_potion"</summary>
        public string OutcomeType;
        public int    OutcomeValue; // used for HP amount, potion level, etc.
    }

    [System.Serializable]
    public struct EventDefinition
    {
        public EventNodeType       Type;
        public string              Title;
        public string              Description;
        public List<EventChoice>   Choices;
    }

    public static class EventDatabase
    {
        public static EventDefinition GetFountain() => new EventDefinition
        {
            Type        = EventNodeType.Fountain,
            Title       = "Фонтан",
            Description = "Из трещины в скале бьёт сверкающий источник.\nВода светится мягким голубым светом.",
            Choices     = new List<EventChoice>
            {
                new EventChoice
                {
                    Label              = "Выпить",
                    OutcomeDescription = "70% полное лечение / 30% −1 макс. HP",
                    OutcomeType        = "fountain_drink",
                    OutcomeValue       = 0
                },
                new EventChoice
                {
                    Label              = "Наполнить флакон",
                    OutcomeDescription = "Получить случайное зелье lv2",
                    OutcomeType        = "random_potion",
                    OutcomeValue       = 2
                }
            }
        };

        public static EventDefinition GetMerchant() => new EventDefinition
        {
            Type        = EventNodeType.Merchant,
            Title       = "Торговец",
            Description = "Странствующий алхимик раскладывает свой товар.\n«Могу предложить кое-что особенное...»",
            Choices     = new List<EventChoice>
            {
                new EventChoice
                {
                    Label              = "Купить зелье (−1 HP)",
                    OutcomeDescription = "Получить случайное зелье lv2",
                    OutcomeType        = "buy_potion",
                    OutcomeValue       = 1
                },
                new EventChoice
                {
                    Label              = "Купить зелье (−2 HP)",
                    OutcomeDescription = "Получить случайное зелье lv3",
                    OutcomeType        = "buy_potion",
                    OutcomeValue       = 2
                },
                new EventChoice
                {
                    Label              = "Уйти",
                    OutcomeDescription = "Ничего не происходит",
                    OutcomeType        = "nothing",
                    OutcomeValue       = 0
                }
            }
        };

        public static EventDefinition GetAltar() => new EventDefinition
        {
            Type        = EventNodeType.Altar,
            Title       = "Алтарь",
            Description = "Древний алтарь из тёмного камня.\nНа нём горит холодное пламя. Он требует жертву.",
            Choices     = new List<EventChoice>
            {
                new EventChoice
                {
                    Label              = "Пожертвовать зелье",
                    OutcomeDescription = "Удалить случайное зелье → получить реликвию",
                    OutcomeType        = "sacrifice_potion",
                    OutcomeValue       = 0
                },
                new EventChoice
                {
                    Label              = "Уйти",
                    OutcomeDescription = "Ничего не происходит",
                    OutcomeType        = "nothing",
                    OutcomeValue       = 0
                }
            }
        };

        public static EventDefinition GetChest() => new EventDefinition
        {
            Type        = EventNodeType.Chest,
            Title       = "Сундук",
            Description = "Запылённый сундук стоит у стены.\nЗамок давно сломан — осталось лишь открыть крышку.",
            Choices     = new List<EventChoice>
            {
                new EventChoice
                {
                    Label              = "Взять зелье",
                    OutcomeDescription = "Получить случайное зелье lv2",
                    OutcomeType        = "random_potion",
                    OutcomeValue       = 2
                },
                new EventChoice
                {
                    Label              = "+3 HP",
                    OutcomeDescription = "Восстановить 3 HP",
                    OutcomeType        = "heal",
                    OutcomeValue       = 3
                },
                new EventChoice
                {
                    Label              = "+1 макс. HP",
                    OutcomeDescription = "Увеличить максимальное HP на 1",
                    OutcomeType        = "gain_max_hp",
                    OutcomeValue       = 1
                }
            }
        };

        private static readonly EventNodeType[] _allTypes =
        {
            EventNodeType.Fountain, EventNodeType.Merchant,
            EventNodeType.Altar, EventNodeType.Chest
        };

        public static EventDefinition GetRandom()
        {
            var type = _allTypes[UnityEngine.Random.Range(0, _allTypes.Length)];
            return Get(type);
        }

        public static EventDefinition Get(EventNodeType type)
        {
            return type switch
            {
                EventNodeType.Fountain => GetFountain(),
                EventNodeType.Merchant => GetMerchant(),
                EventNodeType.Altar    => GetAltar(),
                EventNodeType.Chest    => GetChest(),
                _                      => GetFountain()
            };
        }
    }
}
