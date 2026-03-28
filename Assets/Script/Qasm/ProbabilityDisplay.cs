using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ProbabilityDisplay : MonoBehaviour
{
    [Header("UI References")]
    // ★ 關鍵：這裡要拖入的是 Column 裡面的「藍色 Fill 圖片」
    // 因為只有 Fill 會長高，Column 是不會動的
    public RectTransform[] targetFills;

    [Header("Settings")]
    public float fullHeightPixels = 200f; // 請確保這跟你的 Line_100 高度一致
    public float animationSpeed = 5f;

    private Coroutine[] runningCoroutines;

    private void Awake()
    {
        // 初始化陣列
        if (targetFills != null)
        {
            runningCoroutines = new Coroutine[targetFills.Length];
        }
    }

    public void UpdateProbabilities(float p00, float p01, float p10, float p11)
    {
        float[] probs = new float[] { p00, p01, p10, p11 };

        if (targetFills == null || targetFills.Length < 4)
        {
            Debug.LogError("錯誤：沒有拖入足夠的 Fill 物件！請檢查 Inspector。");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (targetFills[i] != null)
            {
                // 計算目標高度
                float targetHeight = probs[i] * fullHeightPixels;

                // 停止舊動畫，開始新動畫
                if (runningCoroutines[i] != null) StopCoroutine(runningCoroutines[i]);
                runningCoroutines[i] = StartCoroutine(AnimateBar(targetFills[i], targetHeight, probs[i]));
            }
        }
    }

    IEnumerator AnimateBar(RectTransform fillRect, float targetHeight, float probability)
    {
        float currentHeight = fillRect.sizeDelta.y;

        // ★ 順便更新 Fill 裡面的百分比文字 (如果有加的話)
        // 假設文字是 Fill 的子物件
        var text = fillRect.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            // 如果機率是 0，就隱藏文字，比較好看
            text.text = probability > 0.01f ? $"{probability * 100:F0}%" : "";
        }

        // 動畫迴圈
        while (Mathf.Abs(currentHeight - targetHeight) > 0.1f)
        {
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * animationSpeed);

            // 只改變高度 Y，寬度 X 保持不變
            fillRect.sizeDelta = new Vector2(fillRect.sizeDelta.x, currentHeight);

            yield return null;
        }

        // 確保最後精準設定
        fillRect.sizeDelta = new Vector2(fillRect.sizeDelta.x, targetHeight);
    }

    // 給 Controller 呼叫的接口
    public void UpdateHistogram(double[] probabilities)
    {
        if (probabilities == null || probabilities.Length < 4) return;
        UpdateProbabilities((float)probabilities[0], (float)probabilities[1], (float)probabilities[2], (float)probabilities[3]);
    }
}