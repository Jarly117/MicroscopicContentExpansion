﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using System.Collections.Generic;
using System.Linq;
using TabletopTweaks.Core.Utilities;
using static Kingmaker.Blueprints.BlueprintAbilityResource;
using static MicroscopicContentExpansion.Main;

namespace MicroscopicContentExpansion.NewContent.AntipaladinFeatures {
    internal class FiendishBoon {
        const string NAME = "Fiendish Boon";

        const string DESCRIPTION = @"Upon reaching 5th level, an antipaladin receives a boon from his dark patrons. This boon can take one of two forms. Once the form is chosen, it cannot be changed.
The first type of bond allows the antipaladin to enhance his weapon as a standard action by calling upon the aid of a fiendish spirit for 1 minute per antipaladin level. When called, the spirit causes the weapon to shed unholy light as a torch. At 5th level, this spirit grants the weapon a +1 enhancement bonus. For every three levels beyond 5th, the weapon gains another +1 enhancement bonus, to a maximum of +6 at 20th level.
Adding these properties consumes an amount of bonus equal to the property’s cost (sorted and listed below).
These bonuses can be added to the weapon, stacking with existing weapon bonuses to a maximum of +5, or they can be used to add any of the following weapon properties:
+1: flaming, keen, vicious
+2: anarchic, flaming burst, unholy, wounding
+3: speed
+5: vorpal
These bonuses are added to any properties the weapon already has, but duplicate abilities do not stack. If the weapon is not magical, at least a +1 enhancement bonus must be added before any other properties can be added. The bonus and properties granted by the spirit are determined when the spirit is called and cannot be changed until the spirit is called again. The fiendish spirit imparts no bonuses if the weapon is held by anyone other than the antipaladin but resumes giving bonuses if returned to the antipaladin. These bonuses apply to only one end of a double weapon. An antipaladin can use this ability once per day at 5th level, and one additional time per day for every four levels beyond 5th, to a total of four times per day at 17th level.
The second type of bond allows an antipaladin to gain the service of a fiendish animal. This functions as druid's animal companion. Servant immediately gains fiendish template. At 15th level, an antipaladin’s servant gains spell resistance equal to the antipaladin’s level + 11.";

        const string WEAPON_BOND_DESCRIPTION = "Upon reaching 5th level, an antipaladin forms a divine bond with his weapon. " +
                            "As a standard action, he can call upon the aid of a fiendish " +
                            "spirit for 1 minute per antipaladin level.\nAt 5th level, this spirit grants the weapon a +1 enhancement " +
                            "bonus. For every three levels beyond 5th, the weapon gains another +1 " +
                            "enhancement bonus, to a maximum of +6 at 20th level. These bonuses can be added to the weapon, stacking " +
                            "with existing weapon bonuses to a maximum of +5.\nAlternatively, they can be used to add any of the " +
                            "following weapon properties: " +
                            "flaming, keen, vicious, anarchic, flaming burst, unholy, wounding, speed, and vorpal." +
                            " Adding these properties consumes an amount of bonus equal to the property's cost. These bonuses are added" +
                            " to any properties the weapon already has, but duplicate abilities do not stack.\nAn antipaladin can use this" +
                            " ability once per day at 5th level, and one additional time per day for every four levels beyond 5th, to" +
                            " a total of four times per day at 17th level.";

        public static void AddFiendinshBoon() {
            var AntipaladinClassRef = BlueprintTools.GetModBlueprintReference<BlueprintCharacterClassReference>(MCEContext, "AntipaladinClass");

            var AntipaladinCompanionSelection = AddFiendinshBoonCompanion();
            var AntipaladinFiendishBoonWeapon = AddFiendinshBoonWeapon();

            Helpers.CreateBlueprint<BlueprintFeatureSelection>(MCEContext, "AntipaladinFiendishBoonSelection", bp => {
                bp.SetName(MCEContext, NAME);
                bp.SetDescription(MCEContext, DESCRIPTION);
                bp.m_AllFeatures = new BlueprintFeatureReference[] {
                    AntipaladinCompanionSelection.ToReference<BlueprintFeatureReference>(),
                    AntipaladinFiendishBoonWeapon.ToReference<BlueprintFeatureReference>()
                };
                bp.Mode = SelectionMode.Default;
                bp.Groups = new FeatureGroup[] { FeatureGroup.None };
                bp.IsClassFeature = true;
            });
        }

        private static BlueprintProgression AddFiendinshBoonWeapon() {
            var AntipaladinClassRef = BlueprintTools.GetModBlueprintReference<BlueprintCharacterClassReference>(MCEContext, "AntipaladinClass");

            var icon = BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("a68cd0fbf5d21ef4f8b9375ec0ac53b9").Icon;

            var weaponBondResource = Helpers.CreateBlueprint<BlueprintAbilityResource>(MCEContext, "AntipaladinWeaponBondResource", bp => {
                bp.m_Icon = icon;
                bp.m_MaxAmount = new Amount() {
                    BaseValue = 1,
                };
            });

            var weaponBondDurationBuff = Helpers.CreateBlueprint<BlueprintBuff>(MCEContext, "AntipaladinWeaponBondDurationBuff", bp => {
                bp.SetName(MCEContext, "Fiendish Weapon Bond");
                bp.SetDescription(MCEContext, WEAPON_BOND_DESCRIPTION);
                bp.m_Icon = icon;
                bp.m_Flags = BlueprintBuff.Flags.StayOnDeath;
                bp.Stacking = StackingType.Replace;
                bp.Frequency = DurationRate.Rounds;
            });

            var paladinWeaponBondSwitchAbility = BlueprintTools.GetBlueprint<BlueprintAbility>("7ff088ab58c69854b82ea95c2b0e35b4");

            var weaponBondSwitchAbility = Helpers.CreateBlueprint<BlueprintAbility>(MCEContext, "AntipaladinWeaponBondSwitchAbility", bp => {
                bp.SetName(MCEContext, "Fiendish Weapon Bond");
                bp.SetDescription(MCEContext, WEAPON_BOND_DESCRIPTION);
                bp.NeedEquipWeapons = true;
                bp.Animation = Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle.EnchantWeapon;
                bp.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
                bp.AvailableMetamagic = Metamagic.Quicken | Metamagic.Extend | Metamagic.Heighten;
                bp.CanTargetEnemies = false;
                bp.CanTargetFriends = false;
                bp.Type = AbilityType.Supernatural;
                bp.Range = AbilityRange.Personal;
                bp.m_Icon = icon;
                bp.LocalizedDuration = Helpers.CreateString(MCEContext, "AntipaladinWeaponBondSwitchAbility.Duration", "1 minute per antipaladin level");
                bp.LocalizedSavingThrow = Helpers.CreateString(MCEContext, "AntipaladinWeaponBondSwitchAbility.SavingThrow", "None");
                bp.AddComponent<AbilityEffectRunAction>(c => {
                    c.Actions = Helpers.CreateActionList(
                        new ContextActionWeaponEnchantPool() {
                            Group = ActivatableAbilityGroup.DivineWeaponProperty,
                            EnchantPool = EnchantPoolType.DivineWeaponBond,
                            m_DefaultEnchantments = new BlueprintItemEnchantmentReference[] {
                                BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("d704f90f54f813043a525f304f6c0050"),
                                BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("9e9bab3020ec5f64499e007880b37e52"),
                                BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("d072b841ba0668846adeb007f623bd6c"),
                                BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("6a6a0901d799ceb49b33d4851ff72132"),
                                BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("746ee366e50611146821d61e391edf16")
                            },
                            DurationValue = new ContextDurationValue() {
                                Rate = DurationRate.Minutes,
                                DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                                DiceCountValue = new ContextValue() {
                                    ValueType = ContextValueType.Simple,
                                    Value = 0,
                                    ValueRank = Kingmaker.Enums.AbilityRankType.Default,
                                    ValueShared = AbilitySharedValue.Damage,
                                    Property = Kingmaker.UnitLogic.Mechanics.Properties.UnitProperty.None
                                },
                                BonusValue = new ContextValue() {
                                    ValueType = ContextValueType.Rank,
                                    Value = 0,
                                    ValueRank = Kingmaker.Enums.AbilityRankType.Default,
                                    ValueShared = AbilitySharedValue.Damage,
                                    Property = Kingmaker.UnitLogic.Mechanics.Properties.UnitProperty.None
                                },
                                m_IsExtendable = true
                            }
                        },
                        new ContextActionApplyBuff() {
                            m_Buff = weaponBondDurationBuff.ToReference<BlueprintBuffReference>(),
                            DurationValue = new ContextDurationValue() {
                                Rate = DurationRate.Minutes,
                                DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                                DiceCountValue = new ContextValue() {
                                    ValueType = ContextValueType.Simple,
                                    Value = 0,
                                    ValueRank = Kingmaker.Enums.AbilityRankType.Default,
                                    ValueShared = AbilitySharedValue.Damage,
                                    Property = Kingmaker.UnitLogic.Mechanics.Properties.UnitProperty.None
                                },
                                BonusValue = new ContextValue() {
                                    ValueType = ContextValueType.Rank,
                                    Value = 1,
                                    ValueRank = Kingmaker.Enums.AbilityRankType.Default,
                                    ValueShared = AbilitySharedValue.Damage,
                                    Property = Kingmaker.UnitLogic.Mechanics.Properties.UnitProperty.None
                                },
                                m_IsExtendable = true
                            }
                        }
                    ); ;
                });
                bp.AddComponent<ContextRankConfig>(c => {
                    c.m_Type = Kingmaker.Enums.AbilityRankType.Default;
                    c.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
                    c.m_Stat = StatType.Unknown;
                    c.m_Progression = ContextRankProgression.AsIs;
                    c.m_Max = 20;
                    c.m_Class = new BlueprintCharacterClassReference[] { AntipaladinClassRef };
                });
                bp.AddComponent<AbilityResourceLogic>(c => {
                    c.m_RequiredResource = weaponBondResource.ToReference<BlueprintAbilityResourceReference>();
                    c.m_IsSpendResource = true;
                });
                bp.AddComponents(paladinWeaponBondSwitchAbility.GetComponents<AbilitySpawnFx>());
                bp.AddComponent<AbilityCasterAlignment>(c => {
                    c.Alignment = Kingmaker.UnitLogic.Alignments.AlignmentMaskType.Evil;
                });
            });

            var weaponBondFlaming = createWeaponBondChoice("Flaming",
                BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("7902941ef70a0dc44bcfc174d6193386").Icon,
                BlueprintTools.GetBlueprintReference<BlueprintBuffReference>("b3d7a8ddf339989478aacd7dd8d97841"), 1);
            var weaponBondKeen = createWeaponBondChoice("Keen",
                BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("27d76f1afda08a64d897cc81201b5218").Icon,
                BlueprintTools.GetBlueprintReference<BlueprintBuffReference>("1cc068cf355b8464da5fb8e476f74019"), 1);

            var weaponBondViciousBuff = createWeaponBondBuff("Vicious",
                BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("a1455a289da208144981e4b1ef92cc56"));

            var weaponBondVicious = createWeaponBondChoice("Vicious",
                BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("27d76f1afda08a64d897cc81201b5218").Icon,
                weaponBondViciousBuff.ToReference<BlueprintBuffReference>(), 1);

            var weaponBond = Helpers.CreateBlueprint<BlueprintFeature>(MCEContext, "AntipaladinWeaponBondFeature", bp => {
                bp.SetName(MCEContext, "Fiendish Weapon Bond (+1)");
                bp.SetDescription(MCEContext, WEAPON_BOND_DESCRIPTION);
                bp.m_Icon = icon;
                bp.AddComponent<AddFacts>(c => {
                    c.m_Facts = new BlueprintUnitFactReference[] {
                        weaponBondSwitchAbility.ToReference<BlueprintUnitFactReference>(),
                        weaponBondFlaming.ToReference<BlueprintUnitFactReference>(),
                        weaponBondKeen.ToReference<BlueprintUnitFactReference>(),
                        weaponBondVicious.ToReference<BlueprintUnitFactReference>()
                    };
                });
                bp.AddComponent<AddAbilityResources>(c => {
                    c.m_Resource = weaponBondResource.ToReference<BlueprintAbilityResourceReference>();

                });
            });

            var weaponBondAnarchicBuff = createWeaponBondBuff("Anarchic"
                , BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("57315bc1e1f62a741be0efde688087e9"));

            var weaponBondAnarchic = createWeaponBondChoice("Anarchic",
                BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("8ed07b0cc56223c46953348f849f3309").Icon,
                weaponBondAnarchicBuff.ToReference<BlueprintBuffReference>(), 2);

            var weaponBondFlamingBurst = createWeaponBondChoice("FlamingBurst",
                BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("3af19bdbd6215434f8421a860cc98363").Icon,
                BlueprintTools.GetBlueprintReference<BlueprintBuffReference>("78552038c4a76a04ba78e18cf4fcfd5c"), 2);

            var weaponBondUnholyBuff = createWeaponBondBuff("Unholy"
                 , BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("d05753b8df780fc4bb55b318f06af453"));
            var weaponBondUnholy = createWeaponBondChoice("Unholy",
                BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("561803a819460f34ea1fe079edabecce").Icon,
                weaponBondUnholyBuff.ToReference<BlueprintBuffReference>(), 2);

            var weaponBond2 = createWeaponBondFeaturePlusX(2, icon,
                weaponBondAnarchic.ToReference<BlueprintUnitFactReference>(),
                weaponBondFlamingBurst.ToReference<BlueprintUnitFactReference>(),
                weaponBondUnholy.ToReference<BlueprintUnitFactReference>()
                );

            var weaponBondSpeed = createWeaponBondChoice("Speed",
                BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("ed1ef581af9d9014fa1386216b31cdae").Icon,
                BlueprintTools.GetBlueprintReference<BlueprintBuffReference>("f260f8100cd9f6749bf071c930eb287d"), 3);

            var weaponBond3 = createWeaponBondFeaturePlusX(3, icon, weaponBondSpeed.ToReference<BlueprintUnitFactReference>());
            var weaponBond4 = createWeaponBondFeaturePlusX(4, icon);

            var weaponBondVorpalBuff = createWeaponBondBuff("Vorpal"
                 , BlueprintTools.GetBlueprintReference<BlueprintItemEnchantmentReference>("2f60bfcba52e48a479e4a69868e24ebc"));
            var weaponBondVorpal = createWeaponBondChoice("Vorpal",
                BlueprintTools.GetBlueprint<BlueprintProgression>("e08a817f475c8794aa56fdd904f43a57").Icon,
                weaponBondVorpalBuff.ToReference<BlueprintBuffReference>(), 5);

            var weaponBond5 = createWeaponBondFeaturePlusX(5, icon, weaponBondVorpal.ToReference<BlueprintUnitFactReference>());
            var weaponBond6 = createWeaponBondFeaturePlusX(6, icon);

            return Helpers.CreateBlueprint<BlueprintProgression>(MCEContext, "AntipaladinWeaponBondProgression", bp => {
                bp.SetName(MCEContext, "Fiendish Bond");
                bp.SetDescription(MCEContext, WEAPON_BOND_DESCRIPTION);
                bp.m_Classes = new BlueprintProgression.ClassWithLevel[] {
                    new BlueprintProgression.ClassWithLevel{
                        m_Class = AntipaladinClassRef
                    }
                };
                bp.LevelEntries = new LevelEntry[] {
                    Helpers.CreateLevelEntry(5, weaponBond),
                    Helpers.CreateLevelEntry(8, weaponBond2),
                    Helpers.CreateLevelEntry(11, weaponBond3),
                    Helpers.CreateLevelEntry(14, weaponBond4),
                    Helpers.CreateLevelEntry(17, weaponBond5),
                    Helpers.CreateLevelEntry(20, weaponBond6),
                };
            });
        }

        private static BlueprintActivatableAbility createWeaponBondChoice(string name, UnityEngine.Sprite icon,
            BlueprintBuffReference bondBuff, int weight = 1) {
            return Helpers.CreateBlueprint<BlueprintActivatableAbility>(MCEContext, $"AntipaladinWeaponBond{name}Choice", bp => {
                bp.SetName(MCEContext, $"Fiendish Weapon Bond - {name}");
                bp.SetDescription(MCEContext, WEAPON_BOND_DESCRIPTION);
                bp.m_Icon = icon;
                bp.DeactivateImmediately = true;
                bp.Group = ActivatableAbilityGroup.DivineWeaponProperty;
                bp.ActivationType = AbilityActivationType.Immediately;
                bp.m_ActivateWithUnitCommand = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free;
                bp.m_ActivateOnUnitAction = AbilityActivateOnUnitActionType.Attack;
                bp.WeightInGroup = weight;
                bp.m_Buff = bondBuff;
            });
        }

        private static BlueprintBuff createWeaponBondBuff(string name, BlueprintItemEnchantmentReference enchant) {
            return Helpers.CreateBlueprint<BlueprintBuff>(MCEContext, $"AntipaladinWeaponBond{name}Buff", bp => {
                bp.m_Flags = BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.StayOnDeath;
                bp.Stacking = StackingType.Stack;
                bp.Frequency = DurationRate.Rounds;
                bp.AddComponent<AddBondProperty>(c => {
                    c.EnchantPool = EnchantPoolType.DivineWeaponBond;
                    c.m_Enchant = enchant;
                });
            });
        }

        private static BlueprintFeature createWeaponBondFeaturePlusX(int bonus, UnityEngine.Sprite icon, params BlueprintUnitFactReference[] facts) {
            return Helpers.CreateBlueprint<BlueprintFeature>(MCEContext, $"AntipaladinWeaponBondPlus{bonus}", bp => {
                bp.SetName(MCEContext, $"Fiendish Weapon Bond (+{bonus})");
                bp.SetDescription(MCEContext, WEAPON_BOND_DESCRIPTION);
                bp.m_Icon = icon;
                if (facts != null && facts.Length > 0) {
                    bp.AddComponent<AddFacts>(c => {
                        c.m_Facts = facts;
                    });
                }
                bp.AddComponent<IncreaseActivatableAbilityGroupSize>(c => {
                    c.Group = ActivatableAbilityGroup.DivineWeaponProperty;
                });
            });
        }
        private static BlueprintFeatureSelection AddFiendinshBoonCompanion() {
            var AntipaladinClassRef = BlueprintTools.GetModBlueprintReference<BlueprintCharacterClassReference>(MCEContext, "AntipaladinClass");

            BlueprintFeature AnimalCompanionRank = BlueprintTools.GetBlueprint<BlueprintFeature>("1670990255e4fe948a863bafd5dbda5d");
            var AntipaladinAnimalCompanionProgression = Helpers.CreateBlueprint<BlueprintProgression>(MCEContext, "AntipaladinAnimalCompanionProgression", bp => {
                bp.SetName(MCEContext, "Antipaladin Animal Companion Progression");
                bp.SetName(MCEContext, "");
                bp.Ranks = 1;
                bp.IsClassFeature = true;
                bp.m_FeaturesRankIncrease = new List<BlueprintFeatureReference>();
                bp.LevelEntries = Enumerable.Range(6, 20)
                    .Select(i => new LevelEntry {
                        Level = i,
                        m_Features = new List<BlueprintFeatureBaseReference> {
                            AnimalCompanionRank.ToReference<BlueprintFeatureBaseReference>()
                        },
                    })
                    .ToArray();
                bp.m_Classes = new BlueprintProgression.ClassWithLevel[] {
                    new BlueprintProgression.ClassWithLevel{
                        m_Class = AntipaladinClassRef
                    }
                };
                for (int i = 0; i < 3; i++) {
                    bp.AddComponent<AddFeatureOnApply>(bp => {
                        bp.m_Feature = AnimalCompanionRank.ToReference<BlueprintFeatureReference>();

                    });
                }
                bp.UIGroups = new UIGroup[0];
            });

            var PaladinDivineMountSelection = BlueprintTools.GetBlueprint<BlueprintFeatureSelection>("e2f0e0efc9e155e43ba431984429678e");
            var MountTargetFeature = BlueprintTools.GetBlueprint<BlueprintFeature>("cb06f0e72ffb5c640a156bd9f8000c1d");
            var AnimalCompanionArchetypeSelection = BlueprintTools.GetBlueprint<BlueprintFeature>("65af7290b4efd5f418132141aaa36c1b");

            var AntipaladinCompanionSelection = Helpers.CreateBlueprint<BlueprintFeatureSelection>(MCEContext, "AntipaladinCompanionSelection", bp => {
                bp.SetName(MCEContext, "Fiendish Boon");
                bp.SetDescription(MCEContext, "");
                bp.IsClassFeature = true;
                bp.ReapplyOnLevelUp = true;
                bp.Groups = new FeatureGroup[] { FeatureGroup.Feat };
                bp.Mode = SelectionMode.Default;
                bp.Group = FeatureGroup.AnimalCompanion;
                bp.m_Icon = PaladinDivineMountSelection.Icon;
                bp.Ranks = 1;
                bp.IsPrerequisiteFor = new List<BlueprintFeatureReference>();
                bp.AddFeatures(
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("472091361cf118049a2b4339c4ea836a"), //continue
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("f6f1cdcc404f10c4493dc1e51208fd6f"), //bear
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("afb817d80b843cc4fa7b12289e6ebe3d"), //boar
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("f9ef7717531f5914a9b6ecacfad63f46"), //centipede
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("f894e003d31461f48a02f5caec4e3359"), //dog
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("aa92fea676be33d4dafd176d699d7996"), //elk
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("9dc58b5901677c942854019d1dd98374"), //horse
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("2ee2ba60850dd064e8b98bf5c2c946ba"), //leopard
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("6adc3aab7cde56b40aa189a797254271"), //mammoth
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("ece6bde3dfc76ba4791376428e70621a"), //monitor lizard
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("126712ef923ab204983d6f107629c895"), //smilodon
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("2d3f409bb0956d44187e9ec8340163f8"), //triceratops
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("89420de28b6bb9443b62ce489ae5423b"), //velociraptor
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("67a9dc42b15d0954ca4689b13e8dedea"), //wolf
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("bfeb9be0a3c9420b8b2beecc8171029c"), //horse preorder
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("44f4d77689434e07a5a44dcb65b25f71"), //smilodon preorder
                    BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("52c854f77105445a9457572ab5826c00")  //triceratops preorder

                );
                bp.AddComponent<AddFeatureOnApply>(c => {
                    c.m_Feature = AnimalCompanionRank.ToReference<BlueprintFeatureReference>();
                });
                bp.AddComponent<AddFeatureOnApply>(c => {
                    c.m_Feature = MountTargetFeature.ToReference<BlueprintFeatureReference>();
                });
                bp.AddComponent<AddFeatureOnApply>(c => {
                    c.m_Feature = AntipaladinAnimalCompanionProgression.ToReference<BlueprintFeatureReference>();
                });

                bp.AddComponent<AddFeatureOnApply>(c => {
                    c.m_Feature = AnimalCompanionArchetypeSelection.ToReference<BlueprintFeatureReference>();
                });
                var FiendishTemplateFromTTTBase = BlueprintTools.GetBlueprintReference<BlueprintFeatureReference>("970ffc97-344c-496d-b8ef-24118b5689b0");
                if (!FiendishTemplateFromTTTBase.IsEmpty()) {
                    bp.AddComponent<AddFeatureToPet>(c => {
                        c.m_Feature = FiendishTemplateFromTTTBase;
                    });
                }
            });

            return AntipaladinCompanionSelection;
        }
    }
}
