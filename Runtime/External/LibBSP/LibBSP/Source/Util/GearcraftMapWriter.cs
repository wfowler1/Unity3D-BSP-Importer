using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace LibBSP
{
    /// <summary>
    /// A class that takes an <see cref="Entities"/> object and can convert it into a <c>string</c>,
    /// to output to a Gearcraft map file.
    /// </summary>
    public class GearcraftMapWriter
    {

        private Entities _entities;
        private IFormatProvider _format = CultureInfo.CreateSpecificCulture("en-US");

        /// <summary>
        /// Creates a new instance of a <see cref="GearcraftMapWriter"/> object that will operate on "<paramref name="from"/>".
        /// </summary>
        /// <param name="from">The <see cref="Entities"/> object to output to a <c>string</c>.</param>
        public GearcraftMapWriter(Entities from)
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
        /// Process the data in an <see cref="Entity"/> into the passed <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to process.</param>
        /// <param name="index">The index of this <see cref="Entity"/> in the map.</param>
        /// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="entity"/> to.</param>
        private void ParseEntity(Entity entity, int index, StringBuilder sb)
        {
            sb.Append("{ // Entity ")
            .Append(index.ToString())
            .Append("\r\n");
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
            if (brush.patch != null || brush.ef2Terrain != null || brush.mohTerrain != null)
            {
                return;
            }
            if (brush.sides.Count < 4)
            {
                // Can't create a brush with less than 4 sides
                return;
            }
            sb.Append("{ // Brush ")
            .Append(index.ToString())
            .Append("\r\n");
            if (brush.isDetail)
            {
                sb.Append("\"BRUSHFLAGS\" \"DETAIL\"\r\n");
            }
            foreach (MAPBrushSide brushSide in brush.sides)
            {
                ParseBrushSide(brushSide, sb);
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
            sb.Append("( ")
            .Append(brushside.vertices[0].X().ToString("###0.000000", _format))
            .Append(" ")
            .Append(brushside.vertices[0].Y().ToString("###0.000000", _format))
            .Append(" ")
            .Append(brushside.vertices[0].Z().ToString("###0.000000", _format))
            .Append(" ) ( ")
            .Append(brushside.vertices[1].X().ToString("###0.000000", _format))
            .Append(" ")
            .Append(brushside.vertices[1].Y().ToString("###0.000000", _format))
            .Append(" ")
            .Append(brushside.vertices[1].Z().ToString("###0.000000", _format))
            .Append(" ) ( ")
            .Append(brushside.vertices[2].X().ToString("###0.000000", _format))
            .Append(" ")
            .Append(brushside.vertices[2].Y().ToString("###0.000000", _format))
            .Append(" ")
            .Append(brushside.vertices[2].Z().ToString("###0.000000", _format))
            .Append(" ) ")
            .Append(brushside.texture)
            .Append(" [ ")
            .Append(brushside.textureInfo.UAxis.X().ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.UAxis.Y().ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.UAxis.Z().ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.Translation.X().ToString("###0.####", _format))
            .Append(" ] [ ")
            .Append(brushside.textureInfo.VAxis.X().ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.VAxis.Y().ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.VAxis.Z().ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.Translation.Y().ToString("###0.####", _format))
            .Append(" ] ")
            .Append(brushside.textureInfo.rotation.ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.scale.X().ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.scale.Y().ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.textureInfo.Flags)
            .Append(" ")
            .Append(brushside.material)
            .Append(" [ ")
            .Append(brushside.lgtScale.ToString("###0.####", _format))
            .Append(" ")
            .Append(brushside.lgtRot.ToString("###0.####", _format))
            .Append(" ]\r\n");
        }
    }
}
