#define BURST
#define VERBOSE
#define DEBUG
#define DEBUG_VERBOSE
#define UNITY_ASSERTIONS

using Game;
using Game.Citizens;
using Game.Modding;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Colossal.Logging;
using Colossal.IO.AssetDatabase;

namespace test
{
    public class TestMod : IMod
    {
        public static ILog log = LogManager.GetLogger(nameof(TestMod), true);
        public static MySettings Settings { get; set; }
        public static MyOptions Options { get; set; }

        public void OnCreateWorld(UpdateSystem updateSystem)
        {

            Settings.BoolSetting = true;
            Options.StringSetting = "Test";
            UnityEngine.Debug.LogWarning("*****TESTMOD*****" + nameof(OnCreateWorld));
            updateSystem.UpdateAt<PrintPopulationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DeltaTimePrintSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<TestModSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            UnityEngine.Debug.Log("*****TESTMOD*****" + nameof(OnDispose));
            log.Warn(nameof(OnDispose));
        }

        public void OnLoad()
        {
            UnityEngine.Debug.Log("*****TESTMOD*****" + nameof(OnLoad));
            log.Warn(nameof(OnLoad));
            Settings = new MySettings();
            Options = new MyOptions();

            AssetDatabase.global.LoadSettings("Mod Settings Section", Settings, new MySettings());

            AssetDatabase.global.LoadSettings("Mod Options Section", Options, new MyOptions());
        
            
        }
    }

    public partial class DeltaTimePrintSystem : GameSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            TestMod.log.Warn("*****TESTMOD*****" + $"[{nameof(DeltaTimePrintSystem)}] {nameof(OnCreate)}");
        }
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            TestMod.log.Warn("*****TESTMOD*****" + $"[{nameof(DeltaTimePrintSystem)}] DeltaTime: {deltaTime}");
        }
    }

    public partial class PrintPopulationSystem : GameSystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private EntityQuery m_HouseholdQuery;

        private NativeArray<int> m_ResultArray;
        protected override void OnCreate()
        {
            base.OnCreate();

            TestMod.log.Warn("*****TESTMOD*****" + $"[{nameof(PrintPopulationSystem)}] {nameof(OnCreate)}");

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            m_HouseholdQuery = GetEntityQuery(
                ComponentType.ReadOnly<Household>(),
                ComponentType.Exclude<TouristHousehold>(),
                ComponentType.Exclude<CommuterHousehold>(),
                ComponentType.ReadOnly<Game.Buildings.PropertyRenter>(),
                ComponentType.Exclude<Game.Common.Deleted>(),
                ComponentType.Exclude<Game.Tools.Temp>()
                );

            m_ResultArray = new NativeArray<int>(1, Allocator.Persistent);
        }
        protected override void OnUpdate()
        {
            if (m_SimulationSystem.frameIndex % 128 == 75)
            {
                TestMod.log.Warn($"[{nameof(PrintPopulationSystem)}] Population: {m_ResultArray[0]}");

                var popJob = new CountPopulationJob
                {
                    m_HouseholdChunks = m_HouseholdQuery.ToArchetypeChunkArray(Allocator.TempJob),
                    m_HouseholdCitizenType = GetBufferTypeHandle<HouseholdCitizen>(true),
                    m_CommuterType = GetComponentTypeHandle<CommuterHousehold>(true),
                    m_MovingAwayType = GetComponentTypeHandle<Game.Agents.MovingAway>(true),
                    m_TouristType = GetComponentTypeHandle<TouristHousehold>(true),
                    m_HouseholdType = GetComponentTypeHandle<Household>(true),

                    m_HealthProblems = GetComponentLookup<HealthProblem>(true),
                    m_Citizens = GetComponentLookup<Citizen>(true),

                    m_Result = m_ResultArray,
                };

                Dependency = popJob.Schedule();
                CompleteDependency();
            }
        }

#if BURST
        [BurstCompile]
#endif
        public struct CountPopulationJob : IJob
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> m_HouseholdChunks;
            [ReadOnly] public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;
            [ReadOnly] public ComponentTypeHandle<TouristHousehold> m_TouristType;
            [ReadOnly] public ComponentTypeHandle<CommuterHousehold> m_CommuterType;
            [ReadOnly] public ComponentTypeHandle<Game.Agents.MovingAway> m_MovingAwayType;
            [ReadOnly] public ComponentTypeHandle<Household> m_HouseholdType;

            [ReadOnly] public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblems;

            public NativeArray<int> m_Result;

            public void Execute()
            {
#if VERBOSE
                UnityEngine.Debug.LogWarning("*****TESTMOD*****" + $"Start executing {nameof(CountPopulationJob)}");
#endif
                m_Result[0] = 0;

                for (int i = 0; i < m_HouseholdChunks.Length; ++i)
                {
                    ArchetypeChunk chunk = m_HouseholdChunks[i];
                    BufferAccessor<HouseholdCitizen> citizenBuffers = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
                    NativeArray<Household> households = chunk.GetNativeArray(ref m_HouseholdType);

                    if (chunk.Has(ref m_TouristType) || chunk.Has(ref m_CommuterType) || chunk.Has(ref m_MovingAwayType))
                        continue;

                    for (int j = 0; j < chunk.Count; ++j)
                    {
                        if ((households[j].m_Flags & HouseholdFlags.MovedIn) == 0)
                            continue;

                        DynamicBuffer<HouseholdCitizen> citizens = citizenBuffers[j];
                        for (int k = 0; k < citizens.Length; ++k)
                        {
                            Entity citizen = citizens[k].m_Citizen;
                            if (m_Citizens.HasComponent(citizen) && !CitizenUtils.IsDead(citizen, ref m_HealthProblems))
                                m_Result[0] += 1;
                        }
                    }
                }
#if VERBOSE
                UnityEngine.Debug.LogWarning("*****TESTMOD*****" + $"Finish executing {nameof(CountPopulationJob)}");
#endif
            }
        }
    }

#if BURST
    [BurstCompile]
#endif
    public partial class TestModSystem : GameSystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private NativeArray<int> m_Array;

        protected override void OnCreate()
        {
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_Array = new NativeArray<int>(5, Allocator.Persistent);
        }
        protected override void OnUpdate()
        {
            if (m_SimulationSystem.frameIndex % 128 == 75)
            {
                TestMod.log.Info(string.Join(", ", m_Array));

                var testJob = new TestJob
                {
                    m_Array = m_Array,
                };

                Dependency = testJob.Schedule();
            }
        }

#if BURST
        [BurstCompile]
#endif
        public struct TestJob : IJob
        {
            public NativeArray<int> m_Array;

            public void Execute()
            {
#if VERBOSE
                UnityEngine.Debug.LogWarning($"Start executing {nameof(TestJob)}");
#endif
                for (int i = 0; i < m_Array.Length; i += 1)
                {
                    m_Array[i] = m_Array[i] + i;
                }
#if VERBOSE
                UnityEngine.Debug.LogWarning("*****TESTMOD*****" + $"Finish executing {nameof(TestJob)}");
#endif
            }

#if BURST
            [BurstCompile]
#endif
            public static void WorkTime(in long start, in long current, out long duration)
            {
                duration = current - start;
            }
        }
    }
}
