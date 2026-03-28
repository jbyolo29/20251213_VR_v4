using System;
using System.Numerics;

namespace QuantumViz.Core
{
    public static class GateLibrary
    {
        public static readonly Complex[,] I = { { 1, 0 }, { 0, 1 } };
        public static readonly Complex[,] X = { { 0, 1 }, { 1, 0 } };
        public static readonly Complex[,] Y = { { 0, -Complex.ImaginaryOne }, { Complex.ImaginaryOne, 0 } };
        public static readonly Complex[,] Z = { { 1, 0 }, { 0, -1 } };
        public static readonly Complex[,] H = {
            { 1/Math.Sqrt(2),  1/Math.Sqrt(2) },
            { 1/Math.Sqrt(2), -1/Math.Sqrt(2) }
        };
        public static Complex[,] Phase(double phi)
        {
            return new Complex[,] { { 1, 0 }, { 0, Complex.FromPolarCoordinates(1, phi) } };
        }
        public static Complex[,] Rx(double th)
        {
            double c = Math.Cos(th / 2), s = Math.Sin(th / 2);
            return new Complex[,] { { c, -Complex.ImaginaryOne * s }, { -Complex.ImaginaryOne * s, c } };
        }
        public static Complex[,] Ry(double th)
        {
            double c = Math.Cos(th / 2), s = Math.Sin(th / 2);
            return new Complex[,] { { c, -s }, { s, c } };
        }
        public static Complex[,] Rz(double th)
        {
            // diag(e^{-i th/2}, e^{i th/2})
            return new Complex[,] {
                { Complex.FromPolarCoordinates(1, -th/2), 0 },
                { 0, Complex.FromPolarCoordinates(1,  th/2) }
            };
        }

        public static readonly Complex[,] S = { { 1, 0 }, { 0, Complex.ImaginaryOne } };
        public static readonly Complex[,] Sdg = { { 1, 0 }, { 0, -Complex.ImaginaryOne } };
        public static readonly Complex[,] T = { { 1, 0 }, { 0, Complex.FromPolarCoordinates(1, Math.PI / 4) } };
        public static readonly Complex[,] Tdg = { { 1, 0 }, { 0, Complex.FromPolarCoordinates(1, -Math.PI / 4) } };
    }
}
