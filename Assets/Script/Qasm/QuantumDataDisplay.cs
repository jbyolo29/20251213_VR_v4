using UnityEngine;
using TMPro; // 引用 TextMeshPro
using System;

public class QuantumDataDisplay : MonoBehaviour
{
    [Header("同學的 UI 結構")]
    public TextMeshProUGUI titleText;   // 對應 hierarchy 裡的 "Title"
    public TextMeshProUGUI contentText; // 對應 hierarchy 裡的 "Text"

    // 當收到數據時執行
    public void UpdateDisplay(QasmProgramIR data)
    {
        // 1. 確保 UI 連結沒問題
        if (contentText == null) return;

        // 如果標題沒字，幫它補上
        if (titleText != null) titleText.text = "QUANTUM DATA";

        if (data.probabilities == null || data.probabilities.Length == 0) return;

        // --- 2. 計算數學 (跟之前一樣) ---
        // 抓取 Qubit 0 的機率 (Index 1 + 3)
        double prob1 = 0;
        if (data.probabilities.Length >= 2) prob1 += data.probabilities[1];
        if (data.probabilities.Length >= 4) prob1 += data.probabilities[3];

        double probPercent = prob1 * 100.0;
        double thetaRad = 2.0 * Math.Asin(Math.Sqrt(prob1));
        double thetaDeg = thetaRad * (180.0 / Math.PI);
        double alpha = Math.Sqrt(1.0 - prob1);

        double phiDeg = 0;
        if (data.raw_statevector != null && data.raw_statevector.Length > 1)
        {
            phiDeg = data.raw_statevector[1].phase * (180.0 / Math.PI);
        }

        // --- 3. 更新內容文字框 ---
        // 這裡會蓋過原本的文字
        string info = string.Format(
            "<size=120%><color=#FFA500>θ: {0:F1}°</color>   <color=#00FFFF>φ: {1:F1}°</color></size>\n\n" +
            "Prob |1>: {2:F1}%\n" +
            "Alpha: {3:F2}",
            thetaDeg,
            phiDeg,
            probPercent,
            alpha
        );

        contentText.text = info;
    }
}