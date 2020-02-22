using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LibBSP;

namespace BSPImporter {
#if UNITY_5_6_OR_NEWER
	using Vertex = UIVertex;
#endif

	/// <summary>
	/// Static class with helper utilities and extension methods for <see cref="Mesh"/> objects, to help
	/// with building meshes for BSPs.
	/// </summary>
	public static class MeshUtils {
		
		public const int maxMeshVertices = 32767;
		public const float inch2MeterScale = 0.0254f;

		/// <summary>
		/// Loads all <see cref="Vertex"/> objects references by <paramref name="face"/> and loads their
		/// data into a <see cref="Mesh"/>, along with triangles, and returns the result.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> which <paramref name="face"/> comes from.</param>
		/// <param name="face">The <see cref="Face"/> to build a <see cref="Mesh"/> from.</param>
		/// <returns>A <see cref="Mesh"/> built from the data given by <paramref name="face"/>.</returns>
		public static Mesh CreateFaceMesh(BSP bsp, Face face, Vector2 dims, int curveTessellationLevel) {
			Mesh mesh = null;
			if (face.NumVertices > 0) {
				if (face.NumIndices == 0 && face.Type == 2) {
					mesh = CreatePatchMesh(bsp, face, curveTessellationLevel);
				} else {
					mesh = LoadVerticesFromFace(bsp, face);
				}
			} else if (face.NumEdgeIndices > 0) {
				mesh = LoadVerticesFromEdges(bsp, face);
			}
			if (mesh != null) {
				TextureInfo textureInfo = bsp.GetTextureInfo(face);
				if (textureInfo.Data != null && textureInfo.Data.Length > 0) {
					mesh.CalculateUVs(textureInfo, dims);
				}
				mesh.NegateVs();
			}
			return mesh;
		}

		/// <summary>
		/// Given an <see cref="IList{T}"/>&lt;<see cref="Vertex"/>&gt;, this will build a <see cref="Mesh"/> using
		/// those vertices and their normals, UVs, etc. This will NOT build the <see cref="Mesh"/> triangles array.
		/// </summary>
		/// <param name="vertices">The vertices to build a mesh from.</param>
		/// <returns>A <see cref="Mesh"/> object built using <paramref name="vertices"/>.</returns>
		public static Mesh LoadVertices(IList<Vertex> vertices) {
			Mesh mesh = new Mesh();
			mesh.Clear();

			Vector3[] positions = new Vector3[vertices.Count];
			Vector3[] normals = new Vector3[vertices.Count];
			Vector2[] uv = new Vector2[vertices.Count];
			Vector2[] uv2 = new Vector2[vertices.Count];
			Color[] colors = new Color[vertices.Count];
			for (int i = 0; i < vertices.Count; ++i) {
				Vertex current = vertices[i].SwizzleYZ().ScaleInch2Meter();
				positions[i] = current.position;
				normals[i] = current.normal;
				uv[i] = current.uv0;
				uv2[i] = current.uv1;
				colors[i] = current.color;
			}
			mesh.vertices = positions;
			mesh.normals = normals;
			mesh.uv = uv;
			mesh.uv2 = uv2;
			mesh.colors = colors;

			return mesh;
		}

		/// <summary>
		/// Given a <see cref="Face"/> with a list of <see cref="Vertex"/> objects and triangles, loads those
		/// vertices into a <see cref="Mesh"/> object and returns it.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> object <paramref name="face"/> came from.</param>
		/// <param name="face">The <see cref="Face"/> to build a <see cref="Mesh"/> from.</param>
		/// <returns>The <see cref="Mesh"/> generated from the vertices and triangles in <see cref="Face"/>.</returns>
		public static Mesh LoadVerticesFromFace(BSP bsp, Face face) {
			Mesh mesh = LoadVertices(bsp.GetReferencedObjects<Vertex>(face, "vertices"));
			List<long> indices = bsp.GetReferencedObjects<long>(face, "indices");
			int[] triangles = new int[indices.Count];
			for (int i = 0; i < indices.Count; ++i) {
				triangles[i] = (int)indices[i];
			}
			mesh.triangles = triangles;

			return mesh;
		}

		/// <summary>
		/// Given a <see cref="Face"/> with a list of <see cref="Edge"/>s, generates a <see cref="Mesh"/> with the
		/// correct vertices and tessellates them into triangle fans. This is fine, since BSP faces are guaranteed
		/// to be closed and convex.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> object <paramref name="face"/> came from.</param>
		/// <param name="face">The <see cref="Face"/> to build a <see cref="Mesh"/> from.</param>
		/// <returns>The <see cref="Mesh"/> generated from the <see cref="Edge"/>s referenced by <see cref="Face"/>.</returns>
		public static Mesh LoadVerticesFromEdges(BSP bsp, Face face) {
			Vertex[] vertices = new Vertex[face.NumEdgeIndices];
			int[] triangles = new int[(face.NumEdgeIndices - 2) * 3];
			int firstSurfEdge = (int)bsp.surfEdges[face.FirstEdgeIndexIndex];
			if (firstSurfEdge > 0) {
				vertices[0] = bsp.vertices[bsp.edges[firstSurfEdge].FirstVertexIndex];
			} else {
				vertices[0] = bsp.vertices[bsp.edges[-firstSurfEdge].SecondVertexIndex];
			}

			int currtriangle = 0;
			int currvert = 1;
			for (int i = 1; i < face.NumEdgeIndices - 1; i++) {
				int currSurfEdge = (int)bsp.surfEdges[face.FirstEdgeIndexIndex + i];
				Vertex first;
				Vertex second;
				if (currSurfEdge > 0) {
					first = bsp.vertices[bsp.edges[currSurfEdge].FirstVertexIndex];
					second = bsp.vertices[bsp.edges[currSurfEdge].SecondVertexIndex];
				} else {
					first = bsp.vertices[bsp.edges[-currSurfEdge].SecondVertexIndex];
					second = bsp.vertices[bsp.edges[-currSurfEdge].FirstVertexIndex];
				}
				if (first.position != vertices[0].position && second.position != vertices[0].position) { // All tris involve first vertex, so disregard edges referencing it
					triangles[currtriangle * 3] = 0;
					bool firstFound = false;
					bool secondFound = false;
					for (int j = 1; j < currvert; j++) {
						if (first.position == vertices[j].position) {
							triangles[(currtriangle * 3) + 1] = j;
							firstFound = true;
						}
					}
					if (!firstFound) {
						vertices[currvert] = first;
						triangles[(currtriangle * 3) + 1] = currvert;
						currvert++;
					}
					for (int j = 1; j < currvert; j++) {
						if (second.position == vertices[j].position) {
							triangles[(currtriangle * 3) + 2] = j;
							secondFound = true;
						}
					}
					if (!secondFound) {
						vertices[currvert] = second;
						triangles[(currtriangle * 3) + 2] = currvert;
						currvert++;
					}
					currtriangle++;
				}
			}

			Mesh mesh = LoadVertices(vertices);
			mesh.triangles = triangles;

			return mesh;
		}

		/// <summary>
		/// Calculates the UVs on all vertices in this <see cref="Mesh"/> given a <see cref="TextureInfo"/>
		/// containing U and V axis projections.
		/// </summary>
		/// <param name="mesh">This <see cref="Mesh"/>.</param>
		/// <param name="textureInfo">
		/// A <see cref="TextureInfo"/> containing axes for projecting the texture onto the <see cref="Face"/> this
		/// <see cref="Mesh"/> was created from.
		/// </param>
		/// <param name="dims">
		/// The width and height of the <see cref="Texture2D"/> bring projected onto the <see cref="Face"/> this
		/// <see cref="Mesh"/> was created from.
		/// </param>
		public static void CalculateUVs(this Mesh mesh, TextureInfo textureInfo, Vector2 dims) {
			Vector2[] uv = new Vector2[mesh.vertices.Length];
			Matrix4x4 textureMatrixInverse = textureInfo.BuildTexMatrix().inverse;
			for (int i = 0; i < uv.Length; ++i) {
				Vector3 transformVertex = textureMatrixInverse.MultiplyPoint3x4(mesh.vertices[i]);
				Vector2 uv0 = textureInfo.CalculateUV(transformVertex, dims);
				uv[i] = uv0;
			}
			mesh.uv = uv;
		}

		/// <summary>
		/// Transforms the vertices in this <see cref="Mesh"/> using the passed <see cref="Matrix4x4"/> as the basis.
		/// </summary>
		/// <param name="mesh">This <see cref="Mesh"/>.</param>
		/// <param name="transform">The matrix to use to transform the versices in this <see cref="Mesh"/>.</param>
		public static void TransformVertices(this Mesh mesh, Matrix4x4 transform) {
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			for (int i = 0; i < vertices.Length; ++i) {
				vertices[i] = transform.MultiplyPoint3x4(vertices[i]);
				normals[i] = transform.MultiplyVector(normals[i]);
			}
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.RecalculateBounds();
		}

		/// <summary>
		/// On each UV in this <see cref="Mesh"/>, replace each V coordinate with -V.
		/// </summary>
		/// <remarks>
		/// The UVs in Quake-based engines map textures on V coordinates in the opposite direction
		/// Unity does. One of them maps textures on surfaces from bottom-up, the other does it from
		/// top-down. Negating the Vs keeps the visuals consistent.
		/// </remarks>
		/// <param name="mesh">This <see cref="Mesh"/>.</param>
		public static void NegateVs(this Mesh mesh) {
			Vector2[] uv = mesh.uv;
			for (int i = 0; i < uv.Length; ++i) {
				uv[i] = new Vector2(uv[i].x, -uv[i].y);
			}
			mesh.uv = uv;
		}

		/// <summary>
		/// Combines all <see cref="Mesh"/> objects in <paramref name="meshes"/> and merges them using
		/// <see cref="Mesh.CombineMeshes(CombineInstance[], bool, bool)"/> with the passed options, and
		/// returns the result.
		/// </summary>
		/// <param name="meshes">An array of <see cref="Mesh"/> objects to combine.</param>
		/// <param name="mergeSubMeshes">
		/// Should all meshes be combined into a single submesh? If <c>true</c>, there will only be one submesh
		/// and only one <see cref="Material"/> may be used on the <see cref="Mesh"/>.
		/// </param>
		/// <param name="useMatrices">
		/// Should the transform <see cref="Matrix4x4"/>s in the <see cref="CombineInstance"/> array be used to
		/// transform the vertices?
		/// </param>
		/// <returns>A <see cref="Mesh"/> built from all objects in <paramref name="meshes"/> combined.</returns>
		public static Mesh CombineAllMeshes(Mesh[] meshes, bool mergeSubMeshes, bool useMatrices) {
			CombineInstance[] combine = new CombineInstance[meshes.Length];
			Mesh combinedMesh = null;

			for (int i = 0; i < meshes.Length; ++i) {
				combine[i] = new CombineInstance() {
					mesh = meshes[i],
					transform = Matrix4x4.identity,
				};
			}
			
			combinedMesh = new Mesh();
			combinedMesh.Clear();
			combinedMesh.CombineMeshes(combine.ToArray(), mergeSubMeshes, useMatrices);
			return combinedMesh;
		}

		/// <summary>
		/// Adds a <see cref="MeshFilter"/>, <see cref="MeshRenderer"/> and <see cref="MeshCollider"/> referencing this
		/// <see cref="Mesh"/> to <paramref name="gameObject"/> using the materials in <paramref name="materials"/>.
		/// </summary>
		/// <param name="mesh">This <see cref="Mesh"/>.</param>
		/// <param name="materials">An array of <see cref="Material"/>s used to render this <see cref="Mesh"/>.</param>
		/// <param name="gameObject">The <see cref="GameObject"/> to use for this <see cref="Mesh"/>.</param>
		public static void AddMeshToGameObject(this Mesh mesh, Material[] materials, GameObject gameObject) {
			MeshFilter filter = gameObject.GetComponent<MeshFilter>();
			if (filter == null) {
				filter = gameObject.AddComponent<MeshFilter>();
			}
			filter.sharedMesh = mesh;

			MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
			if (renderer == null) {
				renderer = gameObject.AddComponent<MeshRenderer>();
			}
			renderer.sharedMaterials = materials;

			MeshCollider collider = gameObject.GetComponent<MeshCollider>();
			if (collider == null) {
				collider = gameObject.AddComponent<MeshCollider>();
			}
		}

		/// <summary>
		/// Builds a Displacement <see cref="Mesh"/> from the <see cref="DisplacementInfo"/> referenced by
		/// <paramref name="face"/>, with UVs calculated from <see cref="Face.textureInfo"/> using
		/// <paramref name="dims"/> as the <see cref="Texture2D"/> width and height.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> object which <paramref name="face"/> came from.</param>
		/// <param name="face">A face referencing a <see cref="DisplacementInfo"/> and a <see cref="TextureInfo"/>.</param>
		/// <param name="dims">The dimensions of the <see cref="Texture2D"/> to map onto the resulting <see cref="Mesh"/>.</param>
		/// <returns>The <see cref="Mesh"/> created from the <see cref="DisplacementInfo"/>.</returns>
		public static Mesh CreateDisplacementMesh(BSP bsp, Face face, Vector2 dims) {
			Mesh mesh = null;
			if (face.NumEdgeIndices > 0) {
				mesh = LoadVerticesFromEdges(bsp, face);
			} else {
				Debug.LogWarning("Cannot create displacement, face contains no edges.");
				return null;
			}

			Vector3[] faceCorners = mesh.vertices;
			int[] faceTriangles = mesh.triangles;
			if (faceCorners.Length != 4 || faceTriangles.Length != 6) {
				Debug.LogWarning("Cannot create displacement mesh because " + faceCorners.Length + " corners and " + faceTriangles.Length + " triangle indices.");
				return null;
			}

			Displacement displacement = bsp.dispInfos[face.DisplacementIndex];
			int numSideTriangles = (int)Mathf.Pow(2, displacement.Power);

			DisplacementVertex[] displacementVertices = displacement.Vertices.ToArray();

			Vector3[] corners = new Vector3[4];
			Vector3 start = displacement.StartPosition.SwizzleYZ() * inch2MeterScale;
			if ((faceCorners[faceTriangles[0]] - start).sqrMagnitude < .01f) {
				corners[0] = faceCorners[faceTriangles[0]];
				corners[1] = faceCorners[faceTriangles[1]];
				corners[2] = faceCorners[faceTriangles[5]];
				corners[3] = faceCorners[faceTriangles[4]];
			} else if ((faceCorners[faceTriangles[1]] - start).sqrMagnitude < .01f) {
				corners[0] = faceCorners[faceTriangles[1]];
				corners[1] = faceCorners[faceTriangles[4]];
				corners[2] = faceCorners[faceTriangles[0]];
				corners[3] = faceCorners[faceTriangles[5]];
			} else if ((faceCorners[faceTriangles[5]] - start).sqrMagnitude < .01f) {
				corners[0] = faceCorners[faceTriangles[5]];
				corners[1] = faceCorners[faceTriangles[0]];
				corners[2] = faceCorners[faceTriangles[4]];
				corners[3] = faceCorners[faceTriangles[1]];
			} else if ((faceCorners[faceTriangles[4]] - start).sqrMagnitude < .01f) {
				corners[0] = faceCorners[faceTriangles[4]];
				corners[1] = faceCorners[faceTriangles[5]];
				corners[2] = faceCorners[faceTriangles[1]];
				corners[3] = faceCorners[faceTriangles[0]];
			} else {
				Debug.LogWarning("Cannot create displacement mesh because start position isn't one of the face corners.\n" +
					"Start position: " + start + "\n" +
					"Corners: " + faceCorners[faceTriangles[0]] + " " + faceCorners[faceTriangles[1]] + " " + faceCorners[faceTriangles[5]] + " " + faceCorners[faceTriangles[4]]);
				return null;
			}

			Vector3[] offsets = new Vector3[displacementVertices.Length];
			for (int i = 0; i < displacementVertices.Length; ++i) {
				offsets[i] = displacementVertices[i].Normal.SwizzleYZ() * displacementVertices[i].Magnitude * inch2MeterScale;
			}
			Vector2[] uv = new Vector2[4];
			Vector2[] uv2 = new Vector2[4];

			mesh.vertices = corners;
			mesh.uv = uv;
			mesh.uv2 = uv2;
			mesh.CalculateUVs(bsp.GetTextureInfo(face), dims);
			mesh.CalculateTerrainVertices(offsets, numSideTriangles);
			mesh.triangles = BuildDisplacementTriangles(numSideTriangles);
			mesh.NegateVs();
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();

			return mesh;
		}

		/// <summary>
		/// Builds a <see cref="Mesh"/> from the passed <see cref="LODTerrain"/> and returns it.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> which <paramref name="lodTerrain"/> came from.</param>
		/// <param name="lodTerrain">The <see cref="LODTerrain"/> to generate a <see cref="Mesh"/> from.</param>
		/// <returns>The <see cref="Mesh"/> created from the <see cref="LODTerrain"/>.</returns>
		public static Mesh CreateMoHAATerrainMesh(BSP bsp, LODTerrain lodTerrain) {
			Vector3 origin = new Vector3(lodTerrain.X * 64, lodTerrain.Y * 64, lodTerrain.BaseZ);
			Vector3[] corners = GetCornersForTerrain(origin, 512, (lodTerrain.Flags & (1 << 6)) > 0);
			Vector3[] offsets = new Vector3[81];
			for (int y = 0; y < 9; ++y) {
				for (int x = 0; x < 9; ++x) {
					if ((lodTerrain.Flags & (1 << 6)) > 0) {
						offsets[(x * 9) + y] = (Vector3.up * lodTerrain.Heightmap[y, x] * 2 * inch2MeterScale);
					} else {
						offsets[(y * 9) + x] = (Vector3.up * lodTerrain.Heightmap[y, x] * 2 * inch2MeterScale);
					}
				}
			}

			Vector2[] uv = new Vector2[] {
				new Vector2(lodTerrain.UVs[0], lodTerrain.UVs[1]),
				new Vector2(lodTerrain.UVs[2], lodTerrain.UVs[3]),
				new Vector2(lodTerrain.UVs[4], lodTerrain.UVs[5]),
				new Vector2(lodTerrain.UVs[6], lodTerrain.UVs[7]),
			};
			Vector2[] uv2 = new Vector2[4];
			
			//Vector3[] textureAxes = TextureInfo.TextureAxisFromPlane(new Plane(corners[0], corners[1], corners[2]));
			//TextureInfo info = new TextureInfo(textureAxes[0].SwizzleYZ(), -textureAxes[1].SwizzleYZ(), Vector2.zero, Vector2.one / 2f, 0, 0, 0);

			Mesh mesh = new Mesh();
			mesh.Clear();
			mesh.vertices = corners;
			mesh.uv = uv;
			mesh.uv2 = uv2;
			mesh.CalculateTerrainVertices(offsets, 8);
			mesh.triangles = BuildDisplacementTriangles(8);
			mesh.NegateVs();
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			
			return mesh;
		}

		/// <summary>
		/// Generates corners for a terrain, starting from <paramref name="origin"/> with side length
		/// <paramref name="side"/>.
		/// </summary>
		/// <param name="origin">The starting position of the terrain.</param>
		/// <param name="side">The side length of the terrain.</param>
		/// <param name="inverted">If <c>true</c>, the terrain will render from below. Use for ceilings.</param>
		/// <returns>Array of <see cref="Vector3"/> containing the generated corners.</returns>
		public static Vector3[] GetCornersForTerrain(Vector3 origin, float side, bool inverted) {
			Vector3[] corners = new Vector3[] {
				origin.SwizzleYZ().ScaleInch2Meter(),
				new Vector3(origin.x, origin.y + side, origin.z).SwizzleYZ().ScaleInch2Meter(),
				new Vector3(origin.x + side, origin.y, origin.z).SwizzleYZ().ScaleInch2Meter(),
				new Vector3(origin.x + side, origin.y + side, origin.z).SwizzleYZ().ScaleInch2Meter(),
			};

			if (inverted) {
				Vector3 temp = corners[1];
				corners[1] = corners[3];
				corners[3] = temp;
			}

			return corners;
		}

		/// <summary>
		/// Calculates the vertices for a terrain created from the terrain corners in this <see cref="Mesh"/>'s
		/// <see cref="Mesh.vertices"/> using <param name="displacementMap"/>.
		/// </summary>
		/// <param name="mesh">This <see cref="Mesh"/>.</param>
		/// <param name="displacementMap"><see cref="Vector3"/> array, defining for each vertex how far to move it from its origin.</param>
		/// <param name="numSideTriangles">How many triangles will make up one side of the generated terrain <see cref="Mesh"/>.</param>
		private static void CalculateTerrainVertices(this Mesh mesh, Vector3[] displacementMap, int numSideTriangles) {
			int numSideVertices = numSideTriangles + 1;
			Vector3[] vertices = new Vector3[numSideVertices * numSideVertices];
			Vector3[] corners = mesh.vertices;
			Vector2[] calculatedUV = new Vector2[numSideVertices * numSideVertices];
			Vector2[] uv = mesh.uv;
			Vector2[] calculatedUV2 = new Vector2[numSideVertices * numSideVertices];
			Vector2[] uv2 = mesh.uv2;

			// Calculate position of the vertices (interpolate between face corners, apply normal and length)
			for (int i = 0; i < numSideVertices; ++i) { // row
				float rowPosition = i / (float)(numSideTriangles);
				Vector3 rowStart = Vector3.Lerp(corners[0], corners[1], rowPosition);
				Vector3 rowEnd = Vector3.Lerp(corners[2], corners[3], rowPosition);
				Vector2 uvStart = Vector2.Lerp(uv[0], uv[1], rowPosition);
				Vector2 uvEnd = Vector2.Lerp(uv[2], uv[3], rowPosition);
				Vector2 uv2Start = Vector2.Lerp(uv2[0], uv2[1], rowPosition);
				Vector2 uv2End = Vector2.Lerp(uv2[2], uv2[3], rowPosition);
				for (int j = 0; j < numSideVertices; ++j) { // column
					int current = (i * numSideVertices) + j;
					float columnPosition = j / (float)(numSideTriangles);
					vertices[current] = Vector3.Lerp(rowStart, rowEnd, columnPosition) + (displacementMap[current]);
					calculatedUV[current] = Vector2.Lerp(uvStart, uvEnd, columnPosition);
					calculatedUV2[current] = Vector2.Lerp(uv2Start, uv2End, columnPosition);
				}
			}

			mesh.vertices = vertices;
			mesh.uv = calculatedUV;
			mesh.uv2 = calculatedUV2;
		}

		/// <summary>
		/// Builds a triangle index array for a terrain with <paramref name="numSideTriangles"/> triangles to a side.
		/// </summary>
		/// <remarks>
		/// The triangles generated in this method will generate a set of triangles with alternating orientations. This
		/// matches the tessellation of terrain vertices used in the game engines.
		/// </remarks>
		/// <param name="numSideTriangles">How many triangles will make up one side of the generated terrain <see cref="Mesh"/>.</param>
		/// <returns><see cref="int"/> array for building triangles for a terrain <see cref="Mesh"/>.</returns>
		private static int[] BuildDisplacementTriangles(int numSideTriangles) {
			int[] triangles = new int[numSideTriangles * numSideTriangles * 6];
			int numSideVertices = numSideTriangles + 1;

			// Build triangles
			// Loop for each QUAD being built out of this face. Since the face is a quadrilateral we will divide it into <2 ^ power> ^ 2
			// quads that when added together will give the same surface as the face. Since our vertices are in lines, we will need to
			// index them by row and column.
			for (int i = 0; i < numSideTriangles; ++i) {
				for (int j = 0; j < numSideTriangles; ++j) {
					triangles[(i * numSideTriangles * 6) + (j * 6)] = (i * numSideVertices) + j;
					triangles[(i * numSideTriangles * 6) + (j * 6) + 5] = ((i + 1) * numSideVertices) + j + 1;
					triangles[(i * numSideTriangles * 6) + (j * 6) + 2] = (i * numSideVertices) + j + 1;
					triangles[(i * numSideTriangles * 6) + (j * 6) + 4] = ((i + 1) * numSideVertices) + j;
					if ((i + j) % 2 == 0) {
						triangles[(i * numSideTriangles * 6) + (j * 6) + 1] = ((i + 1) * numSideVertices) + j + 1;
						triangles[(i * numSideTriangles * 6) + (j * 6) + 3] = (i * numSideVertices) + j;
					} else {
						triangles[(i * numSideTriangles * 6) + (j * 6) + 1] = ((i + 1) * numSideVertices) + j;
						triangles[(i * numSideTriangles * 6) + (j * 6) + 3] = (i * numSideVertices) + j + 1;
					}
				}
			}

			return triangles;
		}

		/// <summary>
		/// Builds a <see cref="Mesh"/> for all control vertices referenced by <paramref name="face"/> using
		/// <paramref name="curveTessellationLevel"/> to determine how many vertices will be used to build
		/// the curves.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> which <paramref name="face"/> came from.</param>
		/// <param name="face">The <see cref="Face"/> whose vertices will be interpreted as curve control points.</param>
		/// <param name="curveTessellationLevel">The number of times to tessellate the curves in this patch.</param>
		/// <returns>A <see cref="Mesh"/> built using the curve data in <paramref name="face"/>.</returns>
		public static Mesh CreatePatchMesh(BSP bsp, Face face, int curveTessellationLevel) {
			List<Mesh> curveSubmeshes = new List<Mesh>();
			List<Vertex> controls = bsp.GetReferencedObjects<Vertex>(face, "vertices");
			Vector2 size = face.PatchSize;
			int xSize = (int)Mathf.Round(size[0]);
			for (int i = 0; i < size[1] - 2; i += 2) {
				for (int j = 0; j < size[0] - 2; j += 2) {

					int rowOff = (i * xSize);
					Vertex[] thisCurveControls = new Vertex[9];

					// Store control points
					thisCurveControls[0] = controls[rowOff + j];
					thisCurveControls[1] = controls[rowOff + j + 1];
					thisCurveControls[2] = controls[rowOff + j + 2];
					rowOff += xSize;
					thisCurveControls[3] = controls[rowOff + j];
					thisCurveControls[4] = controls[rowOff + j + 1];
					thisCurveControls[5] = controls[rowOff + j + 2];
					rowOff += xSize;
					thisCurveControls[6] = controls[rowOff + j];
					thisCurveControls[7] = controls[rowOff + j + 1];
					thisCurveControls[8] = controls[rowOff + j + 2];

					curveSubmeshes.Add(CreateQuadraticBezierMesh(thisCurveControls, curveTessellationLevel));
				}
			}

			return CombineAllMeshes(curveSubmeshes.ToArray(), true, false);
		}

		/// <summary>
		/// Builds a <see cref="Mesh"/> using a 3x3 set of control points for the biquadratic Bezier patch.
		/// </summary>
		/// <param name="bezierControls">A 3x3 set of <see cref="Vertex"/> control points for the patch.</param>
		/// <param name="curveTessellationLevel">The number of times to tessellate the curves in this patch.</param>
		/// <returns>The generated <see cref="Mesh"/> object built from <paramref name="bezierControls"/>.</returns>
		public static Mesh CreateQuadraticBezierMesh(Vertex[] bezierControls, int curveTessellationLevel) {
			Mesh mesh = LoadVertices(TessellateCurveVertices(bezierControls, curveTessellationLevel));
			mesh.triangles = BuildCurveTriangles(curveTessellationLevel);
			return mesh;
		}

		/// <summary>
		/// Tessellates the set of 3x3 control points in <paramref name="bezierControls"/> into a <see cref="Vertex"/> array
		/// using <paramref name="curveTessellationLevel"/> to determine how many triangles to use.
		/// </summary>
		/// <remarks>
		/// Thanks to Morgan McGuire's July 11, 2003 article "Rendering Quake 3 Maps" for
		/// this algorithm, which he in turn credits to Paul Baker's "Octagon" project.
		/// http://graphics.cs.brown.edu/games/quake/quake3.html
		/// </remarks>
		/// <param name="bezierControls">A 3x3 set of <see cref="Vertex"/> control points for the patch.</param>
		/// <param name="curveTessellationLevel">The number of times to tessellate the curves in this patch.</param>
		/// <returns><see cref="Vertex"/> array containing the vertices for the patch <see cref="Mesh"/>.</returns>
		private static Vertex[] TessellateCurveVertices(Vertex[] bezierControls, int curveTessellationLevel) {
			Vertex[] vertices = new Vertex[(curveTessellationLevel + 1) * (curveTessellationLevel + 1)];
			
			for (int i = 0; i <= curveTessellationLevel; ++i) {
				float p = (float)i / curveTessellationLevel;

				Vector3[] temp = new Vector3[3];
				Vector2[] tempUVs = new Vector2[3];
				Vector2[] tempUV2s = new Vector2[3];

				for (int j = 0; j < 3; ++j) {
					temp[j] = InterpolateCurve(bezierControls[3 * j].position, bezierControls[(3 * j) + 1].position, bezierControls[(3 * j) + 2].position, p);
					tempUVs[j] = InterpolateCurve(bezierControls[3 * j].uv0, bezierControls[(3 * j) + 1].uv0, bezierControls[(3 * j) + 2].uv0, p);
					tempUV2s[j] = InterpolateCurve(bezierControls[3 * j].uv1, bezierControls[(3 * j) + 1].uv1, bezierControls[(3 * j) + 2].uv1, p);
				}

				for (int j = 0; j <= curveTessellationLevel; ++j) {
					float a2 = (float)j / curveTessellationLevel;

					vertices[i * (curveTessellationLevel + 1) + j].position = InterpolateCurve(temp[0], temp[1], temp[2], a2);
					vertices[i * (curveTessellationLevel + 1) + j].uv0 = InterpolateCurve(tempUVs[0], tempUVs[1], tempUVs[2], a2);
					vertices[i * (curveTessellationLevel + 1) + j].uv1 = InterpolateCurve(tempUV2s[0], tempUV2s[1], tempUV2s[2], a2);
				}
			}

			return vertices;
		}

		/// <summary>
		/// Builds a triangle index array for a patch <see cref="Mesh"/> using <paramref name="curveTessellationLevel"/>.
		/// </summary>
		/// <remarks>
		/// The triangles generated in this method will generate a set of triangles with the same orientations. This
		/// matches the tessellation of patch vertices used in the game engines.
		/// </remarks>
		/// <param name="curveTessellationLevel">The number of times to tessellate the curves in this patch.</param>
		/// <returns><see cref="int"/> array for building triangles for a patch <see cref="Mesh"/>.</returns>
		private static int[] BuildCurveTriangles(int curveTessellationLevel) {
			int[] triangles = new int[curveTessellationLevel * curveTessellationLevel * 6];

			for (int row = 0; row < curveTessellationLevel; row++) {
				for (int col = 0; col < curveTessellationLevel; col++) {
					triangles[((row * curveTessellationLevel) + col) * 6] = (row * (curveTessellationLevel + 1)) + col;
					triangles[(((row * curveTessellationLevel) + col) * 6) + 1] = (row * (curveTessellationLevel + 1)) + col + 1;
					triangles[(((row * curveTessellationLevel) + col) * 6) + 2] = (row * (curveTessellationLevel + 1)) + col + (curveTessellationLevel + 1);
					triangles[(((row * curveTessellationLevel) + col) * 6) + 3] = (row * (curveTessellationLevel + 1)) + col + 1;
					triangles[(((row * curveTessellationLevel) + col) * 6) + 4] = (row * (curveTessellationLevel + 1)) + col + (curveTessellationLevel + 1) + 1;
					triangles[(((row * curveTessellationLevel) + col) * 6) + 5] = (row * (curveTessellationLevel + 1)) + col + (curveTessellationLevel + 1);
				}
			}

			return triangles;
		}

		/// <summary>
		/// Interpolates the position along the curve defined by control points <paramref name="v1"/> <paramref name="v2"/>
		/// and <paramref name="v3"/> by the interpolant <paramref name="t"/>.
		/// </summary>
		/// <param name="v1">First control point for the curve.</param>
		/// <param name="v2">Second control point for the curve.</param>
		/// <param name="v3">Third control point for the curve.</param>
		/// <param name="t">Interpolant on the curve. Value should be between 0 and 1, inclusive.</param>
		/// <returns>The point on the curve at <paramref name="t"/> normalized distance along the curve.</returns>
		private static Vector3 InterpolateCurve(Vector3 v1, Vector3 v2, Vector3 v3, float t) {
			float pinv = 1.0f - t;
			return v1 * (pinv * pinv) + v2 * (2 * pinv * t) + v3 * (t * t);
		}

	}
}
