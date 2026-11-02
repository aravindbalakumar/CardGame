using UnityEngine.EventSystems;

namespace Game
{
    using UnityEngine;
    using Game.ActorAndPlayers;
    using Game.ScriptableObjects.Card;
    using System.Collections.Generic;
    // using Game.ActorAndPlayers.AI;
    // using Game.ActorAndPlayers.AI.Behaviour;
    using System;
    using Unity.Behavior;
    using Game.DataStructures;

    public class BotPlayer : AbstractActor,IPointerDownHandler
    {
        Player opponentPlayer;
        [SerializeField] BehaviorGraphAgent _agent;
        public List<Skill> bot_Attack_Skill;
        public List<Skill> bot_Defense_Skill;
        public List<Skill> bot_heal_Skill;
        private bool skillUsedThisTurn;
        public override void Initialize()
        {
            base.Initialize();
            // Legacy mesh bots have no Behavior Graph; only graph-backed bots bind a blackboard.
            if (_agent == null) return;
            _agent.BlackboardReference.SetVariableValue("isMyTurn", false);
            _agent.BlackboardReference.SetVariableValue("health", this.Health);
            _agent.BlackboardReference.SetVariableValue("armor", this.Armor);
            _agent.BlackboardReference.SetVariableValue("opponentHealth",  GameHandler.instance.player.Health);
            _agent.BlackboardReference.SetVariableValue("targetActor", GameHandler.instance.player);
            _agent.BlackboardReference.SetVariableValue("Self", this.gameObject);
            _agent.BlackboardReference.SetVariableValue("Self_Reference", this);
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (GameHandler.instance.player.SelectedCard != null)
            {
                if(GameHandler.instance.player.SelectedCard.cardTargetGroup.Contains(this.gameObject.layer))
                {
                    GameHandler.OnAbstractActorClickedWithCard?.Invoke(this,GameHandler.instance.player.SelectedCard.cardData);
                }
            }
            else
            {
                GameHandler.OnAbstractActorClicked?.Invoke(this);
            }
        }
        public void UseSkill(Skill skill,AbstractActor target)
        {
            var handler = GameHandler.instance;
            if (handler == null || handler.CurrentTurnActor != this || !isMyTurn || skillUsedThisTurn ||
                Health <= 0 || target == null || target.Health <= 0 || skill == null) return;
            skillUsedThisTurn = true;
            handler.ProcessSkillAction(target, skill);
        }

        public override void StartTurn()
        {
            var handler = GameHandler.instance;
            if (handler == null || handler.CurrentTurnActor != this) return;
            skillUsedThisTurn = false;
            isMyTurn = true;
            // Keep the graph's turn gate closed while the configured presentation owns this turn.
            if (handler.BotTurnAnimation != null && handler.BotTurnAnimation(this)) return;
            if (_agent != null) _agent.BlackboardReference.SetVariableValue("isMyTurn", true);
        }

        public override void EndTurn()
        {
            var handler = GameHandler.instance;
            if (handler == null || handler.CurrentTurnActor != this || handler.CardActionInProgress) return;
            if (_agent != null) _agent.BlackboardReference.SetVariableValue("isMyTurn", false);
            handler.TryEndTurn(this);
        }
        
        public override void UpdateVisualSystem(LayerMask? mask)
        {
            base.UpdateVisualSystem(mask);
            if (outline == null) return;
            if (mask.HasValue)
            {
                if (mask.Value.Contains(this.gameObject))
                {
                    outline.enabled = true;
                }
                else
                {
                    outline.enabled = false;
                }
            }
            else
            {
                outline.enabled = false;
            }
        }
    
    }
    


}


