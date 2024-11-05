using System;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyBeneficialEnchantments
{
    [CommandHandler(
        "verify-beneficial-enchantments",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies enchantment registry has correct StatModType for Beneficial spells and optionally fixes"
    )]
    public static void HandleEnchantments(Session session, params string[] parameters)
    {
        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        using (var ctx = new ShardDbContext())
        {
            ctx.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            var numMissingBeneficialFlag = 0;
            var numValid = 0;

            foreach (var enchantment in ctx.BiotaPropertiesEnchantmentRegistry)
            {
                if (enchantment.StatModType == 0)
                {
                    //numValid++;
                    continue;
                }

                var spell = new Entity.Spell(enchantment.SpellId);

                if (spell != null)
                {
                    var statModType = (EnchantmentTypeFlags)enchantment.StatModType;

                    if (spell.IsBeneficial && !statModType.HasFlag(EnchantmentTypeFlags.Beneficial))
                    {
                        foundIssues = true;

                        numMissingBeneficialFlag++;

                        if (fix)
                        {
                            statModType |= EnchantmentTypeFlags.Beneficial;
                            enchantment.StatModType = (uint)statModType;
                        }

                        Console.WriteLine(
                            $"Spell {spell.Name} ({spell.Id}) on 0x{enchantment.ObjectId:X8} is missing Beneficial flag{fixStr}"
                        );
                    }
                    else
                    {
                        numValid++;
                    }
                }
            }

            if (!fix && foundIssues)
            {
                Console.WriteLine(
                    $"Dry run completed. Type 'verify-beneficial-enchantments fix' to fix {numMissingBeneficialFlag:N0} issues."
                );
            }

            if (fix)
            {
                ctx.SaveChanges();
                Console.WriteLine($"Fixed {numMissingBeneficialFlag:N0} incorrect enchantments");
            }

            if (!foundIssues)
            {
                Console.WriteLine($"Verified {ctx.BiotaPropertiesEnchantmentRegistry.Count():N0} enchantments");
            }
        }
    }
}
