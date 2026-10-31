using System;
using Game;
using Game.ActorAndPlayers;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CanCastSkill", story: "can [bot] cast [Skill]", category: "Conditions", id: "8224394a5c80128b35d1fd22029f8194")]
public partial class CanCastSkillCondition : Condition
{
    [SerializeReference] public BlackboardVariable<BotPlayer> bot;
    [SerializeReference] public BlackboardVariable<SkillType> Skill;
    public override bool IsTrue()
    {
        if (bot.Value == null)
        {
            return false;
        }

        bool doesHaveSkill = false;
        switch (Skill.Value)
        {
            case SkillType.Attack:
                doesHaveSkill =bot.Value.bot_Attack_Skill!=null&& bot.Value.bot_Attack_Skill.Count>0 ;
                break;
            case SkillType.Defence:
                doesHaveSkill =bot.Value.bot_Defense_Skill!=null&& bot.Value.bot_Defense_Skill.Count>0 ;
                break;
            case SkillType.Heal:
                doesHaveSkill =bot.Value.bot_heal_Skill!=null&&bot.Value.bot_heal_Skill.Count>0 ;
                break;
            case SkillType.None:
                doesHaveSkill = false;
                break;
            default: doesHaveSkill = false;
                break;
        }

        return doesHaveSkill;

    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
