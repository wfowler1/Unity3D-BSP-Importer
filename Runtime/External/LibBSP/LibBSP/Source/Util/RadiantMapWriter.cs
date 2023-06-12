#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#if !UNITY_5_6_OR_NEWER
#define OLDUNITY
#endif
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace LibBSP
{
#if UNITY
    using Vector3 = UnityEngine.Vector3;
#elif GODOT
    using Vector3 = Godot.Vector3;
#elif NEOAXIS
    using Vector3 = NeoAxis.Vector3F;
#else
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// A class that takes an <see cref="Entities"/> object and can convert it into a <c>string</c>,
    /// to output to a Radiant map file.
    /// </summary>
    public class RadiantMapWriter
    {

        private Entities _entities;
        private IFormatProvider _format = CultureInfo.CreateSpecificCulture("en-US");

        /// <summary>
        /// Creates a new instance of a <see cref="RadiantMapWriter"/> object that will operate on <paramref name="from"/>.
        /// </summary>
        /// <param name="from">The <see cref="Entities"/> object to output to a <c>string</c>.</param>
        public RadiantMapWriter(Entities from)
        {
            this._entities = from;
        }

        /// <summary>
        /// Parses the <see cref="Entities"/> object pointed to by this object into a <c>string</c>, to output to a file.
        /// </summary>
        /// <returns>A <c>string</c> representation of the <see cref="Entities"/> pointed to by this object.</returns>
        public string ParseMap()
        {
            // This initial buffer is probably too small (512kb) but should minimize the amount of allocations needed.
            StringBuilder sb = new StringBuilder(524288);
            for (int i = 0; i < _entities.Count; ++i)
            {
                ParseEntity(_entities[i], i, sb);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Process the data in an <see cref="Entity"/> into the passed <c>StringBuilder</c>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to process.</param>
        /// <param name="index">The index of this <see cref="Entity"/> in the map.</param>
        /// <param name="sb">A <c>StringBuilder</c> object to append processed data from <paramref name="entity"/> to.</param>
        private void ParseEntity(Entity entity, int index, StringBuilder sb)
        {
            sb.Append("// entity ")
            .Append(index)
            .Append("\r\n{\r\n");
            foreach (KeyValuePair<string, string> kvp in entity)
            {
                sb.Append("\"")
                .Append(kvp.Key)
                .Append("\" \"")
                .Append(kvp.Value)
                .Append("\"\r\n");
            }
            for (int i = 0; i < entity.brushes.Count; ++i)
            {
                ParseBrush(entity.brushes[i], i, sb);
            }
            sb.Append("}\r\n");
        }

        /// <summary>
        /// Process the data in a <see cref="MAPBrush"/> into the passed <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="brush">The <see cref="MAPBrush"/> to process.</param>
        /// <param name="index">The index of <paramref name="brush"/> in the <see cref="Entity"/>.</param>
        /// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="brush"/> to.</param>
        private void ParseBrush(MAPBrush brush, int index, StringBuilder sb)
        {
            // Unsupported features. Ignore these completely.
            if (brush.mohTerrain != null)
            {
                return;
            }
            if (brush.sides.Count < 4 && brush.patch == null && brush.ef2Terrain == null)
            {
                // Can't create a brush with less than 4 sides
                return;
            }
            sb.Append("// brush ")
            .Append(index.ToString())
            .Append("\r\n{\r\n");
            if (brush.patch != null)
            {
                ParsePatch(brush.patch, sb);
            }
            else if (brush.ef2Terrain != null)
            {
                ParseTerrain(brush.ef2Terrain, sb);
            }
            else
            {
                foreach (MAPBrushSide brushSide in brush.sides)
                {
                    ParseBrushSide(brushSide, sb);
                }
            }
            sb.Append("}\r\n");
        }

        /// <summary>
        /// Process the data in a <see cref="MAPBrushSide"/> into the passed <c>StringBuilder</c>.
        /// </summary>
        /// <param name="brushside">The <see cref="MAPBrushSide"/> to process.</param>
        /// <param name="sb">A <c>StringBuilder</c> object to append processed data from <paramref name="brushside"/> to.</param>
        private void ParseBrushSide(MAPBrushSide brushside, StringBuilder sb)
        {
            sb.Append("( ")
            .Append(brushside.vertices[0].X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.vertices[0].Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.vertices[0].Z().ToString("###0.##########", _format))
            .Append(" ) ( ")
            .Append(brushside.vertices[1].X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.vertices[1].Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.vertices[1].Z().ToString("###0.##########", _format))
            .Append(" ) ( ")
            .Append(brushside.vertices[2].X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.vertices[2].Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.vertices[2].Z().ToString("###0.##########", _format))
            .Append(" ) ")
            .Append(brushside.texture)
            .Append(" ")
            .Append(brushside.textureInfo.Translation.X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.textureInfo.Translation.Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.textureInfo.rotation.ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.textureInfo.scale.X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.textureInfo.scale.Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.textureInfo.Flags)
            .Append(" 0 0\r\n");
        }

        /// <summary>
        /// Process the data in a <see cref="MAPPatch"/> into the passed <c>StringBuilder</c>.
        /// </summary>
        /// <param name="patch">The <see cref="MAPPatch"/> to process.</param>
        /// <param name="sb">A <c>StringBuilder</c> object to append processed data from <paramref name="patch"/> to.</param>
        private void ParsePatch(MAPPatch patch, StringBuilder sb)
        {
            sb.Append("patchDef2\r\n{\r\n")
            .Append(patch.texture)
            .Append("\r\n( ")
            .Append((int)Math.Round(patch.dims.X()))
            .Append(" ")
            .Append((int)Math.Round(patch.dims.Y()))
            .Append(" 0 0 0 )\r\n(\r\n");
            for (int i = 0; i < patch.dims.X(); ++i)
            {
                sb.Append("( ");
                for (int j = 0; j < patch.dims.Y(); ++j)
                {
                    Vector3 vertex = patch.points[((int)Math.Round(patch.dims.X()) * j) + i];
                    sb.Append("( ")
                    .Append(vertex.X().ToString("###0.#####", _format))
                    .Append(" ")
                    .Append(vertex.Y().ToString("###0.#####", _format))
                    .Append(" ")
                    .Append(vertex.Z().ToString("###0.#####", _format))
                    .Append(" 0 0 ) ");
                }
                sb.Append(")\r\n");
            }
            sb.Append(")\r\n}\r\n");
        }

        /// <summary>
        /// Process the data in a <see cref="MAPTerrainEF2"/> into the passed <c>StringBuilder</c>.
        /// </summary>
        /// <param name="terrain">The <see cref="MAPTerrainEF2"/> to process.</param>
        /// <param name="sb">A <c>StringBuilder</c> object to append processed data from <paramref name="terrain"/> to.</param>
        private void ParseTerrain(MAPTerrainEF2 terrain, StringBuilder sb)
        {
            sb.Append("  terrainDef\r\n  {\r\n    TEX( ")
            .Append(terrain.texture)
            .Append(" ")
            .Append(terrain.textureShiftS.ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.textureShiftT.ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.texRot.ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.texScaleX.ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.texScaleY.ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.flags)
            .Append(" 0 0 )\r\n    TD( ")
            .Append(terrain.sideLength.ToString("###0", _format))
            .Append(" ")
            .Append(terrain.start.X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.start.Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.start.Z().ToString("###0.##########", _format))
            .Append(" )\r\n    IF( ")
            .Append(terrain.IF.X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.IF.Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.IF.Z().ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.IF.W().ToString("###0.##########", _format))
            .Append(" )\r\n    LF( ")
            .Append(terrain.LF.X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.LF.Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.LF.Z().ToString("###0.##########", _format))
            .Append(" ")
            .Append(terrain.LF.W().ToString("###0.##########", _format))
            .Append(" )\r\n    V(\r\n");
            for (int i = 0; i < terrain.heightMap.GetLength(0); ++i)
            {
                sb.Append("      ");
                for (int j = 0; j < terrain.heightMap.GetLength(1); ++j)
                {
                    sb.Append(terrain.heightMap[i, j].ToString("###0.##########", _format))
                    .Append(" ");
                }
                sb.Append("\r\n");
            }
            sb.Append("    )\r\n    A(\r\n");
            for (int i = 0; i < terrain.alphaMap.GetLength(0); ++i)
            {
                sb.Append("      ");
                for (int j = 0; j < terrain.alphaMap.GetLength(1); ++j)
                {
                    sb.Append(terrain.alphaMap[i, j].ToString("###0.##########", _format))
                    .Append(" ");
                }
                sb.Append("\r\n");
            }
            sb.Append("    )\r\n  }\r\n");
        }

    }
}
