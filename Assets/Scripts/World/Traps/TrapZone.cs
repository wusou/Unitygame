using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 陷阱触发区：按配置触发 TrapDefinition 中的效果。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TrapZone : MonoBehaviour
{
    [Header("陷阱定义")]
    [SerializeField] private TrapDefinition trapDefinition;

    [Header("触发方式")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool triggerOnStay;
    [SerializeField, Min(0.05f)] private float stayTickInterval = 0.5f;
    [SerializeField] private bool triggerOnExit;

    [Header("目标过滤")]
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private bool affectPlayer = true;
    [SerializeField] private bool affectEnemy;

    private Collider2D zoneCollider;
    private readonly Dictionary<int, int> targetInsideCounter = new();
    private readonly Dictionary<int, float> nextStayTriggerTimes = new();

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!TryBuildContext(other, out var targetKey, out var context))
        {
            return;
        }

        targetInsideCounter.TryGetValue(targetKey, out var counter);
        targetInsideCounter[targetKey] = counter + 1;

        if (counter == 0 && triggerOnEnter)
        {
            trapDefinition.TriggerEnter(context);
        }

        if (triggerOnStay)
        {
            nextStayTriggerTimes[targetKey] = Time.time + Mathf.Max(0.05f, stayTickInterval);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!triggerOnStay)
        {
            return;
        }

        if (!TryBuildContext(other, out var targetKey, out var context))
        {
            return;
        }

        if (!targetInsideCounter.ContainsKey(targetKey))
        {
            targetInsideCounter[targetKey] = 1;
        }

        if (!nextStayTriggerTimes.TryGetValue(targetKey, out var nextTime))
        {
            nextTime = Time.time;
        }

        if (Time.time < nextTime)
        {
            return;
        }

        trapDefinition.TriggerStay(context);
        nextStayTriggerTimes[targetKey] = Time.time + Mathf.Max(0.05f, stayTickInterval);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!TryBuildContext(other, out var targetKey, out var context))
        {
            return;
        }

        if (!targetInsideCounter.TryGetValue(targetKey, out var counter))
        {
            return;
        }

        counter--;
        if (counter > 0)
        {
            targetInsideCounter[targetKey] = counter;
            return;
        }

        targetInsideCounter.Remove(targetKey);
        nextStayTriggerTimes.Remove(targetKey);

        if (triggerOnExit)
        {
            trapDefinition.TriggerExit(context);
        }
    }

    private bool TryBuildContext(Collider2D other, out int targetKey, out TrapContext context)
    {
        targetKey = 0;
        context = default;

        if (trapDefinition == null || other == null || other == zoneCollider)
        {
            return false;
        }

        if (targetLayers.value != 0 && (targetLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return false;
        }

        var targetObject = ResolveTargetObject(other);
        if (targetObject == null || targetObject == gameObject)
        {
            return false;
        }

        var targetPlayerHealth = targetObject.GetComponent<PlayerHealth>();
        var targetEnemy = targetObject.GetComponent<EnemyBase>();

        if (!affectPlayer && targetPlayerHealth != null)
        {
            targetPlayerHealth = null;
        }

        if (!affectEnemy && targetEnemy != null)
        {
            targetEnemy = null;
        }

        if (targetPlayerHealth == null && targetEnemy == null)
        {
            return false;
        }

        targetKey = targetObject.GetInstanceID();
        context = new TrapContext(this, targetObject, other, targetPlayerHealth, targetEnemy, Time.deltaTime);
        return true;
    }

    private static GameObject ResolveTargetObject(Collider2D other)
    {
        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.gameObject;
        }

        return other.transform.root != null ? other.transform.root.gameObject : other.gameObject;
    }
}
