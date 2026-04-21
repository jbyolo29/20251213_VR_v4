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
    public QuantumDataDisplay dataDisplay;

    private void Start()
    {
        serverUrl = "http://127.0.0.1:5000/api/run_qasm_debug";
    }

    public void RunSimulation()
    {
        if (circuit == null) return;
        string qasmCode = circuit.GetQASM();
        if (string.IsNullOrEmpty(qasmCode)) return;
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

                // 雙軌解析：同時拿到 UI 機率資料與 Debug 步驟資料
                QasmProgramIR responseData = JsonUtility.FromJson<QasmProgramIR>(jsonResponse);
                QasmDebugResponse debugData = JsonUtility.FromJson<QasmDebugResponse>(jsonResponse);

                if (controller != null)
                {
                    controller.UpdateUIPanel(responseData);
                    controller.UpdateBlochSpheres(debugData); // 傳遞 Debug 序列資料
                }

                if (dataDisplay != null && responseData != null)
                {
                    dataDisplay.UpdateDisplay(responseData);
                }
            }
            else
            {
                Debug.LogError("<color=red>[QasmRunner Error]</color> 連線失敗: " + request.error);
            }
        }
    }

    private string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\n", "\\n");
    }
}

// --- 恢復你最原本的資料結構 ---
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
    public float radius;
}