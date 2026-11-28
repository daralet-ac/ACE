# Leatherworking and Weaving (the Tailoring split)

Tailoring is now two trade skills.

| Skill | ID | Was | Tinkers | Crafts |
|---|---|---|---|---|
| Leatherworking | 29 | Tailoring (retail Armor Tinkering) | Light armor, with hides (Leather, Armoredillo, Gromnie, Reedshark) | Armor Patches from leather |
| Weaving | 26 | unused (retail Arms and Armor Repair) | Cloth armor that has armor, with fabrics (Linen, Satin, Silk, Velvet, Wool) | Woven Bandages from fur, for healing kits |

Both the material and the armor have to match: hides only go on Light armor (Leatherworking), fabrics only go on Cloth armor (Weaving). Imbues on armor use Leatherworking for Light and Weaving for Cloth (Heavy is still Blacksmithing).

## Existing characters

Skill 29 keeps its ID, so everyone with Tailoring now has Leatherworking at the same level, along with their augmentation, cantrips and gems. On their first login after this change (`PropertyBool.TailoringSplitMigrated`, 188), a character with Tailoring Trained or Specialized also gets Weaving at the same advancement class and experience. Their `TradeSkill` quest count goes up by one, capped at 2.

The Armor Tinkering augmentation now specializes both skills.

The trade skill limit stays at 2. A former Tailor who also had a second trade skill keeps all three, as a grandfathered case.

## Hideworking chain

All WCIDs are in the ID Tracking sheets: Items > Animal Parts, Items > Skill and Attribute Redistribution, Creatures > NPCs, and Recipes > Daralet Custom.

A hide goes one way or the other, as it would for a real tanner: scraped bare and tanned into leather, or tanned with the hair left on as a fur. You can't get both from one hide.

- The trainers hand out the tools, the way Chef Martel hands out the campfire: the **Tanning Kit** (1052552) comes from the Master Leatherworker and the **Furrier's Kit** (1052565) from the Master Weaver. You get one when you train the skill. If you're already trained and don't have one, using the master gives you another. Both kits are bonded and worth nothing. Leather Traders (1010070-73) no longer sell Armor Patches.
- **Tanning Kit on a Beast Hide** (recipes 10012-10015, no skill): Tattered / Beast / Sturdy / Rugged hide → Rough / Cured / Supple / Fine Leather (1052553-56).
- **Furrier's Kit on a Beast Hide** (recipes 10028-10031, no skill): Tattered / Beast / Sturdy / Rugged hide → Coarse / normal / Thick / Lustrous Fur (1052557-60).
- **Leather on the Tanning Kit** (10016-10019, Leatherworking, difficulty 1/50/100/150): makes a Poor/normal/Quality/Exquisite Armor Patch (1052540-43).
- **Fur on the Furrier's Kit** (10020-10023, Weaving, difficulty 1/50/100/150): makes a Poor/normal/Quality/Exquisite Woven Bandage (1052561-64).
- **Armor Patches** (`ArmorPatch.cs`): every patch adds 10% of the armor's base armor level (at least 1), stored as the armor's `ArmorPatchAmount`. A patch's quality only sets the highest wield requirement it fits, from `PropertyInt.ArmorPatchMaxWieldReq` (541): Poor 125, normal 200, Quality 230, Exquisite 270. The armor's requirement is its attribute requirement, or its tier's requirement if it has none. Patches still need one per armor slot, and an item takes one patch. Patches made before this change carry only the old flat amount (25/50/100/200), which maps to the same four caps.
- **Bandage on a kit** (no skill, once per kit via `PropertyBool.HealkitBandaged` 189): the kit's HealkitMod goes up by a tenth of its tier's base (10% more effective). There is one recipe per kit tier, covering that tier's Healing, Stamina and Mana Kit. A Poor bandage fits Crude kits only, and every other bandage fits two tiers:
  - Poor: Crude (10024)
  - normal: Plain (10025), Good (10026)
  - Quality: Excellent (10027), Peerless (10032)
  - Exquisite: Treated (10033), Plentiful (10034)
  - Eternal, Medicated and Renegade Herbal kits can't take a bandage.

Leather and fur are bonded, like the hides. Bandages are tradeable.

## Trainers (Zaikhal, landblock 8090)

- Almira bint Qan'ra (1010007) is now the Master Leatherworker and teaches skill 29.
- Rashida bint Hamid (1010084) is the new Master Weaver and teaches skill 26. Her instance is 0x78090C20 in cell 0x80900100 at (32.45, 150.2), between Almira and the Leather Trader, and she is linked to the town generator 0x78090101.

## Gems of Forgetfulness

1053548 is now the Leatherworking Gem. The new Weaving Gem 1053559 is sold by the Prismatic Golem (1010082).

## Dat checklist (portal.dat SkillTable 0x0E000004)

In the dat-editor (`Source/misc/skillTable.cs` `modifyForDaralet`, or by hand), add or change these entries. The server matches them by ID.

**Skill 29 (rename the current "Tailoring" entry)**
- Name: `Leatherworking`
- Description: `Lets you improve the properties of light armor and stitch armor patches from leather. Obtained via trainer NPC.`
- Icon: keep the current Tailoring icon (0x0600209A), or pick a leather one
- Category: 2 (NonWeapon / Other)
- MinLevel: Unusable (2)
- TrainedCost / SpecializedCost: 1000 / 1000 (trainer-only, like every trade skill)
- Formula: none (attr1 None, attr2 None, divisor 1, addition 0)
- ChargenUse 1, LearnMod 1, LowerBound 120, UpperBound 900

**Skill 26 (new entry; it is not in the Daralet table today)**
- Name: `Weaving`
- Description: `Lets you improve the properties of cloth armor and weave fur into bandages for healing kits. Obtained via trainer NPC.`
- Icon: pick a cloth/thread icon (placeholder: the Tailoring icon 0x0600209A)
- Category 2, MinLevel Unusable (2), costs 1000 / 1000, formula none, ChargenUse 1, LearnMod 1, LowerBound 120, UpperBound 900

In code, that is:

```csharp
Skills[(uint)eSkills.ArmorTinkering].Name = "Leatherworking";
Skills[(uint)eSkills.ArmorTinkering].Description = "Lets you improve the properties of light armor and stitch armor patches from leather. Obtained via trainer NPC.";
addSkill((uint)eSkills.ArmsAndArmorRepair, 0x0600209a, "Weaving", "Lets you improve the properties of cloth armor and weave fur into bandages for healing kits. Obtained via trainer NPC.", eSkillCategory.NonWeapon, eSkillMinLevel.Unusable, 1000, 1000, new SkillFormula(eAttributes.None, 1, eAttributes.None, 1, 1, 0));
```

The server needs the updated portal.dat too: `TrainSkill` and the skill list read `SkillTable`. Until skill 26 is in the dat, Weaving has no entry, so deploy the dat together with the server.

## Elsewhere

- daralet-website (branch `feature/leatherworking-weaving`): the planner lists both trade skills. The tinkering calculator has Leatherworking (Light armor, hides) and Weaving (Cloth armor, fabrics) as separate disciplines, and the market salvage filter groups materials the same way. The skill guide and changelog prose in `daralet-site.html` still say Tailoring, and there is no Weaving icon yet (it reuses the Tailoring one).
- daralet-toolkit `BankCommands.cs` lists the `tailoring` bank category, which still exists and also answers to `leatherworking` and `weaving`.
- With hides on Light only and fabrics on Cloth only, the Cloth-armor effects of hides and the Light-armor effects of fabrics in `Salvage.cs` can no longer be reached. They're left in place in case the gating is loosened later.
