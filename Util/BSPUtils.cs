using UnityEngine;
using System.Collections.Generic;
using LibBSP;

namespace BSPImporter {
	public static class BSPUtils {

		public const float inch2meterScale = 0.0254f;

		public static int numVertices = 0;

		public static int GetTextureIndex(BSP bspObject, Face face) {
			if(face.texture >= 0) {
				return face.texture;
			} else {
				if(face.textureScale > 0) {
					return bspObject.texDatas[bspObject.texInfo[face.textureScale].texture].stringTableIndex;
				}
			}
			return 0;
		}

		public static List<Face> GetFacesInModel(BSP bspObject, Model model) {
			List<Face> result = null;
			if(model.firstFace >= 0) {
				for(int i = 0; i < model.numFaces; i++) {
					if(result == null) {
						result = new List<Face>();
					}
					result.Add(bspObject.faces[model.firstFace + i]);
				}
			} else {
				bool[] faceUsed = new bool[bspObject.faces.Count];
				List<Leaf> leaves = GetLeavesInModel(bspObject, model);
				foreach(Leaf leaf in leaves) {
					if(leaf.firstMarkFace >= 0) {
						for(int i = 0; i < leaf.numMarkFaces; i++) {
							int currentFace = (int)bspObject.markSurfaces[leaf.firstMarkFace + i];
							if(!faceUsed[currentFace]) {
								faceUsed[currentFace] = true;
								if(result == null) {
									result = new List<Face>();
								}
								result.Add(bspObject.faces[currentFace]);
							}
						}
					}
				}
			}
			return result;
		}

		public static List<Leaf> GetLeavesInModel(BSP bspObject, Model model) {
			List<Leaf> result = null;
			if(model.firstLeaf < 0) {
				if(model.headNode >= 0) {
					result = GetLeavesInNode(bspObject, bspObject.nodes[model.headNode]);
				}
			} else {
				result = new List<Leaf>(model.numLeaves);
				for(int i = 0; i < model.numLeaves; i++) {
					result.Add(bspObject.leaves[model.firstLeaf + i]);
				}
			}
			return result;
		}

		public static List<Leaf> GetLeavesInNode(BSP bspObject, Node node) {
			List<Leaf> nodeLeaves = new List<Leaf>();
			Stack<Node> nodestack = new Stack<Node>();
			nodestack.Push(node);

			Node currentNode;

			while(!(nodestack.Count == 0)) {
				currentNode = nodestack.Pop();
				int right = currentNode.child2;
				if(right >= 0) {
					nodestack.Push(bspObject.nodes[right]);
				} else {
					nodeLeaves.Add(bspObject.leaves[(right * (-1)) - 1]);
				}
				int left = currentNode.child1;
				if(left >= 0) {
					nodestack.Push(bspObject.nodes[left]);
				} else {
					nodeLeaves.Add(bspObject.leaves[(left * (-1)) - 1]);
				}
			}
			return nodeLeaves;
		}

		public static List<Brush> GetBrushesInLeaf(BSP bspObject, Leaf leaf) {
			List<Brush> result = null;
			if(leaf.firstMarkBrush >= 0) {
				result = new List<Brush>(leaf.numMarkBrushes);
				for(int i = 0; i < leaf.numMarkBrushes; i++) {
					result.Add(bspObject.brushes[(int)bspObject.markBrushes[leaf.firstMarkBrush + i]]);
				}
			}
			return result;
		}

		public static List<Face> GetFacesInLeaf(BSP bspObject, Leaf leaf) {
			List<Face> result = null;
			if(leaf.firstMarkFace >= 0) {
				result = new List<Face>(leaf.numMarkFaces);
				for(int i = 0; i < leaf.numMarkFaces; i++) {
					result.Add(bspObject.faces[(int)bspObject.markSurfaces[leaf.firstMarkFace + i]]);
				}
			}
			return result;
		}

		public static List<Brush> GetBrushesInModel(BSP bspObject, Model model) {
			List<Brush> result = null;
			if(bspObject.version != MapType.Quake) {
				if(model.firstBrush >= 0) {
					result = new List<Brush>(model.numBrushes);
					for(int i = 0; i < model.numBrushes; i++) {
						result.Add(bspObject.brushes[model.firstBrush + i]);
					}
				} else {
					List<Leaf> leaves = GetLeavesInNode(bspObject, bspObject.nodes[model.headNode]);
					result = new List<Brush>();
					foreach(Leaf leaf in leaves) {
						result.AddRange(GetBrushesInLeaf(bspObject, leaf));
					}
				}
			}
			return result;
		}

		public static UIVertex[] GetVertices(BSP bspObject, Face face, Vector3 translate, ref int[] triangles) {
			UIVertex[] vertices = null;
			if(face.numVertices >= 0) {
				vertices = new UIVertex[face.numVertices];
				for(int i = 0; i < vertices.Length; i++) {
					vertices[i] = BSPUtils.Swizzle(bspObject.vertices[face.firstVertex + i].Scale(BSPUtils.inch2meterScale))/*.Translate(translate)*/;
				}
			} else {
				vertices = new UIVertex[face.numEdges];
				triangles = new int[(vertices.Length - 2) * 3];
				int firstSurfEdge = (int)bspObject.surfEdges[face.firstEdge];
				if(firstSurfEdge > 0) {
					vertices[0] = bspObject.vertices[bspObject.edges[firstSurfEdge].firstVertex];
				} else {
					vertices[0] = bspObject.vertices[bspObject.edges[-firstSurfEdge].secondVertex];
				}
				int currtriangle = 0;
				int currvert = 1;
				for(int i = 1; i < face.numEdges - 1; i++) {
					int currSurfEdge = (int)bspObject.surfEdges[face.firstEdge + i];
					UIVertex first;
					UIVertex second;
					if(currSurfEdge > 0) {
						first = bspObject.vertices[bspObject.edges[currSurfEdge].firstVertex];
						second = bspObject.vertices[bspObject.edges[currSurfEdge].secondVertex];
					} else {
						first = bspObject.vertices[bspObject.edges[-currSurfEdge].secondVertex];
						second = bspObject.vertices[bspObject.edges[-currSurfEdge].firstVertex];
					}
					if(first.position != vertices[0].position && second.position != vertices[0].position) { // All tris involve first vertex, so disregard edges referencing it
						triangles[currtriangle * 3] = 0;
						bool firstFound = false;
						bool secondFound = false;
						for(int j = 1; j < currvert; j++) {
							if(first.position == vertices[j].position) {
								triangles[(currtriangle * 3) + 1] = j;
								firstFound = true;
							}
						}
						if(!firstFound) {
							vertices[currvert] = first;
							triangles[(currtriangle * 3) + 1] = currvert;
							currvert++;
						}
						for(int j = 1; j < currvert; j++) {
							if(second.position == vertices[j].position) {
								triangles[(currtriangle * 3) + 2] = j;
								secondFound = true;
							}
						}
						if(!secondFound) {
							vertices[currvert] = second;
							triangles[(currtriangle * 3) + 2] = currvert;
							currvert++;
						}
						currtriangle++;
					}
				}
				for(int i = 0; i < vertices.Length; i++) {
					vertices[i] = BSPUtils.Swizzle(vertices[i].Scale(BSPUtils.inch2meterScale))/*.Translate(translate)*/;
				}
			}
			return vertices;
		}

		public static Mesh CombineAllMeshes(Mesh[] meshes, Transform transform, bool mergeSubMeshes, bool useMatrices) {
			CombineInstance[] combine = new CombineInstance[meshes.Length];
			for(int i = 0; i < combine.Length; i++) {
				combine[i].mesh = meshes[i];
				combine[i].transform = transform.localToWorldMatrix;
			}
			Mesh combinedMesh = new Mesh();
			combinedMesh.CombineMeshes(combine, mergeSubMeshes, useMatrices);
			return combinedMesh;
		}

		public static Mesh LegacyBuildFaceMesh(Vector3[] vertices, int[] triangles, TextureInfo texinfo, Vector3 origin, Texture2D texture) {
			Vector3 sAxis = Swizzle(texinfo.axes[0] / inch2meterScale); // Convert from Quake (left-handed, Z-up, inches) coordinate system to Unity (right-handed, Y-up, meters) coordinates
			Vector3 tAxis = Swizzle(texinfo.axes[1] / inch2meterScale); // This is NOT a typo. The texture axis vectors need to be DIVIDED by the conversion.
			//Vector2 originShifts = new Vector2(Vector3.Dot(origin, texinfo.SAxis.normalized) * texinfo.SAxis.magnitude, Vector3.Dot(origin, texinfo.TAxis.normalized) * texinfo.TAxis.magnitude);
			Vector2 originShifts = Vector2.zero;
			Matrix4x4 texmatinverse = BuildTexMatrix(sAxis, tAxis).inverse;
			Vector2[] uvs = new Vector2[vertices.Length];
			Mesh mesh = new Mesh();
			mesh.Clear();
			for(int l = 0; l < vertices.Length; l++) {
				Vector3 textureCoord = texmatinverse.MultiplyPoint3x4(vertices[l]);
				if(texture != null) {
					uvs[l] = CalcUV(sAxis, tAxis, textureCoord, texinfo.shifts[0], texinfo.shifts[1], originShifts.x, originShifts.y, texture.width, texture.height);
				} else {
					uvs[l] = CalcUV(sAxis, tAxis, textureCoord, texinfo.shifts[0], texinfo.shifts[1], originShifts.x, originShifts.y, 64, 64);
				}
			}
			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			numVertices += vertices.Length;
			return mesh;
		}

		public static Mesh Q3BuildFaceMesh(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector3 origin) {
			Mesh mesh = new Mesh();
			mesh.Clear();
			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			return mesh;
		}

		public static Matrix4x4 BuildTexMatrix(Vector3 sAxis, Vector3 tAxis) {
			Vector3 STNormal = Vector3.Cross(sAxis, tAxis);
			Matrix4x4 texmatrix = Matrix4x4.identity;
			texmatrix[0, 0] = sAxis[0];
			texmatrix[0, 1] = tAxis[0];
			texmatrix[0, 2] = STNormal[0];
			texmatrix[1, 0] = sAxis[1];
			texmatrix[1, 1] = tAxis[1];
			texmatrix[1, 2] = STNormal[1];
			texmatrix[2, 0] = sAxis[2];
			texmatrix[2, 1] = tAxis[2];
			texmatrix[2, 2] = STNormal[2];
			return texmatrix;
		}

		public static Vector2 CalcUV(Vector3 sAxis, Vector3 tAxis, Vector3 textureCoord, float sShift, float tShift, float sOrigin, float tOrigin, int texWidth, int texHeight) {
			return new Vector2((sAxis.sqrMagnitude * textureCoord[0] + sShift - sOrigin) / (float)texWidth, -(tAxis.sqrMagnitude * textureCoord[1] + tShift - tOrigin) / (float)texHeight);
		}

		/// <summary>
		/// Swaps the Y and Z coordinates of a Vector, converting between Quake's Z-Up coordinates to Unity's Y-Up.
		/// </summary>
		/// <param name="v">The Vector to "swizzle"</param>
		/// <returns>The "swizzled" Vector</returns>
		public static Vector3 Swizzle(Vector3 v) {
			return new Vector3(v.x, v.z, v.y);
		}

		public static UIVertex Swizzle(UIVertex v) {
			UIVertex ret = new UIVertex();
			ret.position = Swizzle(v.position);
			ret.normal = Swizzle(v.normal);
			ret.color = v.color;
			ret.uv0 = v.uv0;
			ret.uv1 = v.uv1;
			ret.tangent = v.tangent;
			return ret;
		}

		public static void ParseEntity(Entity entity, GameObject bspGameObject, GameObject entityGameObject) {
			switch(entity["classname"]) {
				case "light":
					Light light = entityGameObject.AddComponent<Light>();
					light.type = LightType.Point;
					NormalizeParseLight(entity, light);
					break;
				case "light_environment":
					Light envlight = entityGameObject.AddComponent<Light>();
					envlight.type = LightType.Directional;
					envlight.shadows = LightShadows.Soft;
					envlight.shadowBias = 0.04f;
					NormalizeParseLight(entity, envlight);
					break;
				case "light_spot":
					Light spotlight = entityGameObject.AddComponent<Light>();
					spotlight.type = LightType.Spot;
					NormalizeParseLight(entity, spotlight);
					break;
				case "info_player_start":
					entityGameObject.tag = "Respawn";
					break;
				case "env_fog":
					RenderSettings.fog = true;
					RenderSettings.fogMode = FogMode.Linear;
					try {
						RenderSettings.fogStartDistance = System.Single.Parse(entity["fogstart"]) * BSPUtils.inch2meterScale;
					} catch {
						RenderSettings.fogStartDistance = 0.0f;
					}
					try {
						RenderSettings.fogEndDistance = System.Single.Parse(entity["fogend"]) * BSPUtils.inch2meterScale;
					} catch {
						RenderSettings.fogEndDistance = 0.0f;
					}
					RenderSettings.fogColor = NormalizeParseColor(entity["rendercolor"]);
					try {
						RenderSettings.fogDensity = System.Single.Parse(entity["density"]) * BSPUtils.inch2meterScale;
					} catch {
						RenderSettings.fogDensity = 0.01f;
					}
					break;
			}

		}

		// Takes in a light entity with color values between 0 and 255, and converts it to Unity lighting
		public static void NormalizeParseLight(Entity ent, Light light) {
			if(ent.ContainsKey("_light")) {
				Color color = NormalizeParseColor(ent["_light"]);
				light.color = color;
				light.intensity = color.a;
			}
		}

		// Takes in a string or array of strings representing color values between 0 and 255, and converts it to a Unity Color
		public static Color NormalizeParseColor(string col) { return NormalizeParseColor(col.Split(' ')); }
		public static Color NormalizeParseColor(string[] col) {
			float[] colorAsNums = new float[4];
			for(int i = 0; i < 4 && i < col.Length; i++) {
				try {
					colorAsNums[i] = System.Single.Parse(col[i]);
				} catch { ; }
			}
			return new Color(colorAsNums[0] / 255.0f, colorAsNums[1] / 255.0f, colorAsNums[2] / 255.0f, colorAsNums[3] / 255.0f);
		}

		// Only for Source engine.
		public static int FindTexDataWithTexture(this BSP bsp, string texture) {
			for (int i = 0; i < bsp.texDatas.Count; i++) {
				string temp = bsp.textures.GetTextureAtOffset((uint)bsp.texTable[bsp.texDatas[i].stringTableIndex]);
				if (temp.Equals(texture)) {
					return i;
				}
			}
			return -1;
		}

		public static Mesh BuildDisplacementMesh(Vector3[] faceCorners, int[] faceTriangles, TextureInfo texinfo, BSP bsp, DisplacementInfo disp, Texture2D texture) {
			if (faceCorners.Length != 4 || faceTriangles.Length != 6) {
				Debug.LogWarning("Cannot create displacement mesh because " + faceCorners.Length + " corners and " + faceTriangles.Length + " triangle indices!");
				return null;
			}
			Vector3 sAxis = Swizzle(texinfo.axes[0] / inch2meterScale);
			Vector3 tAxis = Swizzle(texinfo.axes[1] / inch2meterScale);
			Matrix4x4 texmatinverse = BuildTexMatrix(sAxis, tAxis).inverse;

			int power = disp.power;
			int side = (int)Mathf.Pow(2, power);
			int sideVertices = side + 1;

			Mesh ret = new Mesh();
			DisplacementVertex[] displacementVertices = bsp.dispVerts.GetVerticesInDisplacement(disp.dispVertStart, disp.power);
			Vector3[] vertices = new Vector3[displacementVertices.Length];
			int[] triangles = new int[side * side * 6];

			Vector3[] corners = new Vector3[4];
			Vector3 start = Swizzle(disp.startPosition) * inch2meterScale;
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
				Debug.LogWarning("Cannot create displacement mesh because start position isn't one of the face corners!\n" +
					"Start position: " + start + "\n" +
					"Corners: " + faceCorners[faceTriangles[0]] + " " + faceCorners[faceTriangles[1]] + " " + faceCorners[faceTriangles[5]] + " " + faceCorners[faceTriangles[4]]);
				return null;
			}

			// Calculate initial position of the vertices (interpolate between face corners)
			for (int i = 0; i < sideVertices; ++i) { // row
				Vector3 rowStart = Vector3.Lerp(corners[0], corners[1], i / (float)(sideVertices - 1));
				Vector3 rowEnd = Vector3.Lerp(corners[2], corners[3], i / (float)(sideVertices - 1));
				for (int j = 0; j < sideVertices; ++j) { // column
					vertices[(i * sideVertices) + j] = Vector3.Lerp(rowStart, rowEnd, j / (float)(sideVertices - 1));
				}
			}
			// Build triangles
			// Loop for each QUAD being built out of this face. Since the face is a quadrilateral we will divide it into <2 ^ power> ^ 2
			// quads that when added together will give the same surface as the face. Since our vertices are in lines, we will need to
			// index them by row and column.
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("Tesselation triangulation:");
			sb.Append("\npower " + power);
			for (int i = 0; i < side; ++i) {
				for (int j = 0; j < side; ++j) {
					if ((i + j) % 2 == 0) {
						triangles[(i * side * 6) + (j * 6)] = (i * sideVertices) + j;
						triangles[(i * side * 6) + (j * 6) + 1] = ((i + 1) * sideVertices) + j + 1;
						triangles[(i * side * 6) + (j * 6) + 2] = (i * sideVertices) + j + 1;
						triangles[(i * side * 6) + (j * 6) + 3] = (i * sideVertices) + j;
						triangles[(i * side * 6) + (j * 6) + 4] = ((i + 1) * sideVertices) + j;
						triangles[(i * side * 6) + (j * 6) + 5] = ((i + 1) * sideVertices) + j + 1;
					} else {
						triangles[(i * side * 6) + (j * 6)] = (i * sideVertices) + j;
						triangles[(i * side * 6) + (j * 6) + 1] = ((i + 1) * sideVertices) + j;
						triangles[(i * side * 6) + (j * 6) + 2] = (i * sideVertices) + j + 1;
						triangles[(i * side * 6) + (j * 6) + 3] = (i * sideVertices) + j + 1;
						triangles[(i * side * 6) + (j * 6) + 4] = ((i + 1) * sideVertices) + j;
						triangles[(i * side * 6) + (j * 6) + 5] = ((i + 1) * sideVertices) + j + 1;
					}
				}
			}

			// Calculate UVs for each vertex in the tesellated face
			Vector2[] uvs = new Vector2[vertices.Length];
			for (int l = 0; l < vertices.Length; l++) {
				Vector3 textureCoord = texmatinverse.MultiplyPoint3x4(vertices[l]);
				if (texture != null) {
					uvs[l] = CalcUV(sAxis, tAxis, textureCoord, texinfo.shifts[0], texinfo.shifts[1], 0, 0, texture.width, texture.height);
				} else {
					uvs[l] = CalcUV(sAxis, tAxis, textureCoord, texinfo.shifts[0], texinfo.shifts[1], 0, 0, 64, 64);
				}
			}

			// Apply displacement vectors to the vertices
			for (int i = 0; i < vertices.Length; ++i) {
				vertices[i] = vertices[i] + (Swizzle(displacementVertices[i].normal) * displacementVertices[i].dist * inch2meterScale);
			}

			ret.vertices = vertices;
			ret.triangles = triangles;
			ret.uv = uvs;
			ret.RecalculateNormals();
			numVertices += vertices.Length;
			return ret;
		}

	}
}
