using System;
using System.Linq;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Factories.Tables;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using Spell = ACE.Server.Entity.Spell;
using Time = ACE.Common.Time;

namespace ACE.Server.WorldObjects;

partial class Player
{
    // Warrior
    public bool ProvokeIsActive => LastProvokeActivated > Time.GetUnixTime() - ProvokeActivatedDuration;
    private double LastProvokeActivated;
    private double ProvokeActivatedDuration = 10;

    public bool PhalanxIsActive;

    public bool ParryIsActive => LastParryActivated > Time.GetUnixTime() - ParryActivatedDuration;
    private double LastParryActivated;
    private double ParryActivatedDuration = 10;

    // Blademaster
    public bool FuryEnrageIsActive => LastFuryEnrageActivated > Time.GetUnixTime() - FuryEnrageActivatedDuration;
    private double LastFuryEnrageActivated;
    private double FuryEnrageActivatedDuration = 10;
    public bool FuryStanceIsActive;
    public float FuryMeter;
    public float EnrageLevel;

    // Archer
    public bool SteadyShotIsActive => LastSteadyShotActivated > Time.GetUnixTime() - SteadyShotActivatedDuration;
    private double LastSteadyShotActivated;
    private double SteadyShotActivatedDuration = 10;

    public bool MultiShotIsActive => LastMultishotActivated > Time.GetUnixTime() - MultishotActivatedDuration;
    private double LastMultishotActivated;
    private double MultishotActivatedDuration = 10;

    public bool EvasiveStanceIsActive;

    // Vagabond
    public bool VanishIsActive => LastVanishActivated > Time.GetUnixTime() - VanishActivatedDuration;
    private double LastVanishActivated;
    private double VanishActivatedDuration = 5;

    public bool BackstabIsActive => LastBackstabActivated > Time.GetUnixTime() - BackstabActivatedDuration;
    private double LastBackstabActivated;
    private double BackstabActivatedDuration = 10;

    public bool SmokescreenIsActive => LastSmokescreenActivated > Time.GetUnixTime() - SmokescreenActivatedDuration;
    private double LastSmokescreenActivated;
    private double SmokescreenActivatedDuration = 10;

    // Sorcerer
    public bool OverloadIsActive => LastOverloadActivated > Time.GetUnixTime() - OverloadActivatedDuration;
    private double LastOverloadActivated;
    private double OverloadActivatedDuration = 10;
    public bool OverloadActivated;
    public bool OverloadDumped = false;

    public bool BatteryIsActive => LastBatteryActivated > Time.GetUnixTime() - BatteryActivatedDuration;
    private double LastBatteryActivated;
    private double BatteryActivatedDuration = 10;

    public bool ManaBarrierIsActive;

    // Spellsword
    public bool ReflectIsActive => LastReflectActivated > Time.GetUnixTime() - ReflectActivatedDuration;
    private double LastReflectActivated;
    private double ReflectActivatedDuration = 10;

    public bool EnchantedWeaponIsActive => LastEnchantedWeaponActivated > Time.GetUnixTime() - EnchantedWeaponActivatedDuration;
    private double LastEnchantedWeaponActivated;
    private double EnchantedWeaponActivatedDuration = 10;
    public Spell EnchantedWeaponStoredSpell;

    public CombatAbility EquippedCombatAbility
    {
        get
        {
            var combatFocus = GetEquippedCombatFocus();
            if (combatFocus == null)
            {
                return CombatAbility.None;
            }

            var combatAbility = combatFocus.GetCombatAbility();
            return combatAbility;

        }
    }

    public bool TryUsePhalanx(Gem gem)
    {
        if (!VerifyCombatFocus(CombatAbility.Phalanx))
        {
            return false;
        }

        if (GetEquippedShield() is null)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Phalanx requires an equipped shield.",
                    ChatMessageType.Broadcast
                )
            );
            return false;
        }

        if (!PhalanxIsActive)
        {
            PhalanxIsActive = true;

            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You raise your shield into a defensive stance!",
                    ChatMessageType.Broadcast
                )
            );
            PlayParticleEffect(PlayScript.ShieldUpGrey, Guid);

            return false;
        }

        PhalanxIsActive = false;

        Session.Network.EnqueueSend(
            new GameMessageSystemChat($"You lower your shield.", ChatMessageType.Broadcast)
        );
        PlayParticleEffect(PlayScript.DispelLife, Guid);

        return true;
    }

    public bool TryUseProvoke(WorldObject ability)
    {
        if (!VerifyCombatFocus(CombatAbility.Provoke))
        {
            return false;
        }
        LastProvokeActivated = Time.GetUnixTime();

        Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Give them cause for provocation! For the next ten seconds, your attacks with at least 50% power generate double threat!.",
                ChatMessageType.Broadcast
            )
        );

        var nearbyMonsters = GetNearbyMonsters(10);

        foreach (var target in nearbyMonsters)
        {
            var skillCheck = SkillCheck.GetSkillChance(
                Strength.Current * 2,
                target.GetCreatureSkill(Skill.Perception).Current
            );

            if (ThreadSafeRandom.Next(0.0f, 1.0f) > skillCheck)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"{target.Name} can see right through you. Your taunt failed.",
                        ChatMessageType.Broadcast
                    )
                );
                continue;
            }

            var areaThreatBonus = Convert.ToInt32((float)(Level ?? 1) / (target.Level ?? 1) * 100);

            target.IncreaseTargetThreatLevel(this, areaThreatBonus); // this amount is doubled from the Provoke threat bonus

            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You successfully provoke {target.Name}, increasingly your threat level substantially.",
                    ChatMessageType.Broadcast
                )
            );

            PlayParticleEffect(PlayScript.VisionUpWhite, Guid);
            target.PlayParticleEffect(PlayScript.VisionDownBlack, target.Guid);
        }

        return true;
    }

    public bool TryUseParry(Gem gem)
    {
        if (!VerifyCombatFocus(CombatAbility.Parry))
        {
            return false;
        }

        if (ParryIsActive)
        {
            return false;
        }

        LastParryActivated = Time.GetUnixTime();

        PlayParticleEffect(PlayScript.EnchantUpRed, Guid);

        return true;
    }

    public bool TryUseFury(Gem gem)
    {
        if (!VerifyCombatFocus(CombatAbility.Fury))
        {
            return false;
        }

        if (!FuryStanceIsActive && !FuryEnrageIsActive)
        {
            FuryStanceIsActive = true;
            FuryMeter = 0.0f;

            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You channel your inner fury!",
                    ChatMessageType.Broadcast
                )
            );
            PlayParticleEffect(PlayScript.SkillUpOrange, Guid);

            return false;
        }

        if (FuryEnrageIsActive)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat($"You cannot activate Fury while Enraged.", ChatMessageType.Broadcast)
            );
            return false;
        }

        if (FuryStanceIsActive && FuryMeter < 0.5f)
        {
            FuryStanceIsActive = false;
            FuryMeter = 0.0f;

            Session.Network.EnqueueSend(
                new GameMessageSystemChat($"You calm down and release rage.", ChatMessageType.Broadcast)
            );

            PlayParticleEffect(PlayScript.SkillDownOrange, Guid);

            return true;
        }

        FuryStanceIsActive = false;
        EnrageLevel = FuryMeter;
        FuryMeter = 0.0f;
        LastFuryEnrageActivated = Time.GetUnixTime();

        Session.Network.EnqueueSend(
            new GameMessageSystemChat($"You unleash your rage ({Math.Round(EnrageLevel * 100)}%)!", ChatMessageType.Broadcast)
        );
        PlayParticleEffect(PlayScript.EnchantUpOrange, Guid);

        return true;
    }

    public bool TryUseMultishot(Gem gem)
    {
        if (!VerifyCombatFocus(CombatAbility.Multishot))
        {
            return false;
        }

        if (MultiShotIsActive)
        {
            return false;
        }

        LastMultishotActivated = Time.GetUnixTime();

        PlayParticleEffect(PlayScript.EnchantUpYellow, Guid);

        return true;
    }

    public bool TryUseSteadyShot(Gem gem)
    {
        if (!VerifyCombatFocus(CombatAbility.SteadyShot))
        {
            return false;
        }

        if (SteadyShotIsActive)
        {
            return false;
        }

        LastSteadyShotActivated = Time.GetUnixTime();

        PlayParticleEffect(PlayScript.SkillUpYellow, Guid);

        return true;
    }

    public bool TryUseSmokescreen(WorldObject ability)
    {
        if (!VerifyCombatFocus(CombatAbility.Smokescreen))
        {
            return false;
        }

        var nearbyMonsters = GetNearbyMonsters(15);

        foreach (var target in nearbyMonsters)
        {
            var skillCheck = SkillCheck.GetSkillChance(
                GetModdedDeceptionSkill(),
                target.GetCreatureSkill(Skill.Perception).Current
            );

            if (ThreadSafeRandom.Next(0.0f, 1.0f) > skillCheck)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"{target.Name} can see right through you. Your attempt at reducing threat failed.",
                        ChatMessageType.Broadcast
                    )
                );
                continue;
            }

            target.IncreaseTargetThreatLevel(this, -100);
            LastSmokescreenActivated = Time.GetUnixTime();

            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You successfully reduce your threat level towards {target.Name}.",
                    ChatMessageType.Broadcast
                )
            );

            PlayParticleEffect(PlayScript.VisionUpWhite, Guid);
            target.PlayParticleEffect(PlayScript.VisionDownBlack, target.Guid);
        }

        return true;
    }

    public bool TryUseVanish(WorldObject ability)
    {
        if (!VerifyCombatFocus(CombatAbility.Vanish))
        {
            return false;
        }

        if (IsStealthed)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You cannot use Vanish while stealth.",
                    ChatMessageType.Broadcast
                )
            );
            return false;
        }

        var thieverySkill = GetCreatureSkill(Skill.Thievery); // Thievery
        if (thieverySkill.AdvancementClass < SkillAdvancementClass.Trained)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Vanish requires trained Thievery.",
                    ChatMessageType.Broadcast
                )
            );
            return false;
        }

        // the smoke is enough to fool monsters from far away?
        var nearbyMonsters = GetNearbyMonsters(15);

        var success = true;
        foreach (var target in nearbyMonsters)
        {
            // generous bonus to skill check to start
            var skillCheck = SkillCheck.GetSkillChance(
                GetModdedThieverySkill() + 100,
                target.GetCreatureSkill(Skill.Perception).Current
            );

            if (ThreadSafeRandom.Next(0.0f, 1.0f) > skillCheck)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Some thief you are! {target.Name} was not fooled by your attempt to vanish, and prevents you from entering stealth.",
                        ChatMessageType.Broadcast
                    )
                );
                success = false;
                break;
            }

            var targetTier = target.Tier ?? 1;
            var staminaCost = -2 * Math.Clamp(targetTier, 1, 7);
            UpdateVitalDelta(Stamina, staminaCost);
        }

        if (success)
        {
            var smoke = WorldObjectFactory.CreateNewWorldObject(1051113);
            smoke.Location = Location;
            smoke.EnterWorld();

            this.LastVanishActivated = Time.GetUnixTime();
            this.BeginStealth();
        }

        return true;
    }

    public void TryUseExposePhysicalWeakness(WorldObject ability)
    {
        var target = LastAttackedCreature;

        if (this == target || target.IsDead)
        {
            return;
        }

        var targetAsPlayer = target as Player;

        var attackerPerceptionSkill = GetModdedPerceptionSkill();
        var targetDeceptionSkill = target.GetModdedDeceptionSkill();

        var avoidChance = 1.0f - SkillCheck.GetSkillChance(attackerPerceptionSkill, targetDeceptionSkill);

        var roll = ThreadSafeRandom.Next(0.0f, 1.0f);
        if (avoidChance > roll)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{target.Name}'s deception prevents you from exposing a weakness!",
                    ChatMessageType.Broadcast
                )
            );

            if (targetAsPlayer != null)
            {
                Proficiency.OnSuccessUse(
                    targetAsPlayer,
                    target.GetCreatureSkill(Skill.Deception),
                    attackerPerceptionSkill
                );
                targetAsPlayer.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Your deception prevents {Name} from exposing a weakness!",
                        ChatMessageType.Broadcast
                    )
                );
            }
            return;
        }

        Proficiency.OnSuccessUse(this, GetCreatureSkill(Skill.Perception), targetDeceptionSkill);

        var vulnerabilitySpellLevels = SpellLevelProgression.GetSpellLevels(SpellId.VulnerabilityOther1);
        var imperilSpellLevels = SpellLevelProgression.GetSpellLevels(SpellId.ImperilOther1);

        if (vulnerabilitySpellLevels.Count == 0 || imperilSpellLevels.Count == 0)
        {
            return;
        }

        var overRoll = roll - avoidChance;

        var maxSpellLevel = (int)Math.Clamp(Math.Floor((double)attackerPerceptionSkill / 50), 1, 7);
        if (IsShrouded())
        {
            maxSpellLevel = Math.Min(maxSpellLevel, LevelScaling.MaxExposeSpellLevel(target));
        }

        var spellLevel = (int)Math.Clamp(Math.Floor(overRoll * 10), 1, maxSpellLevel);

        var vulnerabilitySpell = new Spell(vulnerabilitySpellLevels[spellLevel - 1]);
        var imperilSpellLevel = new Spell(imperilSpellLevels[spellLevel - 1]);

        string spellTypePrefix;
        switch (spellLevel)
        {
            default:
                spellTypePrefix = "a slight";
                break;
            case 2:
                spellTypePrefix = "a minor";
                break;
            case 3:
                spellTypePrefix = "a moderate";
                break;
            case 4:
                spellTypePrefix = "a major";
                break;
            case 5:
                spellTypePrefix = "a severe";
                break;
            case 6:
                spellTypePrefix = "a crippling";
                break;
            case 7:
                spellTypePrefix = "a tremendous";
                break;
        }

        Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Your perception allows you to expose {spellTypePrefix} physical weakness on {target.Name}!",
                ChatMessageType.Broadcast
            )
        );
        if (targetAsPlayer != null)
        {
            targetAsPlayer.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{Name}'s perception exposes {spellTypePrefix} physical weakness on you!",
                    ChatMessageType.Broadcast
                )
            );
        }

        if (vulnerabilitySpell.NonComponentTargetType == ItemType.None)
        {
            TryCastSpell(vulnerabilitySpell, null, this, null, false, false, false, false);
        }
        else
        {
            TryCastSpell(vulnerabilitySpell, target, this, null, false, false, false, false);
        }

        if (GetCreatureSkill(Skill.Perception).AdvancementClass == SkillAdvancementClass.Specialized)
        {
            if (imperilSpellLevel.NonComponentTargetType == ItemType.None)
            {
                TryCastSpell(imperilSpellLevel, null, this, null, false, false, false, false);
            }
            else
            {
                TryCastSpell(imperilSpellLevel, target, this, null, false, false, false, false);
            }
        }

        if (targetAsPlayer == null)
        {
            CheckForSigilTrinketOnCastEffects(target, vulnerabilitySpell, false, Skill.Perception, (int)SigilTrinketPerceptionEffect.Exposure);
        }
    }

    public void TryUseExposeMagicalWeakness(WorldObject ability)
    {
        {
            var target = LastAttackedCreature;

            if (this == target || target.IsDead)
            {
                return;
            }

            var targetAsPlayer = target as Player;

            var attackerPerceptionSkill = GetModdedPerceptionSkill();
            var targetDeceptionSkill = target.GetModdedDeceptionSkill();

            var avoidChance = 1.0f - SkillCheck.GetSkillChance(attackerPerceptionSkill, targetDeceptionSkill);

            var roll = ThreadSafeRandom.Next(0.0f, 1.0f);
            if (avoidChance > roll)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"{target.Name}'s deception prevents you from exposing a weakness!",
                        ChatMessageType.Broadcast
                    )
                );

                if (targetAsPlayer != null)
                {
                    Proficiency.OnSuccessUse(
                        targetAsPlayer,
                        target.GetCreatureSkill(Skill.Deception),
                        attackerPerceptionSkill
                    );
                    targetAsPlayer.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Your deception prevents {Name} from exposing a weakness!",
                            ChatMessageType.Broadcast
                        )
                    );
                }
                return;
            }

            Proficiency.OnSuccessUse(this, GetCreatureSkill(Skill.Perception), targetDeceptionSkill);

            var magicYieldSpellLevels = SpellLevelProgression.GetSpellLevels(SpellId.MagicYieldOther1);
            var succumbSpellLevels = SpellLevelProgression.GetSpellLevels(SpellId.ExposeWeakness1); // Succumb

            if (magicYieldSpellLevels.Count == 0 || succumbSpellLevels.Count == 0)
            {
                return;
            }

            var overRoll = roll - avoidChance;

            var maxSpellLevel = (int)Math.Clamp(Math.Floor((double)attackerPerceptionSkill / 50), 1, 7);
            if (IsShrouded())
            {
                maxSpellLevel = Math.Min(maxSpellLevel, LevelScaling.MaxExposeSpellLevel(target));
            }

            var spellLevel = (int)Math.Clamp(Math.Floor(overRoll * 10), 1, maxSpellLevel);

            var magicYieldSpell = new Spell(magicYieldSpellLevels[spellLevel - 1]);
            var succumbSpellLevel = new Spell(succumbSpellLevels[spellLevel - 1]);

            string spellTypePrefix;
            switch (spellLevel)
            {
                default:
                    spellTypePrefix = "a slight";
                    break;
                case 2:
                    spellTypePrefix = "a minor";
                    break;
                case 3:
                    spellTypePrefix = "a moderate";
                    break;
                case 4:
                    spellTypePrefix = "a major";
                    break;
                case 5:
                    spellTypePrefix = "a severe";
                    break;
                case 6:
                    spellTypePrefix = "a crippling";
                    break;
                case 7:
                    spellTypePrefix = "a tremendous";
                    break;
            }

            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Your perception allows you to expose {spellTypePrefix} magical weakness on {target.Name}!",
                    ChatMessageType.Broadcast
                )
            );
            if (targetAsPlayer != null)
            {
                targetAsPlayer.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"{Name}'s perception exposes {spellTypePrefix} magical weakness on you!",
                        ChatMessageType.Broadcast
                    )
                );
            }

            if (magicYieldSpell.NonComponentTargetType == ItemType.None)
            {
                TryCastSpell(magicYieldSpell, null, this, null, false, false, false, false);
            }
            else
            {
                TryCastSpell(magicYieldSpell, target, this, null, false, false, false, false);
            }

            if (GetCreatureSkill(Skill.Perception).AdvancementClass == SkillAdvancementClass.Specialized)
            {
                if (succumbSpellLevel.NonComponentTargetType == ItemType.None)
                {
                    TryCastSpell(succumbSpellLevel, null, this, null, false, false, false, false);
                }
                else
                {
                    TryCastSpell(succumbSpellLevel, target, this, null, false, false, false, false);
                }
            }

            if (targetAsPlayer == null)
            {
                CheckForSigilTrinketOnCastEffects(target, magicYieldSpell, false, Skill.Perception, (int)SigilTrinketPerceptionEffect.Exposure);
            }
        }
    }

    public bool TryUseEnchantedBlade(WorldObject ability)
    {
        var gemAbility = ability as Gem;

        if (ability.CombatAbilityId is null)
        {
            _log.Error("{Ability} is missing a CombatAbilityId", ability.Name);

            if (gemAbility != null)
            {
                gemAbility.CombatAbilitySuccess = false;
            }

            return false;
        }

        var equippedFocus = GetEquippedCombatFocus();
        if (equippedFocus is not { CombatFocusType: (int)CombatFocusType.Spellsword })
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{ability.Name} can only be used while a Spellsword Focus is equipped.",
                    ChatMessageType.Broadcast
                )
            );
            return false;
        }

        var equippedMeleeWeapon = GetEquippedMeleeWeapon();
        if (equippedMeleeWeapon is null)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{ability.Name} can only be used while a melee weapon is equipped.",
                    ChatMessageType.Broadcast
                )
            );
            return false;
        }

        if ((CombatAbility)ability.CombatAbilityId
            is CombatAbility.EnchantedBladeArc
            or CombatAbility.EnchantedBladeVolley
            or CombatAbility.EnchantedBladeBlast
            && GetCreatureSkill(Skill.WarMagic).AdvancementClass < SkillAdvancementClass.Trained)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{ability.Name} requires trained war magic.",
                    ChatMessageType.Broadcast
                )
            );
            return false;
        }

        if ((CombatAbility)ability.CombatAbilityId
            is CombatAbility.EnchantedBladeDrainLife
            or CombatAbility.EnchantedBladeDrainStamina
            or CombatAbility.EnchantedBladeDrainMana
            && GetCreatureSkill(Skill.LifeMagic).AdvancementClass < SkillAdvancementClass.Trained)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{ability.Name} requires trained life magic.",
                    ChatMessageType.Broadcast
                )
            );
            return false;
        }

        var weaponDamageType = equippedMeleeWeapon.W_DamageType;
        if (weaponDamageType is DamageType.SlashPierce)
        {
            weaponDamageType = SlashThrustToggle ? DamageType.Pierce : DamageType.Slash;
        }

        var baseSpell = (CombatAbility)ability.CombatAbilityId switch
        {
            CombatAbility.EnchantedBladeArc => GetLevelOneArcOfDamageType(weaponDamageType),
            CombatAbility.EnchantedBladeBlast => GetLevelOneBlastOfDamageType(weaponDamageType),
            CombatAbility.EnchantedBladeVolley => GetLevelOneVolleyOfDamageType(weaponDamageType),
            CombatAbility.EnchantedBladeDrainLife => new Spell(SpellId.DrainHealth1),
            CombatAbility.EnchantedBladeDrainStamina => new Spell(SpellId.DrainStamina1),
            CombatAbility.EnchantedBladeDrainMana => new Spell(SpellId.DrainMana1),
            _ => null
        };

        if (baseSpell is null)
        {
            _log.Error("TryUseEnchantedBlade() - baseSpell is null");
            return false;
        }

        var weaponSpellcraft = equippedMeleeWeapon.ItemSpellcraft;

        if (weaponSpellcraft is null)
        {
            _log.Error("TryUseEnchantedBlade() - {Weapon} does not have spellcraft", equippedMeleeWeapon.Name);
            return false;
        }

        var warSkill = GetModdedWarMagicSkill();
        var averagedMagicSkill = (uint)((warSkill + weaponSpellcraft) * 0.5);
        var roll = Convert.ToInt32(ThreadSafeRandom.Next(averagedMagicSkill * 0.5f, warSkill));
        int[] diff = [50, 100, 200, 300, 350, 400, 450];
        var closest = diff.MinBy(x => Math.Abs(x - roll));
        var level = Array.IndexOf(diff, closest);

        var finalSpellId = SpellLevelProgression.GetSpellAtLevel((SpellId)baseSpell.Id, level + 1);

        EnchantedWeaponStoredSpell = new Spell(finalSpellId);

        var manaCost = (int)EnchantedWeaponStoredSpell.BaseMana * 2;
        if (Mana.Current < manaCost)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat($"You do not have enough mana.", ChatMessageType.Broadcast)
            );
            return false;
        }

        UpdateVitalDelta(Mana, -manaCost);

        var particalIntensity = Math.Clamp((level - 1) * (1.0f / 6.0f), 0.0f, 1.0f);
        //var playScript = GetPlayScriptColor(weaponDamageType);

        PlayParticleEffect(PlayScript.EnchantUpPurple, Guid, particalIntensity);

        return true;
    }

    private PlayScript GetPlayScriptColor(DamageType weaponDamageType)
    {
        return weaponDamageType switch
        {
            DamageType.Slash => PlayScript.EnchantUpOrange,
            DamageType.Pierce => PlayScript.EnchantUpYellow,
            DamageType.Bludgeon => PlayScript.EnchantUpWhite,
            DamageType.Cold => PlayScript.EnchantUpBlue,
            DamageType.Fire => PlayScript.EnchantUpRed,
            DamageType.Acid => PlayScript.EnchantUpGreen,
            DamageType.Electric => PlayScript.EnchantUpPurple,
            _ => PlayScript.EnchantUpGrey
        };
    }

    private Spell GetLevelOneArcOfDamageType(DamageType weaponDamageType)
    {
        return weaponDamageType switch
        {
            DamageType.Slash => new Spell(SpellId.BladeArc1),
            DamageType.Pierce => new Spell(SpellId.ForceArc1),
            DamageType.Bludgeon => new Spell(SpellId.ShockArc1),
            DamageType.Cold => new Spell(SpellId.FrostArc1),
            DamageType.Fire => new Spell(SpellId.FlameArc1),
            DamageType.Acid => new Spell(SpellId.AcidArc1),
            DamageType.Electric => new Spell(SpellId.LightningArc1),
            _ => null
        };
    }

    private Spell GetLevelOneBlastOfDamageType(DamageType weaponDamageType)
    {
        return weaponDamageType switch
        {
            DamageType.Slash => new Spell(SpellId.BladeBlast1),
            DamageType.Pierce => new Spell(SpellId.ForceBlast1),
            DamageType.Bludgeon => new Spell(SpellId.ShockBlast1),
            DamageType.Cold => new Spell(SpellId.FrostBlast1),
            DamageType.Fire => new Spell(SpellId.FlameBlast1),
            DamageType.Acid => new Spell(SpellId.AcidBlast1),
            DamageType.Electric => new Spell(SpellId.LightningBlast1),
            _ => null
        };
    }

    private Spell GetLevelOneVolleyOfDamageType(DamageType weaponDamageType)
    {
        return weaponDamageType switch
        {
            DamageType.Slash => new Spell(SpellId.BladeVolley1),
            DamageType.Pierce => new Spell(SpellId.ForceVolley1),
            DamageType.Bludgeon => new Spell(SpellId.BludgeoningVolley1),
            DamageType.Cold => new Spell(SpellId.FrostVolley1),
            DamageType.Fire => new Spell(SpellId.FlameVolley1),
            DamageType.Acid => new Spell(SpellId.AcidVolley1),
            DamageType.Electric => new Spell(SpellId.LightningVolley1),
            _ => null
        };
    }

    public bool TryUseManaBarrier()
    {
        if (!VerifyCombatFocus(CombatAbility.ManaBarrier))
        {
            return false;
        }

        if (!ManaBarrierIsActive)
        {
            ManaBarrierIsActive = true;

            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You draw on your stored mana to form an enchanted shield around yourself!",
                    ChatMessageType.Broadcast
                )
            );
            PlayParticleEffect(PlayScript.ShieldUpBlue, Guid);

            return false;
        }

        ManaBarrierIsActive = false;

        Session.Network.EnqueueSend(
            new GameMessageSystemChat($"You dispel your mana barrier.", ChatMessageType.Broadcast)
        );
        PlayParticleEffect(PlayScript.DispelLife, Guid);

        return true;
    }

    public bool TryUseEvasiveStance()
    {
        if (!VerifyCombatFocus(CombatAbility.EvasiveStance))
        {
            return false;
        }

        if (!EvasiveStanceIsActive)
        {
            EvasiveStanceIsActive = true;

            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You move into an evasive stance!",
                    ChatMessageType.Broadcast
                )
            );
            PlayParticleEffect(PlayScript.ShieldUpYellow, Guid);

            return false;
        }

        EvasiveStanceIsActive = false;

        Session.Network.EnqueueSend(
            new GameMessageSystemChat($"You move out of your evasive stance.", ChatMessageType.Broadcast)
        );
        PlayParticleEffect(PlayScript.DispelLife, Guid);

        return true;
    }

    public void TryUseShroud()
    {
        if (EnchantmentManager.HasSpell(5379))
        {
            if (IsBusy)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You cannot dispel the Shroud while performing other actions.",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }

            if (Teleporting)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You cannot dispel the Shroud while teleporting.",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }

            if (LastSuccessCast_Time > Time.GetUnixTime() - 5.0)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You cannot dispel the Shroud if you have recently cast a spell.",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
            if (CurrentLandblock != null && CurrentLandblock.IsDungeon)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You cannot dispel the Shroud while inside a dungeon.",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }

            if (Fellowship != null)
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You must leave your Fellowship before you can dispel the Shroud.",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }

            var enchantment = EnchantmentManager.GetEnchantment(5379);
            if (enchantment != null)
            {
                EnchantmentManager.Dispel(enchantment);
                HandleSpellHooks(new Spell(5379));
                PlayParticleEffect(PlayScript.DispelCreature, Guid);
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You dispel the Shroud, and your innate strength returns.",
                        ChatMessageType.Broadcast
                    )
                );
            }
        }
        else
        {
            var spell = new Spell(5379);
            var addResult = EnchantmentManager.Add(spell, null, null, true);
            Session.Network.EnqueueSend(
                new GameEventMagicUpdateEnchantment(
                    Session,
                    new Enchantment(this, addResult.Enchantment)
                )
            );
            HandleSpellHooks(spell);
            PlayParticleEffect(PlayScript.SkillDownVoid, Guid);
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You activate the crystal, shrouding yourself and reducing your innate power.",
                    ChatMessageType.Broadcast
                )
            );
        }
    }

    private bool VerifyCombatFocus(CombatAbility combatAbility)
    {
        switch (combatAbility)
        {
            case CombatAbility.Phalanx:
                if (GetEquippedCombatFocus() is not {CombatFocusType: (int)CombatFocusType.Warrior})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Provoke can only be used with a Warrior Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
            case CombatAbility.Provoke:
                if (GetEquippedCombatFocus() is not {CombatFocusType:
                        (int)CombatFocusType.Warrior
                        or (int)CombatFocusType.Spellsword
                        or (int)CombatFocusType.Blademaster})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Provoke can only be used with a Warrior Focus, Blademaster Focus, or Spellsword Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
            case CombatAbility.Parry:
                if (GetEquippedCombatFocus() is not {CombatFocusType:
                        (int)CombatFocusType.Warrior
                        or (int)CombatFocusType.Spellsword
                        or (int)CombatFocusType.Blademaster})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Parry can only be used with a Warrior Focus, Blademaster Focus, or Spellsword Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
            case CombatAbility.Fury:
                if (GetEquippedCombatFocus() is not {CombatFocusType:
                        (int)CombatFocusType.Blademaster
                        or (int)CombatFocusType.Warrior
                        or (int)CombatFocusType.Archer})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Fury can only be used with a Blademaster Focus, Warrior Focus, or Archer Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
            case CombatAbility.Multishot:
                if (GetEquippedCombatFocus() is not {CombatFocusType: (int)CombatFocusType.Archer})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Multishot can only be used with an Archer Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
            case CombatAbility.SteadyShot:
                if (GetEquippedCombatFocus() is not {CombatFocusType:
                        (int)CombatFocusType.Archer
                        or (int)CombatFocusType.Blademaster
                        or (int)CombatFocusType.Vagabond})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Steady Shot can only be used with an Archer Focus, Blademaster Focus, or Vagabond Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
            case CombatAbility.ManaBarrier:
                if (GetEquippedCombatFocus() is not {CombatFocusType:
                        (int)CombatFocusType.Sorcerer
                        or (int)CombatFocusType.Vagabond
                        or (int)CombatFocusType.Spellsword})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Mana Barrier can only be used with a Sorcerer Focus, Vagabond Focus, or Spellsword Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
            case CombatAbility.EvasiveStance:
                if (GetEquippedCombatFocus() is not {CombatFocusType:
                        (int)CombatFocusType.Blademaster
                        or (int)CombatFocusType.Archer
                        or (int)CombatFocusType.Vagabond})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Mana Barrier can only be used with a Sorcerer Focus, Vagabond Focus, or Spellsword Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
            case CombatAbility.Smokescreen:
                if (GetEquippedCombatFocus() is not {CombatFocusType:
                        (int)CombatFocusType.Vagabond
                        or (int)CombatFocusType.Archer
                        or (int)CombatFocusType.Sorcerer})
                {
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Smokescreen can only be used with a Vagabond Focus, Archer Focus, or Spellsword Focus",
                            ChatMessageType.Broadcast
                        )
                    );
                    return false;
                }
                break;
        }

        return true;
    }

    public static int HandleRecklessStamps(Player playerAttacker)
    {
        var scaledStamps = Math.Round(playerAttacker.ScaleWithPowerAccuracyBar(20f));

        scaledStamps += 20f;

        var numStrikes = playerAttacker.GetNumStrikes(playerAttacker.AttackType);
        if (numStrikes == 2)
        {
            if (playerAttacker.GetEquippedWeapon().W_WeaponType == WeaponType.TwoHanded)
            {
                scaledStamps /= 2f;
            }
            else
            {
                scaledStamps /= 1.5f;
            }
        }

        if (
            playerAttacker.AttackType == AttackType.OffhandPunch
            || playerAttacker.AttackType == AttackType.Punch
            || playerAttacker.AttackType == AttackType.Punches
        )
        {
            scaledStamps /= 1.25f;
        }

        if (!playerAttacker.QuestManager.HasQuest($"{playerAttacker.Name},Reckless"))
        {
            playerAttacker.QuestManager.Stamp($"{playerAttacker.Name},Reckless");
            playerAttacker.QuestManager.Increment($"{playerAttacker.Name},Reckless", (int)scaledStamps);
        }
        else if (playerAttacker.QuestManager.GetCurrentSolves($"{playerAttacker.Name},Reckless") < 500)
        {
            playerAttacker.QuestManager.Increment($"{playerAttacker.Name},Reckless", (int)scaledStamps);
        }

        var stacks = playerAttacker.QuestManager.GetCurrentSolves($"{playerAttacker.Name},Reckless");

        return stacks;
    }

    public static int HandleOverloadStamps(Player sourcePlayer, int? spellTypeScaler, uint spellLevel)
    {
        var baseStamps = 50;
        var playerLevel = (int)(sourcePlayer.Level / 10);

        if (playerLevel > 7)
        {
            playerLevel = 7;
        }

        if (spellLevel == 1)
        {
            if (playerLevel > 2)
            {
                baseStamps /= playerLevel;
            }
        }
        if (spellLevel == 2)
        {
            baseStamps = (int)(baseStamps * 1.5);

            if (playerLevel > 3)
            {
                baseStamps /= playerLevel;
            }
        }
        if (spellLevel == 3)
        {
            baseStamps = (int)(baseStamps * 2);

            if (playerLevel > 4)
            {
                baseStamps /= playerLevel;
            }
        }
        if (spellLevel == 4)
        {
            baseStamps = (int)(baseStamps * 2.25);

            if (playerLevel > 5)
            {
                baseStamps /= playerLevel;
            }
        }
        if (spellLevel == 5)
        {
            baseStamps = (int)(baseStamps * 2.5);

            if (playerLevel > 6)
            {
                baseStamps /= playerLevel;
            }
        }
        if (spellLevel == 6)
        {
            baseStamps = (int)(baseStamps * 2.5);

            if (playerLevel > 7)
            {
                baseStamps /= playerLevel;
            }
        }
        if (spellLevel == 7)
        {
            baseStamps = (int)(baseStamps * 2.5);
        }

        if (spellTypeScaler != null)
        {
            baseStamps = baseStamps / (int)spellTypeScaler;
        }

        if (!sourcePlayer.QuestManager.HasQuest($"{sourcePlayer.Name},Overload"))
        {
            sourcePlayer.QuestManager.Stamp($"{sourcePlayer.Name},Overload");
            sourcePlayer.QuestManager.Increment($"{sourcePlayer.Name},Overload", (int)baseStamps);
        }
        else if (sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload") < 500)
        {
            sourcePlayer.QuestManager.Increment($"{sourcePlayer.Name},Overload", (int)baseStamps);
        }

        var stacks = sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload");
        if (stacks > 250)
        {
            var overloadChance = 0.075f * (stacks - 250) / 250;
            if (overloadChance > ThreadSafeRandom.Next(0f, 1f))
            {
                var damage = sourcePlayer.Health.MaxValue / 10;
                sourcePlayer.UpdateVitalDelta(sourcePlayer.Health, -(int)damage);
                sourcePlayer.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Overloaded! You lose control of the energies flowing through you, suffering {damage} points of damage!",
                        ChatMessageType.Magic
                    )
                );
                sourcePlayer.PlayParticleEffect(PlayScript.Fizzle, sourcePlayer.Guid);
                sourcePlayer.DamageHistory.Add(sourcePlayer, DamageType.Health, (uint)-damage);
                if (sourcePlayer.IsDead)
                {
                    var lastDamager = sourcePlayer.DamageHistory.LastDamager;
                    sourcePlayer.OnDeath(lastDamager, DamageType.Health, false);
                    sourcePlayer.Die();
                }
            }
        }

        // Max Stacks = 500, so a percentage is stacks / 5
        var overloadPercent = sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload") / 5;
        if (overloadPercent > 100)
        {
            overloadPercent = 100;
        }

        if (overloadPercent < 0)
        {
            overloadPercent = 0;
        }

        return overloadPercent;
    }
}
