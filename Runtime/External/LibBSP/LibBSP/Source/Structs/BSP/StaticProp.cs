#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Text;

namespace LibBSP
{
#if UNITY
    using Color = UnityEngine.Color32;
    using Vector3 = UnityEngine.Vector3;
#elif GODOT
    using Color = Godot.Color;
    using Vector3 = Godot.Vector3;
#elif NEOAXIS
    using Color = NeoAxis.ColorByte;
    using Vector3 = NeoAxis.Vector3F;
#else
    using Color = System.Drawing.Color;
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// Handles the data needed for a static prop object.
    /// </summary>
    public struct StaticProp : ILumpObject
    {

        /// <summary>
        /// The <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
        /// </summary>
        public ILump Parent { get; private set; }

        /// <summary>
        /// Array of <c>byte</c>s used as the data source for this <see cref="ILumpObject"/>.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// The <see cref="LibBSP.MapType"/> to use to interpret <see cref="Data"/>.
        /// </summary>
        public MapType MapType
        {
            get
            {
                if (Parent == null || Parent.Bsp == null)
                {
                    return MapType.Undefined;
                }
                return Parent.Bsp.MapType;
            }
        }

        /// <summary>
        /// The version number of the <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
        /// </summary>
        public int LumpVersion
        {
            get
            {
                if (Parent == null)
                {
                    return 0;
                }
                return Parent.LumpInfo.version;
            }
        }

        /// <summary>
        /// Gets or sets the origin of this model.
        /// </summary>
        public Vector3 Origin
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Vector3Extensions.ToVector3(Data);
                        }
                    }
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            value.GetBytes().CopyTo(Data, 0);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the Angles for this model.
        /// </summary>
        public Vector3 Angles
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Vector3Extensions.ToVector3(Data, 12);
                        }
                    }
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            value.GetBytes().CopyTo(Data, 12);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name of the model for this <see cref="StaticProp"/> from <see cref="StaticProps.ModelDictionary"/>.
        /// </summary>
        public string Model
        {
            get
            {
                return ((StaticProps)Parent).ModelDictionary[ModelIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of this model's name in <see cref="StaticProps.ModelDictionary"/>.
        /// </summary>
        public short ModelIndex
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return BitConverter.ToInt16(Data, 24);
                        }
                    }
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 24);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the <see cref="Leaf"/> indices referenced by this <see cref="StaticProp"/>.
        /// </summary>
        public IEnumerable<short> LeafIndices
        {
            get
            {
                for (int i = 0; i < NumLeafIndices; ++i)
                {
                    yield return ((StaticProps)Parent).LeafIndices[FirstLeafIndexIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first <see cref="Leaf"/> index in <see cref="StaticProps.LeafIndices"/>
        /// for the leaves containing this <see cref="StaticProp"/>.
        /// </summary>
        public short FirstLeafIndexIndex
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        {
                            return BitConverter.ToInt16(Data, 26);
                        }
                    }
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 26);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of <see cref="Leaf"/> indices in <see cref="StaticProps.LeafIndices"/> for leaves
        /// which contain this <see cref="StaticProp"/>.
        /// </summary>
        public short NumLeafIndices
        {
            get
            {
                if (MapType == MapType.Source)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        {
                            return BitConverter.ToInt16(Data, 28);
                        }
                    }
                }

                return -1;
            }
            set
            {
                if (MapType == MapType.Source)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 28);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the solidity type of this <see cref="StaticProp"/>.
        /// </summary>
        public byte Solidity
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Data[30];
                        }
                    }
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            Data[30] = value;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the flags of this <see cref="StaticProp"/>.
        /// </summary>
        public byte Flags
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Data[31];
                        }
                    }
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        {
                            Data[31] = value;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the model skin used by this <see cref="StaticProp"/>.
        /// </summary>
        public int Skin
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return BitConverter.ToInt32(Data, 32);
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return BitConverter.ToInt32(Data, 36);
                            }
                            else
                            {
                                return BitConverter.ToInt32(Data, 32);
                            }
                        }
                    }
                }
                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 32);
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                BitConverter.GetBytes(value).CopyTo(Data, 36);
                            }
                            else
                            {
                                BitConverter.GetBytes(value).CopyTo(Data, 32);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the distance at which this <see cref="StaticProp"/> will start to fade out.
        /// </summary>
        public float MinimumFadeDistance
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return BitConverter.ToSingle(Data, 36);
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return BitConverter.ToSingle(Data, 40);
                            }
                            else
                            {
                                return BitConverter.ToSingle(Data, 36);
                            }
                        }
                    }
                }

                return float.NaN;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 36);
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                BitConverter.GetBytes(value).CopyTo(Data, 40);
                            }
                            else
                            {
                                BitConverter.GetBytes(value).CopyTo(Data, 36);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the distance at which this <see cref="StaticProp"/> will finish fading out.
        /// </summary>
        public float MaximumFadeDistance
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return BitConverter.ToSingle(Data, 40);
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return BitConverter.ToInt32(Data, 44);
                            }
                            else
                            {
                                return BitConverter.ToInt32(Data, 40);
                            }
                        }
                    }
                }

                return float.NaN;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 40);
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                BitConverter.GetBytes(value).CopyTo(Data, 44);
                            }
                            else
                            {
                                BitConverter.GetBytes(value).CopyTo(Data, 40);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the lighting origin of this <see cref="StaticProp"/>.
        /// </summary>
        public Vector3 LightingOrigin
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Vector3Extensions.ToVector3(Data, 44);
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return Vector3Extensions.ToVector3(Data, 48);
                            }
                            else
                            {
                                return Vector3Extensions.ToVector3(Data, 44);
                            }
                        }
                    }
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            value.GetBytes().CopyTo(Data, 44);
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                value.GetBytes().CopyTo(Data, 48);
                            }
                            else
                            {
                                value.GetBytes().CopyTo(Data, 44);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the fade distance scale for this <see cref="StaticProp"/>.
        /// </summary>
        public float ForcedFadeScale
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return BitConverter.ToSingle(Data, 56);
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return BitConverter.ToSingle(Data, 60);
                            }
                            else
                            {
                                return BitConverter.ToSingle(Data, 56);
                            }
                        }
                    }
                }

                return float.NaN;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 56);
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                BitConverter.GetBytes(value).CopyTo(Data, 60);
                            }
                            else
                            {
                                BitConverter.GetBytes(value).CopyTo(Data, 56);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum DirectX version supported for this <see cref="StaticProp"/> to be visible.
        /// </summary>
        public short MinimumDirectXLevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    switch (LumpVersion)
                    {
                        case 6:
                        case 7:
                        {
                            return BitConverter.ToInt16(Data, 60);
                        }
                    }
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    switch (LumpVersion)
                    {
                        case 6:
                        case 7:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 60);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum DirectX version supported for this <see cref="StaticProp"/> to be visible.
        /// </summary>
        public short MaximumDirectXLevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    switch (LumpVersion)
                    {
                        case 6:
                        case 7:
                        {
                            return BitConverter.ToInt16(Data, 62);
                        }
                    }
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    switch (LumpVersion)
                    {
                        case 6:
                        case 7:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 62);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum CPU level supported for this <see cref="StaticProp"/> to be visible.
        /// </summary>
        public byte MinimumCPULevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Data[60];
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return Data[64];
                            }
                            else
                            {
                                return Data[60];
                            }
                        }
                    }
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            Data[60] = value;
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                Data[64] = value;
                            }
                            else
                            {
                                Data[60] = value;
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum CPU level supported for this <see cref="StaticProp"/> to be visible.
        /// </summary>
        public byte MaximumCPULevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Data[61];
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return Data[65];
                            }
                            else
                            {
                                return Data[61];
                            }
                        }
                    }
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            Data[61] = value;
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                Data[65] = value;
                            }
                            else
                            {
                                Data[61] = value;
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum GPU level supported for this <see cref="StaticProp"/> to be visible.
        /// </summary>
        public byte MinimumGPULevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Data[62];
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return Data[66];
                            }
                            else
                            {
                                return Data[62];
                            }
                        }
                    }
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            Data[62] = value;
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                Data[66] = value;
                            }
                            else
                            {
                                Data[62] = value;
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum GPU level supported for this <see cref="StaticProp"/> to be visible.
        /// </summary>
        public byte MaximumGPULevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return Data[63];
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return Data[67];
                            }
                            else
                            {
                                return Data[63];
                            }
                        }
                    }
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            Data[63] = value;
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                Data[67] = value;
                            }
                            else
                            {
                                Data[63] = value;
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the diffuse modulation of this <see cref="StaticProp"/>.
        /// </summary>
        public Color DiffuseModulaton
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            return ColorExtensions.FromArgb(Data[67], Data[64], Data[65], Data[66]);
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                return ColorExtensions.FromArgb(Data[72], Data[69], Data[70], Data[71]);
                            }
                            else
                            {
                                return ColorExtensions.FromArgb(Data[67], Data[64], Data[65], Data[66]);
                            }
                        }
                    }
                }

                return ColorExtensions.FromArgb(255, 255, 255, 255);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    switch (LumpVersion)
                    {
                        case 7:
                        case 8:
                        case 10:
                        case 11:
                        case 12:
                        {
                            value.GetBytes().CopyTo(Data, 64);
                            break;
                        }
                        case 9:
                        {
                            if (Data.Length == 76)
                            {
                                value.GetBytes().CopyTo(Data, 69);
                            }
                            else
                            {
                                value.GetBytes().CopyTo(Data, 64);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the uniform scale of this <see cref="StaticProp"/>.
        /// </summary>
        public float Scale
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    switch (LumpVersion)
                    {
                        case 11:
                        {
                            return BitConverter.ToSingle(Data, 76);
                        }
                    }
                }

                return float.NaN;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    switch (LumpVersion)
                    {
                        case 11:
                        {
                            BitConverter.GetBytes(value).CopyTo(Data, 76);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Entity.Name"/> of this <see cref="StaticProp"/> <see cref="Entity"/>.
        /// </summary>
        public string Name
        {
            get
            {
                if (MapType == MapType.Source20)
                {
                    switch (LumpVersion)
                    {
                        case 5:
                        case 6:
                        {
                            if (Data.Length > 128)
                            {
                                return Data.ToNullTerminatedString(Data.Length - 128, 128);
                            }
                            return null;
                        }
                    }
                }

                return null;
            }
            set
            {
                if (MapType == MapType.Source20)
                {
                    switch (LumpVersion)
                    {
                        case 5:
                        case 6:
                        {
                            if (Data.Length > 128)
                            {
                                // Zero out the bytes
                                for (int i = 0; i < 128; ++i)
                                {
                                    Data[Data.Length - i - 1] = 0;
                                }
                                if (value != null)
                                {
                                    byte[] strBytes = Encoding.ASCII.GetBytes(value);
                                    Array.Copy(strBytes, 0, Data, Data.Length - 128, Math.Min(strBytes.Length, 127));
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="StaticProp"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="StaticProp"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public StaticProp(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="StaticProp"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="StaticProp"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="Model"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Model"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public StaticProp(StaticProp source, ILump parent)
        {
            Parent = parent;

            if (parent != null && parent.Bsp != null)
            {
                if (source.Parent != null && source.Parent.Bsp != null && source.Parent.Bsp.MapType == parent.Bsp.MapType && source.LumpVersion == parent.LumpInfo.version)
                {
                    Data = new byte[source.Data.Length];
                    Array.Copy(source.Data, Data, source.Data.Length);
                    return;
                }
                else
                {
                    //Data = new byte[GetStructLength(parent.Bsp.version, parent.LumpInfo.version)];
                }
            }
            else
            {
                //if (source.Parent?.Bsp != null)
                //{
                //    Data = new byte[GetStructLength(source.Parent.Bsp.version, source.Parent.LumpInfo.version)];
                //}
                //else
                //{
                //    Data = new byte[GetStructLength(MapType.Undefined, 0)];
                //}
            }

            Data = new byte[192]; // Maximum known length. GetStructLength doesn't have enough information currently.

            Origin = source.Origin;
            Angles = source.Angles;
            ModelIndex = source.ModelIndex;
            FirstLeafIndexIndex = source.FirstLeafIndexIndex;
            NumLeafIndices = source.NumLeafIndices;
            Solidity = source.Solidity;
            Flags = source.Flags;
            Skin = source.Skin;
            MinimumFadeDistance = source.MinimumFadeDistance;
            MaximumFadeDistance = source.MaximumFadeDistance;
            LightingOrigin = source.LightingOrigin;
            ForcedFadeScale = source.ForcedFadeScale;
            MinimumDirectXLevel = source.MinimumDirectXLevel;
            MaximumDirectXLevel = source.MaximumDirectXLevel;
            MinimumCPULevel = source.MinimumCPULevel;
            MaximumCPULevel = source.MaximumCPULevel;
            MinimumGPULevel = source.MinimumGPULevel;
            MaximumGPULevel = source.MaximumGPULevel;
            DiffuseModulaton = source.DiffuseModulaton;
            Scale = source.Scale;
            Name = source.Name;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="StaticProps"/> object.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="StaticProps"/> object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static StaticProps LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new StaticProps(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
        }

        /// <summary>
        /// This is a variable length structure. Return -1. The <see cref="StaticProps"/> class will handle object creation.
        /// </summary>
        /// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
        /// <param name="lumpVersion">The version number for the lump.</param>
        /// <returns>-1</returns>
        public static int GetStructLength(MapType mapType, int lumpVersion = 0)
        {
            return -1;
        }

    }
}
