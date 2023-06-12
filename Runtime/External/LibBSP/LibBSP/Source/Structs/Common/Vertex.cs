#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#if !UNITY_5_6_OR_NEWER
// UIVertex was introduced in Unity 4.5 but it only had color, position and one UV.
// From 4.6.0 until 5.5.6 it was missing two sets of UVs.
#define OLDUNITY
#endif
#endif

#if !NEOAXIS && (!UNITY || OLDUNITY)

using System;

namespace LibBSP
{
#if UNITY
    using Color = UnityEngine.Color32;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;
    using Vector4 = UnityEngine.Vector4;
#elif GODOT
    using Color = Godot.Color;
    using Vector2 = Godot.Vector2;
    using Vector3 = Godot.Vector3;
    using Vector4 = Godot.Quat;
#else
    using Color = System.Drawing.Color;
    using Vector2 = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
    using Vector4 = System.Numerics.Vector4;
#endif

    /// <summary>
    /// Vertex struct, including fields for normal, tangent and four sets of UVs.
    /// </summary>
    [Serializable]
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Color color;
        public Vector2 uv0;
        public Vector2 uv1;
        public Vector2 uv2;
        public Vector2 uv3;
        public Vector4 tangent;

        /// <summary>
        /// Simple Vertex with sensible settings.
        /// </summary>
        public static Vertex simpleVert
        {
            get
            {
                return new Vertex
                {
                    color = ColorExtensions.FromArgb(255, 255, 255, 255),
                    normal = new Vector3(0, 0, -1),
                    position = new Vector3(0, 0, 0),
                    tangent = new Vector4(1, 0, 0, -1),
                    uv0 = new Vector2(0, 0),
                    uv1 = new Vector2(0, 0),
                    uv2 = new Vector2(0, 0),
                    uv3 = new Vector2(0, 0),
                };
            }
        }
    }
}
#endif
