using System.Collections;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat.Diagnostics
{
    /// <summary>
    /// Automated clash timing test for the test scene.
    /// Watches for enemy telegraph, fires player heavy attack at the correct time,
    /// and shows the clash/hit result. Can cycle through timing offsets to demonstrate
    /// which delays produce clashes and which produce hits.
    ///
    /// <para><b>How to use:</b> Press <b>T</b> to toggle auto-fire.
    /// Press <b>Y</b> to cycle through timing offsets. Watch console and on-screen
    /// text for clash results.</para>
    ///
    /// <para><b>Timing reference (Slasher vs TestDummy):</b></para>
    /// <list type="bullet">
    ///   <item>Enemy telegraph: 1.0s → opens enemy clash window for 1.0s</item>
    ///   <item>Press heavy at 0-0.65s into telegraph → player hitbox hits during enemy clash → CLASH</item>
    ///   <item>Press heavy at 0.65-1.0s into telegraph → enemy hitbox hits during player clash → CLASH</item>
    ///   <item>Press heavy after 1.0s → too late → HIT (player takes damage)</item>
    /// </list>
    /// </summary>
    public class ClashTimingAutoTest : MonoBehaviour
    {
        [Header("References — auto-detected if null")]
        [SerializeField] private ComboController playerCombo;
        [SerializeField] private DefenseSystem playerDefense;

        [Header("Timing")]
        [Tooltip("Seconds after enemy telegraph starts to fire player heavy attack.\n" +
                 "0-0.65 = early clash (player hitbox during enemy window)\n" +
                 "0.65-1.0 = late clash (enemy hitbox during player window)\n" +
                 "> 1.0 = too late (takes hit)")]
        [Range(0f, 2f)]
        [SerializeField] private float heavyAttackDelay = 0.3f;

        [Header("Mode")]
        [Tooltip("Auto-fire heavy attack on each enemy telegraph.")]
        [SerializeField] private bool autoFire = true;

        [Tooltip("Cycle through test delays to show the timing window.")]
        [SerializeField] private bool cycleTimings;

        // Timing offsets for cycle mode: covers early clash, late clash, and miss
        private static readonly float[] TEST_DELAYS =
            { 0.1f, 0.3f, 0.5f, 0.65f, 0.8f, 0.95f, 1.1f, 1.5f };

        private IAttacker _enemyAttacker;
        private DefenseSystem _enemyDefense;
        private PlayerDamageable _playerDamageable;
        private int _cycleIndex;
        private int _clashCount;
        private int _hitCount;
        private float _playerHealthBefore;
        private TextMesh _statusText;
        private TextMesh _timingGuideText;
        private Coroutine _watchLoop;

        private void Start()
        {
            AutoDetectReferences();
            CreateUI();

            if (_enemyAttacker == null)
            {
                UpdateStatus("ERROR: No enemy found in scene!", Color.red);
                enabled = false;
                return;
            }

            SubscribeToEvents();
            _watchLoop = StartCoroutine(WatchForTelegraph());
            UpdateStatus("Waiting for enemy telegraph... (T=toggle auto, Y=cycle timings)");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                autoFire = !autoFire;
                UpdateStatus($"Auto-fire: {(autoFire ? "ON" : "OFF")}");
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                cycleTimings = !cycleTimings;
                UpdateStatus($"Cycle timings: {(cycleTimings ? "ON" : "OFF")}");
            }
        }

        private void OnDestroy()
        {
            if (_watchLoop != null)
                StopCoroutine(_watchLoop);

            UnsubscribeFromEvents();
        }

        // ── Telegraph Detection + Auto-Fire ─────────────────────────────

        private IEnumerator WatchForTelegraph()
        {
            while (true)
            {
                // Wait for enemy to enter clash window (telegraph start)
                yield return new WaitUntil(() => _enemyAttacker != null && _enemyAttacker.IsInClashWindow);

                float delay = cycleTimings
                    ? TEST_DELAYS[_cycleIndex % TEST_DELAYS.Length]
                    : heavyAttackDelay;

                UpdateTimingGuide(delay);
                Debug.Log(
                    $"[ClashTimingTest] Telegraph detected! Firing heavy in {delay:F2}s");

                // Snapshot player health before the exchange
                if (_playerDamageable != null)
                    _playerHealthBefore = _playerDamageable.CurrentHealth;

                if (autoFire)
                {
                    yield return new WaitForSeconds(delay);

                    if (playerCombo != null && !playerCombo.IsComboActive)
                    {
                        playerCombo.RequestHeavyAttack();
                        bool enemyStillInClash = _enemyAttacker?.IsInClashWindow ?? false;
                        Debug.Log(
                            $"[ClashTimingTest] Heavy FIRED at {delay:F2}s delay. " +
                            $"Enemy still in clash window: {enemyStillInClash}");
                        UpdateStatus($"Heavy fired at {delay:F2}s | Enemy clash: {enemyStillInClash}");
                    }
                    else
                    {
                        Debug.Log(
                            "[ClashTimingTest] Skipped — player combo already active");
                    }
                }

                // Wait for the full exchange to resolve
                yield return new WaitForSeconds(2.5f);

                // Check result: did the player take damage?
                bool playerTookDamage = _playerDamageable != null &&
                    _playerDamageable.CurrentHealth < _playerHealthBefore;

                if (playerTookDamage)
                    _hitCount++;

                string resultLabel = playerTookDamage ? "TOOK HIT" : "NO DAMAGE";
                string resultColor = playerTookDamage ? "red" : "green";

                UpdateStatus(
                    $"Delay: {delay:F2}s → {resultLabel} | " +
                    $"Clashes: {_clashCount} | Hits: {_hitCount}");

                ShowFloatingText(
                    playerTookDamage ? "HIT!" : "CLASHED!",
                    playerTookDamage ? Color.red : Color.yellow);

                Debug.Log(
                    $"[ClashTimingTest] Result: delay={delay:F2}s → {resultLabel}. " +
                    $"Clashes={_clashCount}, Hits={_hitCount}");

                if (cycleTimings)
                    _cycleIndex++;

                // Wait until the enemy resets before looking for the next telegraph
                yield return new WaitUntil(() =>
                    _enemyAttacker == null || !_enemyAttacker.IsInClashWindow);
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ── Event Subscriptions ─────────────────────────────────────────

        private void SubscribeToEvents()
        {
            // Player's DefenseSystem fires OnClash when enemy hits player → Clashed
            if (playerDefense != null)
            {
                playerDefense.OnClash += OnPlayerClashed;
                playerDefense.OnDeflect += _ => ShowFloatingText("DEFLECT", Color.green);
                playerDefense.OnDodge += _ => ShowFloatingText("DODGE", Color.cyan);
            }

            // Enemy's DefenseSystem fires OnClash when player hits enemy → Clashed
            if (_enemyDefense != null)
            {
                _enemyDefense.OnClash += OnEnemyClashed;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (playerDefense != null)
                playerDefense.OnClash -= OnPlayerClashed;

            if (_enemyDefense != null)
                _enemyDefense.OnClash -= OnEnemyClashed;
        }

        private void OnPlayerClashed(ClashEventData data)
        {
            _clashCount++;
            Debug.Log($"[ClashTimingTest] Player CLASHED (enemy hit resolved as clash)");
        }

        private void OnEnemyClashed(ClashEventData data)
        {
            _clashCount++;
            Debug.Log($"[ClashTimingTest] Enemy CLASHED (player hit resolved as clash)");
        }

        // ── Reference Detection ─────────────────────────────────────────

        private void AutoDetectReferences()
        {
            if (playerCombo == null)
                playerCombo = FindFirstObjectByType<ComboController>();

            if (playerDefense == null && playerCombo != null)
                playerDefense = playerCombo.GetComponent<DefenseSystem>();

            _playerDamageable = playerCombo != null
                ? playerCombo.GetComponent<PlayerDamageable>()
                : FindFirstObjectByType<PlayerDamageable>();

            // Find enemy via IAttacker (Shared interface) to avoid cross-pillar import
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IAttacker attacker && mb.GetComponent<ComboController>() == null)
                {
                    // It's an IAttacker but not the player (player has ComboController)
                    _enemyAttacker = attacker;
                    _enemyDefense = mb.GetComponent<DefenseSystem>();
                    break;
                }
            }
        }

        // ── UI ──────────────────────────────────────────────────────────

        private void CreateUI()
        {
            // Status bar at bottom
            var statusGO = new GameObject("ClashTest_Status");
            _statusText = statusGO.AddComponent<TextMesh>();
            _statusText.fontSize = 24;
            _statusText.characterSize = 0.1f;
            _statusText.anchor = TextAnchor.LowerCenter;
            _statusText.alignment = TextAlignment.Center;
            _statusText.color = Color.white;
            statusGO.transform.position = new Vector3(0f, -4.5f, 0f);

            // Timing guide at top-right
            var guideGO = new GameObject("ClashTest_TimingGuide");
            _timingGuideText = guideGO.AddComponent<TextMesh>();
            _timingGuideText.fontSize = 20;
            _timingGuideText.characterSize = 0.08f;
            _timingGuideText.anchor = TextAnchor.UpperRight;
            _timingGuideText.alignment = TextAlignment.Right;
            _timingGuideText.color = new Color(1f, 1f, 0.6f, 0.8f);
            guideGO.transform.position = new Vector3(9f, 4f, 0f);

            _timingGuideText.text =
                "=== CLASH TIMING ===\n" +
                "Telegraph: 1.0s\n" +
                "0-0.65s = early clash\n" +
                "0.65-1.0s = late clash\n" +
                "> 1.0s = TOO LATE\n" +
                "\n" +
                "T = toggle auto-fire\n" +
                "Y = cycle timings";
        }

        private void UpdateStatus(string text, Color? color = null)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
                _statusText.color = color ?? Color.white;
            }
        }

        private void UpdateTimingGuide(float currentDelay)
        {
            if (_timingGuideText == null) return;

            string verdict;
            Color col;
            if (currentDelay <= 0.65f)
            {
                verdict = "EARLY CLASH";
                col = Color.green;
            }
            else if (currentDelay <= 1.0f)
            {
                verdict = "LATE CLASH";
                col = Color.yellow;
            }
            else
            {
                verdict = "TOO LATE";
                col = Color.red;
            }

            _timingGuideText.color = col;
            _timingGuideText.text =
                $"=== CLASH TIMING ===\n" +
                $"Current delay: {currentDelay:F2}s\n" +
                $"Prediction: {verdict}\n" +
                $"\n" +
                $"Telegraph: 1.0s\n" +
                $"0-0.65s = early clash\n" +
                $"0.65-1.0s = late clash\n" +
                $"> 1.0s = TOO LATE\n" +
                $"\n" +
                $"T = toggle auto-fire\n" +
                $"Y = cycle timings";
        }

        private void ShowFloatingText(string text, Color color)
        {
            var go = new GameObject("ClashResult");
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 36;
            tm.characterSize = 0.15f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;

            Vector3 pos = playerCombo != null
                ? playerCombo.transform.position + Vector3.up * 2f
                : new Vector3(0f, 2f, 0f);
            go.transform.position = pos;

            StartCoroutine(FloatAndFade(go, tm));
        }

        private IEnumerator FloatAndFade(GameObject go, TextMesh tm)
        {
            float elapsed = 0f;
            Vector3 startPos = go.transform.position;
            Color startColor = tm.color;

            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 1.5f;
                go.transform.position = startPos + Vector3.up * t * 1.5f;
                tm.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            Destroy(go);
        }
    }
}
