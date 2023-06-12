#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#if !UNITY_5_6_OR_NEWER
#define OLDUNITY
#endif
#endif

using System;
using System.Collections.Generic;
using System.Globalization;

namespace LibBSP
{
#if UNITY
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;
    using Vector4 = UnityEngine.Vector4;
    using Color = UnityEngine.Color32;
#if !OLDUNITY
    using Vertex = UnityEngine.UIVertex;
#endif
#elif GODOT
    using Vector2 = Godot.Vector2;
    using Vector3 = Godot.Vector3;
    using Vector4 = Godot.Quat;
    using Color = Godot.Color;
#elif NEOAXIS
    using Vector2 = NeoAxis.Vector2F;
    using Vector3 = NeoAxis.Vector3F;
    using Vector4 = NeoAxis.Vector4F;
    using Color = NeoAxis.ColorByte;
    using Vertex = NeoAxis.StandardVertex;
#else
    using Vector2 = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
    using Vector4 = System.Numerics.Vector4;
    using Color = System.Drawing.Color;
#endif

    /// <summary>
    /// Class containing all data necessary to render a Bezier patch.
    /// </summary>
    [Serializable]
    public class MAPPatch
    {

        private static IFormatProvider _format = CultureInfo.CreateSpecificCulture("en-US");

        public Vector3[] points;
        public Vector2 dims;
        public string texture;

        /// <summary>
        /// Creates a new empty <see cref="MAPPatch"/> object. Internal data will have to be set manually.
        /// </summary>
        public MAPPatch() { }

        /// <summary>
        /// Constructs a new <see cref="MAPPatch"/> object using the supplied string array as data.
        /// </summary>
        /// <param name="lines">Data to parse.</param>
        public MAPPatch(string[] lines)
        {

            texture = lines[2];
            List<Vertex> vertices = new List<Vertex>(9);

            switch (lines[0])
            {
                case "patchDef3":
                case "patchDef2":
                {
                    string[] line = lines[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    dims = new Vector2(float.Parse(line[1], _format), float.Parse(line[2], _format));
                    for (int i = 0; i < dims.X(); ++i)
                    {
                        line = lines[i + 5].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < dims.Y(); ++j)
                        {
                            Vector3 point = new Vector3(float.Parse(line[2 + (j * 7)], _format), float.Parse(line[3 + (j * 7)], _format), float.Parse(line[4 + (j * 7)], _format));
                            Vector2 uv = new Vector2(float.Parse(line[5 + (j * 7)], _format), float.Parse(line[6 + (j * 7)], _format));
                            Vertex vertex = VertexExtensions.CreateVertex(point, new Vector3(0, 0, -1), ColorExtensions.FromArgb(255, 255, 255, 255), uv, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector4(1, 0, 0, -1));
                            vertices.Add(vertex);
                        }
                    }
                    break;
                }
                case "patchTerrainDef3":
                {
                    string[] line = lines[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    dims = new Vector2(float.Parse(line[1], _format), float.Parse(line[2], _format));
                    for (int i = 0; i < dims.X(); ++i)
                    {
                        line = lines[i + 5].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < dims.Y(); ++j)
                        {
                            Vector3 point = new Vector3(float.Parse(line[2 + (j * 12)], _format), float.Parse(line[3 + (j * 12)], _format), float.Parse(line[4 + (j * 12)], _format));
                            Vector2 uv = new Vector2(float.Parse(line[5 + (j * 12)], _format), float.Parse(line[6 + (j * 12)], _format));
                            Color color = ColorExtensions.FromArgb(byte.Parse(line[7 + (j * 12)]), byte.Parse(line[8 + (j * 12)]), byte.Parse(line[9 + (j * 12)]), byte.Parse(line[10 + (j * 12)]));
                            Vertex vertex = VertexExtensions.CreateVertex(point, new Vector3(0, 0, -1), color, uv, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector4(1, 0, 0, -1));
                            vertices.Add(vertex);
                        }
                    }
                    break;
                }
                default:
                {
                    throw new ArgumentException(string.Format("Unknown patch type {0}! Call a scientist! ", lines[0]));
                }
            }
        }

    }
}
