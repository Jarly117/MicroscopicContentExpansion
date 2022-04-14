﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using MicroscopicContentExpansion.MCEHelpers;
using TabletopTweaks.Core.Utilities;
using static MicroscopicContentExpansion.Main;

namespace MicroscopicContentExpansion.NewContent.AntipaladinFeatures {
    internal class ChannelNegativeEnergy {
        private const string NAME = "Channel Negative Energy";
        private const string DESCRIPTION = "When an antipaladin reaches 4th level, he gains the supernatural ability to" +
            " channel negative energy like a cleric. Using this ability consumes two uses of his touch of corruption ability." +
            " An antipaladin uses his level as his effective cleric level when channeling negative energy." +
            " This is a Charisma-based ability.";

        public static void AddChannelNegativeEnergy() {

            var ChannelEnergyFact = BlueprintTools.GetBlueprint<BlueprintUnitFact>("93f062bc0bf70e84ebae436e325e30e8");
            var ChannelNegativeEnergy = BlueprintTools.GetBlueprint<BlueprintAbility>("89df18039ef22174b81052e2e419c728");
            var ChannelEnergyResource = BlueprintTools.GetBlueprint<BlueprintAbilityResource>("5e2bba3e07c37be42909a12945c27de7");
            var MythicChannelProperty = BlueprintTools.GetBlueprintReference<BlueprintUnitPropertyReference>("152e61de154108d489ff34b98066c25c");
            var SelectiveChannel = BlueprintTools.GetBlueprint<BlueprintFeature>("fd30c69417b434d47b6b03b9c1f568ff");
            var ExtraChannel = BlueprintTools.GetBlueprint<BlueprintFeature>("cd9f19775bd9d3343a31a065e93f0c47");

            var TouchOfCorruptionResource = BlueprintTools.GetModBlueprint<BlueprintAbilityResource>(MCEContext, "AntipaladinTouchOfCorruptionResource");
            var AntipaladinClassRef = BlueprintTools.GetModBlueprintReference<BlueprintCharacterClassReference>(MCEContext, "AntipaladinClass");
            var NegativeEnergyAffinity = BlueprintTools.GetBlueprint<BlueprintFeature>("d5ee498e19722854198439629c1841a5");
            var DeathDomainGreaterLiving = BlueprintTools.GetBlueprint<BlueprintFeature>("fd7c08ccd3c7773458eb9613db3e93ad");
            var LifeDominantSoul = BlueprintTools.GetBlueprint<BlueprintFeature>("8f58b4029511b5345981ffaf1da5ea2e");

            var ChannelEnergyAntipaladinHarm = Helpers.CreateBlueprint<BlueprintAbility>(MCEContext, "AntipaladinChannelEnergyHarm", bp => {
                bp.SetName(MCEContext, $"{NAME} - Damage Living");
                bp.SetDescription(MCEContext, DESCRIPTION);
                bp.m_DescriptionShort = bp.m_Description;
                bp.m_Icon = ChannelNegativeEnergy.Icon;
                bp.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
                bp.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
                bp.CanTargetEnemies = true;
                bp.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                bp.EffectOnAlly = AbilityEffectOnUnit.None;
                bp.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Omni;
                bp.ActionType = UnitCommand.CommandType.Standard;
                bp.ResourceAssetIds = ChannelNegativeEnergy.ResourceAssetIds;
                bp.Range = AbilityRange.Personal;
                bp.Type = AbilityType.Special;
                bp.AddComponent<AbilityResourceLogic>(c => {
                    c.m_RequiredResource = TouchOfCorruptionResource.ToReference<BlueprintAbilityResourceReference>();
                    c.m_IsSpendResource = true;
                    c.Amount = 2;
                });
                bp.AddComponent<AbilityTargetsAround>(c => {
                    c.m_Radius = 30.Feet();
                    c.m_TargetType = TargetType.Any;
                    c.m_Condition = MCETools.EmptyCondition();
                });
                bp.AddComponent<AbilityEffectRunAction>(c => {
                    ActionList dealDamage = MCETools.DoSingle<ContextActionSavingThrow>(s => {
                        s.m_ConditionalDCIncrease = new ContextActionSavingThrow.ConditionalDCIncrease[0];
                        s.Type = SavingThrowType.Will;
                        s.CustomDC = new ContextValue();
                        s.Actions = MCETools.DoSingle<ContextActionDealDamage>(ac => {
                            ac.DamageType = new DamageTypeDescription() {
                                Type = DamageType.Energy,
                                Common = new DamageTypeDescription.CommomData(),
                                Physical = new DamageTypeDescription.PhysicalData(),
                                Energy = Kingmaker.Enums.Damage.DamageEnergyType.NegativeEnergy
                            };
                            ac.Duration = new ContextDurationValue() {
                                m_IsExtendable = true,
                                DiceCountValue = new ContextValue(),
                                BonusValue = new ContextValue()
                            };
                            ac.Value = new ContextDiceValue() {
                                DiceType = Kingmaker.RuleSystem.DiceType.D6,
                                DiceCountValue = new ContextValue() {
                                    ValueType = ContextValueType.Rank,
                                    ValueRank = AbilityRankType.DamageDice
                                },
                                BonusValue = new ContextValue() {
                                    ValueType = ContextValueType.Rank,
                                    ValueRank = AbilityRankType.DamageBonus
                                }
                            };
                            ac.IsAoE = true;
                            ac.HalfIfSaved = true;
                        });
                    });

                    c.Actions = MCETools.DoSingle<Conditional>(ac => {
                        ac.ConditionsChecker = MCETools.IfSingle<ContextConditionCasterHasFact>(cond => {
                            cond.m_Fact = SelectiveChannel.ToReference<BlueprintUnitFactReference>();
                        });
                        ac.IfTrue = MCETools.DoSingle<Conditional>(cc => {
                            cc.ConditionsChecker = MCETools.IfMany(
                                        new ContextConditionIsEnemy() { },
                                        new ContextConditionHasFact() {
                                            Not = true,
                                            m_Fact = NegativeEnergyAffinity.ToReference<BlueprintUnitFactReference>()
                                        }
                            );
                            cc.IfTrue = dealDamage;
                            cc.IfFalse = MCETools.DoNothing();
                        });
                        ac.IfFalse = MCETools.DoSingle<Conditional>(cnd => {
                            cnd.ConditionsChecker = MCETools.IfSingle<ContextConditionHasFact>(iff => {
                                iff.Not = true;
                                iff.m_Fact = NegativeEnergyAffinity.ToReference<BlueprintUnitFactReference>();
                            });
                            cnd.IfTrue = dealDamage;
                            cnd.IfFalse = MCETools.DoNothing();
                        });
                    });
                });

                bp.AddComponent<AbilitySpawnFx>(c => {
                    c.PrefabLink = ChannelNegativeEnergy.GetComponent<AbilitySpawnFx>().PrefabLink;
                    c.PositionAnchor = AbilitySpawnFxAnchor.None;
                    c.OrientationAnchor = AbilitySpawnFxAnchor.None;
                });

                bp.AddContextRankConfig(c => {
                    c.m_Type = AbilityRankType.DamageDice;
                    c.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
                    c.m_Progression = ContextRankProgression.OnePlusDiv2;
                    c.m_Class = new BlueprintCharacterClassReference[] { AntipaladinClassRef };
                    c.m_UseMin = true;
                });

                bp.AddComponent<AbilityCasterAlignment>(c => {
                    c.Alignment = Kingmaker.UnitLogic.Alignments.AlignmentMaskType.Evil;
                });

                bp.AddContextRankConfig(c => {
                    c.m_Type = AbilityRankType.DamageBonus;
                    c.m_BaseValueType = ContextRankBaseValueType.CustomProperty;
                    c.m_Progression = ContextRankProgression.AsIs;
                    c.m_CustomProperty = MythicChannelProperty;
                });

                bp.AddComponent<SpellDescriptorComponent>(c => {
                    c.Descriptor = new SpellDescriptorWrapper(SpellDescriptor.ChannelNegativeHarm);
                });

            });


            var ChannelEnergyAntipaladinHeal = Helpers.CreateBlueprint<BlueprintAbility>(MCEContext, "AntipaladinChannelEnergyHeal", bp => {
                bp.SetName(MCEContext, $"{NAME} - Heal Undead");
                bp.SetDescription(MCEContext, DESCRIPTION);
                bp.m_DescriptionShort = bp.m_Description;
                bp.m_Icon = ChannelNegativeEnergy.Icon;
                bp.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
                bp.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
                bp.CanTargetEnemies = true;
                bp.EffectOnAlly = AbilityEffectOnUnit.None;
                bp.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                bp.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Omni;
                bp.ActionType = UnitCommand.CommandType.Standard;
                bp.ResourceAssetIds = ChannelNegativeEnergy.ResourceAssetIds;
                bp.Range = AbilityRange.Personal;
                bp.Type = AbilityType.Special;
                bp.AddComponent<AbilityResourceLogic>(c => {
                    c.m_RequiredResource = TouchOfCorruptionResource.ToReference<BlueprintAbilityResourceReference>();
                    c.m_IsSpendResource = true;
                    c.Amount = 2;
                });
                bp.AddComponent<AbilityTargetsAround>(c => {
                    c.m_Radius = 30.Feet();
                    c.m_TargetType = TargetType.Any;
                    c.m_Condition = MCETools.EmptyCondition();
                });
                bp.AddComponent<AbilityEffectRunAction>(c => {
                    var healNormal = MCETools.DoSingle<ContextActionHealTarget>(ac => {
                        ac.Value = new ContextDiceValue() {
                            DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                            DiceCountValue = new ContextValue() {
                                ValueType = ContextValueType.Simple,
                                Value = 0,
                                ValueRank = AbilityRankType.Default,
                                ValueShared = AbilitySharedValue.Damage,
                                Property = UnitProperty.None
                            },
                            BonusValue = new ContextValue() {
                                ValueType = ContextValueType.Shared,
                                Value = 0,
                                ValueRank = AbilityRankType.Default,
                                ValueShared = AbilitySharedValue.Heal,
                                Property = UnitProperty.None
                            }
                        };
                    });

                    var healLifeDominantSoul = MCETools.DoSingle<ContextActionHealTarget>(ac => {
                        ac.Value = new ContextDiceValue() {
                            DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                            DiceCountValue = new ContextValue() {
                                ValueType = ContextValueType.Simple,
                                Value = 0,
                                ValueRank = AbilityRankType.Default,
                                ValueShared = AbilitySharedValue.Damage,
                                Property = UnitProperty.None
                            },
                            BonusValue = new ContextValue() {
                                ValueType = ContextValueType.Shared,
                                Value = 0,
                                ValueRank = AbilityRankType.Default,
                                ValueShared = AbilitySharedValue.StatBonus,
                                Property = UnitProperty.None
                            }
                        };
                    });

                    var ifAllyWithoutLifeDominantSoul = MCETools.IfMany(
                                        new ContextConditionIsAlly() { },
                                        new ContextConditionHasFact() {
                                            m_Fact = LifeDominantSoul.ToReference<BlueprintUnitFactReference>(),
                                            Not = true
                                        }
                            );

                    var ifAllyWithLifeDominantSoul = MCETools.IfMany(
                                        new ContextConditionIsAlly() { },
                                        new ContextConditionHasFact() {
                                            m_Fact = LifeDominantSoul.ToReference<BlueprintUnitFactReference>()
                                        }
                            );
                    var hasSelectiveChannel = MCETools.IfSingle<ContextConditionCasterHasFact>(cond => {
                        cond.m_Fact = SelectiveChannel.ToReference<BlueprintUnitFactReference>();
                    });

                    var hasLifeDominantSoul = MCETools.IfSingle<ContextConditionHasFact>(iff => {
                        iff.m_Fact = LifeDominantSoul.ToReference<BlueprintUnitFactReference>();
                    });

                    var whenHasNegativeEnergyAffinityUpper = MCETools.DoSingle<Conditional>(ac => {
                        ac.ConditionsChecker = hasSelectiveChannel;
                        ac.IfTrue = MCETools.DoSingle<Conditional>(cc => {
                            cc.ConditionsChecker = ifAllyWithoutLifeDominantSoul;
                            cc.IfTrue = healNormal;
                            cc.IfFalse = MCETools.DoSingle<Conditional>(icc => {
                                icc.ConditionsChecker = ifAllyWithLifeDominantSoul;
                                icc.IfTrue = healLifeDominantSoul;
                                icc.IfFalse = MCETools.DoNothing();
                            });
                        });
                        ac.IfFalse = MCETools.DoSingle<Conditional>(cnd => {
                            cnd.ConditionsChecker = hasLifeDominantSoul;
                            cnd.IfTrue = healLifeDominantSoul;
                            cnd.IfFalse = healNormal;
                        });
                    });

                    var ifHasNegativeEnergyOrDeathDomain = new ConditionsChecker() {
                        Conditions = new Condition[] {
                                        new ContextConditionHasFact() {
                                            m_Fact = NegativeEnergyAffinity.ToReference<BlueprintUnitFactReference>()
                                        },
                                        new ContextConditionHasFact() {
                                            m_Fact = DeathDomainGreaterLiving.ToReference<BlueprintUnitFactReference>()
                                        }
                            },
                        Operation = Operation.Or
                    };

                    c.Actions = MCETools.DoSingle<Conditional>(cond => {
                        cond.ConditionsChecker = ifHasNegativeEnergyOrDeathDomain;
                        cond.IfTrue = whenHasNegativeEnergyAffinityUpper;
                        cond.IfFalse = MCETools.DoNothing();
                    });
                });

                bp.AddComponent<AbilitySpawnFx>(c => {
                    c.PrefabLink = ChannelNegativeEnergy.GetComponent<AbilitySpawnFx>().PrefabLink;
                    c.PositionAnchor = AbilitySpawnFxAnchor.None;
                    c.OrientationAnchor = AbilitySpawnFxAnchor.None;
                });

                bp.AddComponent<ContextCalculateSharedValue>(c => {
                    c.ValueType = AbilitySharedValue.Heal;
                    c.Value = new ContextDiceValue() {
                        DiceType = Kingmaker.RuleSystem.DiceType.D6,
                        DiceCountValue = new ContextValue() {
                            ValueType = ContextValueType.Rank,
                            Value = 0,
                            ValueRank = AbilityRankType.Default,
                            ValueShared = AbilitySharedValue.Damage,
                            Property = UnitProperty.None
                        },
                        BonusValue = new ContextValue() {
                            ValueType = ContextValueType.Rank,
                            Value = 0,
                            ValueRank = AbilityRankType.DamageBonus,
                            ValueShared = AbilitySharedValue.Damage,
                            Property = UnitProperty.None
                        }
                    };
                    c.Modifier = 1.0;
                });

                bp.AddContextRankConfig(c => {
                    c.m_Type = AbilityRankType.Default;
                    c.m_Stat = StatType.Unknown;
                    c.m_SpecificModifier = ModifierDescriptor.None;
                    c.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
                    c.m_Progression = ContextRankProgression.OnePlusDiv2;
                    c.m_Class = new BlueprintCharacterClassReference[] { AntipaladinClassRef };
                });

                bp.AddComponent<AbilityUseOnRest>(c => {
                    c.Type = AbilityUseOnRestType.HealMassUndead;
                    c.BaseValue = 1;
                    c.AddCasterLevel = false;
                    c.MultiplyByCasterLevel = true;
                    c.MaxCasterLevel = 20;
                });

                bp.AddContextRankConfig(c => {
                    c.m_Type = AbilityRankType.DamageBonus;
                    c.m_BaseValueType = ContextRankBaseValueType.CustomProperty;
                    c.m_Progression = ContextRankProgression.AsIs;
                    c.m_SpecificModifier = ModifierDescriptor.None;
                    c.m_CustomProperty = MythicChannelProperty;
                });

                bp.AddComponent<ContextCalculateSharedValue>(c => {
                    c.ValueType = AbilitySharedValue.StatBonus;
                    c.Value = new ContextDiceValue() {
                        DiceType = Kingmaker.RuleSystem.DiceType.D6,
                        DiceCountValue = new ContextValue() {
                            ValueType = ContextValueType.Simple,
                            Value = 0,
                            ValueRank = AbilityRankType.Default,
                            ValueShared = AbilitySharedValue.Damage,
                            Property = UnitProperty.None
                        },
                        BonusValue = new ContextValue() {
                            ValueType = ContextValueType.Shared,
                            Value = 0,
                            ValueRank = AbilityRankType.StatBonus,
                            ValueShared = AbilitySharedValue.Heal,
                            Property = UnitProperty.None
                        }
                    };
                    c.Modifier = 0.5;
                });

                bp.AddContextRankConfig(c => {
                    c.m_Type = AbilityRankType.DamageBonus;
                    c.m_BaseValueType = ContextRankBaseValueType.CustomProperty;
                    c.m_Progression = ContextRankProgression.AsIs;
                    c.m_SpecificModifier = ModifierDescriptor.None;
                    c.m_CustomProperty = MythicChannelProperty;
                });

                bp.AddComponent<SpellDescriptorComponent>(c => {
                    c.Descriptor = new SpellDescriptorWrapper(SpellDescriptor.ChannelNegativeHeal);
                });

                bp.AddComponent<AbilityCasterAlignment>(c => {
                    c.Alignment = Kingmaker.UnitLogic.Alignments.AlignmentMaskType.Evil;
                });

            });


            var ChannelNegativeEnergyFeature = Helpers.CreateBlueprint<BlueprintFeature>(MCEContext, "AntipaladinChannelNegativeEnergyFeature", bp => {
                bp.SetName(MCEContext, NAME);
                bp.SetDescription(MCEContext, DESCRIPTION);
                bp.m_DescriptionShort = bp.m_Description;
                bp.m_Icon = ChannelNegativeEnergy.Icon;
                bp.Ranks = 1;
                bp.Groups = new FeatureGroup[0];
                bp.IsClassFeature = true;
                bp.AddComponent<AddFacts>(c => {
                    c.m_Facts = new BlueprintUnitFactReference[] {
                        ChannelEnergyFact.ToReference<BlueprintUnitFactReference>(),
                        ChannelEnergyAntipaladinHarm.ToReference<BlueprintUnitFactReference>(),
                        ChannelEnergyAntipaladinHeal.ToReference<BlueprintUnitFactReference>()
                    };
                });
            });
            SelectiveChannel.AddPrerequisiteFeature(ChannelNegativeEnergyFeature, Prerequisite.GroupType.Any);
            ExtraChannel.AddPrerequisiteFeature(ChannelNegativeEnergyFeature, Prerequisite.GroupType.Any);
        }
    }
}
