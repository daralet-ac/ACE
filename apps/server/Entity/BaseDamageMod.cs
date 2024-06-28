using System;

using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity
{
    public class BaseDamageMod
    {
        public BaseDamage BaseDamage;

        public float DamageBonus   = 0.0f;    // blood drinker
        public float DamageMod     = 1.0f;    // for missile launchers (+113% yumis = 2.13)
        public float VarianceMod   = 1.0f;

        public int ElementalBonus = 0;

        public float MaxDamage
        {
            get
            {
                var maxDamage = (BaseDamage.MaxDamage + DamageBonus + ElementalBonus) * DamageMod;

                if (BaseDamage.MaxDamage >= 0)
                    maxDamage = Math.Max(0, maxDamage);
                else
                    maxDamage = Math.Min(0, maxDamage);

                return maxDamage;   
            }
        }

        public float MinDamage => MaxDamage * (1.0f - BaseDamage.Variance * VarianceMod);

        public Range Range => new Range(MinDamage, MaxDamage);

        public BaseDamageMod(BaseDamage baseDamage)
        {
            BaseDamage = baseDamage;
        }

        public BaseDamageMod(BaseDamage baseDamage, Creature wielder, WorldObject weapon)
        {
            BaseDamage = baseDamage;

            if (weapon == null)
                return;

            if (weapon.IsAmmoLauncher)
                BaseDamage.MaxDamage = (int)Math.Round(BaseDamage.MaxDamage * weapon.EnchantmentManager.GetDamageBonus());

            DamageBonus *= weapon.EnchantmentManager.GetDamageBonus();
            //VarianceMod *= weapon.EnchantmentManager.GetVarianceMod();

            //DamageMod = (float)(weapon.GetProperty(PropertyFloat.DamageMod) ?? 1.0f) + (((float)(weapon.GetProperty(PropertyFloat.DamageMod) ?? 1.0f) - 1.0f) * (weapon.EnchantmentManager.GetDamageMod() - 1.0f));
            DamageMod = (float)(weapon.GetProperty(PropertyFloat.DamageMod) ?? 1.0f);

            if (weapon.IsEnchantable)
            {
                if (weapon.IsAmmoLauncher)
                    BaseDamage.MaxDamage = (int)Math.Round(BaseDamage.MaxDamage * wielder.EnchantmentManager.GetDamageBonus());

                // factor in wielder auras for enchantable weapons
                DamageBonus *= wielder.EnchantmentManager.GetDamageBonus();
                //VarianceMod *= wielder.EnchantmentManager.GetVarianceMod();

                //DamageMod += (DamageMod - 1.0f) * (wielder.EnchantmentManager.GetDamageMod() - 1.0f);;
            }
        }
    }
}
