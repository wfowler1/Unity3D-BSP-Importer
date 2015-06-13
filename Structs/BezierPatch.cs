using UnityEngine;
using System.Collections.Generic;

namespace BSPImporter {
	public class BezierPatch : MonoBehaviour {

		private MeshFilter filter;
		new private MeshRenderer renderer;
		new private MeshCollider collider;

		//private int level;
		private UIVertex[] vertex;
		private int[] indexes;
		private int[] trianglesPerRow;
		private int[] rowIndexes;

		public UIVertex[] controls;
		public Vector2 size;

		void Start() {

		}

		void Update() {

		}

		public Mesh CreatePatchMesh(int level, Material mat) {
			List<Mesh> curveMeshes = new List<Mesh>();
			List<Mesh> collisionMeshes = new List<Mesh>();
			int xSize = (int)Mathf.Round(size[0]);
			for(int i = 0; i < size[1] - 2; i += 2) {
				for(int j = 0; j < size[0] - 2; j += 2) {

					int rowOff = (i * xSize);
					UIVertex[] thisCurveControls = new UIVertex[9];

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

					curveMeshes.Add(CreateBezierMesh(thisCurveControls, level));
					collisionMeshes.Add(CreateBezierMesh(thisCurveControls, 3));
				}
			}

			/*Mesh mesh = new Mesh();
			mesh.Clear();
			CombineInstance[] combine = new CombineInstance[curveMeshes.Count];
			for(int i = 0; i < combine.Length; i++) {
				combine[i].mesh = curveMeshes[i];
			}
			mesh.CombineMeshes(combine);*/

			Mesh mesh = BSPUtils.CombineAllMeshes(curveMeshes.ToArray(), transform, true, false);

			if(filter != null) {
				Destroy(filter.mesh);
			} else {
				filter = gameObject.GetComponent<MeshFilter>();
				if(filter == null) {
					filter = gameObject.AddComponent<MeshFilter>();
				}
			}
			filter.mesh = mesh;
			if(collider != null) {
				Destroy(collider.sharedMesh);
			} else {
				collider = gameObject.GetComponent<MeshCollider>();
				if(collider == null) {
					collider = gameObject.AddComponent<MeshCollider>();
				}
			}
			collider.sharedMesh = BSPUtils.CombineAllMeshes(collisionMeshes.ToArray(), transform, true, false);
			if(renderer == null) {
				renderer = gameObject.GetComponent<MeshRenderer>();
				if(renderer == null) {
					renderer = gameObject.AddComponent<MeshRenderer>();
				}
			}
			renderer.material = mat;
			return mesh;
		}

		public Mesh CreateBezierMesh(UIVertex[] bezierControls, int level) {
			Tessellate(bezierControls, level);
			Mesh mesh = new Mesh();
			mesh.Clear();
			Vector3[] meshverts = new Vector3[vertex.Length];
			for(int i = 0; i < meshverts.Length; i++) {
				meshverts[i] = vertex[i].position;
			}
			mesh.vertices = meshverts;
			mesh.triangles = indexes;
			Vector2[] uvs = new Vector2[vertex.Length];
			for(int i = 0; i < uvs.Length; i++) {
				uvs[i] = vertex[i].uv0;
			}
			mesh.uv = uvs;
			return mesh;
		}

		// Thanks to Morgan McGuire's July 11, 2003 article "Rendering Quake 3 Maps" for
		// this algorithm, which he in turn credits to Paul Baker's "Octagon" project.
		// http://graphics.cs.brown.edu/games/quake/quake3.html
		public void Tessellate(UIVertex[] bezierControls, int L) {
			//level = L;

			// The number of vertices along a side is 1 + num edges
			int L1 = L + 1;

			vertex = new UIVertex[L1 * L1];

			// Compute the vertices

			for(int i = 0; i <= L; ++i) {
				float p = (float)i / L;

				Vector3[] temp = new Vector3[3];
				Vector2[] tempUVs = new Vector2[3];

				for(int j = 0; j < 3; ++j) {
					int k = 3 * j;
					temp[j] = GetCurvePoint(bezierControls[k + 0].position, bezierControls[k + 1].position, bezierControls[k + 2].position, p);
					tempUVs[j] = GetCurvePoint(bezierControls[k + 0].uv0, bezierControls[k + 1].uv0, bezierControls[k + 2].uv0, p);
				}

				for(int j = 0; j <= L; ++j) {
					float a2 = (float)j / L;

					vertex[i * L1 + j].position = GetCurvePoint(temp[0], temp[1], temp[2], a2);
					vertex[i * L1 + j].uv0 = GetCurvePoint(tempUVs[0], tempUVs[1], tempUVs[2], a2);
				}
			}

			// Compute the indices
			//indexes = new int[(vertex.Length - 2) * 3];
			indexes = new int[L * L * 6];

			for(int row = 0; row < L; row++) {
				for(int col = 0; col < L; col++) {
					indexes[((row * L) + col) * 6] = (row * L1) + col;
					indexes[(((row * L) + col) * 6) + 1] = (row * L1) + col + 1;
					indexes[(((row * L) + col) * 6) + 2] = (row * L1) + col + L1;
					indexes[(((row * L) + col) * 6) + 3] = (row * L1) + col + 1;
					indexes[(((row * L) + col) * 6) + 4] = (row * L1) + col + L1 + 1;
					indexes[(((row * L) + col) * 6) + 5] = (row * L1) + col + L1;
				}
			}

		}

		public static Vector3 GetCurvePoint(Vector3 v1, Vector3 v2, Vector3 v3, float p) {
			float pinv = 1.0f - p;
			return v1 * (pinv * pinv) + v2 * (2 * pinv * p) + v3 * (p * p);
		}

		/*public void OnDrawGizmosSelected() {
			if(filter != null && filter.mesh != null) {
				float radius = 0.4064f / Mathf.Sqrt(filter.mesh.vertices.Length);
				for(int i = 0; i < filter.mesh.vertices.Length; i++) {
					float c = i / (float)(filter.mesh.vertices.Length - 1);
					Vector3 v = filter.mesh.vertices[i];
					Gizmos.color = new Color(c, c, c, 1);
					Gizmos.DrawSphere(v, radius);
				}
			}
		}*/
	}
}
