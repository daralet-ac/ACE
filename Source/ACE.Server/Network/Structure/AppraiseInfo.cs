using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Network.Structure
{
    /// <summary>
    /// Handles calculating and sending all object appraisal info
    /// </summary>
    public class AppraiseInfo
    {
        private static readonly uint EnchantmentMask = 0x80000000;

        public IdentifyResponseFlags Flags;

        public bool Success;    // assessment successful?

        public Dictionary<PropertyInt, int> PropertiesInt;
        public Dictionary<PropertyInt64, long> PropertiesInt64;
        public Dictionary<PropertyBool, bool> PropertiesBool;
        public Dictionary<PropertyFloat, double> PropertiesFloat;
        public Dictionary<PropertyString, string> PropertiesString;
        public Dictionary<PropertyDataId, uint> PropertiesDID;
        public Dictionary<PropertyInstanceId, uint> PropertiesIID;

        public List<uint> SpellBook;

        public ArmorProfile ArmorProfile;
        public CreatureProfile CreatureProfile;
        public WeaponProfile WeaponProfile;
        public HookProfile HookProfile;

        public ArmorMask ArmorHighlight;
        public ArmorMask ArmorColor;
        public WeaponMask WeaponHighlight;
        public WeaponMask WeaponColor;
        public ResistMask ResistHighlight;
        public ResistMask ResistColor;

        public ArmorLevel ArmorLevels;

        public bool IsArmorCapped = false;
        public bool IsArmorBuffed = false;
        public bool IsAttackModBuffed = false;

        // This helps ensure the item will identify properly. Some "items" are technically "Creatures".
        private bool NPCLooksLikeObject;

        public AppraiseInfo()
        {
            Flags = IdentifyResponseFlags.None;
            Success = false;
        }

        /// <summary>
        /// Construct all of the info required for appraising any WorldObject
        /// </summary>
        public AppraiseInfo(WorldObject wo, Player examiner, bool success = true)
        {
            BuildProfile(wo, examiner, success);
        }

        public void BuildProfile(WorldObject wo, Player examiner, bool success = true)
        {
            //Console.WriteLine("Appraise: " + wo.Guid);
            Success = success;

            BuildProperties(wo);
            BuildSpells(wo);

            // Help us make sure the item identify properly
            NPCLooksLikeObject = wo.GetProperty(PropertyBool.NpcLooksLikeObject) ?? false;

            if (PropertiesIID.ContainsKey(PropertyInstanceId.AllowedWielder) && !PropertiesBool.ContainsKey(PropertyBool.AppraisalHasAllowedWielder))
                PropertiesBool.Add(PropertyBool.AppraisalHasAllowedWielder, true);

            if (PropertiesIID.ContainsKey(PropertyInstanceId.AllowedActivator) && !PropertiesBool.ContainsKey(PropertyBool.AppraisalHasAllowedActivator))
                PropertiesBool.Add(PropertyBool.AppraisalHasAllowedActivator, true);

            if (PropertiesString.ContainsKey(PropertyString.ScribeAccount) && !examiner.IsAdmin && !examiner.IsSentinel && !examiner.IsEnvoy && !examiner.IsArch && !examiner.IsPsr)
                PropertiesString.Remove(PropertyString.ScribeAccount);

            if (PropertiesString.ContainsKey(PropertyString.HouseOwnerAccount) && !examiner.IsAdmin && !examiner.IsSentinel && !examiner.IsEnvoy && !examiner.IsArch && !examiner.IsPsr)
                PropertiesString.Remove(PropertyString.HouseOwnerAccount);

            if (PropertiesInt.ContainsKey(PropertyInt.Lifespan))
                PropertiesInt[PropertyInt.RemainingLifespan] = wo.GetRemainingLifespan();

            if (PropertiesInt.TryGetValue(PropertyInt.Faction1Bits, out var faction1Bits))
            {
                // hide any non-default factions, prevent client from displaying ???
                // this is only needed for non-standard faction creatures that use templates, to hide the ??? in the client
                var sendBits = faction1Bits & (int)FactionBits.ValidFactions;
                if (sendBits != faction1Bits)
                {
                    if (sendBits != 0)
                        PropertiesInt[PropertyInt.Faction1Bits] = sendBits;
                    else
                        PropertiesInt.Remove(PropertyInt.Faction1Bits);
                }
            }

            // salvage bag cleanup
            if (wo.WeenieType == WeenieType.Salvage)
            {
                if (wo.GetProperty(PropertyInt.Structure).HasValue)
                    PropertiesInt.Remove(PropertyInt.Structure);

                if(wo.GetProperty(PropertyInt.MaxStructure).HasValue)
                    PropertiesInt.Remove(PropertyInt.MaxStructure);
            }

            // armor / clothing / shield
            if (wo is Clothing || wo.IsShield)
                BuildArmor(wo);

            if (wo is Creature creature)
                BuildCreature(creature);

            if (wo.Damage != null && !(wo is Clothing) || wo is MeleeWeapon || wo is Missile || wo is MissileLauncher || wo is Ammunition || wo is Caster)
                BuildWeapon(wo);

            // TODO: Resolve this issue a better way?
            // Because of the way ACE handles default base values in recipe system (or rather the lack thereof)
            // we need to check the following weapon properties to see if they're below expected minimum and adjust accordingly
            // The issue is that the recipe system likely added 0.005 to 0 instead of 1, which is what *should* have happened.

            //if (wo.WeaponMagicDefense.HasValue && wo.WeaponMagicDefense.Value > 0 && wo.WeaponMagicDefense.Value < 1 && ((wo.GetProperty(PropertyInt.ImbueStackingBits) ?? 0) & 1) != 0)
            //    PropertiesFloat[PropertyFloat.WeaponMagicDefense] += 1;
            //if (wo.WeaponMissileDefense.HasValue && wo.WeaponMissileDefense.Value > 0 && wo.WeaponMissileDefense.Value < 1 && ((wo.GetProperty(PropertyInt.ImbueStackingBits) ?? 0) & 1) != 0)
            //    PropertiesFloat[PropertyFloat.WeaponMissileDefense] += 1;

            // Mask real value of AbsorbMagicDamage and/or Add AbsorbMagicDamage for ImbuedEffectType.IgnoreSomeMagicProjectileDamage
            if (PropertiesFloat.ContainsKey(PropertyFloat.AbsorbMagicDamage) || wo.HasImbuedEffect(ImbuedEffectType.IgnoreSomeMagicProjectileDamage))
                PropertiesFloat[PropertyFloat.AbsorbMagicDamage] = 1;

            if (wo is PressurePlate)
            {
                if (PropertiesInt.ContainsKey(PropertyInt.ResistLockpick))
                    PropertiesInt.Remove(PropertyInt.ResistLockpick);

                if (PropertiesInt.ContainsKey(PropertyInt.Value))
                    PropertiesInt.Remove(PropertyInt.Value);

                if (PropertiesInt.ContainsKey(PropertyInt.EncumbranceVal))
                    PropertiesInt.Remove(PropertyInt.EncumbranceVal);

                PropertiesString.Add(PropertyString.ShortDesc, wo.Active ? "Status: Armed" : "Status: Disarmed");
            }
            else if (wo is Door || wo is Chest)
            {
                // If wo is not locked, do not send ResistLockpick value. If ResistLockpick is sent for unlocked objects, id panel shows bonus to Lockpick skill
                if (!wo.IsLocked && PropertiesInt.ContainsKey(PropertyInt.ResistLockpick))
                    PropertiesInt.Remove(PropertyInt.ResistLockpick);

                // If wo is locked, append skill check percent, as int, to properties for id panel display on chances of success
                if (wo.IsLocked)
                {
                    var resistLockpick = LockHelper.GetResistLockpick(wo);

                    if (resistLockpick != null)
                    {
                        PropertiesInt[PropertyInt.ResistLockpick] = (int)resistLockpick;

                        var pickSkill = examiner.Skills[Skill.Lockpick].Current;

                        var successChance = SkillCheck.GetSkillChance((int)pickSkill, (int)resistLockpick) * 100;

                        if (!PropertiesInt.ContainsKey(PropertyInt.AppraisalLockpickSuccessPercent))
                            PropertiesInt.Add(PropertyInt.AppraisalLockpickSuccessPercent, (int)successChance);
                    }
                }
                // if wo has DefaultLocked property and is unlocked, add that state to the property buckets
                else if (PropertiesBool.ContainsKey(PropertyBool.DefaultLocked))
                    PropertiesBool[PropertyBool.Locked] = false;
            }

            if (wo is Corpse)
            {
                PropertiesBool.Clear();
                PropertiesDID.Clear();
                PropertiesFloat.Clear();
                PropertiesInt64.Clear();

                var discardInts = PropertiesInt.Where(x => x.Key != PropertyInt.EncumbranceVal && x.Key != PropertyInt.Value).Select(x => x.Key).ToList();
                foreach (var key in discardInts)
                    PropertiesInt.Remove(key);
                var discardString = PropertiesString.Where(x => x.Key != PropertyString.LongDesc).Select(x => x.Key).ToList();
                foreach (var key in discardString)
                    PropertiesString.Remove(key);

                PropertiesInt[PropertyInt.Value] = 0;
            }

            if (wo is Portal)
            {
                if (PropertiesInt.ContainsKey(PropertyInt.EncumbranceVal))
                    PropertiesInt.Remove(PropertyInt.EncumbranceVal);
            }

            if (wo is SlumLord slumLord)
            {                
                PropertiesBool.Clear();
                PropertiesDID.Clear();
                PropertiesFloat.Clear();
                PropertiesIID.Clear();
                //PropertiesInt.Clear();
                PropertiesInt64.Clear();
                PropertiesString.Clear();

                var longDesc = "";

                if (slumLord.HouseOwner.HasValue && slumLord.HouseOwner.Value > 0)
                {
                    longDesc = $"The current maintenance has {(slumLord.IsRentPaid() || !PropertyManager.GetBool("house_rent_enabled").Item ? "" : "not ")}been paid.\n";

                    PropertiesInt.Clear();
                }
                else
                {
                    //longDesc = $"This house is {(slumLord.HouseStatus == HouseStatus.Disabled ? "not " : "")}available for purchase.\n"; // this was the retail msg.
                    longDesc = $"This {(slumLord.House.HouseType == HouseType.Undef ? "house" : slumLord.House.HouseType.ToString().ToLower())} is {(slumLord.House.HouseStatus == HouseStatus.Disabled ? "not " : "")}available for purchase.\n";

                    var discardInts = PropertiesInt.Where(x => x.Key != PropertyInt.HouseStatus && x.Key != PropertyInt.HouseType && x.Key != PropertyInt.MinLevel && x.Key != PropertyInt.MaxLevel && x.Key != PropertyInt.AllegianceMinLevel && x.Key != PropertyInt.AllegianceMaxLevel).Select(x => x.Key).ToList();
                    foreach (var key in discardInts)
                        PropertiesInt.Remove(key);
                }

                if (slumLord.HouseRequiresMonarch)
                    longDesc += "You must be a monarch to purchase and maintain this dwelling.\n";

                if (slumLord.AllegianceMinLevel.HasValue)
                {
                    var allegianceMinLevel = PropertyManager.GetLong("mansion_min_rank", -1).Item;
                    if (allegianceMinLevel == -1)
                        allegianceMinLevel = slumLord.AllegianceMinLevel.Value;

                    longDesc += $"Restricted to characters of allegiance rank {allegianceMinLevel} or greater.\n";
                }

                PropertiesString.Add(PropertyString.LongDesc, longDesc);
            }

            if (wo is Container)
            {
                if (PropertiesInt.ContainsKey(PropertyInt.Value))
                    PropertiesInt[PropertyInt.Value] = DatabaseManager.World.GetCachedWeenie(wo.WeenieClassId).GetValue() ?? 0; // Value is masked to base value of Weenie
            }

            if (wo is Storage)
            {
                var longDesc = "";

                if (wo.HouseOwner.HasValue && wo.HouseOwner.Value > 0)
                    longDesc = $"Owned by {wo.ParentLink.HouseOwnerName}\n";

                var discardString = PropertiesString.Where(x => x.Key != PropertyString.Use).Select(x => x.Key).ToList();
                foreach (var key in discardString)
                    PropertiesString.Remove(key);

                PropertiesString.Add(PropertyString.LongDesc, longDesc);
            }

            if (wo is Hook)
            {
                // If the hook has any inventory, we need to send THOSE properties instead.
                var hook = wo as Container;

                string baseDescString = "";
                if (wo.ParentLink.HouseOwner != null)
                {
                    // This is for backwards compatibility. This value was not set/saved in earlier versions.
                    // It will get the player's name and save that to the HouseOwnerName property of the house. This is now done when a player purchases a house.
                    if (wo.ParentLink.HouseOwnerName == null)
                    {
                        var houseOwnerPlayer = PlayerManager.FindByGuid((uint)wo.ParentLink.HouseOwner);
                        if (houseOwnerPlayer != null)
                        {
                            wo.ParentLink.HouseOwnerName = houseOwnerPlayer.Name;
                            wo.ParentLink.SaveBiotaToDatabase();
                        }
                    }
                    baseDescString = "This hook is owned by " + wo.ParentLink.HouseOwnerName + ". "; //if house is owned, display this text
                }

                var containsString = "";
                if (hook.Inventory.Count == 1)
                {
                    WorldObject hookedItem = hook.Inventory.First().Value;

                    // Hooked items have a custom "description", containing the desc of the sub item and who the owner of the house is (if any)
                    BuildProfile(hookedItem, examiner, success);

                    containsString = "It contains: \n";

                    if (!string.IsNullOrWhiteSpace(hookedItem.LongDesc))
                    {
                        containsString += hookedItem.LongDesc;
                    }
                    //else if (PropertiesString.ContainsKey(PropertyString.ShortDesc) && PropertiesString[PropertyString.ShortDesc] != null)
                    //{
                    //    containsString += PropertiesString[PropertyString.ShortDesc];
                    //}
                    else
                    {
                        containsString += hookedItem.Name;
                    }

                    BuildHookProfile(hookedItem);
                }

                //if (PropertiesString.ContainsKey(PropertyString.LongDesc) && PropertiesString[PropertyString.LongDesc] != null)
                //    PropertiesString[PropertyString.LongDesc] = baseDescString + containsString;
                ////else if (PropertiesString.ContainsKey(PropertyString.ShortDesc) && PropertiesString[PropertyString.ShortDesc] != null)
                ////    PropertiesString[PropertyString.LongDesc] = baseDescString + containsString;
                //else
                //    PropertiesString[PropertyString.LongDesc] = baseDescString + containsString;

                PropertiesString[PropertyString.LongDesc] = baseDescString + containsString;

                PropertiesInt.Remove(PropertyInt.Structure);

                // retail should have removed this property and then server side built the same result for the hook longdesc replacement but didn't and ends up with some odd looking appraisals as seen on video/pcaps
                //PropertiesInt.Remove(PropertyInt.AppraisalLongDescDecoration);
            }

            if (wo is ManaStone)
            {
                var useMessage = "";

                if (wo.ItemCurMana.HasValue)
                    useMessage = "Use on a magic item to give the stone's stored Mana to that item.";
                else
                    useMessage = "Use on a magic item to destroy that item and drain its Mana.";

                PropertiesString[PropertyString.Use] = useMessage;
            }

            if (wo is CraftTool && (wo.ItemType == ItemType.TinkeringMaterial || wo.WeenieClassId >= 36619 && wo.WeenieClassId <= 36628 || wo.WeenieClassId >= 36634 && wo.WeenieClassId <= 36636))
            {
                if (PropertiesInt.ContainsKey(PropertyInt.Structure))
                    PropertiesInt.Remove(PropertyInt.Structure);
            }

            if (!Success)
            {
                // todo: what specifically to keep/what to clear

                //PropertiesBool.Clear();
                //PropertiesDID.Clear();
                //PropertiesFloat.Clear();
                //PropertiesIID.Clear();
                //PropertiesInt.Clear();
                //PropertiesInt64.Clear();
                //PropertiesString.Clear();

                if (PropertiesInt.ContainsKey(PropertyInt.Value))
                    PropertiesInt.Remove(PropertyInt.Value);
            }

            BuildFlags();
        }

        private void BuildProperties(WorldObject wo)
        {
            PropertiesInt = wo.GetAllPropertyInt().Where(x => ClientProperties.PropertiesInt.Contains((ushort)x.Key)).ToDictionary(x => x.Key, x => x.Value);
            PropertiesInt64 = wo.GetAllPropertyInt64().Where(x => ClientProperties.PropertiesInt64.Contains((ushort)x.Key)).ToDictionary(x => x.Key, x => x.Value);
            PropertiesBool = wo.GetAllPropertyBools().Where(x => ClientProperties.PropertiesBool.Contains((ushort)x.Key)).ToDictionary(x => x.Key, x => x.Value);
            PropertiesFloat = wo.GetAllPropertyFloat().Where(x => ClientProperties.PropertiesDouble.Contains((ushort)x.Key)).ToDictionary(x => x.Key, x => x.Value);
            PropertiesString = wo.GetAllPropertyString().Where(x => ClientProperties.PropertiesString.Contains((ushort)x.Key)).ToDictionary(x => x.Key, x => x.Value);
            PropertiesDID = wo.GetAllPropertyDataId().Where(x => ClientProperties.PropertiesDataId.Contains((ushort)x.Key)).ToDictionary(x => x.Key, x => x.Value);
            PropertiesIID = wo.GetAllPropertyInstanceId().Where(x => ClientProperties.PropertiesInstanceId.Contains((ushort)x.Key)).ToDictionary(x => x.Key, x => x.Value);

            if (wo is Player player)
            {
                // handle character options
                if (!player.GetCharacterOption(CharacterOption.AllowOthersToSeeYourDateOfBirth))
                    PropertiesString.Remove(PropertyString.DateOfBirth);
                if (!player.GetCharacterOption(CharacterOption.AllowOthersToSeeYourAge))
                    PropertiesInt.Remove(PropertyInt.Age);
                if (!player.GetCharacterOption(CharacterOption.AllowOthersToSeeYourChessRank))
                    PropertiesInt.Remove(PropertyInt.ChessRank);
                if (!player.GetCharacterOption(CharacterOption.AllowOthersToSeeYourFishingSkill))
                    PropertiesInt.Remove(PropertyInt.FakeFishingSkill);
                if (!player.GetCharacterOption(CharacterOption.AllowOthersToSeeYourNumberOfDeaths))
                    PropertiesInt.Remove(PropertyInt.NumDeaths);
                if (!player.GetCharacterOption(CharacterOption.AllowOthersToSeeYourNumberOfTitles))
                    PropertiesInt.Remove(PropertyInt.NumCharacterTitles);

                // handle dynamic properties for appraisal
                if (player.Allegiance != null && player.AllegianceNode != null)
                {
                    if (player.Allegiance.AllegianceName != null)
                        PropertiesString[PropertyString.AllegianceName] = player.Allegiance.AllegianceName;

                    if (player.AllegianceNode.IsMonarch)
                    {
                        PropertiesInt[PropertyInt.AllegianceFollowers] = player.AllegianceNode.TotalFollowers;
                    }
                    else
                    {
                        var monarch = player.Allegiance.Monarch;
                        var patron = player.AllegianceNode.Patron;

                        PropertiesString[PropertyString.MonarchsTitle] = AllegianceTitle.GetTitle((HeritageGroup)(monarch.Player.Heritage ?? 0), (Gender)(monarch.Player.Gender ?? 0), monarch.Rank) + " " + monarch.Player.Name;
                        PropertiesString[PropertyString.PatronsTitle] = AllegianceTitle.GetTitle((HeritageGroup)(patron.Player.Heritage ?? 0), (Gender)(patron.Player.Gender ?? 0), patron.Rank) + " " + patron.Player.Name;
                    }
                }

                if (player.Fellowship != null)
                    PropertiesString[PropertyString.Fellowship] = player.Fellowship.FellowshipName;
            }
            AddPropertyEnchantments(wo);
        }

        private void AddPropertyEnchantments(WorldObject wo)
        {
            if (wo == null) return;

            if (PropertiesInt.ContainsKey(PropertyInt.ArmorLevel))
            {
                PropertiesInt[PropertyInt.ArmorLevel] += wo.EnchantmentManager.GetArmorMod();

                var baseArmor = PropertiesInt[PropertyInt.ArmorLevel];

                var wielder = wo.Wielder as Player;
                if (wielder != null && ((wo.ClothingPriority ?? 0) & (CoverageMask)CoverageMaskHelper.Underwear) == 0)
                {
                    int armor;

                    if (wo.IsShield)
                        armor = (int)wielder.GetSkillModifiedShieldLevel(baseArmor);
                    else
                        armor = (int)wielder.GetSkillModifiedArmorLevel(baseArmor, (ArmorWeightClass)(wo.ArmorWeightClass ?? 0));
                    if (armor < baseArmor)
                    {
                        PropertiesInt[PropertyInt.ArmorLevel] = armor;
                        IsArmorCapped = true;
                    }
                    else if (armor > baseArmor)
                    {
                        PropertiesInt[PropertyInt.ArmorLevel] = armor;
                        IsArmorBuffed = true;
                    }
                }
            }

            if (wo.ItemSkillLimit != null)
                PropertiesInt[PropertyInt.AppraisalItemSkill] = (int)wo.ItemSkillLimit;
            else
                PropertiesInt.Remove(PropertyInt.AppraisalItemSkill);

            //if (PropertiesFloat.ContainsKey(PropertyFloat.WeaponDefense) && !(wo is Ammunition))
            //{
            //    var defenseMod = wo.EnchantmentManager.GetDefenseMod();
            //    var auraDefenseMod = wo.Wielder != null && wo.IsEnchantable ? wo.Wielder.EnchantmentManager.GetDefenseMod() : 0.0f;

            //    PropertiesFloat[PropertyFloat.WeaponDefense] += defenseMod + auraDefenseMod;
            //}

            if (PropertiesFloat.ContainsKey(PropertyFloat.WeaponOffense))
            {
                var attackMod = wo.EnchantmentManager.GetAttackMod();
                var auraAttackMod = wo.Wielder != null && wo.IsEnchantable ? wo.Wielder.EnchantmentManager.GetAttackMod() : 0.0f;

                PropertiesFloat[PropertyFloat.WeaponOffense] += attackMod + auraAttackMod;

                if (auraAttackMod > 0)
                {
                    IsAttackModBuffed = true;
                }
            }

            if (PropertiesFloat.TryGetValue(PropertyFloat.ManaConversionMod, out var manaConvMod))
            {
                if (manaConvMod != 0)
                {
                    // hermetic link/void
                    var enchantmentMod = ResistMaskHelper.GetManaConversionMod(wo);

                    if (enchantmentMod != 1.0f)
                    {
                        PropertiesFloat[PropertyFloat.ManaConversionMod] *= enchantmentMod;

                        ResistHighlight = ResistMaskHelper.GetHighlightMask(wo);
                        ResistColor = ResistMaskHelper.GetColorMask(wo);
                    }
                }
                else if (!PropertyManager.GetBool("show_mana_conv_bonus_0").Item)
                {
                    PropertiesFloat.Remove(PropertyFloat.ManaConversionMod);
                }
            }

            if (PropertiesFloat.ContainsKey(PropertyFloat.ElementalDamageMod))
            {
                var enchantmentBonus = ResistMaskHelper.GetElementalDamageBonus(wo);

                if (enchantmentBonus != 0)
                {
                    PropertiesFloat[PropertyFloat.ElementalDamageMod] += enchantmentBonus;

                    ResistHighlight = ResistMaskHelper.GetHighlightMask(wo);
                    ResistColor = ResistMaskHelper.GetColorMask(wo);
                }
            }

            // LONG DESCRIPTION

            if (wo.NumTimesTinkered <= 0)
            {

                var appraisalLongDescDecoration = AppraisalLongDescDecorations.None;

                if (wo.ItemWorkmanship > 0)
                    appraisalLongDescDecoration |= AppraisalLongDescDecorations.PrependWorkmanship;
                if (wo.MaterialType > 0)
                    appraisalLongDescDecoration |= AppraisalLongDescDecorations.PrependMaterial;
                if (wo.GemType > 0 && wo.GemCount > 0)
                    appraisalLongDescDecoration |= wo.NumTimesTinkered > 0 ? 0 : AppraisalLongDescDecorations.AppendGemInfo;
                if (appraisalLongDescDecoration > 0 && wo.LongDesc != null && wo.LongDesc.StartsWith(wo.Name))
                    PropertiesInt[PropertyInt.AppraisalLongDescDecoration] = (int)appraisalLongDescDecoration;
                else
                    PropertiesInt.Remove(PropertyInt.AppraisalLongDescDecoration);
            }

            if (wo.NumTimesTinkered >= 1)
            {
                // for tinkered items, we need to replace the Decorations

                string prependMaterial = RecipeManager.GetMaterialName((MaterialType)wo.MaterialType);

                string prependWorkmanship = Salvage.WorkmanshipNames[(int)wo.ItemWorkmanship - 1];

                string modifiedGemType = RecipeManager.GetMaterialName(wo.GemType ?? MaterialType.Unknown);

                if (wo.GemCount > 1)
                    if ((int)wo.GemType == 26 || (int)wo.GemType == 37 || (int)wo.GemType == 40 || (int)wo.GemType == 46 || (int)wo.GemType == 49)
                        modifiedGemType += "es";
                    else if ((int)wo.GemType == 38)
                        modifiedGemType = "Rubies";
                    else
                        modifiedGemType += "s";

                // then we need to check the tinker log array, parse it and convert values to items to write to long desc

                string longDescAdditions;
                bool hasLongDescAdditions = false;

                if (wo.NumTimesTinkered > 0 && wo.TinkerLog != null)
                {
                    hasLongDescAdditions = true;

                    string[] tinkerLogArray = wo.TinkerLog.Split(',');

                    int[] tinkeringTypes = new int[80];

                    if (wo.GemCount != null && wo.GemCount >= 1)
                    {
                        longDescAdditions = $"{prependWorkmanship} {prependMaterial} {wo.Name}, set with {wo.GemCount} {modifiedGemType}\n\nThis item has been tinkered with:\n";
                    }
                    else
                        longDescAdditions = $"{prependWorkmanship} {prependMaterial} {wo.Name}\n\nThis item has been tinkered with:\n";

                    foreach (string s in tinkerLogArray)
                    {
                        if (int.TryParse(s, out int index))

                            if (index >= 0 && index < tinkeringTypes.Length)
                            {
                                tinkeringTypes[index] += 1;
                            }
                    }

                    int sumofTinksinLog = 0;

                    // cycle through the parsed tinkering log, adding Material Name and value stored in the element (num times that salvage has been applied)

                    for (int index = 0; index < tinkeringTypes.Length; index++)
                    {
                        int value = tinkeringTypes[index];
                        if (value > 0)
                        {
                            if (ACE.Entity.Enum.MaterialType.IsDefined(typeof(MaterialType), (MaterialType)index))
                            {
                                MaterialType materialType = (MaterialType)index;
                                longDescAdditions += $"\n \t    {RecipeManager.GetMaterialName(materialType)}:  {value}";
                            }
                            else
                            {
                                Console.WriteLine($"Unknown variable at index {index}: {value}");
                            }
                            sumofTinksinLog += value;

                        }

                    }

                    // check for failure on first tink, or string of failures with no success
                    if (sumofTinksinLog == 0 && wo.NumTimesTinkered >= 1)
                        longDescAdditions += $"\n\n \t    Failures:    {wo.NumTimesTinkered}";

                    // check for any failures whatsoever by comparing sumofTinksInLog to NumTimesTinkered
                    else
                    {
                        sumofTinksinLog -= wo.NumTimesTinkered;
                        if (sumofTinksinLog < 0)
                            longDescAdditions += $"\n\n \t    Failures:  {Math.Abs(sumofTinksinLog)}";
                    }

                    if (hasLongDescAdditions)
                    {
                        longDescAdditions += "";
                        PropertiesString[PropertyString.LongDesc] = longDescAdditions;
                    }
                }
                
            }


            // USE 

            string extraPropertiesText;
            if (PropertiesString.TryGetValue(PropertyString.Use, out var useText) && useText.Length > 0)
                extraPropertiesText = $"{useText}\n";
            else
                extraPropertiesText = "";
            bool hasExtraPropertiesText = false;

            // Protection Levels
            if (PropertiesInt.TryGetValue(PropertyInt.ArmorLevel, out var armorLevel) && armorLevel == 0 && wo.ArmorWeightClass == (int)ArmorWeightClass.Cloth)
            {
                var slashingMod = (float)wo.ArmorModVsSlash;
                var piercingMod = (float)wo.ArmorModVsPierce;
                var bludgeoningMod = (float)wo.ArmorModVsBludgeon;
                var fireMod = (float)wo.ArmorModVsFire;
                var coldMod = (float)wo.ArmorModVsCold;
                var acidMod = (float)wo.ArmorModVsAcid;
                var electricMod = (float)wo.ArmorModVsElectric;

                extraPropertiesText += $"Slashing: {GetProtectionLevelText(slashingMod)} ({string.Format("{0:0.00}", slashingMod)}) \n";
                extraPropertiesText += $"Piercing: {GetProtectionLevelText(piercingMod)} ({string.Format("{0:0.00}", piercingMod)}) \n";
                extraPropertiesText += $"Bludgeoning: {GetProtectionLevelText(bludgeoningMod)} ({string.Format("{0:0.00}", bludgeoningMod)}) \n";
                extraPropertiesText += $"Fire: {GetProtectionLevelText(fireMod)} ({string.Format("{0:0.00}", fireMod)}) \n";
                extraPropertiesText += $"Cold: {GetProtectionLevelText(coldMod)} ({string.Format("{0:0.00}", coldMod)}) \n";
                extraPropertiesText += $"Acid: {GetProtectionLevelText(acidMod)} ({string.Format("{0:0.00}", acidMod)}) \n";
                extraPropertiesText += $"Electric: {GetProtectionLevelText(electricMod)} ({string.Format("{0:0.00}", electricMod)}) \n\n";

                hasExtraPropertiesText = true;
            }

            // -------- WEAPON PROPERTIES --------
            // Aegis Rending
            if (PropertiesInt.TryGetValue(PropertyInt.ImbuedEffect, out var imbuedEffect) && imbuedEffect == 0x8000)
            {
                extraPropertiesText += $"Additional Properties: Aegis Rending\n";

                hasExtraPropertiesText = true;
            }
            // Ignore Armor
            if (PropertiesFloat.TryGetValue(PropertyFloat.IgnoreArmor, out var ignoreArmor) && ignoreArmor != 0)
            {
                var wielder = (Creature)wo.Wielder;
                extraPropertiesText += $"+{Math.Round((ignoreArmor * 100), 0)}% Armor Cleaving\n";

                hasExtraPropertiesText = true;
            }
            // Aegis Cleaving
            if (PropertiesFloat.TryGetValue(PropertyFloat.IgnoreAegis, out var ignoreAegis) && ignoreAegis != 0)
            {
                var wielder = (Creature)wo.Wielder;
                extraPropertiesText += $"+{Math.Round((ignoreAegis * 100), 0)}% Aegis Cleaving\n";

                hasExtraPropertiesText = true;
            }
            // Crit Multiplier
            if (PropertiesFloat.TryGetValue(PropertyFloat.CriticalMultiplier, out var critMultiplier) && critMultiplier > 1)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"+{Math.Round((critMultiplier - 1) * 100, 0)}% Critical Damage\n";

                hasExtraPropertiesText = true;
            }
            // Crit Chance
            if (PropertiesFloat.TryGetValue(PropertyFloat.CriticalFrequency, out var critFrequency) && critFrequency > 0.0f)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"+{Math.Round((critFrequency - 0.1) * 100, 1)}% Critical Chance\n";

                hasExtraPropertiesText = true;
            }
            // Stamina Reduction Mod
            if (PropertiesFloat.TryGetValue(PropertyFloat.StaminaCostReductionMod, out var staminaCostReductionMod) && staminaCostReductionMod > 0.001f)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"{Math.Round((staminaCostReductionMod - 0.1) * 100, 1)}% Stamina Cost Reduction\n";

                hasExtraPropertiesText = true;
            }
            // Spell Proc Rate
            if (PropertiesFloat.TryGetValue(PropertyFloat.ProcSpellRate, out var procSpellRate) && procSpellRate > 0.0f)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"Cast on strike chance: {Math.Round(procSpellRate * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }

            // -------- WEAPON ATTACK/DEFENSE MODS --------
            extraPropertiesText += "\n";
            // Attack Mod for Bows
            if (PropertiesFloat.TryGetValue(PropertyFloat.WeaponOffense, out var weaponOffense) && weaponOffense > 1.001)
            {
                var weaponMod = (weaponOffense - 1) * 100;
                if (wo.WeaponSkill == Skill.Bow || wo.WeaponSkill == Skill.Crossbow || wo.WeaponSkill == Skill.MissileWeapons)
                {
                    extraPropertiesText += $"Bonus to Attack Skill: +{Math.Round(weaponMod, 1)}%\n";
                }

                hasExtraPropertiesText = true;
            }
            // Weapon Mod - War Magic
            if (PropertiesFloat.TryGetValue(PropertyFloat.WeaponWarMagicMod, out var weaponWarMagicMod) && weaponWarMagicMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"Bonus to War Magic Skill: +{Math.Round((weaponWarMagicMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Weapon Mod - Life Magic
            if (PropertiesFloat.TryGetValue(PropertyFloat.WeaponLifeMagicMod, out var weaponLifeMagicMod) && weaponLifeMagicMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"Bonus to Life Magic Skill: +{Math.Round((weaponLifeMagicMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Weapon Physical Defense
            if (PropertiesFloat.TryGetValue(PropertyFloat.WeaponPhysicalDefense, out var weaponPhysicalDefense) && weaponPhysicalDefense > 1.001)
            {
                var weaponMod = (weaponPhysicalDefense + wo.EnchantmentManager.GetAdditiveMod(PropertyFloat.WeaponPhysicalDefense) - 1) * 100;
                extraPropertiesText += $"Bonus to Physical Defense: +{Math.Round(weaponMod, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Weapon Magic Defense
            if (PropertiesFloat.TryGetValue(PropertyFloat.WeaponMagicalDefense, out var weaponMagicalDefense) && weaponMagicalDefense > 1.001)
            {
                var weaponMod = (weaponMagicalDefense + wo.EnchantmentManager.GetAdditiveMod(PropertyFloat.WeaponPhysicalDefense) - 1) * 100;
                extraPropertiesText += $"Bonus to Magic Defense: +{Math.Round(weaponMod, 1)}%\n";

                hasExtraPropertiesText = true;
            }

            // Weapon Mod - Life Spell Restoration Mod
            if (PropertiesFloat.TryGetValue(PropertyFloat.WeaponRestorationSpellsMod, out var weaponLifeMagicVitalMod) && weaponLifeMagicVitalMod >= 1.001)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"Healing Bonus for Restoration Spells: +{Math.Round((weaponLifeMagicVitalMod - 1) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }

            // -- ARMOR --

            // Aegis Level
            if (PropertiesInt.TryGetValue(PropertyInt.AegisLevel, out var aegisLevel) && aegisLevel != 0)
            {
                var wielder = (Creature)wo.Wielder;
                if (wielder != null)
                {
                    var totalAegisLevel = wielder.GetAegisLevel();
                    extraPropertiesText += $"Aegis Level: {aegisLevel}  ({totalAegisLevel})\n";
                }
                else
                    extraPropertiesText += $"Aegis Level: {aegisLevel}\n\n";

                hasExtraPropertiesText = true;
            }

            // Armor Weight Class
            if (PropertiesInt.TryGetValue(PropertyInt.ArmorWeightClass, out var armorWieghtClass) && armorWieghtClass > 0)
            {
                var weightClassText = "";

                if (wo.ArmorWeightClass == (int)ArmorWeightClass.Cloth)
                {
                    weightClassText = "Cloth";
                }
                else if (wo.ArmorWeightClass == (int)ArmorWeightClass.Light)
                {
                    weightClassText = "Light";
                }
                else if (wo.ArmorWeightClass == (int)ArmorWeightClass.Heavy)
                {
                    weightClassText = "Heavy";
                }

                extraPropertiesText += $"Weight Class: {weightClassText}\n";

                hasExtraPropertiesText = true;
            }

            // Armor Penalty - Attack Resource
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorResourcePenalty, out var armoResourcePenalty) && armoResourcePenalty >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalArmorResourcePenalty = wielder.GetArmorResourcePenalty();
                    extraPropertiesText += $"Penalty to Stamina/Mana usage: {Math.Round((armoResourcePenalty) * 100, 1)}%  ({Math.Round((double)(totalArmorResourcePenalty * 100), 2)}%)\n";
                }
                else
                    extraPropertiesText += $"Penalty to Stamina/Mana usage: {Math.Round((armoResourcePenalty) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Jewelry Mod - Mana Conversion
            if (PropertiesFloat.TryGetValue(PropertyFloat.ManaConversionMod, out var manaConversionMod) && manaConversionMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wo.ItemType == ItemType.Jewelry || wo.ItemType == ItemType.Armor || wo.ItemType == ItemType.Clothing)
                {
                    extraPropertiesText += $"Bonus to Mana Conversion Skill: +{Math.Round(manaConversionMod * 100, 1)}%\n";
                }

                hasExtraPropertiesText = true;
            }
            // Armor Mod - War Magic
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorWarMagicMod, out var armorWarMagicMod) && armorWarMagicMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalWarMagicMod = wielder.GetArmorWarMagicMod();
                    extraPropertiesText += $"Bonus to War Magic Skill: +{Math.Round((armorWarMagicMod) * 100, 1)}%  ({Math.Round((double)(totalWarMagicMod * 100), 2)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to War Magic Skill: +{Math.Round((armorWarMagicMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Life Magic
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorLifeMagicMod, out var armorLifeMagicMod) && armorLifeMagicMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalLifeMagicMod = wielder.GetArmorLifeMagicMod();
                    extraPropertiesText += $"Bonus to Life Magic Skill: +{Math.Round((armorLifeMagicMod) * 100, 1)}%  ({Math.Round((double)totalLifeMagicMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Life Magic Skill: +{Math.Round((armorLifeMagicMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Attack Skill
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorAttackMod, out var armorAttackMod) && armorAttackMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalAttackMod = wielder.GetArmorAttackMod();
                    extraPropertiesText += $"Bonus to Attack Skill: +{Math.Round((armorAttackMod) * 100, 1)}%  ({Math.Round((double)totalAttackMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Attack Skill: +{Math.Round((armorAttackMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Physical Def
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorPhysicalDefMod, out var armorPhysicalDefMod) && armorPhysicalDefMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalPhysicalDefMod = wielder.GetArmorPhysicalDefMod();
                    extraPropertiesText += $"Bonus to Physical Defense: +{Math.Round((armorPhysicalDefMod) * 100, 1)}%  ({Math.Round((double)totalPhysicalDefMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Physical Defense: +{Math.Round((armorPhysicalDefMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Magic Def
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorMagicDefMod, out var armorMagicDefenseMod) && armorMagicDefenseMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalMagicDefMod = wielder.GetArmorMagicDefMod();
                    extraPropertiesText += $"Bonus to Magic Defense: +{Math.Round((armorMagicDefenseMod) * 100, 1)}%  ({Math.Round((double)totalMagicDefMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Magic Defense: +{Math.Round((armorMagicDefenseMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Dual Wield
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorDualWieldMod, out var armorDualWieldMod) && armorDualWieldMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalDualWieldMod = wielder.GetArmorDualWieldMod();
                    extraPropertiesText += $"Bonus to Dual Wield Skill: +{Math.Round((armorDualWieldMod) * 100, 1)}%  ({Math.Round((double)totalDualWieldMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Dual Wield Skill: +{Math.Round((armorDualWieldMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Two-handed Combat
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorTwohandedCombatMod, out var armorTwonandedCombatMod) && armorTwonandedCombatMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalTwohandedCombatMod = wielder.GetArmorDualWieldMod();
                    extraPropertiesText += $"Bonus to Two-handed Combat Skill: +{Math.Round((armorTwonandedCombatMod) * 100, 1)}%  ({Math.Round((double)totalTwohandedCombatMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Two-handed Combat Skill: +{Math.Round((armorTwonandedCombatMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Run
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorRunMod, out var armorRunMod) && armorRunMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalRunMod = wielder.GetArmorRunMod();
                    extraPropertiesText += $"Bonus to Run Skill: +{Math.Round((armorRunMod) * 100, 1)}%  ({Math.Round((double)totalRunMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Run Skill: +{Math.Round((armorRunMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Thievery
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorThieveryMod, out var armorThieveryMod) && armorThieveryMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalThieveryMod = wielder.GetArmorThieveryMod();
                    extraPropertiesText += $"Bonus to Thievery Skill: +{Math.Round((armorThieveryMod) * 100, 1)}%  ({Math.Round((double)totalThieveryMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Thievery Skill: +{Math.Round((armorThieveryMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Shield
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorShieldMod, out var armorShieldMod) && armorShieldMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalShieldMod = wielder.GetArmorShieldMod();
                    extraPropertiesText += $"Bonus to Shield Skill: +{Math.Round((armorShieldMod) * 100, 1)}%  ({Math.Round((double)totalShieldMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Shield Skill: +{Math.Round((armorShieldMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Perception
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorPerceptionMod, out var armorAssessMod) && armorAssessMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalAssessMod = wielder.GetArmorAssessMod();
                    extraPropertiesText += $"Bonus to Perception Skill: +{Math.Round((armorAssessMod) * 100, 1)}%  ({Math.Round((double)totalAssessMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Perception Skill: +{Math.Round((armorAssessMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Deception
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorDeceptionMod, out var armorDeceptionMod) && armorDeceptionMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalDecptionMod = wielder.GetArmorDeceptionMod();
                    extraPropertiesText += $"Bonus to Deception Skill: +{Math.Round((armorDeceptionMod) * 100, 1)}%  ({Math.Round((double)totalDecptionMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Deception Skill: +{Math.Round((armorDeceptionMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Max Health
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorHealthMod, out var armorHealthMod) && armorHealthMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalHealthMod = wielder.GetArmorHealthMod();
                    extraPropertiesText += $"Bonus to Maximum Health: +{Math.Round((armorHealthMod) * 100, 1)}%  ({Math.Round((double)totalHealthMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Maximum Health: +{Math.Round((armorHealthMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Health Regen
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorHealthRegenMod, out var armorHealthRegenMod) && armorHealthRegenMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalHealthRegenMod = wielder.GetArmorHealthRegenMod();
                    extraPropertiesText += $"Bonus to Health Regen: +{Math.Round((armorHealthRegenMod) * 100, 1)}%  ({Math.Round((double)totalHealthRegenMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Health Regen: +{Math.Round((armorHealthRegenMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Max Stamina
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorStaminaMod, out var armorStaminaMod) && armorStaminaMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalStaminaMod = wielder.GetArmorStaminaMod();
                    extraPropertiesText += $"Bonus to Maximum Stamina: +{Math.Round((armorStaminaMod) * 100, 1)}%  ({Math.Round((double)totalStaminaMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Maximum Stamina: +{Math.Round((armorStaminaMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Stamina Regen
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorStaminaRegenMod, out var armorStaminaRegenMod) && armorStaminaRegenMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalStaminaRegenMod = wielder.GetArmorStaminaRegenMod();
                    extraPropertiesText += $"Bonus to Stamina Regen: +{Math.Round(armorStaminaRegenMod * 100, 1)}%  ({Math.Round((double)totalStaminaRegenMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Stamina Regen: +{Math.Round((armorStaminaRegenMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Mana
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorManaMod, out var armorManaMod) && armorManaMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalManaMod = wielder.GetArmorManaMod();
                    extraPropertiesText += $"Bonus to Maximum Mana: +{Math.Round((armorManaMod) * 100, 1)}%  ({Math.Round((double)totalManaMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Maximum Mana: +{Math.Round((armorManaMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }
            // Armor Mod - Mana Regen
            if (PropertiesFloat.TryGetValue(PropertyFloat.ArmorManaRegenMod, out var armorManaRegenMod) && armorManaRegenMod >= 0.001)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var totalManaRegenMod = wielder.GetArmorManaRegenMod();
                    extraPropertiesText += $"Bonus to Mana Regen: +{Math.Round((armorManaRegenMod) * 100, 1)}%  ({Math.Round((double)totalManaRegenMod * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Bonus to Mana Regen: +{Math.Round((armorManaRegenMod) * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }

            // Damage Penalty
            if (PropertiesInt.TryGetValue(PropertyInt.DamageRating, out var damageRating) && damageRating < 0)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"Damage Penalty: {damageRating}%\n";

                hasExtraPropertiesText = true;
            }

            // ----- JEWELCRAFTING ------
             if (PropertiesString.TryGetValue(PropertyString.JewelSocket1, out string jewelSocket1))
            {
                string[] parts = jewelSocket1.Split('/');

                hasExtraPropertiesText = true;

                if (wo.WeenieType == WeenieType.Jewel)
                {
                    extraPropertiesText += Jewel.GetJewelDescription(jewelSocket1);
                    extraPropertiesText += $"Once socketed into an item, this jewel becomes permanently attuned to your character. Items with contained jewels become attuned and will remain so until all jewels are removed.\n\nJewels may be unsocketed using an Intricate Carving Tool. There is no skill check or destruction chance.";

                }
                if (wo.WeenieType != WeenieType.Jewel)
                {
                    if (jewelSocket1.StartsWith("Empty"))
                        extraPropertiesText += "\n\t  Empty Jewel Socket\n";

                    else
                    {
                        extraPropertiesText += Jewel.GetSocketDescription(jewelSocket1);
                    }
                }
            }

            if (PropertiesString.TryGetValue(PropertyString.JewelSocket2, out string jewelSocket2))
            {
                string[] parts = jewelSocket2.Split('/');

                hasExtraPropertiesText = true;

                if (wo.WeenieType != WeenieType.Jewel)
                {
                    if (jewelSocket2.StartsWith("Empty"))
                        extraPropertiesText += "\n\t  Empty Jewel Socket\n";

                    else
                    {
                        extraPropertiesText += Jewel.GetSocketDescription(jewelSocket2);
                    }
                }
            }

            // -------- SALVAGE BAGS ---

            if (PropertiesInt.TryGetValue(PropertyInt.Structure, out var structure) && structure >= 0)
            {
                if (wo.WeenieType == WeenieType.Salvage)
                {
                    extraPropertiesText += $"\nThis bag contains {structure} units of salvage.\n";
                    hasExtraPropertiesText = true;
                }
            }
            // -------- EMPOWERED SCARABS --------

            // Max Level
            if (PropertiesInt.TryGetValue(PropertyInt.EmpoweredScarabMaxLevel, out var manaScarabMaxLevel) && manaScarabMaxLevel > 0)
            {
                var wielder = (Creature)wo.Wielder;

                extraPropertiesText += $"\nMax Spell Level: {manaScarabMaxLevel}\n";

                hasExtraPropertiesText = true;
            }

            // Proc Chance
            if (PropertiesFloat.TryGetValue(PropertyFloat.EmpoweredScarabTriggerChance, out var manaScarabTriggerChance) && manaScarabTriggerChance > 0.01)
            {
                extraPropertiesText += $"Proc Chance: {Math.Round((double)manaScarabTriggerChance * 100, 0)}%\n";

                hasExtraPropertiesText = true;
            }

            // Frequency
            if (PropertiesFloat.TryGetValue(PropertyFloat.CooldownDuration, out var cooldownDuration) && cooldownDuration > 0.01)
            {
                if(wo.WeenieType == WeenieType.EmpoweredScarab)
                    extraPropertiesText += $"Cooldown: {Math.Round((double)cooldownDuration, 1)} seconds\n";

                hasExtraPropertiesText = true;
            }

            // Max Level
            if (PropertiesInt.TryGetValue(PropertyInt.MaxStructure, out var maxStructure) && maxStructure > 0)
            {
                if (wo.WeenieType != WeenieType.Salvage)
                    extraPropertiesText += $"Max Number of Uses: {maxStructure}\n";

                hasExtraPropertiesText = true;
            }

            // Intensity
            if (PropertiesFloat.TryGetValue(PropertyFloat.EmpoweredScarabIntensity, out var manaScarabIntensity) && manaScarabIntensity > 0.01)
            {
                extraPropertiesText += $"Bonus Intensity: {Math.Round((double)manaScarabIntensity * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }

            // Mana Reduction
            if (PropertiesFloat.TryGetValue(PropertyFloat.EmpoweredScarabReductionAmount, out var manaScarabReductionAmount) && manaScarabReductionAmount > 0.01)
            {
                extraPropertiesText += $"Mana Cost Reduction: {Math.Round((double)manaScarabReductionAmount * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }

            // Reserved Mana
            if (PropertiesFloat.TryGetValue(PropertyFloat.EmpoweredScarabManaReserved, out var manaScarabManaReserved) && manaScarabManaReserved > 0)
            {
                var wielder = (Creature)wo.Wielder;

                if (wielder != null)
                {
                    var equippedManaScarabs = wielder.GetEquippedEmpoweredScarabs();
                    var totalReservedMana = 0.0;

                    foreach (EmpoweredScarab manaScarab in equippedManaScarabs)
                        totalReservedMana += manaScarab.EmpoweredScarabManaReserved ?? 0;

                    extraPropertiesText += $"Mana Reservation: {Math.Round(manaScarabManaReserved * 100, 1)}% ({Math.Round(totalReservedMana * 100, 1)}%)\n";
                }
                else
                    extraPropertiesText += $"Mana Reservation: {Math.Round(manaScarabManaReserved * 100, 1)}%\n";

                hasExtraPropertiesText = true;
            }

            if (hasExtraPropertiesText)
            {
                extraPropertiesText += "";
                PropertiesString[PropertyString.Use] = extraPropertiesText;
            }
        }

        private string GetProtectionLevelText(float protectionMod)
        {
            switch(protectionMod)
            {
                case <= 0.39f: return "Poor";
                case <= 0.79f: return "Below Average";
                case <= 1.19f: return "Average";
                case <= 1.59f: return "Above Average";
                default: return "Unparalleled";
            }
        }

        private void BuildSpells(WorldObject wo)
        {
            SpellBook = new List<uint>();

            if (wo is Creature)
                return;

            // add primary spell, if exists
            if (wo.SpellDID.HasValue)
                SpellBook.Add(wo.SpellDID.Value);

            // add proc spell, if exists
            if (wo.ProcSpell.HasValue)
                SpellBook.Add(wo.ProcSpell.Value);

            var woSpellDID = wo.SpellDID;   // prevent recursive lock
            var woProcSpell = wo.ProcSpell;

            foreach (var spellId in wo.Biota.GetKnownSpellsIdsWhere(i => i != woSpellDID && i != woProcSpell, wo.BiotaDatabaseLock))
                SpellBook.Add((uint)spellId);
        }

        private void AddEnchantments(WorldObject wo)
        {
            if (wo == null) return;

            // get all currently active item enchantments on the item
            var woEnchantments = wo.EnchantmentManager.GetEnchantments(MagicSchool.PortalMagic);

            foreach (var enchantment in woEnchantments)
                SpellBook.Add((uint)enchantment.SpellId | EnchantmentMask);

            // show auras from wielder, if applicable

            // this technically wasn't a feature in retail

            if (wo.Wielder != null && wo.IsEnchantable && wo.WeenieType != WeenieType.Clothing && !wo.IsShield && PropertyManager.GetBool("show_aura_buff").Item)
            {
                // get all currently active item enchantment auras on the player
                var wielderEnchantments = wo.Wielder.EnchantmentManager.GetEnchantments(MagicSchool.PortalMagic);

                // Only show reflected Auras from player appropriate for wielded weapons
                foreach (var enchantment in wielderEnchantments)
                {
                    if (wo is Caster)
                    {
                        // Caster weapon only item Auras
                        if ((enchantment.SpellCategory == SpellCategory.DefenseModRaising)
                            || (enchantment.SpellCategory == SpellCategory.DefenseModRaisingRare)
                            || (enchantment.SpellCategory == SpellCategory.ManaConversionModRaising)
                            || (enchantment.SpellCategory == SpellCategory.SpellDamageRaising))
                        {
                            SpellBook.Add((uint)enchantment.SpellId | EnchantmentMask);
                        }
                    }
                    else if (wo is Missile || wo is Ammunition)
                    {
                        if ((enchantment.SpellCategory == SpellCategory.DamageRaising)
                            || (enchantment.SpellCategory == SpellCategory.DamageRaisingRare))
                        {
                            SpellBook.Add((uint)enchantment.SpellId | EnchantmentMask);
                        }
                    }
                    else
                    {
                        // Other weapon type Auras
                        if ((enchantment.SpellCategory == SpellCategory.AttackModRaising)
                            || (enchantment.SpellCategory == SpellCategory.AttackModRaisingRare)
                            || (enchantment.SpellCategory == SpellCategory.DamageRaising)
                            || (enchantment.SpellCategory == SpellCategory.DamageRaisingRare)
                            || (enchantment.SpellCategory == SpellCategory.DefenseModRaising)
                            || (enchantment.SpellCategory == SpellCategory.DefenseModRaisingRare)
                            || (enchantment.SpellCategory == SpellCategory.WeaponTimeRaising)
                            || (enchantment.SpellCategory == SpellCategory.WeaponTimeRaisingRare))
                        {
                            SpellBook.Add((uint)enchantment.SpellId | EnchantmentMask);
                        }
                    }
                }
            }
        }

        private void BuildArmor(WorldObject wo)
        {
            if (!Success)
                return;

            ArmorProfile = new ArmorProfile(wo);
            ArmorHighlight = ArmorMaskHelper.GetHighlightMask(wo, IsArmorCapped || IsArmorBuffed);
            ArmorColor = ArmorMaskHelper.GetColorMask(wo, IsArmorBuffed);

            AddEnchantments(wo);
        }

        private void BuildCreature(Creature creature)
        {
            CreatureProfile = new CreatureProfile(creature, Success);

            // only creatures?
            ResistHighlight = ResistMaskHelper.GetHighlightMask(creature);
            ResistColor = ResistMaskHelper.GetColorMask(creature);

	        if (Success && (creature is Player || !creature.Attackable))
	            ArmorLevels = new ArmorLevel(creature);
	
	        AddRatings(creature);

            if (NPCLooksLikeObject)
            {
                var weenie = creature.Weenie ?? DatabaseManager.World.GetCachedWeenie(creature.WeenieClassId);

                if (!weenie.GetProperty(PropertyInt.EncumbranceVal).HasValue)
                    PropertiesInt.Remove(PropertyInt.EncumbranceVal);
            }
            else
                PropertiesInt.Remove(PropertyInt.EncumbranceVal);

            // see notes in CombatPet.Init()
            if (creature is CombatPet && PropertiesInt.ContainsKey(PropertyInt.Faction1Bits))
                PropertiesInt.Remove(PropertyInt.Faction1Bits);
        }

        private void AddRatings(Creature creature)
        {
            if (!Success)
                return;

            var damageRating = creature.GetDamageRating();

            // include heritage / weapon type rating?
            var weapon = creature.GetEquippedWeapon() ?? creature.GetEquippedWand();
            if (creature.GetHeritageBonus(weapon))
                damageRating += 5;

            // factor in weakness here?

            var damageResistRating = creature.GetDamageResistRating();

            // factor in nether dot damage here?

            var critRating = creature.GetCritRating();
            var critDamageRating = creature.GetCritDamageRating();

            var critResistRating = creature.GetCritResistRating();
            var critDamageResistRating = creature.GetCritDamageResistRating();

            var healingBoostRating = creature.GetHealingBoostRating();
            var dotResistRating = creature.GetDotResistanceRating();
            var netherResistRating = creature.GetNetherResistRating();

            var lifeResistRating = creature.GetLifeResistRating();  // drain / harm resistance
            var gearMaxHealth = creature.GetGearMaxHealth();

            var pkDamageRating = creature.GetPKDamageRating();
            var pkDamageResistRating = creature.GetPKDamageResistRating();

            if (damageRating != 0)
                PropertiesInt[PropertyInt.DamageRating] = damageRating;
            if (damageResistRating != 0)
                PropertiesInt[PropertyInt.DamageResistRating] = damageResistRating;

            if (critRating != 0)
                PropertiesInt[PropertyInt.CritRating] = critRating;
            if (critDamageRating != 0)
                PropertiesInt[PropertyInt.CritDamageRating] = critDamageRating;

            if (critResistRating != 0)
                PropertiesInt[PropertyInt.CritResistRating] = critResistRating;
            if (critDamageResistRating != 0)
                PropertiesInt[PropertyInt.CritDamageResistRating] = critDamageResistRating;

            if (healingBoostRating != 0)
                PropertiesInt[PropertyInt.HealingBoostRating] = healingBoostRating;
            if (netherResistRating != 0)
                PropertiesInt[PropertyInt.NetherResistRating] = netherResistRating;
            if (dotResistRating != 0)
                PropertiesInt[PropertyInt.DotResistRating] = dotResistRating;

            if (lifeResistRating != 0)
                PropertiesInt[PropertyInt.LifeResistRating] = lifeResistRating;
            if (gearMaxHealth != 0)
                PropertiesInt[PropertyInt.GearMaxHealth] = gearMaxHealth;

            if (pkDamageRating != 0)
                PropertiesInt[PropertyInt.PKDamageRating] = pkDamageRating;
            if (pkDamageResistRating != 0)
                PropertiesInt[PropertyInt.PKDamageResistRating] = pkDamageResistRating;

            // add ratings from equipped items?
        }

        private void BuildWeapon(WorldObject weapon)
        {
            if (!Success)
                return;

            var weaponProfile = new WeaponProfile(weapon);

            //WeaponHighlight = WeaponMaskHelper.GetHighlightMask(weapon, wielder);
            //WeaponColor = WeaponMaskHelper.GetColorMask(weapon, wielder);
            WeaponHighlight = WeaponMaskHelper.GetHighlightMask(weaponProfile, IsAttackModBuffed);
            WeaponColor = WeaponMaskHelper.GetColorMask(weaponProfile, IsAttackModBuffed);


            if (!(weapon is Caster))
                WeaponProfile = weaponProfile;

            // item enchantments can also be on wielder currently
            AddEnchantments(weapon);
        }

        private void BuildHookProfile(WorldObject hookedItem)
        {
            HookProfile = new HookProfile();
            if (hookedItem.Inscribable)
                HookProfile.Flags |= HookFlags.Inscribable;
            if (hookedItem is Healer)
                HookProfile.Flags |= HookFlags.IsHealer;
            if (hookedItem is Food)
                HookProfile.Flags |= HookFlags.IsFood;
            if (hookedItem is Lockpick)
                HookProfile.Flags |= HookFlags.IsLockpick;
            if (hookedItem.ValidLocations != null)
                HookProfile.ValidLocations = hookedItem.ValidLocations.Value;
            if (hookedItem.AmmoType != null)
                HookProfile.AmmoType = hookedItem.AmmoType.Value;
        }

        /// <summary>
        /// Constructs the bitflags for appraising a WorldObject
        /// </summary>
        private void BuildFlags()
        {
            if (PropertiesInt.Count > 0)
                Flags |= IdentifyResponseFlags.IntStatsTable;
            if (PropertiesInt64.Count > 0)
                Flags |= IdentifyResponseFlags.Int64StatsTable;         				
			if (PropertiesBool.Count > 0)
                Flags |= IdentifyResponseFlags.BoolStatsTable;
            if (PropertiesFloat.Count > 0)
                Flags |= IdentifyResponseFlags.FloatStatsTable;
            if (PropertiesString.Count > 0)
                Flags |= IdentifyResponseFlags.StringStatsTable;
            if (PropertiesDID.Count > 0)
                Flags |= IdentifyResponseFlags.DidStatsTable;
            if (SpellBook.Count > 0)
                Flags |= IdentifyResponseFlags.SpellBook;

            if (ResistHighlight != 0)
                Flags |= IdentifyResponseFlags.ResistEnchantmentBitfield;
            if (ArmorProfile != null)
                Flags |= IdentifyResponseFlags.ArmorProfile;
            if (CreatureProfile != null && !NPCLooksLikeObject)
                Flags |= IdentifyResponseFlags.CreatureProfile;
            if (WeaponProfile != null)
                Flags |= IdentifyResponseFlags.WeaponProfile;
            if (HookProfile != null)
                Flags |= IdentifyResponseFlags.HookProfile;
            if (ArmorHighlight != 0)
                Flags |= IdentifyResponseFlags.ArmorEnchantmentBitfield;
            if (WeaponHighlight != 0)
                Flags |= IdentifyResponseFlags.WeaponEnchantmentBitfield;
            if (ArmorLevels != null)
                Flags |= IdentifyResponseFlags.ArmorLevels;
        }
    }

    public static class AppraiseInfoExtensions
    {
        /// <summary>
        /// Writes the AppraiseInfo to the network stream
        /// </summary>
        public static void Write(this BinaryWriter writer, AppraiseInfo info)
        {
            writer.Write((uint)info.Flags);
            writer.Write(Convert.ToUInt32(info.Success));
            if (info.Flags.HasFlag(IdentifyResponseFlags.IntStatsTable))
                writer.Write(info.PropertiesInt);
            if (info.Flags.HasFlag(IdentifyResponseFlags.Int64StatsTable))
                writer.Write(info.PropertiesInt64);
            if (info.Flags.HasFlag(IdentifyResponseFlags.BoolStatsTable))
                writer.Write(info.PropertiesBool);
            if (info.Flags.HasFlag(IdentifyResponseFlags.FloatStatsTable))
                writer.Write(info.PropertiesFloat);
            if (info.Flags.HasFlag(IdentifyResponseFlags.StringStatsTable))
                writer.Write(info.PropertiesString);
            if (info.Flags.HasFlag(IdentifyResponseFlags.DidStatsTable))
                writer.Write(info.PropertiesDID);
            if (info.Flags.HasFlag(IdentifyResponseFlags.SpellBook))
                writer.Write(info.SpellBook);
            if (info.Flags.HasFlag(IdentifyResponseFlags.ArmorProfile))
                writer.Write(info.ArmorProfile);
            if (info.Flags.HasFlag(IdentifyResponseFlags.CreatureProfile))
                writer.Write(info.CreatureProfile);
            if (info.Flags.HasFlag(IdentifyResponseFlags.WeaponProfile))
                writer.Write(info.WeaponProfile);
            if (info.Flags.HasFlag(IdentifyResponseFlags.HookProfile))
                writer.Write(info.HookProfile);
            if (info.Flags.HasFlag(IdentifyResponseFlags.ArmorEnchantmentBitfield))
            {
                writer.Write((ushort)info.ArmorHighlight);
                writer.Write((ushort)info.ArmorColor);
            }
            if (info.Flags.HasFlag(IdentifyResponseFlags.WeaponEnchantmentBitfield))
            {
                writer.Write((ushort)info.WeaponHighlight);
                writer.Write((ushort)info.WeaponColor);
            }
            if (info.Flags.HasFlag(IdentifyResponseFlags.ResistEnchantmentBitfield))
            {
                writer.Write((ushort)info.ResistHighlight);
                writer.Write((ushort)info.ResistColor);
            }
            if (info.Flags.HasFlag(IdentifyResponseFlags.ArmorLevels))
                writer.Write(info.ArmorLevels);
        }

        private static readonly PropertyIntComparer PropertyIntComparer = new PropertyIntComparer(16);
        private static readonly PropertyInt64Comparer PropertyInt64Comparer = new PropertyInt64Comparer(8);
        private static readonly PropertyBoolComparer PropertyBoolComparer = new PropertyBoolComparer(8);
        private static readonly PropertyFloatComparer PropertyFloatComparer = new PropertyFloatComparer(8);
        private static readonly PropertyStringComparer PropertyStringComparer = new PropertyStringComparer(8);
        private static readonly PropertyDataIdComparer PropertyDataIdComparer = new PropertyDataIdComparer(8);

        // TODO: generics
        public static void Write(this BinaryWriter writer, Dictionary<PropertyInt, int> _properties)
        {
            PackableHashTable.WriteHeader(writer, _properties.Count, PropertyIntComparer.NumBuckets);

            var properties = new SortedDictionary<PropertyInt, int>(_properties, PropertyIntComparer);

            foreach (var kvp in properties)
            {
                writer.Write((uint)kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public static void Write(this BinaryWriter writer, Dictionary<PropertyInt64, long> _properties)
        {
            PackableHashTable.WriteHeader(writer, _properties.Count, PropertyInt64Comparer.NumBuckets);

            var properties = new SortedDictionary<PropertyInt64, long>(_properties, PropertyInt64Comparer);

            foreach (var kvp in properties)
            {
                writer.Write((uint)kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public static void Write(this BinaryWriter writer, Dictionary<PropertyBool, bool> _properties)
        {
            PackableHashTable.WriteHeader(writer, _properties.Count, PropertyBoolComparer.NumBuckets);

            var properties = new SortedDictionary<PropertyBool, bool>(_properties, PropertyBoolComparer);

            foreach (var kvp in properties)
            {
                writer.Write((uint)kvp.Key);
                writer.Write(Convert.ToUInt32(kvp.Value));
            }
        }

        public static void Write(this BinaryWriter writer, Dictionary<PropertyFloat, double> _properties)
        {
            PackableHashTable.WriteHeader(writer, _properties.Count, PropertyFloatComparer.NumBuckets);

            var properties = new SortedDictionary<PropertyFloat, double>(_properties, PropertyFloatComparer);

            foreach (var kvp in properties)
            {
                writer.Write((uint)kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public static void Write(this BinaryWriter writer, Dictionary<PropertyString, string> _properties)
        {
            PackableHashTable.WriteHeader(writer, _properties.Count, PropertyStringComparer.NumBuckets);

            var properties = new SortedDictionary<PropertyString, string>(_properties, PropertyStringComparer);

            foreach (var kvp in properties)
            {
                writer.Write((uint)kvp.Key);
                writer.WriteString16L(kvp.Value);
            }
        }

        public static void Write(this BinaryWriter writer, Dictionary<PropertyDataId, uint> _properties)
        {
            PackableHashTable.WriteHeader(writer, _properties.Count, PropertyDataIdComparer.NumBuckets);

            var properties = new SortedDictionary<PropertyDataId, uint>(_properties, PropertyDataIdComparer);

            foreach (var kvp in properties)
            {
                writer.Write((uint)kvp.Key);
                writer.Write(kvp.Value);
            }
        }
    }
}
