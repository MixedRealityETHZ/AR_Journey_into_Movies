using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Collections;

public class URPFilterSwitcher : MonoBehaviour
{
    [Header("Filter Volume Prefabs")]
    public GameObject[] filterPrefabs;
    public GameObject filterPanel;
    public GameObject Alphaslider;
    public Button ButtonFilter;

    private GameObject currentInstance;

    private bool isPanelVisible = false;
    private CanvasGroup panelGroup;
    private CanvasGroup alphaGroup;

    private void Awake()
    {
        panelGroup = filterPanel.GetComponent<CanvasGroup>();
        if (panelGroup == null) panelGroup = filterPanel.AddComponent<CanvasGroup>();
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        alphaGroup = Alphaslider.GetComponent<CanvasGroup>();
        if (alphaGroup == null) alphaGroup = Alphaslider.AddComponent<CanvasGroup>();
        alphaGroup.alpha = 0f;
        alphaGroup.interactable = false;
        alphaGroup.blocksRaycasts = false;
    }

    public void Start()
    {
        ButtonFilter.onClick.AddListener(OnClickFilter);
    }

    private void OnClickFilter()
    {
        if (isPanelVisible)
            StartCoroutine(FadeOut());
        else
            StartCoroutine(FadeIn());

        isPanelVisible = !isPanelVisible;
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            float a = Mathf.Lerp(0f, 1f, t);
            panelGroup.alpha = a;
            alphaGroup.alpha = a;
            yield return null;
        }
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
        alphaGroup.interactable = true;
        alphaGroup.blocksRaycasts = true;
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            float a = Mathf.Lerp(1f, 0f, t);
            panelGroup.alpha = a;
            alphaGroup.alpha = a;
            yield return null;
        }
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        alphaGroup.interactable = false;
        alphaGroup.blocksRaycasts = false;
    }

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

        // If there��s an existing volume instance, remove it
        if (currentInstance != null)
        {
            Debug.Log("[FilterSwitcher] Destroying previous filter instance.");
            Destroy(currentInstance);
        }

        // Instantiate new filter prefab
        currentInstance = Instantiate(filterPrefabs[index]);
        Debug.Log($"[FilterSwitcher] Instantiate SUCCESS �� currentInstance = {currentInstance.name}");

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
