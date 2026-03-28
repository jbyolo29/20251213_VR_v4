using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class CircuitModel : MonoBehaviour
{
    // 公開變數，讓 Controller 可以讀取
    public int numWires = 2; // Qubit 數量 (行)
    public int numCols = 20; // 時間步長 (列)

    // 儲存電路數據的二維陣列 [row, col]
    private string[,] grid;

    private void Awake()
    {
        grid = new string[numWires, numCols];
    }

    // --- 放置邏輯閘 ---
    public void PlaceGate(int wire, int col, string op)
    {
        if (IsValid(wire, col))
        {
            grid[wire, col] = op;
            Debug.Log($"Placed {op} at [{wire}, {col}]");
        }
    }

    // --- 刪除邏輯閘 ---
    public void DeleteGate(int wire, int col)
    {
        if (IsValid(wire, col))
        {
            grid[wire, col] = null;
        }
    }

    // --- 清除全部 ---
    public void ClearAll()
    {
        grid = new string[numWires, numCols];
    }

    // --- 核心：產生 QASM 代碼 ---
    public string GetQASM()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("OPENQASM 2.0;");
        sb.AppendLine("include \"qelib1.inc\";");
        sb.AppendLine($"qreg q[{numWires}];");
        sb.AppendLine($"creg c[{numWires}];");

        for (int c = 0; c < numCols; c++)
        {
            int controlWire = -1;
            int targetWire = -1;

            // 1. 尋找 CNOT 配對 (控制 C 與 目標 T)
            for (int r = 0; r < numWires; r++)
            {
                string op = grid[r, c];
                if (op == "C") controlWire = r;
                if (op == "T") targetWire = r;
            }

            // 2. 寫入 CNOT 指令
            if (controlWire != -1 && targetWire != -1)
            {
                sb.AppendLine($"cx q[{controlWire}],q[{targetWire}];");
            }

            // 3. 寫入一般邏輯閘 或 測量閘
            for (int r = 0; r < numWires; r++)
            {
                string op = grid[r, c];
                if (string.IsNullOrEmpty(op)) continue;
                if (op == "C" || op == "T") continue;

                // ★ 修改點：判斷是否為測量閘 (假設你的測量閘標籤是 M 或 Measure)
                if (op.ToUpper() == "M" || op.ToUpper() == "MEASURE")
                {
                    // 產生標準 QASM 測量語法：將第 r 條量子線測量到第 r 條古典線
                    sb.AppendLine($"measure q[{r}] -> c[{r}];");
                }
                else
                {
                    // 一般邏輯閘 (如 h, x, y, z)
                    sb.AppendLine($"{op.ToLower()} q[{r}];");
                }
            }
        }

        // ★ 重點：這裡已經移除了自動加在最後的 measure q -> c;
        // 現在只有當你在 Grid 裡放了 M 閘，上面迴圈才會產生測量代碼。

        return sb.ToString();
    }

    private bool IsValid(int wire, int col)
    {
        return wire >= 0 && wire < numWires && col >= 0 && col < numCols;
    }
}