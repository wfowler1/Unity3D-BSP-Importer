#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#if !UNITY_5_6_OR_NEWER
// UIVertex was introduced in Unity 4.5 but it only had color, position and one UV.
// From 4.6.0 until 5.5.6 it was missing two sets of UVs.
#define OLDUNITY
#endif
#endif

using System;
using System.Collections.Generic;
using System.Reflection;

namespace LibBSP {
#if UNITY
	using Color = UnityEngine.Color32;
	using Vector2 = UnityEngine.Vector2;
	using Vector3 = UnityEngine.Vector3;
	using Plane = UnityEngine.Plane;
#if !OLDUNITY
	using Vertex = UnityEngine.UIVertex;
#endif
#elif GODOT
	using Color = Godot.Color;
	using Vector2 = Godot.Vector2;
	using Vector3 = Godot.Vector3;
	using Plane = Godot.Plane;
#elif NEOAXIS
	using Color = NeoAxis.ColorByte;
	using Vector2 = NeoAxis.Vector2F;
	using Vector3 = NeoAxis.Vector3F;
	using Plane = NeoAxis.PlaneF;
	using Vertex = NeoAxis.StandardVertex;
#else
	using Color = System.Drawing.Color;
	using Vector2 = System.Numerics.Vector2;
	using Vector3 = System.Numerics.Vector3;
	using Plane = System.Numerics.Plane;
#endif

	/// <summary>
	/// Holds all the data for a face in a BSP map.
	/// </summary>
	/// <remarks>
	/// Faces is one of the more different lumps between versions. Some of these fields
	/// are only used by one format. However, there are some commonalities which make
	/// it worthwhile to unify these. All formats use a plane, a texture, vertices,
	/// and lightmaps in some way.
	/// </remarks>
	public struct Face : ILumpObject {

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
		public MapType MapType {
			get {
				if (Parent == null || Parent.Bsp == null) {
					return MapType.Undefined;
				}
				return Parent.Bsp.MapType;
			}
		}

		/// <summary>
		/// The version number of the <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
		/// </summary>
		public int LumpVersion {
			get {
				if (Parent == null) {
					return 0;
				}
				return Parent.LumpInfo.version;
			}
		}

		/// <summary>
		/// Gets the Plane used by this <see cref="Face"/>.
		/// </summary>
		public Plane Plane {
			get {
				return Parent.Bsp.Planes[PlaneIndex];
			}
		}

		/// <summary>
		/// Gets or sets the index of the Plane used by this <see cref="Face"/>.
		/// </summary>
		public int PlaneIndex {
			get {
				if (MapType == MapType.Nightfire
					|| MapType == MapType.Vindictus) {
					return BitConverter.ToInt32(Data, 0);
				} else if (MapType == MapType.Source17) {
					return BitConverter.ToUInt16(Data, 32);
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToUInt16(Data, 0);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire
					|| MapType == MapType.Vindictus) {
					bytes.CopyTo(Data, 0);
				} else if (MapType == MapType.Source17) {
					Data[32] = bytes[0];
					Data[33] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					Data[0] = bytes[0];
					Data[1] = bytes[1];
				}
			}
		}
		
		/// <summary>
		/// Does the <see cref="Face"/>'s normal point in the same direction as the Plane's?
		/// </summary>
		public bool PlaneSide {
			get {
				if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)) {
					return BitConverter.ToUInt16(Data, 2) > 0;
				} else if (MapType == MapType.Vindictus) {
					return Data[4] > 0;
				} else if (MapType == MapType.Source17) {
					return Data[34] > 0;
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return Data[2] > 0;
				}

				return true;
			}
			set {
				if (MapType == MapType.Vindictus) {
					Data[4] = (byte)(value ? 1 : 0);
				} else if (MapType == MapType.Source17) {
					Data[34] = (byte)(value ? 1 : 0);
				} else if (MapType.IsSubtypeOf(MapType.Source)
					|| MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)) {
					Data[2] = (byte)(value ? 1 : 0);
				}
			}
		}

		/// <summary>
		/// Does the <see cref="Face"/> lie on a node?
		/// </summary>
		public bool IsOnNode {
			get {
				if (MapType == MapType.Source17) {
					return Data[35] > 0;
				} else if (MapType == MapType.Vindictus) {
					return Data[5] > 0;
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return Data[3] > 0;
				}

				return false;
			}
			set {
				if (MapType == MapType.Source17) {
					Data[35] = (byte)(value ? 1 : 0);
				} else if (MapType == MapType.Vindictus) {
					Data[5] = (byte)(value ? 1 : 0);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					Data[3] = (byte)(value ? 1 : 0);
				}
			}
		}

		/// <summary>
		/// For formats which use <see cref="Edge"/>s, enumerates the <see cref="Edge"/> indices referenced
		/// by this <see cref="Face"/>. A negative index means the <see cref="Edge"/> is used backward from
		/// its second <see cref="Vertex"/> to its first.
		/// </summary>
		public IEnumerable<int> EdgeIndices {
			get {
				for (int i = 0; i < NumEdgeIndices; ++i) {
					yield return (int)Parent.Bsp.FaceEdges[FirstEdgeIndexIndex + i];
				}
			}
		}

		/// <summary>
		/// For formats which use <see cref="Edge"/>s, gets or sets the index of the first <see cref="Edge"/>
		/// index in this <see cref="Face"/>. A negative index means the <see cref="Edge"/> is used backward
		/// from its second <see cref="Vertex"/> to first.
		/// </summary>
		[Index("Edges")] public int FirstEdgeIndexIndex {
			get {
				if (MapType == MapType.Source17) {
					return BitConverter.ToInt32(Data, 36);
				} else if (MapType == MapType.Vindictus) {
					return BitConverter.ToInt32(Data, 8);
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToInt32(Data, 4);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Source17) {
					bytes.CopyTo(Data, 36);
				} else if (MapType == MapType.Vindictus) {
					bytes.CopyTo(Data, 8);
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					bytes.CopyTo(Data, 4);
				}
			}
		}
		
		/// <summary>
		/// For formats which use <see cref="Edge"/>s, gets or sets the count of <see cref="Edge"/> indices
		/// in this <see cref="Face"/>.
		/// </summary>
		[Count("Edges")] public int NumEdgeIndices {
			get {
				if (MapType == MapType.Source17) {
					return BitConverter.ToUInt16(Data, 40);
				} else if (MapType == MapType.Vindictus) {
					return BitConverter.ToInt32(Data, 12);
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToUInt16(Data, 8);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Source17) {
					Data[40] = bytes[0];
					Data[41] = bytes[1];
				} else if (MapType == MapType.Vindictus) {
					bytes.CopyTo(Data, 12);
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					Data[8] = bytes[0];
					Data[9] = bytes[1];
				}
			}
		}

		/// <summary>
		/// Gets the <see cref="LibBSP.Texture"/> referenced by this <see cref="Face"/>.
		/// </summary>
		public Texture Texture {
			get {
				return Parent.Bsp.Textures[TextureIndex];
			}
		}
		
		/// <summary>
		/// Gets or sets the index of the <see cref="LibBSP.Texture"/> used by this <see cref="Face"/>.
		/// </summary>
		public int TextureIndex {
			get {
				if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 24);
				} else if (MapType.IsSubtypeOf(MapType.Quake2)) {
					return BitConverter.ToUInt16(Data, 10);
				} else if (MapType.IsSubtypeOf(MapType.CoD)) {
					return BitConverter.ToInt16(Data, 0);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					return BitConverter.ToInt32(Data, 0);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 24);
				} else if (MapType.IsSubtypeOf(MapType.Quake2)) {
					Data[10] = bytes[0];
					Data[11] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.CoD)) {
					Data[0] = bytes[0];
					Data[1] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					bytes.CopyTo(Data, 0);
				}
			}
		}

		/// <summary>
		/// Enumerates the <see cref="Vertex"/> objects referenced by this <see cref="Face"/>.
		/// </summary>
		public IEnumerable<Vertex> Vertices {
			get {
				for (int i = 0; i < NumVertices; ++i) {
					yield return Parent.Bsp.Vertices[FirstVertexIndex + i];
				}
			}
		}
		
		/// <summary>
		/// Gets or sets the index of the first <see cref="Vertex"/> used by this <see cref="Face"/>.
		/// </summary>
		[Index("Vertices")] public int FirstVertexIndex {
			get {
				if (MapType == MapType.Nightfire
					|| MapType == MapType.CoD
					|| MapType == MapType.CoDDemo
					|| MapType == MapType.CoD2) {
					return BitConverter.ToInt32(Data, 4);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					return BitConverter.ToInt32(Data, 12);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire
					|| MapType == MapType.CoD
					|| MapType == MapType.CoDDemo
					|| MapType == MapType.CoD2) {
					bytes.CopyTo(Data, 4);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					bytes.CopyTo(Data, 12);
				}
			}
		}
		
		/// <summary>
		/// Gets or sets the number of <see cref="Vertex"/> objects used by this <see cref="Face"/>.
		/// </summary>
		[Count("Vertices")] public int NumVertices {
			get {
				if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 8);
				} else if (MapType == MapType.CoD4) {
					return BitConverter.ToInt16(Data, 16);
				} else if (MapType.IsSubtypeOf(MapType.CoD)) {
					return BitConverter.ToInt16(Data, 8);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					return BitConverter.ToInt32(Data, 16);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 8);
				} else if (MapType == MapType.CoD4) {
					Data[16] = bytes[0];
					Data[17] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.CoD)) {
					Data[8] = bytes[0];
					Data[9] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					bytes.CopyTo(Data, 16);
				}
			}
		}

		/// <summary>
		/// For Nightfire, gets the material referenced by this <see cref="Face"/>.
		/// </summary>
		public Texture Material {
			get {
				return Parent.Bsp.Materials[TextureIndex];
			}
		}
		
		/// <summary>
		/// For Nightfire, gets or sets the index of the material referenced by this <see cref="Face"/>.
		/// </summary>
		public int MaterialIndex {
			get {
				if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 28);
				}
				
				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 28);
				}
			}
		}

		/// <summary>
		/// Gets the <see cref="LibBSP.TextureInfo"/> referenced by this <see cref="Face"/>.
		/// </summary>
		public TextureInfo TextureInfo {
			get {
				return Parent.Bsp.TextureInfo[TextureInfoIndex];
			}
		}
		
		/// <summary>
		/// Gets or sets the index of the <see cref="LibBSP.TextureInfo"/> used by this <see cref="Face"/>.
		/// </summary>
		public int TextureInfoIndex {
			get {
				if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 32);
				} else if (MapType == MapType.Vindictus) {
					return BitConverter.ToInt32(Data, 16);
				} else if (MapType == MapType.Source17) {
					return BitConverter.ToUInt16(Data, 42);
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToUInt16(Data, 10);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 32);
				} else if (MapType == MapType.Vindictus) {
					bytes.CopyTo(Data, 16);
				} else if (MapType == MapType.Source17) {
					Data[42] = bytes[0];
					Data[43] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					Data[10] = bytes[0];
					Data[11] = bytes[1];
				}
			}
		}

		/// <summary>
		/// Gets the <see cref="LibBSP.TextureInfo"/> referenced by this <see cref="Face"/> for lightmaps.
		/// </summary>
		public TextureInfo LightmapTextureInfo {
			get {
				return Parent.Bsp.TextureInfo[LightmapTextureInfoIndex];
			}
		}

		/// <summary>
		/// Gets or sets the index of the <see cref="LibBSP.TextureInfo"/> used for lightmaps by this <see cref="Face"/>.
		/// </summary>
		public int LightmapTextureInfoIndex {
			get {
				if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 36);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 36);
				}
			}
		}

		/// <summary>
		/// Gets or sets the index of the <see cref="LibBSP.Displacement"/> using this <see cref="Face"/>.
		/// </summary>
		public int DisplacementIndex {
			get {
				if (MapType == MapType.Vindictus) {
					return BitConverter.ToInt32(Data, 20);
				} else if (MapType == MapType.Source17) {
					return BitConverter.ToInt16(Data, 44);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToInt16(Data, 12);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Vindictus) {
					bytes.CopyTo(Data, 20);
				} else if (MapType == MapType.Source17) {
					Data[44] = bytes[0];
					Data[45] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					Data[12] = bytes[0];
					Data[13] = bytes[1];
				}
			}
		}

		/// <summary>
		/// Gets or sets the surface fog volume ID of this <see cref="Face"/>.
		/// </summary>
		public int SurfaceFogVolumeID {
			get {
				if (MapType == MapType.Vindictus) {
					return BitConverter.ToInt32(Data, 24);
				} else if (MapType == MapType.Source17) {
					return BitConverter.ToInt16(Data, 46);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToInt16(Data, 14);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Vindictus) {
					bytes.CopyTo(Data, 24);
				} else if (MapType == MapType.Source17) {
					Data[46] = bytes[0];
					Data[47] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					Data[14] = bytes[0];
					Data[15] = bytes[1];
				}
			}
		}

		/// <summary>
		/// Gets the original <see cref="Face"/> which was split to create this <see cref="Face"/>.
		/// </summary>
		public Face OriginalFace {
			get {
				return Parent.Bsp.OriginalFaces[OriginalFaceIndex];
			}
		}

		/// <summary>
		/// Gets or sets the index of the original <see cref="Face"/> which was split to create this <see cref="Face"/>.
		/// </summary>
		public int OriginalFaceIndex {
			get {
				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						return BitConverter.ToInt32(Data, 60);
					} else {
						return BitConverter.ToInt32(Data, 56);
					}
				} else if (MapType == MapType.Source17) {
					return BitConverter.ToInt32(Data, 96);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToInt32(Data, 44);
				}
				
				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						bytes.CopyTo(Data, 60);
					} else {
						bytes.CopyTo(Data, 56);
					}
				} else if (MapType == MapType.Source17) {
					bytes.CopyTo(Data, 96);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					bytes.CopyTo(Data, 44);
				}
			}
		}
		
		/// <summary>
		/// Gets or sets the type (flags) of this face.
		/// </summary>
		public int Type {
			get {
				if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return BitConverter.ToInt32(Data, 8);
				} else if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 20);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					bytes.CopyTo(Data, 8);
				} else if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 20);
				}
			}
		}

		/// <summary>
		/// Gets or sets the Effect used by this face.
		/// </summary>
		public int Effect {
			get {
				if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return BitConverter.ToInt32(Data, 4);
				}
					
				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					bytes.CopyTo(Data, 4);
				}
			}
		}

		/// <summary>
		/// For formats which use triangle corner indices, enumerates the indices of <see cref="Vertex"/>
		/// objects used by this <see cref="Face"/>.
		/// </summary>
		public IEnumerable<int> Indices {
			get {
				for (int i = 0; i < NumIndices; ++i) {
					yield return (int)Parent.Bsp.Indices[FirstIndexIndex + i];
				}
			}
		}
		
		/// <summary>
		/// For formats which use triangle corner indices, gets or sets the index first <see cref="Vertex"/> index
		/// used by this <see cref="Face"/>.
		/// </summary>
		[Index("Indices")] public int FirstIndexIndex {
			get {
				if (MapType == MapType.Nightfire
					|| MapType == MapType.CoD
					|| MapType == MapType.CoDDemo
					|| MapType == MapType.CoD2) {
					return BitConverter.ToInt32(Data, 12);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					return BitConverter.ToInt32(Data, 20);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire
					|| MapType == MapType.CoD
					|| MapType == MapType.CoDDemo
					|| MapType == MapType.CoD2) {
					bytes.CopyTo(Data, 12);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					bytes.CopyTo(Data, 20);
				}
			}
		}

		/// <summary>
		/// For formats which use triangle corner indices, gets or sets the count of <see cref="Vertex"/> indices
		/// used by this <see cref="Face"/>.
		/// </summary>
		[Count("Indices")] public int NumIndices {
			get {
				if (MapType == MapType.CoD4) {
					return BitConverter.ToInt16(Data, 18);
				} else if (MapType.IsSubtypeOf(MapType.CoD)) {
					return BitConverter.ToInt16(Data, 10);
				} else if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 16);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					return BitConverter.ToInt32(Data, 24);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.CoD4) {
					Data[18] = bytes[0];
					Data[19] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.CoD)) {
					Data[10] = bytes[0];
					Data[11] = bytes[1];
				} else if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 16);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					bytes.CopyTo(Data, 24);
				}
			}
		}

		/// <summary>
		/// Gets or sets the average light colors for this <see cref="Face"/> in a Source v17 BSP.
		/// </summary>
		public Color[] AverageLightColors {
			get {
				if (MapType == MapType.Source17) {
					Color[] colors = new Color[8];
					for (int i = 0; i < colors.Length; ++i) {
						colors[i] = ColorExtensions.FromArgb(Data[(i * 4) + 3], Data[(i * 4)], Data[(i * 4) + 1], Data[(i * 4) + 2]);
					}
					return colors;
				}

				return new Color[0];
			}
			set {
				if (MapType == MapType.Source17) {
					for (int i = 0; i < value.Length; ++i) {
						value[i].GetBytes().CopyTo(Data, i * 4);
					}
				}
			}
		}
		
		/// <summary>
		/// Gets or sets the flags for lightmap style used by this <see cref="Face"/>.
		/// </summary>
		public byte[] LightmapStyles {
			get {
				if (MapType == MapType.SiN) {
					byte[] bytes = new byte[16];
					Array.Copy(Data, 12, bytes, 0, bytes.Length);
					return bytes;
				} else if (MapType == MapType.SoF) {
					byte[] bytes = new byte[4];
					Array.Copy(Data, 22, bytes, 0, bytes.Length);
					return bytes;
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)) {
					byte[] bytes = new byte[4];
					Array.Copy(Data, 12, bytes, 0, bytes.Length);
					return bytes;
				} else if (MapType == MapType.Vindictus) {
					byte[] bytes = new byte[8];
					if (LumpVersion == 2) {
						Array.Copy(Data, 28, bytes, 0, bytes.Length);
					} else {
						Array.Copy(Data, 24, bytes, 0, bytes.Length);
					}
					return bytes;
				} else if (MapType == MapType.Source17) {
					byte[] bytes = new byte[8];
					Array.Copy(Data, 48, bytes, 0, bytes.Length);
					return bytes;
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					byte[] bytes = new byte[4];
					Array.Copy(Data, 16, bytes, 0, bytes.Length);
					return bytes;
				} else if (MapType == MapType.Nightfire) {
					byte[] bytes = new byte[4];
					Array.Copy(Data, 40, bytes, 0, bytes.Length);
					return bytes;
				}

				return new byte[0];
			}
			set {
				if (MapType == MapType.SiN) {
					Array.Copy(value, 0, Data, 12, Math.Min(value.Length, 16));
				} else if (MapType == MapType.SoF) {
					Array.Copy(value, 0, Data, 22, Math.Min(value.Length, 4));
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)) {
					Array.Copy(value, 0, Data, 12, Math.Min(value.Length, 4));
				} else if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						Array.Copy(value, 0, Data, 28, Math.Min(value.Length, 8));
					} else {
						Array.Copy(value, 0, Data, 24, Math.Min(value.Length, 8));
					}
				} else if (MapType == MapType.Source17) {
					Array.Copy(value, 0, Data, 48, Math.Min(value.Length, 8));
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					Array.Copy(value, 0, Data, 16, Math.Min(value.Length, 4));
				} else if (MapType == MapType.Nightfire) {
					Array.Copy(value, 0, Data, 40, Math.Min(value.Length, 4));
				}
			}
		}

		/// <summary>
		/// Gets or sets the flags for day lightmap style used by this <see cref="Face"/> in a Source v17 BSP.
		/// </summary>
		public byte[] DayLightStyle {
			get {
				if (MapType == MapType.Source17) {
					byte[] bytes = new byte[8];
					Array.Copy(Data, 56, bytes, 0, bytes.Length);
					return bytes;
				}

				return new byte[0];
			}
			set {
				if (MapType == MapType.Source17) {
					Array.Copy(value, 0, Data, 56, Math.Min(value.Length, 8));
				}
			}
		}

		/// <summary>
		/// Gets or sets the flags for night lightmap style used by this <see cref="Face"/> in a Source v17 BSP.
		/// </summary>
		public byte[] NightLightStyle {
			get {
				if (MapType == MapType.Source17) {
					byte[] bytes = new byte[8];
					Array.Copy(Data, 64, bytes, 0, bytes.Length);
					return bytes;
				}

				return new byte[0];
			}
			set {
				if (MapType == MapType.Source17) {
					Array.Copy(value, 0, Data, 64, Math.Min(value.Length, 8));
				}
			}
		}

		/// <summary>
		/// Gets or sets the offset or index into the lightmap data this <see cref="Face"/> uses.
		/// </summary>
		public int Lightmap {
			get {
				if (MapType == MapType.Raven) {
					return BitConverter.ToInt32(Data, 36);
				} else if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 44);
				} else if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						return BitConverter.ToInt32(Data, 36);
					} else {
						return BitConverter.ToInt32(Data, 32);
					}
				} else if (MapType == MapType.Source17) {
					return BitConverter.ToInt32(Data, 72);
				} else if (MapType.IsSubtypeOf(MapType.CoD) && MapType != MapType.CoD4) {
					return BitConverter.ToInt16(Data, 2);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)
					|| MapType == MapType.SiN
					|| MapType == MapType.SoF) {
					return BitConverter.ToInt32(Data, 28);
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)) {
					return BitConverter.ToInt32(Data, 16);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToInt32(Data, 20);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Raven) {
					bytes.CopyTo(Data, 36);
				} else if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 44);
				} else if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						bytes.CopyTo(Data, 36);
					} else {
						bytes.CopyTo(Data, 32);
					}
				} else if (MapType == MapType.Source17) {
					bytes.CopyTo(Data, 72);
				} else if (MapType.IsSubtypeOf(MapType.CoD) && MapType != MapType.CoD4) {
					Data[2] = bytes[0];
					Data[3] = bytes[1];
				} else if (MapType.IsSubtypeOf(MapType.Quake3)
					|| MapType == MapType.SiN
					|| MapType == MapType.SoF) {
					bytes.CopyTo(Data, 28);
				} else if (MapType.IsSubtypeOf(MapType.Quake)
					|| MapType.IsSubtypeOf(MapType.Quake2)) {
					bytes.CopyTo(Data, 16);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					bytes.CopyTo(Data, 20);
				}
			}
		}

		/// <summary>
		/// Gets or sets the second index into the lightmap data this <see cref="Face"/> uses, in a <see cref="MapType.Raven"/> BSP.
		/// </summary>
		public int Lightmap2 {
			get {
				if (MapType == MapType.Raven) {
					return BitConverter.ToInt32(Data, 40);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Raven) {
					bytes.CopyTo(Data, 40);
				}
			}
		}

		/// <summary>
		/// Gets or sets the third index into the lightmap data this <see cref="Face"/> uses, in a <see cref="MapType.Raven"/> BSP.
		/// </summary>
		public int Lightmap3 {
			get {
				if (MapType == MapType.Raven) {
					return BitConverter.ToInt32(Data, 44);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Raven) {
					bytes.CopyTo(Data, 44);
				}
			}
		}

		/// <summary>
		/// Gets or sets the fourth index into the lightmap data this <see cref="Face"/> uses, in a <see cref="MapType.Raven"/> BSP.
		/// </summary>
		public int Lightmap4 {
			get {
				if (MapType == MapType.Raven) {
					return BitConverter.ToInt32(Data, 48);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Raven) {
					bytes.CopyTo(Data, 48);
				}
			}
		}

		/// <summary>
		/// Gets or sets the area of this <see cref="Face"/>.
		/// </summary>
		public float Area {
			get {
				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						return BitConverter.ToSingle(Data, 40);
					} else {
						return BitConverter.ToSingle(Data, 36);
					}
				} else if (MapType == MapType.Source17) {
					return BitConverter.ToSingle(Data, 76);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToSingle(Data, 24);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						bytes.CopyTo(Data, 40);
					} else {
						bytes.CopyTo(Data, 36);
					}
				} else if (MapType == MapType.Source17) {
					bytes.CopyTo(Data, 76);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					bytes.CopyTo(Data, 24);
				}
			}
		}

		/// <summary>
		/// Gets the lightmap texture mins used by this <see cref="Face"/>.
		/// </summary>
		public Vector2 LightmapStart {
			get {
				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						return new Vector2(BitConverter.ToInt32(Data, 44), BitConverter.ToInt32(Data, 48));
					} else {
						return new Vector2(BitConverter.ToInt32(Data, 40), BitConverter.ToInt32(Data, 44));
					}
				} else if (MapType == MapType.Raven) {
					return new Vector2(BitConverter.ToInt32(Data, 52), BitConverter.ToInt32(Data, 56));
				} else if (MapType == MapType.Source17) {
					return new Vector2(BitConverter.ToInt32(Data, 80), BitConverter.ToInt32(Data, 84));
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return new Vector2(BitConverter.ToInt32(Data, 32), BitConverter.ToInt32(Data, 36));
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return new Vector2(BitConverter.ToInt32(Data, 28), BitConverter.ToInt32(Data, 32));
				}

				return new Vector2(0, 0);
			}
			set {
				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						BitConverter.GetBytes((int)value.X()).CopyTo(Data, 44);
						BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 48);
					} else {
						BitConverter.GetBytes((int)value.X()).CopyTo(Data, 40);
						BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 44);
					}
				} else if (MapType == MapType.Raven) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 52);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 56);
				} else if (MapType == MapType.Source17) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 80);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 84);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 32);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 36);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 28);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 32);
				}
			}
		}

		/// <summary>
		/// Gets the second lightmap texture mins used by this <see cref="Face"/>, in a <see cref="MapType.Raven"/> BSP.
		/// </summary>
		public Vector2 Lightmap2Start {
			get {
				if (MapType == MapType.Raven) {
					return new Vector2(BitConverter.ToInt32(Data, 60), BitConverter.ToInt32(Data, 64));
				}

				return new Vector2(0, 0);
			}
			set {
				if (MapType == MapType.Raven) {
					value.GetBytes().CopyTo(Data, 60);
				}
			}
		}

		/// <summary>
		/// Gets the third lightmap texture mins used by this <see cref="Face"/>, in a <see cref="MapType.Raven"/> BSP.
		/// </summary>
		public Vector2 Lightmap3Start {
			get {
				if (MapType == MapType.Raven) {
					return new Vector2(BitConverter.ToInt32(Data, 68), BitConverter.ToInt32(Data, 72));
				}

				return new Vector2(0, 0);
			}
			set {
				if (MapType == MapType.Raven) {
					value.GetBytes().CopyTo(Data, 68);
				}
			}
		}

		/// <summary>
		/// Gets the fourth lightmap texture mins used by this <see cref="Face"/>, in a <see cref="MapType.Raven"/> BSP.
		/// </summary>
		public Vector2 Lightmap4Start {
			get {
				if (MapType == MapType.Raven) {
					return new Vector2(BitConverter.ToInt32(Data, 76), BitConverter.ToInt32(Data, 80));
				}

				return new Vector2(0, 0);
			}
			set {
				if (MapType == MapType.Raven) {
					value.GetBytes().CopyTo(Data, 76);
				}
			}
		}

		/// <summary>
		/// Gets the lightmap texture size used by this <see cref="Face"/> in luxels.
		/// </summary>
		public Vector2 LightmapSize {
			get {
				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						return new Vector2(BitConverter.ToInt32(Data, 52), BitConverter.ToInt32(Data, 56));
					} else {
						return new Vector2(BitConverter.ToInt32(Data, 48), BitConverter.ToInt32(Data, 52));
					}
				} else if (MapType == MapType.Raven) {
					return new Vector2(BitConverter.ToInt32(Data, 84), BitConverter.ToInt32(Data, 88));
				} else if (MapType == MapType.Source17) {
					return new Vector2(BitConverter.ToInt32(Data, 88), BitConverter.ToInt32(Data, 92));
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return new Vector2(BitConverter.ToInt32(Data, 40), BitConverter.ToInt32(Data, 44));
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return new Vector2(BitConverter.ToInt32(Data, 36), BitConverter.ToInt32(Data, 40));
				}

				return new Vector2(0, 0);
			}
			set {
				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						BitConverter.GetBytes((int)value.X()).CopyTo(Data, 52);
						BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 56);
					} else {
						BitConverter.GetBytes((int)value.X()).CopyTo(Data, 48);
						BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 52);
					}
				} else if (MapType == MapType.Raven) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 84);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 88);
				} else if (MapType == MapType.Source17) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 88);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 92);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 40);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 44);
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 36);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 40);
				}
			}
		}

		/// <summary>
		/// Gets or sets the lightmap origin for this <see cref="Face"/>.
		/// </summary>
		public Vector3 LightmapOrigin {
			get {
				if (MapType == MapType.Raven) {
					return Vector3Extensions.ToVector3(Data, 92);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return Vector3Extensions.ToVector3(Data, 48);
				}

				return new Vector3(0, 0, 0);
			}
			set {
				if (MapType == MapType.Raven) {
					value.GetBytes().CopyTo(Data, 92);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					value.GetBytes().CopyTo(Data, 48);
				}
			}
		}

		/// <summary>
		/// Gets or sets the lightmap U axis for this <see cref="Face"/>.
		/// </summary>
		public Vector3 LightmapUAxis {
			get {
				if (MapType == MapType.Raven) {
					return Vector3Extensions.ToVector3(Data, 104);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return Vector3Extensions.ToVector3(Data, 60);
				}

				return new Vector3(0, 0, 0);
			}
			set {
				if (MapType == MapType.Raven) {
					value.GetBytes().CopyTo(Data, 104);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					value.GetBytes().CopyTo(Data, 60);
				}
			}
		}

		/// <summary>
		/// Gets or sets the lightmap V Axis for this <see cref="Face"/>.
		/// </summary>
		public Vector3 LightmapVAxis {
			get {
				if (MapType == MapType.Raven) {
					return Vector3Extensions.ToVector3(Data, 116);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return Vector3Extensions.ToVector3(Data, 72);
				}

				return new Vector3(0, 0, 0);
			}
			set {
				if (MapType == MapType.Raven) {
					value.GetBytes().CopyTo(Data, 116);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					value.GetBytes().CopyTo(Data, 72);
				}
			}
		}

		/// <summary>
		/// Gets or sets the normal for this <see cref="Face"/>.
		/// </summary>
		public Vector3 Normal {
			get {
				if (MapType == MapType.Raven) {
					return Vector3Extensions.ToVector3(Data, 128);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return Vector3Extensions.ToVector3(Data, 84);
				}

				return new Vector3(0, 0, 0);
			}
			set {
				if (MapType == MapType.Raven) {
					value.GetBytes().CopyTo(Data, 128);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					value.GetBytes().CopyTo(Data, 84);
				}
			}
		}

		/// <summary>
		/// Gets or sets the size of the <see cref="Patch"/> used by this <see cref="Face"/>.
		/// </summary>
		public Vector2 PatchSize {
			get {
				if (MapType == MapType.Raven) {
					return new Vector2(BitConverter.ToInt32(Data, 140), BitConverter.ToInt32(Data, 144));
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					return new Vector2(BitConverter.ToInt32(Data, 96), BitConverter.ToInt32(Data, 100));
				}

				return new Vector2(0, 0);
			}
			set {
				if (MapType == MapType.Raven) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 140);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 144);
				} else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)) {
					BitConverter.GetBytes((int)value.X()).CopyTo(Data, 96);
					BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 100);
				}
			}
		}

		/// <summary>
		/// Gets or sets number of primitives for this <see cref="Face"/>.
		/// </summary>
		public int NumPrimitives {
			get {
				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						return BitConverter.ToInt32(Data, 64);
					} else {
						return BitConverter.ToInt32(Data, 60);
					}
				} else if (MapType.IsSubtypeOf(MapType.Source) && MapType != MapType.Source17) {
					return BitConverter.ToInt16(Data, 48);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						bytes.CopyTo(Data, 64);
					} else {
						bytes.CopyTo(Data, 60);
					}
				} else if (MapType.IsSubtypeOf(MapType.Source) && MapType != MapType.Source17) {
					Data[48] = bytes[0];
					Data[49] = bytes[1];
				}
			}
		}

		/// <summary>
		/// Gets or sets first primitive used by this <see cref="Face"/>.
		/// </summary>
		public int FirstPrimitive {
			get {
				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						return BitConverter.ToInt32(Data, 68);
					} else {
						return BitConverter.ToInt32(Data, 64);
					}
				} else if (MapType.IsSubtypeOf(MapType.Source) && MapType != MapType.Source17) {
					return BitConverter.ToInt16(Data, 50);
				}
			
				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						bytes.CopyTo(Data, 68);
					} else {
						bytes.CopyTo(Data, 64);
					}
				} else if (MapType.IsSubtypeOf(MapType.Source) && MapType != MapType.Source17) {
					Data[50] = bytes[0];
					Data[51] = bytes[1];
				}
			}
		}

		/// <summary>
		/// Gets or sets the smoothing groups for this <see cref="Face"/>.
		/// </summary>
		public int SmoothingGroups {
			get {
				if (MapType == MapType.Source17) {
					return BitConverter.ToInt32(Data, 100);
				} else if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						return BitConverter.ToInt32(Data, 72);
					} else {
						return BitConverter.ToInt32(Data, 68);
					}
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToInt16(Data, 52);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Source17) {
					bytes.CopyTo(Data, 100);
				} else if (MapType == MapType.Vindictus) {
					if (LumpVersion == 2) {
						bytes.CopyTo(Data, 72);
					} else {
						bytes.CopyTo(Data, 68);
					}
				} else if (MapType.IsSubtypeOf(MapType.Source)) {
					Data[52] = bytes[0];
					Data[53] = bytes[1];
				}
			}
		}

		/// <summary>
		/// Creates a new <see cref="Face"/> object from a <c>byte</c> array.
		/// </summary>
		/// <param name="data"><c>byte</c> array to parse.</param>
		/// <param name="parent">The <see cref="ILump"/> this <see cref="Face"/> came from.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		public Face(byte[] data, ILump parent = null) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			Data = data;
			Parent = parent;
		}

		/// <summary>
		/// Creates a new <see cref="Face"/> by copying the fields in <paramref name="source"/>, using
		/// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
		/// to use when creating the new <see cref="Face"/>.
		/// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
		/// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
		/// </summary>
		/// <param name="source">The <see cref="Face"/> to copy.</param>
		/// <param name="parent">
		/// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Face"/>.
		/// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
		/// </param>
		public Face(Face source, ILump parent) {
			Parent = parent;

			if (parent != null && parent.Bsp != null) {
				if (source.Parent != null && source.Parent.Bsp != null && source.Parent.Bsp.MapType == parent.Bsp.MapType && source.LumpVersion == parent.LumpInfo.version) {
					Data = new byte[source.Data.Length];
					Array.Copy(source.Data, Data, source.Data.Length);
					return;
				} else {
					Data = new byte[GetStructLength(parent.Bsp.MapType, parent.LumpInfo.version)];
				}
			} else {
				if (source.Parent != null && source.Parent.Bsp != null) {
					Data = new byte[GetStructLength(source.Parent.Bsp.MapType, source.Parent.LumpInfo.version)];
				} else {
					Data = new byte[GetStructLength(MapType.Undefined, 0)];
				}
			}

			PlaneIndex = source.PlaneIndex;
			PlaneSide = source.PlaneSide;
			IsOnNode = source.IsOnNode;
			FirstEdgeIndexIndex = source.FirstEdgeIndexIndex;
			NumEdgeIndices = source.NumEdgeIndices;
			TextureIndex = source.TextureIndex;
			FirstVertexIndex = source.FirstVertexIndex;
			NumVertices = source.NumVertices;
			MaterialIndex = source.MaterialIndex;
			TextureInfoIndex = source.TextureInfoIndex;
			LightmapTextureInfoIndex = source.LightmapTextureInfoIndex;
			DisplacementIndex = source.DisplacementIndex;
			SurfaceFogVolumeID = source.SurfaceFogVolumeID;
			OriginalFaceIndex = source.OriginalFaceIndex;
			Type = source.Type;
			Effect = source.Effect;
			FirstIndexIndex = source.FirstIndexIndex;
			NumIndices = source.NumIndices;
			AverageLightColors = source.AverageLightColors;
			LightmapStyles = source.LightmapStyles;
			DayLightStyle = source.DayLightStyle;
			NightLightStyle = source.NightLightStyle;
			Lightmap = source.Lightmap;
			Lightmap2 = source.Lightmap2;
			Lightmap3 = source.Lightmap3;
			Lightmap4 = source.Lightmap4;
			Area = source.Area;
			LightmapStart = source.LightmapStart;
			Lightmap2Start = source.Lightmap2Start;
			Lightmap3Start = source.Lightmap3Start;
			Lightmap4Start = source.Lightmap4Start;
			LightmapSize = source.LightmapSize;
			LightmapOrigin = source.LightmapOrigin;
			LightmapUAxis = source.LightmapUAxis;
			LightmapVAxis = source.LightmapVAxis;
			Normal = source.Normal;
			PatchSize = source.PatchSize;
			NumPrimitives = source.NumPrimitives;
			FirstPrimitive = source.FirstPrimitive;
			SmoothingGroups = source.SmoothingGroups;
		}

		/// <summary>
		/// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Face}"/>.
		/// </summary>
		/// <param name="data">The data to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <returns>A <see cref="Lump{Face}"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
		public static Lump<Face> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			return new Lump<Face>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
		}

		/// <summary>
		/// Gets the length of this struct's data for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.
		/// </summary>
		/// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
		/// <param name="lumpVersion">The version number for the lump.</param>
		/// <returns>The length, in <c>byte</c>s, of this struct.</returns>
		/// <exception cref="ArgumentException">This struct is not valid or is not implemented for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.</exception>
		public static int GetStructLength(MapType mapType, int lumpVersion = 0) {
			if (mapType == MapType.CoD4) {
				return 24;
			} else if (mapType == MapType.SiN) {
				return 36;
			} else if (mapType == MapType.SoF) {
				return 40;
			} else if (mapType == MapType.Nightfire) {
				return 48;
			} else if (mapType == MapType.Source17) {
				return 104;
			} else if (mapType == MapType.Vindictus) {
				if (lumpVersion == 2) {
					return 76;
				} else {
					return 72;
				}
			} else if (mapType == MapType.Raven) {
				return 148;
			} else if (mapType.IsSubtypeOf(MapType.FAKK2)
				|| mapType.IsSubtypeOf(MapType.MOHAA)) {
				return 108;
			} else if (mapType.IsSubtypeOf(MapType.STEF2)) {
				return 132;
			} else if (mapType.IsSubtypeOf(MapType.CoD)) {
				return 16;
			} else if (mapType.IsSubtypeOf(MapType.Quake)
				|| mapType.IsSubtypeOf(MapType.Quake2)) {
				return 20;
			} else if (mapType.IsSubtypeOf(MapType.Quake3)) {
				return 104;
			} else if (mapType.IsSubtypeOf(MapType.Source)) {
				return 56;
			}

			throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
		}

		/// <summary>
		/// Gets the index for this lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
		public static int GetIndexForLump(MapType type) {
			if (type.IsSubtypeOf(MapType.FAKK2)
				|| type.IsSubtypeOf(MapType.MOHAA)) {
				return 3;
			} else if (type.IsSubtypeOf(MapType.STEF2)) {
				return 5;
			} else if (type.IsSubtypeOf(MapType.Quake2)
				|| type == MapType.CoD
				|| type == MapType.CoDDemo) {
				return 6;
			} else if (type == MapType.Nightfire
				|| type == MapType.CoD4) {
				return 9;
			} else if (type == MapType.Quake3
				|| type == MapType.Raven
				|| type == MapType.ET) {
				return 13;
			} else if (type.IsSubtypeOf(MapType.Source)
				|| type.IsSubtypeOf(MapType.Quake)
				|| type == MapType.CoD2) {
				return 7;
			}

			return -1;
		}

		/// <summary>
		/// Gets the index for the original faces lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
		public static int GetIndexForOriginalFacesLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)) {
				return 27;
			} else if (type == MapType.CoD4) {
				// Not sure where else to put this. This is the simple surfaces lump?
				return 47;
			}

			return -1;
		}

	}
}
