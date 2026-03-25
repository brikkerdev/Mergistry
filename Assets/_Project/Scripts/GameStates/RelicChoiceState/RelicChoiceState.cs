using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Data;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.Screens;
using UnityEngine;

namespace Mergistry.GameStates
{
    /// <summary>
    /// Shown after elite combat victory. Offers 3 relics to choose from.
    /// </summary>
    public class RelicChoiceState : IGameState
    {
        private readonly RelicChoiceScreenView _screen;
        private readonly GameStateMachine     _fsm;
        private readonly FadeView             _fadeView;
        private readonly IRelicService        _relicService;

        private MapState         _mapState;
        private List<RelicType>  _choices;

        public RelicChoiceState(
            RelicChoiceScreenView screen,
            GameStateMachine     fsm,
            FadeView             fadeView,
            IRelicService        relicService)
        {
            _screen       = screen;
            _fsm          = fsm;
            _fadeView     = fadeView;
            _relicService = relicService;
        }

        public void SetMapState(MapState mapState) => _mapState = mapState;

        public void Enter()
        {
            _choices = _relicService.GetRandomRelicChoices(3);

            if (_choices.Count == 0)
            {
                // All relics already acquired, skip directly to map
                Debug.Log("[RelicChoiceState] No relics available, skipping to map");
                _fsm.ChangeState(_mapState);
                return;
            }

            _screen.Show(_choices);
            _screen.OnRelicChosen += OnRelicChosen;
            _fadeView?.FadeIn(0.2f, null);

            Debug.Log($"[RelicChoiceState] Enter — offering {_choices.Count} relics");
        }

        public void Exit()
        {
            _screen.OnRelicChosen -= OnRelicChosen;
            _screen.Hide();
        }

        public void Tick() { }

        private void OnRelicChosen(int index)
        {
            if (index < 0 || index >= _choices.Count) return;

            _relicService.AcquireRelic(_choices[index]);
            _fadeView?.FadeOut(0.2f, () => _fsm.ChangeState(_mapState));
        }
    }
}
