using UnityEngine;
using UnityEngine.Rendering;

public class URPFilterSwitcher : MonoBehaviour
{
    [Header("Filter Volume Prefabs")]
    public GameObject[] filterPrefabs;

    private GameObject currentInstance;

    public void SetFilter(int index)
    {
        Debug.Log($"[FilterSwitcher] SetFilter({index}) called.");

        if (filterPrefabs == null || filterPrefabs.Length == 0)
        {
            Debug.LogError("[FilterSwitcher] filterPrefabs is EMPTY!");
            return;
        }

        if (index < 0 || index >= filterPrefabs.Length)
        {
            Debug.LogError($"[FilterSwitcher] index {index} OUT OF RANGE! Max = {filterPrefabs.Length - 1}");
            return;
        }

        // If there¡¯s an existing volume instance, remove it
        if (currentInstance != null)
        {
            Debug.Log("[FilterSwitcher] Destroying previous filter instance.");
            Destroy(currentInstance);
        }

        // Instantiate new filter prefab
        currentInstance = Instantiate(filterPrefabs[index]);
        Debug.Log($"[FilterSwitcher] Instantiate SUCCESS ¡ú currentInstance = {currentInstance.name}");

        // check if it contains a Volume
        Volume v = currentInstance.GetComponent<Volume>();
        if (v != null)
        {
            Debug.Log($"[FilterSwitcher] Volume FOUND. Profile = {v.profile.name}");
        }
        else
        {
            Debug.LogError("[FilterSwitcher] ERROR: Instantiated object does NOT contain Volume component!");
        }
    }

    public void DisableAllFilters()
    {
        Debug.Log("[FilterSwitcher] DisableAllFilters() called.");

        if (currentInstance != null)
        {
            Debug.Log("[FilterSwitcher] Destroying filter instance.");
            Destroy(currentInstance);
            currentInstance = null;
        }
        else
        {
            Debug.Log("[FilterSwitcher] No currentInstance to destroy.");
        }
    }
}
