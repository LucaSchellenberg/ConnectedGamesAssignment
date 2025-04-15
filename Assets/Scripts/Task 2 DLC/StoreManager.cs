using System.Collections;
using TMPro;
using UnityEngine;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;
    public GameObject DiamondAmount;
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public bool PurchaseItem(float price)
    {
        TMP_Text diamondsText = DiamondAmount.GetComponent<TMP_Text>();
        float.TryParse(diamondsText.text, out float availableDiamonds);
        if (availableDiamonds >= price)
        {
            StartCoroutine(DecreaseNumberGradually(diamondsText, availableDiamonds,
                availableDiamonds - price, 2));
            return true;
        }

        return false;
    }

    IEnumerator DecreaseNumberGradually(TMP_Text diamondsText, float startValue, float endValue, float totalTime)
    {
        float elapsedTime = 0f;
        float currentValue = startValue;
        float rateOfChange = 0f;

        while (currentValue > endValue)
        {
            elapsedTime += Time.deltaTime;
            rateOfChange = Mathf.Lerp(0f, 1f, elapsedTime / totalTime);
            currentValue -= rateOfChange * Time.deltaTime * (startValue - endValue) / totalTime;

            diamondsText.text = Mathf.RoundToInt(currentValue).ToString();

            yield return null;
        }

        // Ensure the final value is exactly the endValue
        diamondsText.text = endValue.ToString();
    }
}
