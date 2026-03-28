using System;
using System.Linq;
using System.Numerics;

namespace QuantumViz.Core
{
    public class QuantumState
    {
        public int QubitCount { get; private set; }
        public Complex[] Amp; // length = 2^n

        public QuantumState(int n)
        {
            QubitCount = n;
            int dim = 1 << n;
            Amp = new Complex[dim];
            Amp[0] = Complex.One; // |0...0>
        }

        public void Normalize()
        {
            double p = Amp.Sum(a => a.Magnitude * a.Magnitude);
            if (p <= 0) return;
            double s = 1.0 / Math.Sqrt(p);
            for (int i = 0; i < Amp.Length; i++) Amp[i] *= s;
        }

        // Apply a 2x2 gate U to target qubit t
        public void ApplySingleQubit(Complex[,] U, int t)
        {
            int dim = Amp.Length;
            int mask = 1 << t;
            for (int i = 0; i < dim; i++)
            {
                if ((i & mask) == 0)
                {
                    int j = i | mask;
                    Complex a0 = Amp[i];
                    Complex a1 = Amp[j];
                    Amp[i] = U[0, 0] * a0 + U[0, 1] * a1;
                    Amp[j] = U[1, 0] * a0 + U[1, 1] * a1;
                }
            }
        }

        // Controlled-X with control c, target t
        public void ApplyCX(int c, int t)
        {
            int dim = Amp.Length;
            int cm = 1 << c;
            int tm = 1 << t;
            for (int i = 0; i < dim; i++)
            {
                if ((i & cm) != 0 && (i & tm) == 0)
                {
                    int j = i | tm;
                    (Amp[i], Amp[j]) = (Amp[j], Amp[i]);
                }
            }
        }

        public void ApplyCZ(int c, int t)
        {
            int dim = Amp.Length;
            int cm = 1 << c;
            int tm = 1 << t;
            for (int i = 0; i < dim; i++)
            {
                if ((i & cm) != 0 && (i & tm) != 0)
                    Amp[i] = -Amp[i];
            }
        }

        // Projective Z-basis measure on target t; returns 0 or 1 and collapses the state.
        public int MeasureZ(int t, Random rng)
        {
            int dim = Amp.Length;
            int mask = 1 << t;
            double p1 = 0.0;
            for (int i = 0; i < dim; i++)
                if ((i & mask) != 0) p1 += Amp[i].Magnitude * Amp[i].Magnitude;

            double r = rng.NextDouble();
            int outcome = (r < p1) ? 1 : 0;

            double norm = Math.Sqrt(outcome == 1 ? p1 : (1.0 - p1));
            if (norm > 0)
            {
                for (int i = 0; i < dim; i++)
                {
                    bool isOne = (i & mask) != 0;
                    if ((outcome == 1) != isOne) Amp[i] = Complex.Zero;
                    else Amp[i] /= norm;
                }
            }
            return outcome;
        }

        // Reset: measure then set to |0> on that qubit
        public void Reset(int t, Random rng)
        {
            int m = MeasureZ(t, rng);
            if (m == 1) ApplySingleQubit(GateLibrary.X, t);
        }

        // Reduced density matrix for single qubit k (2x2)
        public Complex[,] ReducedRho1(int k)
        {
            int n = QubitCount;
            int dim = 1 << n;
            var rho = new Complex[2, 2];

            for (int a = 0; a < dim; a++)
                for (int b = 0; b < dim; b++)
                {
                    // other qubits must match to survive trace
                    int ak = (a >> k) & 1;
                    int bk = (b >> k) & 1;
                    int aMask = a & ~(1 << k);
                    int bMask = b & ~(1 << k);
                    if (aMask == bMask)
                        rho[ak, bk] += Amp[a] * Complex.Conjugate(Amp[b]);
                }
            return rho;
        }

        public static (double x, double y, double z) BlochFromRho(Complex[,] r)
        {
            // x = 2 Re(r01), y = -2 Im(r01), z = r00 - r11
            double x = 2.0 * r[0, 1].Real;
            double y = -2.0 * r[0, 1].Imaginary;
            double z = (r[0, 0] - r[1, 1]).Real;
            return (x, y, z);
        }
    }
}
