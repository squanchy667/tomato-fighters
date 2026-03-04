using System.Reflection;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Interfaces;
using TomatoFighters.World;
using UnityEngine;

/// <summary>
/// Drop onto any scene object. On Start(), validates the entire damage pipeline
/// and dumps diagnostic info to the Console. Remove after debugging.
/// Lives outside asmdef folders so it can reference all pillar assemblies.
/// </summary>
public class DamagePipelineDiagnostic : MonoBehaviour
{
    private const string TAG = "[DIAGNOSTIC]";

    private void Start()
    {
        Debug.Log($"{TAG} ========== DAMAGE PIPELINE DIAGNOSTIC ==========");
        CheckLayerCollisionMatrix();
        CheckPlayerHitboxes();
        CheckPlayerDamageable();
        CheckEnemySetup();
        CheckComboDefinitionWiring();
        Debug.Log($"{TAG} ========== END DIAGNOSTIC ==========");
    }

    private void CheckLayerCollisionMatrix()
    {
        Debug.Log($"{TAG} --- Layer Collision Matrix ---");

        int playerHitbox = LayerMask.NameToLayer("PlayerHitbox");
        int enemyHurtbox = LayerMask.NameToLayer("EnemyHurtbox");
        int enemyHitbox = LayerMask.NameToLayer("EnemyHitbox");
        int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");

        LogLayer("PlayerHitbox", playerHitbox);
        LogLayer("EnemyHurtbox", enemyHurtbox);
        LogLayer("EnemyHitbox", enemyHitbox);
        LogLayer("PlayerHurtbox", playerHurtbox);

        if (playerHitbox < 0 || enemyHurtbox < 0 || enemyHitbox < 0 || playerHurtbox < 0)
        {
            Debug.LogError($"{TAG} MISSING LAYERS — cannot check collision matrix");
            return;
        }

        bool phVsEh = !Physics2D.GetIgnoreLayerCollision(playerHitbox, enemyHurtbox);
        bool ehVsPh = !Physics2D.GetIgnoreLayerCollision(enemyHitbox, playerHurtbox);
        bool phVsPh = !Physics2D.GetIgnoreLayerCollision(playerHitbox, playerHurtbox);
        bool ehVsEh = !Physics2D.GetIgnoreLayerCollision(enemyHitbox, enemyHurtbox);

        Debug.Log($"{TAG} PlayerHitbox({playerHitbox}) <-> EnemyHurtbox({enemyHurtbox}) = {(phVsEh ? "ENABLED OK" : "DISABLED BROKEN")}");
        Debug.Log($"{TAG} EnemyHitbox({enemyHitbox}) <-> PlayerHurtbox({playerHurtbox}) = {(ehVsPh ? "ENABLED OK" : "DISABLED BROKEN")}");
        Debug.Log($"{TAG} PlayerHitbox <-> PlayerHurtbox (same team) = {(phVsPh ? "ENABLED (wrong!)" : "DISABLED OK")}");
        Debug.Log($"{TAG} EnemyHitbox <-> EnemyHurtbox (same team) = {(ehVsEh ? "ENABLED (wrong!)" : "DISABLED OK")}");
    }

    private void LogLayer(string name, int index)
    {
        if (index < 0)
            Debug.LogError($"{TAG} Layer '{name}' NOT FOUND — add it in Project Settings > Tags and Layers");
        else
            Debug.Log($"{TAG} Layer '{name}' = index {index}");
    }

    private void CheckPlayerHitboxes()
    {
        Debug.Log($"{TAG} --- Player Hitbox Setup ---");

        var hitboxManager = FindAnyObjectByType<HitboxManager>(FindObjectsInactive.Include);
        if (hitboxManager == null)
        {
            Debug.LogError($"{TAG} No HitboxManager found in scene!");
            return;
        }

        Debug.Log($"{TAG} HitboxManager found on '{hitboxManager.gameObject.name}' (layer={hitboxManager.gameObject.layer})");

        var ccField = typeof(HitboxManager).GetField("comboController", BindingFlags.NonPublic | BindingFlags.Instance);
        var cc = ccField?.GetValue(hitboxManager) as ComboController;
        Debug.Log($"{TAG} HitboxManager.comboController = {(cc != null ? cc.gameObject.name : "NULL — BROKEN")}");

        var mapField = typeof(HitboxManager).GetField("_hitboxMap", BindingFlags.NonPublic | BindingFlags.Instance);
        if (mapField?.GetValue(hitboxManager) is System.Collections.IDictionary map)
        {
            Debug.Log($"{TAG} HitboxManager._hitboxMap count = {map.Count}");
            foreach (System.Collections.DictionaryEntry entry in map)
            {
                var hbd = entry.Value as HitboxDamage;
                if (hbd != null)
                {
                    var col = hbd.GetComponent<Collider2D>();
                    bool hasTrigger = col != null && col.isTrigger;
                    Debug.Log($"{TAG}   hitboxId='{entry.Key}' -> '{hbd.name}' layer={hbd.gameObject.layer} isTrigger={hasTrigger} active={hbd.gameObject.activeSelf}");
                }
            }
        }

        var fallbackField = typeof(HitboxManager).GetField("useTimerFallback", BindingFlags.NonPublic | BindingFlags.Instance);
        bool fallback = fallbackField != null && (bool)fallbackField.GetValue(hitboxManager);
        Debug.Log($"{TAG} HitboxManager.useTimerFallback = {fallback}");
    }

    private void CheckPlayerDamageable()
    {
        Debug.Log($"{TAG} --- Player IDamageable Setup ---");

        var pd = FindAnyObjectByType<PlayerDamageable>(FindObjectsInactive.Include);
        if (pd == null)
        {
            Debug.LogError($"{TAG} No PlayerDamageable found in scene! Enemy attacks will never register.");
            return;
        }

        Debug.Log($"{TAG} PlayerDamageable on '{pd.gameObject.name}' layer={pd.gameObject.layer} (expect 9=PlayerHurtbox)");

        var col = pd.GetComponent<Collider2D>();
        if (col == null)
            Debug.LogError($"{TAG} PlayerDamageable has no Collider2D — enemy hitbox triggers can't detect it!");
        else
            Debug.Log($"{TAG} Player Collider2D: type={col.GetType().Name} isTrigger={col.isTrigger} (expect false for hurtbox)");

        var rb = pd.GetComponent<Rigidbody2D>();
        Debug.Log($"{TAG} Player Rigidbody2D = {(rb != null ? "present OK" : "MISSING — trigger detection needs Rigidbody2D")}");
    }

    private void CheckEnemySetup()
    {
        Debug.Log($"{TAG} --- Enemy Setup ---");

        var enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (enemies.Length == 0)
        {
            Debug.LogWarning($"{TAG} No EnemyBase found in scene.");
            return;
        }

        foreach (var enemy in enemies)
        {
            Debug.Log($"{TAG} Enemy '{enemy.gameObject.name}' layer={enemy.gameObject.layer} (expect 7=EnemyHurtbox)");

            var col = enemy.GetComponent<Collider2D>();
            if (col == null)
                Debug.LogError($"{TAG}   No Collider2D — player hitbox triggers can't detect this enemy!");
            else
                Debug.Log($"{TAG}   Body Collider2D: type={col.GetType().Name} isTrigger={col.isTrigger} (expect false for hurtbox)");

            var rb = enemy.GetComponent<Rigidbody2D>();
            Debug.Log($"{TAG}   Rigidbody2D = {(rb != null ? "present OK" : "MISSING")}");

            if (enemy is TestDummyEnemy dummy)
            {
                var hitboxField = typeof(TestDummyEnemy).GetField("hitbox", BindingFlags.NonPublic | BindingFlags.Instance);
                var hitbox = hitboxField?.GetValue(dummy) as HitboxDamage;

                if (hitbox == null)
                    Debug.LogError($"{TAG}   TestDummyEnemy.hitbox = NULL — enemy attacks won't work!");
                else
                {
                    var hbCol = hitbox.GetComponent<Collider2D>();
                    Debug.Log($"{TAG}   TestDummy hitbox='{hitbox.name}' layer={hitbox.gameObject.layer} (expect 8=EnemyHitbox) isTrigger={hbCol?.isTrigger}");
                }

                var atkField = typeof(TestDummyEnemy).GetField("attackData", BindingFlags.NonPublic | BindingFlags.Instance);
                var atk = atkField?.GetValue(dummy);
                Debug.Log($"{TAG}   TestDummyEnemy.attackData = {(atk != null ? atk.ToString() : "NULL — enemy damage won't calculate!")}");
            }
        }
    }

    private void CheckComboDefinitionWiring()
    {
        Debug.Log($"{TAG} --- ComboDefinition AttackData Wiring ---");

        var cc = FindAnyObjectByType<ComboController>(FindObjectsInactive.Include);
        if (cc == null)
        {
            Debug.LogError($"{TAG} No ComboController found in scene!");
            return;
        }

        var def = cc.Definition;
        if (def == null)
        {
            Debug.LogError($"{TAG} ComboController.Definition is NULL — no combo tree!");
            return;
        }

        Debug.Log($"{TAG} ComboDefinition '{def.name}' — {def.steps.Length} steps, rootLight={def.rootLightIndex}, rootHeavy={def.rootHeavyIndex}");

        for (int i = 0; i < def.steps.Length; i++)
        {
            var step = def.steps[i];
            string atkName = step.attackData != null ? step.attackData.attackName : "NULL";
            string hitboxId = step.attackData != null ? step.attackData.hitboxId : "N/A";
            bool idEmpty = step.attackData != null && string.IsNullOrEmpty(step.attackData.hitboxId);

            string status = step.attackData == null ? "BROKEN — null attackData" :
                idEmpty ? "BROKEN — empty hitboxId" : "OK";

            Debug.Log($"{TAG}   step[{i}] {step.attackType}{(step.isFinisher ? " FINISHER" : "")} — attack='{atkName}' hitboxId='{hitboxId}' {status}");
        }
    }
}
