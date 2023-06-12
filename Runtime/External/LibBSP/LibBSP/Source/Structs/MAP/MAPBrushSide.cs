#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System.Collections.Generic;
using System;
using System.Globalization;

namespace LibBSP
{
#if UNITY
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;
    using Plane = UnityEngine.Plane;
#elif GODOT
    using Vector2 = Godot.Vector2;
    using Vector3 = Godot.Vector3;
    using Plane = Godot.Plane;
#elif NEOAXIS
    using Vector2 = NeoAxis.Vector2F;
    using Vector3 = NeoAxis.Vector3F;
    using Plane = NeoAxis.PlaneF;
#else
    using Vector2 = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
    using Plane = System.Numerics.Plane;
#endif

    /// <summary>
    /// Class containing data for a brush side. Please note vertices must be set manually or generated through CSG.
    /// </summary>
    [Serializable]
    public class MAPBrushSide
    {

        private static IFormatProvider _format = CultureInfo.CreateSpecificCulture("en-US");

        public Vector3[] vertices;
        public Plane plane;
        public string texture;
        public TextureInfo textureInfo;
        public string material;
        public float lgtScale;
        public float lgtRot;
        public int smoothingGroups;
        public int id;
        public MAPDisplacement displacement;

        /// <summary>
        /// Creates a new empty <see cref="MAPBrushSide"/> object. Internal data will have to be set manually.
        /// </summary>
        public MAPBrushSide() { }

        /// <summary>
        /// Constructs a <see cref="MAPBrushSide"/> object using the provided <c>string</c> array as the data.
        /// </summary>
        /// <param name="lines">Data to parse.</param>
        public MAPBrushSide(string[] lines)
        {
            // If lines.Length is 1, then this line contains all data for a brush side
            if (lines.Length == 1)
            {
                string[] tokens = lines[0].SplitUnlessInContainer(' ', '\"', StringSplitOptions.RemoveEmptyEntries);

                float dist = 0;

                // If this succeeds, assume brushDef3
                if (float.TryParse(tokens[4], out dist))
                {
                    plane = new Plane(new Vector3(float.Parse(tokens[1], _format), float.Parse(tokens[2], _format), float.Parse(tokens[3], _format)), dist);
                    textureInfo = new TextureInfo(new Vector3(float.Parse(tokens[8], _format), float.Parse(tokens[9], _format), float.Parse(tokens[10], _format)),
                                                  new Vector3(float.Parse(tokens[13], _format), float.Parse(tokens[14], _format), float.Parse(tokens[15], _format)),
                                                  new Vector2(0, 0),
                                                  new Vector2(1, 1),
                                                  0, 0, 0);
                    texture = tokens[18];
                }
                else
                {
                    Vector3 v1 = new Vector3(float.Parse(tokens[1], _format), float.Parse(tokens[2], _format), float.Parse(tokens[3], _format));
                    Vector3 v2 = new Vector3(float.Parse(tokens[6], _format), float.Parse(tokens[7], _format), float.Parse(tokens[8], _format));
                    Vector3 v3 = new Vector3(float.Parse(tokens[11], _format), float.Parse(tokens[12], _format), float.Parse(tokens[13], _format));
                    vertices = new Vector3[] { v1, v2, v3 };
                    plane = PlaneExtensions.CreateFromVertices(v1, v2, v3);
                    texture = tokens[15];
                    // GearCraft
                    if (tokens[16] == "[")
                    {
                        textureInfo = new TextureInfo(new Vector3(float.Parse(tokens[17], _format), float.Parse(tokens[18], _format), float.Parse(tokens[19], _format)),
                                                      new Vector3(float.Parse(tokens[23], _format), float.Parse(tokens[24], _format), float.Parse(tokens[25], _format)),
                                                      new Vector2(float.Parse(tokens[20], _format), float.Parse(tokens[26], _format)),
                                                      new Vector2(float.Parse(tokens[29], _format), float.Parse(tokens[30], _format)),
                                                      int.Parse(tokens[31]), 0, float.Parse(tokens[28], _format));
                        material = tokens[32];
                    }
                    else
                    {
                        //<x_shift> <y_shift> <rotation> <x_scale> <y_scale> <content_flags> <surface_flags> <value>
                        Vector3[] axes = TextureInfo.TextureAxisFromPlane(plane);
                        textureInfo = new TextureInfo(axes[0],
                                                      axes[1],
                                                      new Vector2(float.Parse(tokens[16], _format), float.Parse(tokens[17], _format)),
                                                      new Vector2(float.Parse(tokens[19], _format), float.Parse(tokens[20], _format)),
                                                      int.Parse(tokens[22]), 0, float.Parse(tokens[18], _format));
                    }
                }
            }
            else
            {
                bool inDispInfo = false;
                int braceCount = 0;
                textureInfo = new TextureInfo();
                List<string> child = new List<string>(37);
                foreach (string line in lines)
                {
                    if (line == "{")
                    {
                        ++braceCount;
                    }
                    else if (line == "}")
                    {
                        --braceCount;
                        if (braceCount == 1)
                        {
                            child.Add(line);
                            displacement = new MAPDisplacement(child.ToArray());
                            child = new List<string>(37);
                            inDispInfo = false;
                        }
                    }
                    else if (line == "dispinfo")
                    {
                        inDispInfo = true;
                        continue;
                    }

                    if (braceCount == 1)
                    {
                        string[] tokens = line.SplitUnlessInContainer(' ', '\"', StringSplitOptions.RemoveEmptyEntries);
                        switch (tokens[0])
                        {
                            case "material":
                            {
                                texture = tokens[1];
                                break;
                            }
                            case "plane":
                            {
                                string[] points = tokens[1].SplitUnlessBetweenDelimiters(' ', '(', ')', StringSplitOptions.RemoveEmptyEntries);
                                string[] components = points[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                Vector3 v1 = new Vector3(float.Parse(components[0], _format), float.Parse(components[1], _format), float.Parse(components[2], _format));
                                components = points[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                Vector3 v2 = new Vector3(float.Parse(components[0], _format), float.Parse(components[1], _format), float.Parse(components[2], _format));
                                components = points[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                Vector3 v3 = new Vector3(float.Parse(components[0], _format), float.Parse(components[1], _format), float.Parse(components[2], _format));
                                plane = PlaneExtensions.CreateFromVertices(v1, v2, v3);
                                break;
                            }
                            case "uaxis":
                            {
                                string[] split = tokens[1].SplitUnlessBetweenDelimiters(' ', '[', ']', StringSplitOptions.RemoveEmptyEntries);
                                textureInfo.scale = new Vector2(float.Parse(split[1], _format), textureInfo.scale.Y());
                                split = split[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                textureInfo.UAxis = new Vector3(float.Parse(split[0], _format), float.Parse(split[1], _format), float.Parse(split[2], _format));
                                textureInfo.Translation = new Vector2(float.Parse(split[3], _format), textureInfo.Translation.Y());
                                break;
                            }
                            case "vaxis":
                            {
                                string[] split = tokens[1].SplitUnlessBetweenDelimiters(' ', '[', ']', StringSplitOptions.RemoveEmptyEntries);
                                textureInfo.scale = new Vector2(textureInfo.scale.X(), float.Parse(split[1], _format));
                                split = split[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                textureInfo.VAxis = new Vector3(float.Parse(split[0], _format), float.Parse(split[1], _format), float.Parse(split[2], _format));
                                textureInfo.Translation = new Vector2(textureInfo.Translation.X(), float.Parse(split[3], _format));
                                break;
                            }
                            case "rotation":
                            {
                                textureInfo.rotation = float.Parse(tokens[1], _format);
                                break;
                            }
                        }
                    }
                    else if (braceCount > 1)
                    {
                        if (inDispInfo)
                        {
                            child.Add(line);
                        }
                    }
                }
            }
        }

    }
}
