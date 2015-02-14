using UnityEngine;
using System.Collections.Generic;

public static class BSPUtils {

	public const float inch2meterScale = 0.0254f;

	public static int numVertices = 0;

	public static int GetTextureIndex(BSP bspObject, Face face) {
		if(face.Texture >= 0) {
			return face.Texture;
		} else {
			if(face.TextureScale > 0) {
				return bspObject.TexDatas[bspObject.TexInfo[face.TextureScale].Texture].StringTableIndex;
			}
		}
		return 0;
	}

	public static List<Face> GetFacesInModel(BSP bspObject, Model model) {
		List<Face> result = null;
		if(model.FirstFace >= 0) {
			for(int i = 0; i < model.NumFaces; i++) {
				if(result == null) {
					result = new List<Face>();
				}
				result.Add(bspObject.Faces[model.FirstFace + i]);
			}
		} else {
			bool[] faceUsed = new bool[bspObject.Faces.Count];
			List<Leaf> leaves = GetLeavesInModel(bspObject, model);
			foreach(Leaf leaf in leaves) {
				if(leaf.FirstMarkFace >= 0) {
					for(int i = 0; i < leaf.NumMarkFaces; i++) {
						int currentFace = (int)bspObject.MarkSurfaces[leaf.FirstMarkFace + i];
						if(!faceUsed[currentFace]) {
							faceUsed[currentFace] = true;
							if(result == null) {
								result = new List<Face>();
							}
							result.Add(bspObject.Faces[currentFace]);
						}
					}
				}
			}
		}
		return result;
	}

	public static List<Leaf> GetLeavesInModel(BSP bspObject, Model model) {
		List<Leaf> result = null;
		if(model.FirstLeaf < 0) {
			if(model.HeadNode >= 0) {
				result = GetLeavesInNode(bspObject, bspObject.Nodes[model.HeadNode]);
			}
		} else {
			result = new List<Leaf>(model.NumLeaves);
			for(int i = 0; i < model.NumLeaves; i++) {
				result.Add(bspObject.Leaves[model.FirstLeaf + i]);
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
			int right = currentNode.Child2;
			if(right >= 0) {
				nodestack.Push(bspObject.Nodes[right]);
			} else {
				nodeLeaves.Add(bspObject.Leaves[(right * (-1)) - 1]);
			}
			int left = currentNode.Child1;
			if(left >= 0) {
				nodestack.Push(bspObject.Nodes[left]);
			} else {
				nodeLeaves.Add(bspObject.Leaves[(left * (-1)) - 1]);
			}
		}
		return nodeLeaves;
	}

	public static List<Brush> GetBrushesInLeaf(BSP bspObject, Leaf leaf) {
		List<Brush> result = null;
		if(leaf.FirstMarkBrush >= 0) {
			result = new List<Brush>(leaf.NumMarkBrushes);
			for(int i = 0; i < leaf.NumMarkBrushes; i++) {
				result.Add(bspObject.Brushes[(int)bspObject.MarkBrushes[leaf.FirstMarkBrush + i]]);
			}
		}
		return result;
	}

	public static List<Face> GetFacesInLeaf(BSP bspObject, Leaf leaf) {
		List<Face> result = null;
		if(leaf.FirstMarkFace >= 0) {
			result = new List<Face>(leaf.NumMarkFaces);
			for(int i = 0; i < leaf.NumMarkFaces; i++) {
				result.Add(bspObject.Faces[(int)bspObject.MarkSurfaces[leaf.FirstMarkFace + i]]);
			}
		}
		return result;
	}

	public static List<Brush> GetBrushesInModel(BSP bspObject, Model model) {
		List<Brush> result = null;
		if(bspObject.Version != mapType.TYPE_QUAKE) {
			if(model.FirstBrush >= 0) {
				result = new List<Brush>(model.NumBrushes);
				for(int i = 0; i < model.NumBrushes; i++) {
					result.Add(bspObject.Brushes[model.FirstBrush + i]);
				}
			} else {
				List<Leaf> leaves = GetLeavesInNode(bspObject, bspObject.Nodes[model.HeadNode]);
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
		if(face.NumVertices >= 0) {
			vertices = new UIVertex[face.NumVertices];
			for(int i = 0; i < vertices.Length; i++) {
				vertices[i] = BSPUtils.SwapYZ(bspObject.Vertices[face.FirstVertex + i].Scale(BSPUtils.inch2meterScale)).Translate(translate);
			}
		} else {
			vertices = new UIVertex[face.NumEdges + 1];
			triangles = new int[(vertices.Length - 2) * 3];
			int firstSurfEdge = (int)bspObject.SurfEdges[face.FirstEdge];
			if(firstSurfEdge > 0) {
				vertices[0] = bspObject.Vertices[bspObject.Edges[firstSurfEdge].FirstVertex];
			} else {
				vertices[0] = bspObject.Vertices[bspObject.Edges[firstSurfEdge * -1].SecondVertex];
			}
			int currtriangle = 0;
			int currvert = 1;
			for(int i = 1; i < face.NumEdges; i++) {
				int currSurfEdge = (int)bspObject.SurfEdges[face.FirstEdge + i];
				UIVertex first;
				UIVertex second;
				if(currSurfEdge > 0) {
					first = bspObject.Vertices[bspObject.Edges[currSurfEdge].FirstVertex];
					second = bspObject.Vertices[bspObject.Edges[currSurfEdge].SecondVertex];
				} else {
					first = bspObject.Vertices[bspObject.Edges[currSurfEdge * -1].SecondVertex];
					second = bspObject.Vertices[bspObject.Edges[currSurfEdge * -1].FirstVertex];
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
				vertices[i] = BSPUtils.SwapYZ(vertices[i].Scale(BSPUtils.inch2meterScale)).Translate(translate);
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

	public static Mesh LegacyBuildFaceMesh(Vector3[] vertices, int[] triangles, TexInfo texinfo, Vector3 origin, Texture2D texture) {
		Vector3 sAxis = SwapYZ(texinfo.SAxis / inch2meterScale); // Convert from Quake (left-handed, Z-up, inches) coordinate system to Unity (right-handed, Y-up, meters) coordinates
		Vector3 tAxis = SwapYZ(texinfo.TAxis / inch2meterScale); // This is NOT a typo. The texture axis vectors need to be DIVIDED by the conversion.
		Vector2 originShifts = new Vector2(Vector3.Dot(origin, texinfo.SAxis.normalized) * texinfo.SAxis.magnitude, Vector3.Dot(origin, texinfo.TAxis.normalized) * texinfo.TAxis.magnitude);
		Matrix4x4 texmatinverse = BuildTexMatrix(sAxis, tAxis).inverse;
		Vector2[] uvs = new Vector2[vertices.Length];
		Mesh mesh = new Mesh();
		mesh.Clear();
		for(int l = 0; l < vertices.Length; l++) {
			Vector3 textureCoord = texmatinverse.MultiplyPoint3x4(vertices[l]);
			if(texture != null) {
				uvs[l] = CalcUV(sAxis, tAxis, textureCoord, texinfo.SShift, texinfo.TShift, originShifts.x, originShifts.y, texture.width, texture.height);
			} else {
				uvs[l] = CalcUV(sAxis, tAxis, textureCoord, texinfo.SShift, texinfo.TShift, originShifts.x, originShifts.y, 64, 64);
			}
		}
		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		numVertices += vertices.Length;
		return mesh;
	}

	public static Mesh Q3BuildFaceMesh(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector3 origin, Texture2D texture) {
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

	public static Vector3 SwapYZ(Vector3 v) {
		return new Vector3(v.x, v.z, v.y);
	}

	public static UIVertex SwapYZ(UIVertex v) {
		UIVertex ret = new UIVertex();
		ret.position = SwapYZ(v.position);
		ret.normal = SwapYZ(v.normal);
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
		if(ent.hasAttribute("_light")) {
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

}
