namespace Game.ActorAndPlayers.AI
{
    using Game.ActorAndPlayers.AI.Behaviour;
    using Game.DataStructures;
    using System;
    using System.Collections.Generic;

    public class BehaviourTree
    {
        BehaviourNode rootNode;
        BotPlayer botPlayer;
        public BehaviourTree(BehaviourNode rootNode, BotPlayer botPlayer)
        {
            this.rootNode = rootNode;
            this.botPlayer = botPlayer;
            ProcessNode(this.rootNode);
        }

        public void ProcessNode(BehaviourNode node)
        {
            switch (node.nodeType)
            {
                case DataStructures.NodeType.comparison:
                    ProcessComparison(node.conditions, node.shouldIncludeAllCondition, (bool incomingBool) =>
                    {
                        if (incomingBool)
                        {
                            if (node.outcome != null)
                            {
                                ProcessNode(node.outcome);
                            }
                            else
                            {
                                ProcessingFinished();
                            }
                        }
                        else
                        {
                            if (node.negativeOutcome != null)
                            {
                                ProcessNode(node.negativeOutcome);
                            }
                            else
                            {
                                ProcessingFinished();
                            }
                        }
                    }
                    );
                    break;
                case DataStructures.NodeType.execution:
                    ProcessAction(node.botAction, () =>
                    {
                        if (node.outcome != null)
                        {
                            ProcessNode(node.outcome);
                        }
                        else
                        {
                            ProcessingFinished();
                        }
                    }, null);
                    break;
            }
        }

        private void ProcessComparison(List<Comparison> conditions, bool shouldIncludeAll, Action<bool> OnComparisonCompelete)
        {
            bool cumulativeFlag = false;
            for (int i = 0; i < conditions.Count; i++)
            {
                bool compareBool = false;
                switch (conditions[i].attributeToCompare)
                {
                    case DataStructures.Attribute.health:
                        compareBool = conditions[i].Compare(botPlayer.Health);
                        break;
                    case DataStructures.Attribute.armor:
                        compareBool = conditions[i].Compare(botPlayer.Armor);
                        break;
                    case DataStructures.Attribute.healthPercent:
                        compareBool = conditions[i].Compare(botPlayer.Health / botPlayer.MaxHealth);
                        break;
                    case DataStructures.Attribute.armorPercent:
                        compareBool = conditions[i].Compare(botPlayer.Armor / botPlayer.MaxArmor);
                        break;
                    case DataStructures.Attribute.debuffCount:
                        break;
                }
                if (!shouldIncludeAll && compareBool)
                {
                    cumulativeFlag = compareBool;// exiting early if not all conditions are necessary
                    break;
                }
                else
                {
                    if (i == 0)
                    {
                        cumulativeFlag = compareBool; // assinging the first bool
                    }
                    else
                    {
                        cumulativeFlag = shouldIncludeAll ? cumulativeFlag && compareBool : compareBool || cumulativeFlag; // doing boolean operation
                    }
                }
            }
            OnComparisonCompelete?.Invoke(cumulativeFlag);
        }
        private void ProcessAction(BotAction action, Action OnProcessFinished, Action OnProcessNotSuccessfull)
        {
            switch (action)
            {
                case BotAction.heal:
                    botPlayer.FetchAndUseCard(CardActionType.Heal, OnProcessFinished, OnProcessNotSuccessfull);
                    break;
                case BotAction.attack:
                    botPlayer.FetchAndUseCard(CardActionType.Attack, OnProcessFinished, OnProcessNotSuccessfull);
                    break;
                case BotAction.defend:
                    botPlayer.FetchAndUseCard(CardActionType.Armor, OnProcessFinished, OnProcessNotSuccessfull);
                    break;
                case BotAction.flee:
                    botPlayer.Flee(OnProcessFinished, OnProcessNotSuccessfull);
                    break;
            }
        }
        private void ProcessingFinished() => botPlayer.OnTurnEnd();
    }

}