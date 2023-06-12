#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Collections.Generic;

namespace LibBSP
{
#if UNITY
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;
#elif GODOT
    using Vector2 = Godot.Vector2;
    using Vector3 = Godot.Vector3;
#elif NEOAXIS
    using Vector2 = NeoAxis.Vector2F;
    using Vector3 = NeoAxis.Vector3F;
#else
    using Vector2 = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// Class containing all data necessary to render a Terrain from MoHAA.
    /// </summary>
    [Serializable]
    public class MAPTerrainMoHAA
    {

        public Vector2 size;
        public int flags;
        public Vector3 origin;
        public List<Partition> partitions;
        public List<Vertex> vertices;

        /// <summary>
        /// Creates a new empty <see cref="MAPTerrainMoHAA"/> object. Internal data will have to be set manually.
        /// </summary>
        public MAPTerrainMoHAA()
        {
            partitions = new List<Partition>(4);
            vertices = new List<Vertex>(81);
        }

        /// <summary>
        /// Constructs a new <see cref="MAPTerrainMoHAA"/> object using the supplied string array as data.
        /// </summary>
        /// <param name="lines">Data to parse.</param>
        public MAPTerrainMoHAA(string[] lines)
        {
            // TODO: Constructor to parse text
        }

        /// <summary>
        /// Class containing the data for a partition of a <see cref="MAPTerrainMoHAA"/>.
        /// </summary>
        [Serializable]
        public class Partition
        {
            public int unknown1;
            public int unknown2;
            public string shader;
            public int[] textureShift;
            public float rotation;
            public int unknown3;
            public float[] textureScale;
            public int unknown4;
            public int flags;
            public int unknown5;
            public string properties;

            public Partition()
            {
                unknown1 = 0;
                unknown2 = 0;
                shader = "";
                textureShift = new int[2];
                rotation = 0;
                unknown3 = 0;
                textureScale = new float[] { 1, 1 };
                unknown4 = 0;
                flags = 0;
                unknown5 = 0;
                properties = "";
            }
        }

        /// <summary>
        /// Class containing the data for a vertex of a <see cref="MAPTerrainMoHAA"/>.
        /// </summary>
        [Serializable]
        public class Vertex
        {
            public int height;
            public string unknown1;
            public string unknown2;

            public Vertex()
            {
                height = 0;
                unknown1 = "";
                unknown2 = "";
            }
        }

    }
}
