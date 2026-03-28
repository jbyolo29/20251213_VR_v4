using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuantumViz.Core
{
    public class QasmMiniParser
    {
        public class Cmd
        {
            public string Op;
            public double Param; // 用於 rx/ry/rz
            public List<int> Qubits = new();
            public List<int> Cbits = new();
        }

        static readonly Regex reHeader = new(@"^\s*(OPENQASM|include|bit|qubit)", RegexOptions.IgnoreCase);
        static readonly Regex reGate2 = new(@"^\s*([a-z][a-z0-9_]*)\s+q\[(\d+)\]\s*;", RegexOptions.IgnoreCase);
        static readonly Regex reGateCtrl = new(@"^\s*(cx|cz)\s+q\[(\d+)\]\s*,\s*q\[(\d+)\]\s*;", RegexOptions.IgnoreCase);
        static readonly Regex reParam = new(@"^\s*(r[xyz])\s*\(\s*([^\)]+)\s*\)\s*q\[(\d+)\]\s*;", RegexOptions.IgnoreCase);
        static readonly Regex reMeasure = new(@"^\s*c\[(\d+)\]\s*=\s*measure\s+q\[(\d+)\]\s*;", RegexOptions.IgnoreCase);
        static readonly Regex reReset = new(@"^\s*reset\s+q\[(\d+)\]\s*;", RegexOptions.IgnoreCase);

        public static List<Cmd> Parse(string src)
        {
            var list = new List<Cmd>();
            var lines = src.Replace("π", "pi").Split('\n');
            foreach (var raw in lines)
            {
                string s = raw.Split("//")[0].Trim();
                if (s.Length == 0) continue;
                if (reHeader.IsMatch(s)) continue;

                var mP = reParam.Match(s);
                if (mP.Success)
                {
                    string op = mP.Groups[1].Value.ToLower();
                    double val = EvalAngle(mP.Groups[2].Value);
                    int q = int.Parse(mP.Groups[3].Value);
                    list.Add(new Cmd { Op = op, Param = val, Qubits = { q } });
                    continue;
                }
                var mC = reGateCtrl.Match(s);
                if (mC.Success)
                {
                    list.Add(new Cmd
                    {
                        Op = mC.Groups[1].Value.ToLower(),
                        Qubits = { int.Parse(mC.Groups[2].Value), int.Parse(mC.Groups[3].Value) }
                    });
                    continue;
                }
                var m2 = reGate2.Match(s);
                if (m2.Success)
                {
                    list.Add(new Cmd { Op = m2.Groups[1].Value.ToLower(), Qubits = { int.Parse(m2.Groups[2].Value) } });
                    continue;
                }
                var mm = reMeasure.Match(s);
                if (mm.Success)
                {
                    list.Add(new Cmd { Op = "measure", Qubits = { int.Parse(mm.Groups[2].Value) }, Cbits = { int.Parse(mm.Groups[1].Value) } });
                    continue;
                }
                var mr = reReset.Match(s);
                if (mr.Success)
                {
                    list.Add(new Cmd { Op = "reset", Qubits = { int.Parse(mr.Groups[1].Value) } });
                    continue;
                }

                // 忽略 nop、空白等；其他語法可逐步擴充
            }
            return list;
        }

        static double EvalAngle(string expr)
        {
            // 支援: pi, k*pi/2, 數字（度或弧度？→ 這裡預設弧度）
            string t = expr.Replace(" ", "").ToLower();
            t = t.Replace("pi", Math.PI.ToString(CultureInfo.InvariantCulture));
            // 簡易 eval: 只處理 + - * / 與常數
            try { return SimpleEval(t); }
            catch { return 0.0; }
        }

        static double SimpleEval(string e)
        {
            // 非完整 eval，但可處理 a+b, a-b, a*b, a/b
            // 以安全為主：遞迴分割
            int p = e.LastIndexOfAny(new[] { '+', '-' });
            if (p > 0) return (e[p] == '+' ? 1 : -1) * SimpleEval(e[..p]) + SimpleEval(e[(p + 1)..]);
            p = e.LastIndexOfAny(new[] { '*', '/' });
            if (p > 0) { double L = SimpleEval(e[..p]), R = SimpleEval(e[(p + 1)..]); return e[p] == '*' ? L * R : L / R; }
            return double.Parse(e, CultureInfo.InvariantCulture);
        }
    }
}
