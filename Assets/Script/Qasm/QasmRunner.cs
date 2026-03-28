using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using System.IO;

public class QasmRunner : MonoBehaviour
{
    [Header("Configuration")]
    public string serverUrl = "http://127.0.0.1:5000/api/run_qasm_debug";
    public CircuitModel circuit;
    public CircuitGridController controller;

    [Header("UI Link")]
    public QuantumDataDisplay dataDisplay;

    private void Start()
    {
        // 強制覆寫網址，無視 Unity 面板的舊記憶
        serverUrl = "http://127.0.0.1:5000/api/run_qasm_debug";

        if (circuit == null) circuit = FindObjectOfType<CircuitModel>();
        if (controller == null) controller = FindObjectOfType<CircuitGridController>();
        if (dataDisplay == null) dataDisplay = FindObjectOfType<QuantumDataDisplay>();
    }

    public void RunSimulation()
    {
        if (circuit == null) return;
        string qasmCode = circuit.GetQASM();
        if (string.IsNullOrEmpty(qasmCode)) return;

        // 將 QASM 存成實體文字檔 (存放在 Unity 專案的根目錄)
        string qasmFilePath = Application.dataPath + "/../temp_input.qasm";
        File.WriteAllText(qasmFilePath, qasmCode);
        Debug.Log("<color=yellow>已在外部建立 QASM 檔案: </color>" + qasmFilePath);

        Debug.Log("<color=yellow>準備發送至網址: </color>" + serverUrl);
        StartCoroutine(PostQasm(qasmCode));
    }

    IEnumerator PostQasm(string qasm)
    {
        string json = "{\"qasm\": \"" + EscapeJson(qasm) + "\"}";

        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;

                // 將 Python 回傳的 JSON 存成實體文字檔
                string jsonFilePath = Application.dataPath + "/../qasm_ir.json";
                File.WriteAllText(jsonFilePath, jsonResponse);
                Debug.Log("<color=green>已在外部建立 JSON 檔案: </color>" + jsonFilePath);

                // 雙軌解析！舊的給原本的系統，新的抓出來備用
                QasmProgramIR responseData = JsonUtility.FromJson<QasmProgramIR>(jsonResponse);
                QasmDebugResponse debugData = JsonUtility.FromJson<QasmDebugResponse>(jsonResponse);

                // 使用 true 來開啟漂亮排版，讓 Console 裡的資料一行一行整齊顯示
                string formattedJson = JsonUtility.ToJson(debugData, true);
                Debug.Log("<color=cyan>完整 Debug 數據 (已排版): </color>\n" + formattedJson);

                // --- ★ 這裡開始是呼叫端的大更新 ---
                if (controller != null)
                {
                    // 1. 拿舊的機率數據更新 UI 長條圖
                    controller.UpdateUIPanel(responseData);

                    // 2. 拿新的真實物理數據，更新 3D 球與純文字狀態面板
                    controller.UpdateBlochSpheres(debugData);
                }

                // 3. 更新舊的 DATA 狀態面板 (維持原功能)
                if (dataDisplay != null && responseData != null)
                {
                    dataDisplay.UpdateDisplay(responseData);
                }
                // --- 更新結束 ---

                Debug.Log("<color=green>成功從 Python 取得資料並更新畫面！</color>");
            }
            else
            {
                Debug.LogError("Error connecting to Python at [" + serverUrl + "]: " + request.error);
            }
        }
    }

    private string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\n", "\\n");
    }
}

// 接收 Debug 資料的 Class，不破壞原本的 QasmProgramIR
[System.Serializable]
public class QasmDebugResponse
{
    public DebugStep[] debug_steps;
}

[System.Serializable]
public class DebugStep
{
    public int step;
    public string gate;
    public QubitDebugState[] qubits;
}

[System.Serializable]
public class QubitDebugState
{
    public int qubit;
    public float x;
    public float y;
    public float z;
    public float radius; // 這就是未來要餵給霧氣濃度的核心參數！
}