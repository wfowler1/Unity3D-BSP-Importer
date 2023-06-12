#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#if !UNITY_5_6_OR_NEWER
#define OLDUNITY
#endif
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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
    /// to output to a DoomEdit map file.
    /// </summary>
    public class DoomEditMapWriter
    {

        private Entities _entities;
        private IFormatProvider _format = CultureInfo.CreateSpecificCulture("en-US");

        /// <summary>
        /// Creates a new instance of a <see cref="DoomEditMapWriter"/> object that will operate on "<paramref name="from"/>".
        /// </summary>
        /// <param name="from">The <see cref="Entities"/> object to output to a <c>string</c>.</param>
        public DoomEditMapWriter(Entities from)
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
            sb.Append("Version 2\r\n");
            for (int i = 0; i < _entities.Count; ++i)
            {
                ParseEntity(_entities[i], i, sb);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Process the data in an <see cref="Entity"/> into the passed <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to process.</param>
        /// <param name="index">The index of this <see cref="Entity"/> in the map.</param>
        /// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="entity"/> to.</param>
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
            if (brush.ef2Terrain != null || brush.mohTerrain != null)
            {
                return;
            }
            if (brush.sides.Count < 4 && brush.patch == null)
            {
                // Can't create a brush with less than 4 sides
                return;
            }
            sb.Append("// primitive ")
            .Append(index.ToString())
            .Append("\r\n{\r\n");
            if (brush.patch != null)
            {
                ParsePatch(brush.patch, sb);
            }
            else
            {
                sb.Append(" brushDef3\r\n {\r\n");
                foreach (MAPBrushSide brushSide in brush.sides)
                {
                    ParseBrushSide(brushSide, sb);
                }
                sb.Append(" }\r\n");
            }
            sb.Append("}\r\n");
        }

        /// <summary>
        /// Process the data in a <see cref="MAPBrushSide"/> into the passed <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="brushside">The <see cref="MAPBrushSide"/> to process.</param>
        /// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="brushside"/> to.</param>
        private void ParseBrushSide(MAPBrushSide brushside, StringBuilder sb)
        {
            sb.Append("  ( ")
            .Append(brushside.plane.Normal().X().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.plane.Normal().Y().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.plane.Normal().Z().ToString("###0.##########", _format))
            .Append(" ")
            .Append(brushside.plane.Distance().ToString("###0.##########", _format))
            .Append(" ) ( ( 1 0 ")
            .Append(brushside.textureInfo.Translation.X().ToString("###0.##########", _format))
            .Append(" ) ( 0 1 ")
            .Append(brushside.textureInfo.Translation.Y().ToString("###0.##########", _format))
            .Append(" ) ) \"");
            if (!brushside.texture.StartsWith("textures/") && !brushside.texture.StartsWith("models/"))
            {
                sb.Append("textures/");
            }
            sb.Append(brushside.texture)
            .Append("\" 0 0 0\r\n");
        }

        /// <summary>
        /// Process the data in a <see cref="MAPPatch"/> into the passed <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="patch">The <see cref="MAPPatch"/> to process.</param>
        /// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="patch"/> to.</param>
        private void ParsePatch(MAPPatch patch, StringBuilder sb)
        {
            sb.Append(" patchDef2\r\n {\r\n  ");
            if (!patch.texture.StartsWith("textures/") && !patch.texture.StartsWith("models/"))
            {
                sb.Append("textures/");
            }
            sb.Append(patch.texture)
            .Append("\r\n  ( ")
            .Append((int)Math.Round(patch.dims.X()))
            .Append(" ")
            .Append((int)Math.Round(patch.dims.Y()))
            .Append(" 0 0 0 )\r\n  (\r\n");
            for (int i = 0; i < patch.dims.X(); ++i)
            {
                sb.Append("   (  ");
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
            sb.Append("  )\r\n }\r\n");
        }

    }
}
