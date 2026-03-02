using NUnit.Framework;
using TomatoFighters.Combat;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Combat.Combo
{
    [TestFixture]
    public class ComboStateMachineTests
    {
        private ComboStateMachine sm;
        private ComboDefinition definition;

        /// <summary>
        /// Build a test combo tree:
        ///   [0] L → nextL=1, nextH=3
        ///   [1] L → nextL=2, nextH=4
        ///   [2] L finisher (sweep)
        ///   [3] H → nextL=-1, nextH=5
        ///   [4] H finisher (slam)
        ///   [5] H finisher (heavy ender)
        ///   [6] H (root heavy) → nextL=-1, nextH=5
        /// </summary>
        private static ComboDefinition CreateTestDefinition()
        {
            var def = ScriptableObject.CreateInstance<ComboDefinition>();
            def.defaultComboWindow = 0.4f;
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 6;
            def.steps = new ComboStep[]
            {
                // [0] L1 — no cancel
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light1",
                    damageMultiplier = 1.0f,
                    comboWindowDuration = 0f,
                    nextOnLight = 1,
                    nextOnHeavy = 3,
                    canDashCancelOnHit = false,
                    canJumpCancelOnHit = false,
                    isFinisher = false
                },
                // [1] L2 — dash + jump cancel on hit
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light2",
                    damageMultiplier = 1.0f,
                    comboWindowDuration = 0f,
                    nextOnLight = 2,
                    nextOnHeavy = 4,
                    canDashCancelOnHit = true,
                    canJumpCancelOnHit = true,
                    isFinisher = false
                },
                // [2] L3 finisher (sweep)
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "LightFinisher",
                    damageMultiplier = 1.5f,
                    comboWindowDuration = 0f,
                    nextOnLight = -1,
                    nextOnHeavy = -1,
                    isFinisher = true
                },
                // [3] L→L→H (launcher)
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "Launcher",
                    damageMultiplier = 1.3f,
                    comboWindowDuration = 0f,
                    nextOnLight = -1,
                    nextOnHeavy = 5,
                    isFinisher = false
                },
                // [4] L→L→H finisher (slam)
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "HeavySlam",
                    damageMultiplier = 2.0f,
                    comboWindowDuration = 0f,
                    nextOnLight = -1,
                    nextOnHeavy = -1,
                    isFinisher = true
                },
                // [5] Heavy ender finisher
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "HeavyFinisher",
                    damageMultiplier = 1.8f,
                    comboWindowDuration = 0f,
                    nextOnLight = -1,
                    nextOnHeavy = -1,
                    isFinisher = true
                },
                // [6] H1 (root heavy)
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "Heavy1",
                    damageMultiplier = 1.5f,
                    comboWindowDuration = 0.5f,
                    nextOnLight = -1,
                    nextOnHeavy = 5,
                    isFinisher = false
                },
            };
            return def;
        }

        [SetUp]
        public void SetUp()
        {
            sm = new ComboStateMachine();
            definition = CreateTestDefinition();
            sm.SetDefinition(definition);
        }

        [TearDown]
        public void TearDown()
        {
            if (definition != null)
                Object.DestroyImmediate(definition);
        }

        // --- Initial State ---

        [Test]
        public void StartsInIdleState()
        {
            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
        }

        [Test]
        public void StartsWithNoStep()
        {
            Assert.AreEqual(-1, sm.CurrentStepIndex);
        }

        [Test]
        public void StartsWithZeroComboLength()
        {
            Assert.AreEqual(0, sm.ComboLength);
        }

        // --- Starting a Combo ---

        [Test]
        public void LightInput_FromIdle_TransitionsToAttacking()
        {
            sm.ReceiveInput(AttackType.Light);

            Assert.AreEqual(ComboState.Attacking, sm.CurrentState);
        }

        [Test]
        public void LightInput_FromIdle_SetsStepToRootLight()
        {
            sm.ReceiveInput(AttackType.Light);

            Assert.AreEqual(0, sm.CurrentStepIndex);
        }

        [Test]
        public void HeavyInput_FromIdle_SetsStepToRootHeavy()
        {
            sm.ReceiveInput(AttackType.Heavy);

            Assert.AreEqual(6, sm.CurrentStepIndex);
        }

        [Test]
        public void StartingCombo_SetsComboLengthToOne()
        {
            sm.ReceiveInput(AttackType.Light);

            Assert.AreEqual(1, sm.ComboLength);
        }

        [Test]
        public void StartingCombo_FiresStepStartedEvent()
        {
            int firedIndex = -1;
            sm.StepStarted += idx => firedIndex = idx;

            sm.ReceiveInput(AttackType.Light);

            Assert.AreEqual(0, firedIndex);
        }

        // --- Input Buffering ---

        [Test]
        public void InputDuringAttacking_IsBuffered()
        {
            sm.ReceiveInput(AttackType.Light);

            sm.ReceiveInput(AttackType.Light);

            Assert.AreEqual(AttackType.Light, sm.BufferedInput);
        }

        [Test]
        public void BufferedInput_ConsumedOnComboWindowOpen()
        {
            int lastStepFired = -1;
            sm.StepStarted += idx => lastStepFired = idx;

            sm.ReceiveInput(AttackType.Light);    // step 0, Attacking
            sm.ReceiveInput(AttackType.Light);    // buffered
            sm.OnComboWindowOpen();               // consumes buffer → step 1

            Assert.AreEqual(1, lastStepFired);
            Assert.AreEqual(ComboState.Attacking, sm.CurrentState);
            Assert.IsNull(sm.BufferedInput);
        }

        [Test]
        public void BufferedInput_OverwritesPreviousBuffer()
        {
            sm.ReceiveInput(AttackType.Light);    // step 0, Attacking
            sm.ReceiveInput(AttackType.Light);    // buffered Light
            sm.ReceiveInput(AttackType.Heavy);    // overwrites to Heavy

            Assert.AreEqual(AttackType.Heavy, sm.BufferedInput);
        }

        // --- Combo Window ---

        [Test]
        public void OnComboWindowOpen_TransitionsToComboWindow_WhenNoBuffer()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();

            Assert.AreEqual(ComboState.ComboWindow, sm.CurrentState);
        }

        [Test]
        public void InputDuringComboWindow_AdvancesToNextStep()
        {
            sm.ReceiveInput(AttackType.Light);    // step 0
            sm.OnComboWindowOpen();               // ComboWindow
            sm.ReceiveInput(AttackType.Light);    // → step 1

            Assert.AreEqual(1, sm.CurrentStepIndex);
            Assert.AreEqual(2, sm.ComboLength);
            Assert.AreEqual(ComboState.Attacking, sm.CurrentState);
        }

        [Test]
        public void ComboWindow_ExpiresAfterTimeout()
        {
            bool dropped = false;
            sm.ComboDropped += () => dropped = true;

            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.Tick(0.5f); // default window is 0.4s

            Assert.IsTrue(dropped);
            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
        }

        [Test]
        public void ComboWindow_DoesNotExpireBeforeTimeout()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.Tick(0.2f); // half the default window

            Assert.AreEqual(ComboState.ComboWindow, sm.CurrentState);
        }

        [Test]
        public void ComboWindow_UsesStepOverrideDuration()
        {
            // step 6 (root heavy) has comboWindowDuration = 0.5
            sm.ReceiveInput(AttackType.Heavy);    // step 6
            sm.OnComboWindowOpen();
            sm.Tick(0.45f); // past default 0.4 but within 0.5 override

            Assert.AreEqual(ComboState.ComboWindow, sm.CurrentState);
        }

        // --- Branching ---

        [Test]
        public void LightLightLight_ReachesLightFinisher()
        {
            int finisherLength = 0;
            sm.FinisherTriggered += len => finisherLength = len;

            sm.ReceiveInput(AttackType.Light);    // step 0
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);    // step 1
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);    // step 2 (finisher)

            Assert.AreEqual(ComboState.Finisher, sm.CurrentState);
            Assert.AreEqual(2, sm.CurrentStepIndex);
            Assert.AreEqual(3, finisherLength);
        }

        [Test]
        public void LightHeavy_BranchesToLauncher()
        {
            sm.ReceiveInput(AttackType.Light);    // step 0
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Heavy);    // step 3 (launcher)

            Assert.AreEqual(3, sm.CurrentStepIndex);
            Assert.AreEqual(ComboState.Attacking, sm.CurrentState);
        }

        [Test]
        public void LightLightHeavy_BranchesToSlamFinisher()
        {
            int finisherLength = 0;
            sm.FinisherTriggered += len => finisherLength = len;

            sm.ReceiveInput(AttackType.Light);    // step 0
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);    // step 1
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Heavy);    // step 4 (slam finisher)

            Assert.AreEqual(ComboState.Finisher, sm.CurrentState);
            Assert.AreEqual(4, sm.CurrentStepIndex);
            Assert.AreEqual(3, finisherLength);
        }

        [Test]
        public void HeavyHeavy_ReachesHeavyFinisher()
        {
            int finisherLength = 0;
            sm.FinisherTriggered += len => finisherLength = len;

            sm.ReceiveInput(AttackType.Heavy);    // step 6
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Heavy);    // step 5 (heavy finisher)

            Assert.AreEqual(ComboState.Finisher, sm.CurrentState);
            Assert.AreEqual(5, sm.CurrentStepIndex);
            Assert.AreEqual(2, finisherLength);
        }

        // --- Dead-End Branches ---

        [Test]
        public void InvalidBranch_DropsCombo()
        {
            bool dropped = false;
            sm.ComboDropped += () => dropped = true;

            sm.ReceiveInput(AttackType.Heavy);    // step 6
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);    // step 6 nextOnLight = -1

            Assert.IsTrue(dropped);
            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
        }

        // --- Finisher ---

        [Test]
        public void InputDuringFinisher_IsIgnored()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);    // finisher

            sm.ReceiveInput(AttackType.Light);    // should be ignored

            Assert.AreEqual(ComboState.Finisher, sm.CurrentState);
            Assert.IsNull(sm.BufferedInput);
        }

        [Test]
        public void OnFinisherEnd_ResetsToIdle()
        {
            bool ended = false;
            sm.FinisherEnded += () => ended = true;

            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);    // finisher

            sm.OnFinisherEnd();

            Assert.IsTrue(ended);
            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
            Assert.AreEqual(0, sm.ComboLength);
        }

        // --- Reset ---

        [Test]
        public void Reset_ClearsAllState()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.ReceiveInput(AttackType.Heavy); // buffer

            sm.Reset();

            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
            Assert.AreEqual(-1, sm.CurrentStepIndex);
            Assert.AreEqual(0, sm.ComboLength);
            Assert.IsNull(sm.BufferedInput);
        }

        // --- Tick Doesn't Affect Non-ComboWindow States ---

        [Test]
        public void Tick_DuringIdle_DoesNothing()
        {
            sm.Tick(10f);

            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
        }

        [Test]
        public void Tick_DuringAttacking_DoesNothing()
        {
            sm.ReceiveInput(AttackType.Light);

            sm.Tick(10f);

            Assert.AreEqual(ComboState.Attacking, sm.CurrentState);
        }

        [Test]
        public void Tick_DuringFinisher_DoesNothing()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light); // finisher

            sm.Tick(10f);

            Assert.AreEqual(ComboState.Finisher, sm.CurrentState);
        }

        // --- OnComboWindowOpen Guard ---

        [Test]
        public void OnComboWindowOpen_WhenNotAttacking_DoesNothing()
        {
            sm.OnComboWindowOpen(); // called in Idle — should be ignored

            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
        }

        // --- OnFinisherEnd Guard ---

        [Test]
        public void OnFinisherEnd_WhenNotFinisher_DoesNothing()
        {
            sm.ReceiveInput(AttackType.Light);

            sm.OnFinisherEnd(); // called in Attacking — should be ignored

            Assert.AreEqual(ComboState.Attacking, sm.CurrentState);
        }

        // --- Hit-Confirm ---

        [Test]
        public void HitConfirmed_DefaultsFalse()
        {
            sm.ReceiveInput(AttackType.Light);

            Assert.IsFalse(sm.HitConfirmed);
        }

        [Test]
        public void OnHitConfirmed_SetsFlag_DuringAttacking()
        {
            sm.ReceiveInput(AttackType.Light);

            sm.OnHitConfirmed();

            Assert.IsTrue(sm.HitConfirmed);
        }

        [Test]
        public void OnHitConfirmed_SetsFlag_DuringComboWindow()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();

            sm.OnHitConfirmed();

            Assert.IsTrue(sm.HitConfirmed);
        }

        [Test]
        public void OnHitConfirmed_Ignored_DuringIdle()
        {
            sm.OnHitConfirmed();

            Assert.IsFalse(sm.HitConfirmed);
        }

        [Test]
        public void OnHitConfirmed_Ignored_DuringFinisher()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light); // finisher

            sm.OnHitConfirmed();

            Assert.IsFalse(sm.HitConfirmed);
        }

        [Test]
        public void HitConfirmed_ClearedOnReset()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnHitConfirmed();

            sm.Reset();

            Assert.IsFalse(sm.HitConfirmed);
        }

        // --- Cancel Properties ---

        [Test]
        public void CanDashCancel_False_WithoutHitConfirm()
        {
            // Step 1 has canDashCancelOnHit = true (set in test definition)
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light); // step 1

            Assert.IsFalse(sm.CanDashCancel);
        }

        [Test]
        public void CanDashCancel_True_WithHitConfirm_OnCancelableStep()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light); // step 1 (canDashCancelOnHit = true)
            sm.OnHitConfirmed();

            Assert.IsTrue(sm.CanDashCancel);
        }

        [Test]
        public void CanDashCancel_False_OnNonCancelableStep()
        {
            sm.ReceiveInput(AttackType.Light); // step 0 (canDashCancelOnHit = false)
            sm.OnHitConfirmed();

            Assert.IsFalse(sm.CanDashCancel);
        }

        [Test]
        public void CanJumpCancel_True_WithHitConfirm_OnJumpCancelableStep()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.ReceiveInput(AttackType.Light); // step 1 (canJumpCancelOnHit = true)
            sm.OnHitConfirmed();

            Assert.IsTrue(sm.CanJumpCancel);
        }

        [Test]
        public void CanJumpCancel_False_OnNonJumpCancelableStep()
        {
            sm.ReceiveInput(AttackType.Light); // step 0 (canJumpCancelOnHit = false)
            sm.OnHitConfirmed();

            Assert.IsFalse(sm.CanJumpCancel);
        }

        // --- CancelPerformed ---

        [Test]
        public void CancelPerformed_ResetsToIdle()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnHitConfirmed();

            sm.CancelPerformed();

            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
            Assert.AreEqual(0, sm.ComboLength);
            Assert.IsFalse(sm.HitConfirmed);
        }

        [Test]
        public void CancelPerformed_DuringIdle_DoesNothing()
        {
            sm.CancelPerformed(); // should not crash or change state

            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
        }

        [Test]
        public void CancelPerformed_DuringComboWindow_Resets()
        {
            sm.ReceiveInput(AttackType.Light);
            sm.OnComboWindowOpen();
            sm.OnHitConfirmed();

            sm.CancelPerformed();

            Assert.AreEqual(ComboState.Idle, sm.CurrentState);
        }
    }
}
