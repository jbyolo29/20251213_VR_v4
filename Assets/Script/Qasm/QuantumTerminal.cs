using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class QuantumTerminal : MonoBehaviour
{
    public static QuantumTerminal Instance; // 全域存取點

    [Header("UI 綁定")]
    public TextMeshProUGUI terminalText;
    public ScrollRect scrollRect;

    [Header("設定")]
    public float typeSpeed = 0.01f; // 打字機速度
    public string prefix = "root@qiskit:~# "; // 駭客前綴字

    private bool isTyping = false;
    private string currentTextBuffer = "";

    private void Awake()
    {
        // 設定單例模式，讓其他腳本可以直接呼叫 QuantumTerminal.Instance.Log(...)
        if (Instance == null) Instance = this;

        // 初始化畫面
        terminalText.text = ">> Booting Quantum OS v1.0.4...\n>> Connection to local QPU established.\n";
    }

    // ★ 功能 1：印出普通的一行字
    public void Log(string message)
    {
        StartCoroutine(TypeText("\n" + prefix + message));
    }

    // ★ 功能 2：印出 QASM 多行代碼 (速度快一點)
    public void LogQASM(string qasmCode)
    {
        StartCoroutine(TypeText("\n>> [Generated QASM Code]:\n" + qasmCode, 0.002f));
    }

    // ★ 功能 3：模擬計算與進度條 (超帥的 DOS 動畫)
    public void SimulateCalculation()
    {
        StartCoroutine(CalculationRoutine());
    }

    // --- 以下是底層動畫邏輯 ---

    private IEnumerator TypeText(string message, float overrideSpeed = -1f)
    {
        // 確保上一段字打完，避免字串混亂
        while (isTyping) yield return null;
        isTyping = true;

        float speed = overrideSpeed > 0 ? overrideSpeed : typeSpeed;

        foreach (char c in message)
        {
            terminalText.text += c;
            AutoScroll(); // 每打一個字就自動捲動到底部
            yield return new WaitForSeconds(speed);
        }

        isTyping = false;
    }

    private IEnumerator CalculationRoutine()
    {
        while (isTyping) yield return null;
        isTyping = true;

        // 1. 顯示連線中
        string msg = "\n" + prefix + "Sending QASM to server (127.0.0.1:5000)...";
        foreach (char c in msg) { terminalText.text += c; yield return new WaitForSeconds(typeSpeed); }
        yield return new WaitForSeconds(0.5f);

        // 2. 畫出進度條外框
        terminalText.text += "\n>> Calculating: [                    ] 0%";
        AutoScroll();

        int totalBars = 20;
        for (int i = 1; i <= totalBars; i++)
        {
            // 替換最後一行的進度條字串
            int removeLength = 26 + (i > 2 ? 1 : 0); // 扣掉舊的進度條長度
            terminalText.text = terminalText.text.Substring(0, terminalText.text.LastIndexOf('['));

            string progress = new string('=', i) + new string(' ', totalBars - i);
            int percent = (int)(((float)i / totalBars) * 100);

            terminalText.text += "[" + progress + "] " + percent + "%";
            AutoScroll();

            // 隨機延遲，模擬真實計算卡頓感
            yield return new WaitForSeconds(Random.Range(0.02f, 0.15f));
        }

        // 3. 顯示結果
        yield return new WaitForSeconds(0.3f);
        string doneMsg = "\n>> State Vector & Bloch Coordinates Received.\n>> Rendering Visualization...";
        foreach (char c in doneMsg) { terminalText.text += c; yield return new WaitForSeconds(typeSpeed); }

        isTyping = false;
    }

    private void AutoScroll()
    {
        // 強制 Canvas 更新佈局，並把捲動條拉到最下面 (0)
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}