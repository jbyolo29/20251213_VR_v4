using System;
using UnityEngine;

[Serializable]
public class CircuitGate
{
    // gate 名稱：h, x, y, z, p, cx, ccx, rxx, reset...（小寫）
    public string op;

    // 你現在的格子系統：row = qubit index，col = time step
    public int row;
    public int col;

    // 參數（可選），例如 "pi/2"、"pi/4"
    public string param;

    // 多量子位 gate 的額外 qubit（先預留，之後你做「一條線」再用）
    public int row2 = -1;  // target / second qubit
    public int row3 = -1;  // third qubit (ccx)
}
