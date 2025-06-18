using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Bloodcraft.Interfaces;
using Bloodcraft.Patches;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using Il2CppSystem.Text;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft
{
	// Token: 0x0200000C RID: 12
	internal static class Core
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600002B RID: 43 RVA: 0x0000263C File Offset: 0x0000083C
		public static World Server { get; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600002C RID: 44 RVA: 0x00002643 File Offset: 0x00000843
		public static EntityManager EntityManager
		{
			get
			{
				return Core.Server.EntityManager;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600002D RID: 45 RVA: 0x0000264F File Offset: 0x0000084F
		public static ServerGameManager ServerGameManager
		{
			get
			{
				return Core.SystemService.ServerScriptMapper.GetServerGameManager();
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600002E RID: 46 RVA: 0x00002660 File Offset: 0x00000860
		public static SystemService SystemService { get; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600002F RID: 47 RVA: 0x00002667 File Offset: 0x00000867
		// (set) Token: 0x06000030 RID: 48 RVA: 0x0000266E File Offset: 0x0000086E
		public static ServerGameBalanceSettings ServerGameBalanceSettings { get; set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000031 RID: 49 RVA: 0x00002676 File Offset: 0x00000876
		public static double ServerTime
		{
			get
			{
				return Core.ServerGameManager.ServerTime;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000032 RID: 50 RVA: 0x00002682 File Offset: 0x00000882
		public static double DeltaTime
		{
			get
			{
				return (double)Core.ServerGameManager.DeltaTime;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000033 RID: 51 RVA: 0x0000268F File Offset: 0x0000088F
		public static ManualLogSource Log
		{
			get
			{
				return Plugin.LogInstance;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000034 RID: 52 RVA: 0x00002696 File Offset: 0x00000896
		public static IReadOnlySet<WeaponType> BleedingEdge
		{
			get
			{
				return Core._bleedingEdge;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000035 RID: 53 RVA: 0x0000269D File Offset: 0x0000089D
		// (set) Token: 0x06000036 RID: 54 RVA: 0x000026A4 File Offset: 0x000008A4
		public static byte[] NEW_SHARED_KEY { get; internal set; }

		// Token: 0x06000037 RID: 55 RVA: 0x000026AC File Offset: 0x000008AC
		public static void Initialize()
		{
			if (Core._initialized)
			{
				return;
			}
			Core.NEW_SHARED_KEY = Convert.FromBase64String(SecretManager.GetNewSharedKey());
			if (!ComponentRegistry._initialized)
			{
				ComponentRegistry.Initialize();
			}
			new PlayerService();
			new LocalizationService();
			if (ConfigService.Eclipse)
			{
				new EclipseService();
			}
			if (ConfigService.ExtraRecipes)
			{
				Recipes.ModifyRecipes();
			}
			if (ConfigService.StarterKit)
			{
				Configuration.GetStarterKitItems();
			}
			if (ConfigService.PrestigeSystem)
			{
				Buffs.GetPrestigeBuffs();
			}
			if (ConfigService.ClassSystem)
			{
				Configuration.GetClassSpellCooldowns();
				Classes.GetAbilityJewels();
			}
			if (ConfigService.LevelingSystem)
			{
				EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> value;
				if ((value = Core.<>O.<0>__OnUpdate) == null)
				{
					value = (Core.<>O.<0>__OnUpdate = new EventHandler<DeathEventListenerSystemPatch.DeathEventArgs>(LevelingSystem.OnUpdate));
				}
				DeathEventListenerSystemPatch.OnDeathEventHandler += value;
			}
			if (ConfigService.ExpertiseSystem)
			{
				EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> value2;
				if ((value2 = Core.<>O.<1>__OnUpdate) == null)
				{
					value2 = (Core.<>O.<1>__OnUpdate = new EventHandler<DeathEventListenerSystemPatch.DeathEventArgs>(WeaponSystem.OnUpdate));
				}
				DeathEventListenerSystemPatch.OnDeathEventHandler += value2;
			}
			if (ConfigService.QuestSystem)
			{
				new QuestService();
				EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> value3;
				if ((value3 = Core.<>O.<2>__OnUpdate) == null)
				{
					value3 = (Core.<>O.<2>__OnUpdate = new EventHandler<DeathEventListenerSystemPatch.DeathEventArgs>(QuestSystem.OnUpdate));
				}
				DeathEventListenerSystemPatch.OnDeathEventHandler += value3;
			}
			if (ConfigService.FamiliarSystem)
			{
				Configuration.GetExcludedFamiliars();
				if (!ConfigService.LevelingSystem)
				{
					EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> value4;
					if ((value4 = Core.<>O.<3>__OnUpdate) == null)
					{
						value4 = (Core.<>O.<3>__OnUpdate = new EventHandler<DeathEventListenerSystemPatch.DeathEventArgs>(FamiliarLevelingSystem.OnUpdate));
					}
					DeathEventListenerSystemPatch.OnDeathEventHandler += value4;
				}
				EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> value5;
				if ((value5 = Core.<>O.<4>__OnUpdate) == null)
				{
					value5 = (Core.<>O.<4>__OnUpdate = new EventHandler<DeathEventListenerSystemPatch.DeathEventArgs>(FamiliarUnlockSystem.OnUpdate));
				}
				DeathEventListenerSystemPatch.OnDeathEventHandler += value5;
				new BattleService();
				new FamiliarService();
			}
			if (ConfigService.ProfessionSystem)
			{
				Misc.GetStatModPrefabs();
			}
			Core.GetWeaponTypes();
			Core.ModifyPrefabs();
			Buffs.GetStackableBuffs();
			try
			{
				Core.ServerGameBalanceSettings = ServerGameBalanceSettings.Get(Core.SystemService.ServerGameSettingsSystem._ServerBalanceSettings);
				Progression.GetAttributeCaps(default(Entity));
			}
			catch (Exception ex)
			{
				ManualLogSource log = Core.Log;
				bool flag;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(35, 1, ref flag);
				if (flag)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("Error getting attribute soft caps: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted<Exception>(ex);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
			if (Core._resetShardBearers)
			{
				Core.ResetShardBearers();
			}
			Core._initialized = true;
			DebugLoggerPatch._initialized = true;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000028A0 File Offset: 0x00000AA0
		private static World GetServerWorld()
		{
			return World.s_AllWorlds.ToArray().FirstOrDefault((World world) => world.Name == "Server");
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000028D0 File Offset: 0x00000AD0
		public static void StartCoroutine(IEnumerator routine)
		{
			if (Core._monoBehaviour == null)
			{
				Core._monoBehaviour = new GameObject("Bloodcraft").AddComponent<IgnorePhysicsDebugSystem>();
				Object.DontDestroyOnLoad(Core._monoBehaviour.gameObject);
			}
			Core._monoBehaviour.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(routine));
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002920 File Offset: 0x00000B20
		public static AddItemSettings GetAddItemSettings()
		{
			AddItemSettings result = default(AddItemSettings);
			result.EntityManager = Core.EntityManager;
			result.DropRemainder = true;
			result.ItemDataMap = Core.ServerGameManager.ItemLookupMap;
			result.EquipIfPossible = true;
			return result;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002968 File Offset: 0x00000B68
		private static void GetWeaponTypes()
		{
			HashSet<WeaponType> hashSet = new HashSet<WeaponType>();
			foreach (WeaponType item in Configuration.ParseEnumsFromString<WeaponType>(ConfigService.BleedingEdge))
			{
				hashSet.Add(item);
			}
			Core._bleedingEdge = hashSet;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000029CC File Offset: 0x00000BCC
		private static void ModifyPrefabs()
		{
			if (ConfigService.LevelingSystem)
			{
				Entity entity;
				if (Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_EquipBuff_Shared_General, ref entity))
				{
					entity.Add<ScriptSpawn>();
				}
				if (Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_EquipBuff_MagicSource_BloodKey_T01, ref entity))
				{
					entity.Add<ScriptSpawn>();
				}
			}
			if (ConfigService.FamiliarSystem)
			{
				Entity @null = Entity.Null;
				foreach (PrefabGUID prefabGUID in Core._returnBuffs)
				{
					DynamicBuffer<HealOnGameplayEvent> dynamicBuffer;
					if (Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGUID, ref @null) && @null.TryGetBuffer(out dynamicBuffer))
					{
						HealOnGameplayEvent healOnGameplayEvent = dynamicBuffer[0];
						healOnGameplayEvent.showSCT = false;
						dynamicBuffer[0] = healOnGameplayEvent;
					}
				}
				DynamicBuffer<BuffByItemCategoryCount> buffer;
				if (Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.CHAR_VampireMale, ref @null) && @null.TryGetBuffer(out buffer) && buffer.IsIndexWithinRange(1) && buffer[1].ItemCategory.Equals(16384L))
				{
					buffer.RemoveAt(1);
				}
			}
			Entity entity2;
			if (Core._shouldApplyBonusStats && Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(Buffs.BonusStatsBuff, ref entity2))
			{
				entity2.Add<ScriptSpawn>();
				entity2.Add<BloodBuffScript_Scholar_MovementSpeedOnCast>();
				DynamicBuffer<ModifyUnitStatBuff_DOTS> dynamicBuffer2;
				if (entity2.TryGetBuffer(out dynamicBuffer2))
				{
					dynamicBuffer2.Clear();
				}
			}
			if (ConfigService.BearFormDash)
			{
				foreach (PrefabGUID prefabGUID2 in Core._bearFormBuffs)
				{
					Entity entity3;
					DynamicBuffer<ReplaceAbilityOnSlotBuff> dynamicBuffer3;
					if (Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGUID2, ref entity3) && entity3.TryGetBuffer(out dynamicBuffer3))
					{
						ReplaceAbilityOnSlotBuff replaceAbilityOnSlotBuff = dynamicBuffer3[4];
						replaceAbilityOnSlotBuff.NewGroupId = PrefabGUIDs.AB_Shapeshift_Bear_Dash_Group;
						dynamicBuffer3[4] = replaceAbilityOnSlotBuff;
					}
				}
			}
			if (ConfigService.EliteShardBearers)
			{
				foreach (PrefabGUID prefabGUID3 in Core._shardBearerDropTables)
				{
					Entity entity4;
					DynamicBuffer<DropTableDataBuffer> dynamicBuffer4;
					if (Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGUID3, ref entity4) && entity4.TryGetBuffer(out dynamicBuffer4) && !dynamicBuffer4.IsEmpty)
					{
						DropTableDataBuffer dropTableDataBuffer = dynamicBuffer4[0];
						dropTableDataBuffer.DropRate = 0.6f;
						dynamicBuffer4.Add(dropTableDataBuffer);
						dropTableDataBuffer.DropRate = 0.45f;
						dynamicBuffer4.Add(dropTableDataBuffer);
						dropTableDataBuffer.DropRate = 0.3f;
						dynamicBuffer4.Add(dropTableDataBuffer);
					}
				}
			}
			if (Core.BleedingEdge.Any<WeaponType>())
			{
				Entity entity5;
				if (Core.BleedingEdge.Contains(WeaponType.Slashers) && Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(Buffs.VargulfBleedBuff, ref entity5))
				{
					entity5.With(delegate(ref Buff buff)
					{
						buff.MaxStacks = 3;
						buff.IncreaseStacks = true;
					});
				}
				if (Core.BleedingEdge.Contains(WeaponType.Crossbow) || Core.BleedingEdge.Contains(WeaponType.Pistols)) || Core.BleedingEdge.Contains(WeaponType.Longbow))
				{
					ComponentType[] allTypes = new ComponentType[]
					{
						ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
						ComponentType.ReadOnly(Il2CppType.Of<Projectile>()),
						ComponentType.ReadOnly(Il2CppType.Of<LifeTime>()),
						ComponentType.ReadOnly(Il2CppType.Of<Velocity>())
					};
					Core.BleedingEdgePrimaryProjectileRoutine(Core.EntityManager.CreateQueryDesc(allTypes, null, null, new int[1], new EntityQueryOptions?(195))).Start();
				}
			}
			Entity entity6;
			if (ConfigService.TwilightArsenal && Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Axe_T09_ShadowMatter, ref entity6))
			{
				entity6.With(delegate(ref EquippableData equippableData)
				{
					equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_DualHammers_Ability03;
				});
			}
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002DFC File Offset: 0x00000FFC
		private static void ResetShardBearers()
		{
			ComponentType[] all = new ComponentType[]
			{
				ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
				ComponentType.ReadOnly(Il2CppType.Of<VBloodConsumeSource>()),
				ComponentType.ReadOnly(Il2CppType.Of<VBloodUnit>())
			};
			using (NativeAccessor<Entity> nativeAccessor = Core.EntityManager.BuildEntityQuery(all, 2).ToEntityArrayAccessor(2))
			{
				try
				{
					foreach (Entity entity in nativeAccessor)
					{
						PrefabGUID item;
						if (entity.TryGetComponent(out item) && Core._shardBearers.Contains(item))
						{
							entity.Destroy(false);
						}
					}
				}
				catch (Exception ex)
				{
					ManualLogSource log = Core.Log;
					bool flag;
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(27, 1, ref flag);
					if (flag)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[ResetShardBearers] error: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted<Exception>(ex);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002EF4 File Offset: 0x000010F4
		private static IEnumerator BleedingEdgePrimaryProjectileRoutine(EntityQueries.QueryDesc projectileQueryDesc)
		{
			bool pistols = Core.BleedingEdge.Contains(WeaponType.Pistols);
			bool crossbow = Core.BleedingEdge.Contains(WeaponType.Crossbow);
			bool longbow = Core.BleedingEdge.Contains(WeaponType.Longbow);
			yield return EntityQueries.QueryResultStreamAsync(projectileQueryDesc, delegate(EntityQueries.QueryResultStream stream)
			{
				try
				{
					using (stream)
					{
						foreach (EntityQueries.QueryResult queryResult in stream.GetResults())
						{
							Entity entity = queryResult.Entity;
							string prefabName = queryResult.ResolveComponentData<PrefabGUID>().GetPrefabName();
							if (pistols && Core.IsWeaponPrimaryProjectile(prefabName, WeaponType.Pistols))
							{
								entity.With(delegate(ref Projectile projectile)
								{
									projectile.Speed = 100f;
									projectile.Range *= 1.25f;
								});
								entity.HasWith(delegate(ref LifeTime lifeTime)
								{
									lifeTime.Duration *= 1.25f;
								});
							}
							else if (crossbow && Core.IsWeaponPrimaryProjectile(prefabName, WeaponType.Crossbow))
							{
								entity.With(delegate(ref Projectile projectile)
								{
									projectile.Speed = 100f;
								});
							else if (longbow && Core.IsWeaponPrimaryProjectile(prefabName, WeaponType.Longbow))
							{
								entity.With(delegate(ref Projectile projectile)
								{
									projectile.Speed = 100f;
								});
							}
						}
					}
				}
				catch (Exception ex)
				{
					ManualLogSource log = Core.Log;
					bool flag;
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(41, 1, ref flag);
					if (flag)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[BleedingEdgePrimaryProjectileRoutine] - ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted<Exception>(ex);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			});
			yield break;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002F03 File Offset: 0x00001103
		private static bool IsWeaponPrimaryProjectile(string prefabName, WeaponType weaponType)
		{
			return prefabName.ContainsAll(new List<string>
			{
				weaponType.ToString(),
				"Primary",
				"Projectile"
			});
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002F3C File Offset: 0x0000113C
		public static void LogEntity(World world, Entity entity)
		{
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				EntityDebuggingUtility.DumpEntity(world, entity, true, stringBuilder);
				ManualLogSource log = Core.Log;
				bool flag;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(13, 1, ref flag);
				if (flag)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Entity Dump:\n");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>(stringBuilder.ToString());
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
			catch (Exception ex)
			{
				ManualLogSource log2 = Core.Log;
				bool flag;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(22, 1, ref flag);
				if (flag)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("Error dumping entity: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted<string>(ex.Message);
				}
				log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002FD4 File Offset: 0x000011D4
		// Note: this type is marked as 'beforefieldinit'.
		static Core()
		{
			World serverWorld = Core.GetServerWorld();
			if (serverWorld == null)
			{
				throw new Exception("There is no Server world!");
			}
			Core.Server = serverWorld;
			Core.SystemService = new SystemService(Core.Server);
			Core._returnBuffs = new List<PrefabGUID>
			{
				PrefabGUIDs.Buff_Shared_Return,
				PrefabGUIDs.Buff_Shared_Return_NoInvulernable,
				PrefabGUIDs.Buff_Vampire_BloodKnight_Return,
				PrefabGUIDs.Buff_Vampire_Dracula_Return,
				PrefabGUIDs.Buff_Dracula_Return,
				PrefabGUIDs.Buff_WerewolfChieftain_Return,
				PrefabGUIDs.Buff_Werewolf_Return,
				PrefabGUIDs.Buff_Monster_Return,
				PrefabGUIDs.Buff_Purifier_Return,
				PrefabGUIDs.Buff_Blackfang_Morgana_Return,
				PrefabGUIDs.Buff_ChurchOfLight_Paladin_Return,
				PrefabGUIDs.Buff_Gloomrot_Voltage_Return,
				PrefabGUIDs.Buff_Militia_Fabian_Return
			};
			Core._bearFormBuffs = new List<PrefabGUID>
			{
				PrefabGUIDs.AB_Shapeshift_Bear_Buff,
				PrefabGUIDs.AB_Shapeshift_Bear_Skin01_Buff
			};
			Core._shardBearerDropTables = new List<PrefabGUID>
			{
				PrefabGUIDs.DT_Unit_Relic_Manticore_Unique,
				PrefabGUIDs.DT_Unit_Relic_Paladin_Unique,
				PrefabGUIDs.DT_Unit_Relic_Monster_Unique,
				PrefabGUIDs.DT_Unit_Relic_Dracula_Unique,
				PrefabGUIDs.DT_Unit_Relic_Morgana_Unique
			};
			Core._legacies = ConfigService.LegacySystem;
			Core._expertise = ConfigService.ExpertiseSystem;
			Core._classes = ConfigService.ClassSystem;
			Core._familiars = ConfigService.FamiliarSystem;
			Core._resetShardBearers = ConfigService.EliteShardBearers;
			Core._shouldApplyBonusStats = (Core._legacies || Core._expertise || Core._classes || Core._familiars);
			Core._bleedingEdge = new HashSet<WeaponType>();
			Core._initialized = false;
			Core._shardBearers = new HashSet<PrefabGUID>
			{
				PrefabGUIDs.CHAR_Manticore_VBlood,
				PrefabGUIDs.CHAR_ChurchOfLight_Paladin_VBlood,
				PrefabGUIDs.CHAR_Gloomrot_Monster_VBlood,
				PrefabGUIDs.CHAR_Vampire_Dracula_VBlood,
				PrefabGUIDs.CHAR_Blackfang_Morgana_VBlood
			};
		}

		// Token: 0x0400000F RID: 15
		private static MonoBehaviour _monoBehaviour;

		// Token: 0x04000010 RID: 16
		private static readonly List<PrefabGUID> _returnBuffs;

		// Token: 0x04000011 RID: 17
		private static readonly List<PrefabGUID> _bearFormBuffs;

		// Token: 0x04000012 RID: 18
		private static readonly List<PrefabGUID> _shardBearerDropTables;

		// Token: 0x04000013 RID: 19
		private static readonly bool _legacies;

		// Token: 0x04000014 RID: 20
		private static readonly bool _expertise;

		// Token: 0x04000015 RID: 21
		private static readonly bool _classes;

		// Token: 0x04000016 RID: 22
		private static readonly bool _familiars;

		// Token: 0x04000017 RID: 23
		private static readonly bool _resetShardBearers;

		// Token: 0x04000018 RID: 24
		private static readonly bool _shouldApplyBonusStats;

		// Token: 0x04000019 RID: 25
		private static HashSet<WeaponType> _bleedingEdge;

		// Token: 0x0400001A RID: 26
		private const int SECONDARY_SKILL_SLOT = 4;

		// Token: 0x0400001B RID: 27
		private const int BLEED_STACKS = 3;

		// Token: 0x0400001D RID: 29
		public static bool _initialized;

		// Token: 0x0400001E RID: 30
		private static readonly HashSet<PrefabGUID> _shardBearers;

		// Token: 0x020000C7 RID: 199
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x040060B4 RID: 24756
			public static EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> <0>__OnUpdate;

			// Token: 0x040060B5 RID: 24757
			public static EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> <1>__OnUpdate;

			// Token: 0x040060B6 RID: 24758
			public static EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> <2>__OnUpdate;

			// Token: 0x040060B7 RID: 24759
			public static EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> <3>__OnUpdate;

			// Token: 0x040060B8 RID: 24760
			public static EventHandler<DeathEventListenerSystemPatch.DeathEventArgs> <4>__OnUpdate;
		}
	}
}
