using System.Collections.Generic;
using SDG.Framework.Utilities;
using UnityEngine;

namespace SDG.Unturned;

public class PoolReference : MonoBehaviour
{
    public GameObjectPool pool;

    public bool inPool;

    /// <summary>
    /// Enabled for effects held by guns and sentries.
    /// </summary>
    public bool excludeFromDestroyAll;

    private Coroutine invokeAfterDelayCoroutine;

    public void DestroyIntoPool(float t)
    {
        CancelDestroyTimer();
        if (pool == null)
        {
            Object.Destroy(base.gameObject, t);
        }
        else if (base.gameObject.activeInHierarchy)
        {
            invokeAfterDelayCoroutine = TimeUtility.InvokeAfterDelay(DestroyIntoPoolCallback, t);
        }
        else
        {
            pool.Destroy(this);
        }
    }

    internal void CancelDestroyTimer()
    {
        if (invokeAfterDelayCoroutine != null)
        {
            TimeUtility.StaticStopCoroutine(invokeAfterDelayCoroutine);
            invokeAfterDelayCoroutine = null;
        }
    }

    private void DestroyIntoPoolCallback()
    {
        invokeAfterDelayCoroutine = null;
        if (pool == null)
        {
            Object.Destroy(base.gameObject);
        }
        else
        {
            pool.Destroy(this);
        }
    }

    /// <summary>
    /// Listen for OnDestroy callback because mods may be destroying themselves in unexpected ways (e.g., Grenade
    /// component) and still need to be cleaned up.
    /// </summary>
    private void OnDestroy()
    {
        CancelDestroyTimer();
        if (!Level.isExiting)
        {
            if (base.transform.parent != null)
            {
                EffectManager.UnregisterAttachment(base.gameObject);
            }
            if (pool != null && !inPool)
            {
                pool.active.RemoveFast(this);
                pool = null;
            }
        }
    }
}
