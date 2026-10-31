using Game.ActorAndPlayers;
using System;
using Game;
using Game.ScriptableObjects.Card;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CastSkillOfType", story: "[bot] casts [skill] [target]", category: "Action", id: "edca82e51fbae149f280916410746f49")]
public partial class CastSkillOfTypeAction : Action
{
    [SerializeReference] public BlackboardVariable<BotPlayer> bot;
    [SerializeReference] public BlackboardVariable<SkillType> Skill;
    [SerializeReference] public BlackboardVariable<AbstractActor> Target;
    private Skill skillToCast=null;
    private int skillIndex=-1;

    protected override Status OnStart()
    {
        if (bot.Value == null || Skill.Value == SkillType.None || Target.Value==null)
        {
            return Status.Uninitialized;
        }

        int skillIndex=-1;
        switch (Skill.Value)
        {
            case SkillType.Attack:
                if (bot.Value.bot_Attack_Skill != null && bot.Value.bot_Attack_Skill.Count > 0){
                skillIndex = Random.Range(0, bot.Value.bot_Attack_Skill.Count);
                skillToCast = bot.Value.bot_Attack_Skill[skillIndex];
            }
                break;
            case SkillType.Defence:
                if (bot.Value.bot_Defense_Skill != null && bot.Value.bot_Defense_Skill.Count > 0)
                {
                    skillIndex = Random.Range(0, bot.Value.bot_Defense_Skill.Count);
                    skillToCast = bot.Value.bot_Defense_Skill[skillIndex];
                }

                break;
            case SkillType.Heal:
                if (bot.Value.bot_heal_Skill != null && bot.Value.bot_heal_Skill.Count > 0)
                {
                    skillIndex = Random.Range(0, bot.Value.bot_heal_Skill.Count);
                    skillToCast = bot.Value.bot_heal_Skill[skillIndex];
                }

                break;
            case SkillType.None: skillIndex = -1;break;
            default: skillIndex=-1;break;
        }
            
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (skillIndex==-1|| skillToCast==null)
        {
            return Status.Failure;
        }
        
        bot.Value.UseSkill(skillToCast,Target.Value);
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

