using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.WorldObjects.Entity;
using Skill = ACE.Entity.Enum.Skill;

namespace ACE.Server.WorldObjects;

partial class Creature
{
    private int LastNumberOfNearbyPlayers = 0;
    private uint BaseHealth;
    private bool BaseHealthSet = false;

    private uint BaseHeavyWeaponsSkill;
    private bool BaseHeavyWeaponsSkillSet = false;
    private uint BaseDaggerSkill;
    private bool BaseDaggerSkillSet = false;
    private uint BaseStaffSkill;
    private bool BaseStaffSkillSet = false;
    private uint BaseUnarmedSkill;
    private bool BaseUnarmedSkillSet = false;
    private uint BaseBowSkill;
    private bool BaseBowSkillSet = false;
    private uint BaseThrownWeaponsSkill;
    private bool BaseThrownWeaponsSkillSet = false;
    private uint BaseWarMagicSkill;
    private bool BaseWarMagicSkillSet = false;
    private uint BaseLifeMagicSkill;
    private bool BaseLifeMagicSkillSet = false;

    private uint BasePhysicalDefenseSkill;
    private bool BasePhysicalDefenseSet = false;
    private uint BaseMagicDefenseSkill;
    private bool BaseMagicDefenseSet = false;

    private void HandlePlayerCountScaling()
    {

        if (!(UseNearbyPlayerScaling ?? false))
        {
            return;
        }

        var numberOfNearbyPlayers = GetAttackTargets().Count;

        if (numberOfNearbyPlayers <= LastNumberOfNearbyPlayers)
        {
            return;
        }

        LastNumberOfNearbyPlayers = numberOfNearbyPlayers;

        var playerThreshold = NearbyPlayerScalingThreshold ?? 0;
        var extraPlayers = numberOfNearbyPlayers - playerThreshold;

        if (extraPlayers < 1)
        {
            return;
        }

        SetBaseSkills();

        ApplyNearbyPlayerScalingToVitals(extraPlayers);

        ApplyNearbyPlayerScalingToAttack(extraPlayers);

        ApplyNearbyPlayerScalingToDefense(extraPlayers);

        CheckToSpawnPlayerScalingAdds();
    }

    private void ApplyNearbyPlayerScalingToVitals(int extraPlayers)
    {
        var multiplier = (NearbyPlayerVitalsScalingPerExtraPlayer ?? 1.0) * extraPlayers;

        ApplyHealthScaling((multiplier));
    }

    private void ApplyHealthScaling(double multiplier)
    {
        var currentMaxHealth = Health.MaxValue;
        var currentHealth = Health.Current;
        var currentHealthPercentage = (double)currentHealth / currentMaxHealth;

        Vitals[PropertyAttribute2nd.MaxHealth].StartingValue = (uint)(BaseHealth + BaseHealth * multiplier);
        Health.Current = (uint)(Health.MaxValue * currentHealthPercentage);
    }

    private void ApplyNearbyPlayerScalingToAttack(int extraPlayers)
    {
        var multiplier = (NearbyPlayerAttackScalingPerExtraPlayer ?? 1.0) * extraPlayers;

        SetNewSkill(BaseHeavyWeaponsSkill, multiplier, Skill.HeavyWeapons);
        SetNewSkill(BaseDaggerSkill, multiplier, Skill.Dagger);
        SetNewSkill(BaseStaffSkill, multiplier, Skill.Staff);
        SetNewSkill(BaseUnarmedSkill, multiplier, Skill.UnarmedCombat);
        SetNewSkill(BaseBowSkill, multiplier, Skill.Bow);
        SetNewSkill(BaseThrownWeaponsSkill, multiplier, Skill.ThrownWeapon);
        SetNewSkill(BaseWarMagicSkill, multiplier, Skill.WarMagic);
        SetNewSkill(BaseLifeMagicSkill, multiplier, Skill.LifeMagic);
    }

    private void ApplyNearbyPlayerScalingToDefense(int extraPlayers)
    {
        var multiplier = (NearbyPlayerDefenseScalingPerExtraPlayer ?? 1.0) * extraPlayers;

        SetNewSkill(BasePhysicalDefenseSkill, multiplier, Skill.MeleeDefense);
        SetNewSkill(BaseMagicDefenseSkill, multiplier, Skill.MagicDefense);
    }

    private void SetBaseSkills()
    {
        if (!BaseHealthSet)
        {
            BaseHealth = Health.MaxValue;
            BaseHealthSet = true;
        }

        if (!BaseHeavyWeaponsSkillSet)
        {
            BaseHeavyWeaponsSkill = GetCreatureSkill(Skill.HeavyWeapons).Current;
            BaseHeavyWeaponsSkillSet = true;
        }

        if (!BaseDaggerSkillSet)
        {
            BaseDaggerSkill = GetCreatureSkill(Skill.Dagger).Current;
            BaseDaggerSkillSet = true;
        }

        if (!BaseStaffSkillSet)
        {
            BaseStaffSkill = GetCreatureSkill(Skill.Staff).Current;
            BaseStaffSkillSet = true;
        }

        if (!BaseUnarmedSkillSet)
        {
            BaseUnarmedSkill = GetCreatureSkill(Skill.UnarmedCombat).Current;
            BaseUnarmedSkillSet = true;
        }

        if (!BaseBowSkillSet)
        {
            BaseBowSkill = GetCreatureSkill(Skill.Bow).Current;
            BaseBowSkillSet = true;
        }

        if (!BaseThrownWeaponsSkillSet)
        {
            BaseThrownWeaponsSkill = GetCreatureSkill(Skill.ThrownWeapon).Current;
            BaseThrownWeaponsSkillSet = true;
        }

        if (!BaseWarMagicSkillSet)
        {
            BaseWarMagicSkill = GetCreatureSkill(Skill.WarMagic).Current;
            BaseWarMagicSkillSet = true;
        }

        if (!BaseLifeMagicSkillSet)
        {
            BaseLifeMagicSkill = GetCreatureSkill(Skill.LifeMagic).Current;
            BaseLifeMagicSkillSet = true;
        }

        if (!BasePhysicalDefenseSet)
        {
            BasePhysicalDefenseSkill = GetCreatureSkill(Skill.MeleeDefense).Current;
            BasePhysicalDefenseSet = true;
        }

        if (!BaseMagicDefenseSet)
        {
            BaseMagicDefenseSkill = GetCreatureSkill(Skill.MagicDefense).Current;
            BaseMagicDefenseSet = true;
        }
    }

    private void SetNewSkill(uint baseSkill, double multiplier, Skill skillType)
    {
        var newSkill = (uint)(baseSkill + baseSkill * multiplier);
        var propertiesSkill = new PropertiesSkill()
        {
            InitLevel = newSkill,
            SAC = SkillAdvancementClass.Trained
        };

        Skills[skillType] = new CreatureSkill(this, skillType, propertiesSkill);
    }

    private void CheckToSpawnPlayerScalingAdds()
    {
        if (NearbyPlayerScalingAddWcid == null)
        {
            return;
        }

        // TODO: reference NearbyPlayerScalingAddWcid to spawn extra creatures
    }
}
