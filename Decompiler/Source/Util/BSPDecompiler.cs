using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Class for decompiling the data in a <see cref="BSP"/> object.
	/// </summary>
	public class BSPDecompiler {

		private Job _master;

		private BSP _bsp;
		private int _currentSideIndex = 0;
		private int _itemsToProcess = 0;
		private int _itemsProcessed = 0;

		/// <summary>
		/// Creates a new instance of a <see cref="Decompiler"/> object.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> object which will be processed.</param>
		/// <param name="master">The parent <see cref="Job"/> object for this instance.</param>
		public BSPDecompiler(BSP bsp, Job master) {
			this._bsp = bsp;
			this._master = master;

			if (bsp.brushes != null) { _itemsToProcess += bsp.entities.Count; }
			if (bsp.brushes != null) { _itemsToProcess += bsp.brushes.Count; }
			if (bsp.staticProps != null) { _itemsToProcess += bsp.staticProps.Count; }
			if (bsp.cubemaps != null) { _itemsToProcess += bsp.cubemaps.Count; }
		}

		/// <summary>
		/// Begins the decompiling process on the <see cref="BSP"/> object passed to the constructor.
		/// </summary>
		/// <returns>An <see cref="Entities"/> object containing all the processed data.</returns>
		public Entities Decompile() {
			// There's no need to deepcopy; only one process will run on these entities and this will not be saved back to a BSP file
			Entities entities = _bsp.entities;
			foreach (Entity entity in entities) {
				ProcessEntity(entity);
				++_itemsProcessed;
				ReportProgress();
			}
			if (_bsp.staticProps != null) {
				foreach (StaticProp prop in _bsp.staticProps) {
					entities.Add(prop.ToEntity(_bsp.staticProps.dictionary));
					++_itemsProcessed;
					ReportProgress();
				}
			}
			if (_bsp.cubemaps != null) {
				foreach (Cubemap cubemap in _bsp.cubemaps) {
					entities.Add(cubemap.ToEntity());
					++_itemsProcessed;
					ReportProgress();
				}
			}

			return entities;
		}

		/// <summary>
		/// Processes an <see cref="Entity"/> into a state where it can be output into a file that map editors can read.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to process.</param>
		/// <remarks>This method does not return anything, since the <see cref="Entity"/> object is modified by reference.</remarks>
		private void ProcessEntity(Entity entity) {
			int modelNumber = entity.modelNumber;
			// If this Entity has no modelNumber, then this is a no-op. No processing is needed.
			// A modelnumber of 0 indicates the world entity.
			if (modelNumber >= 0) {
				List<Brush> brushes = _bsp.GetBrushesInModel(_bsp.models[modelNumber]);
				// If GetBrushesInModel returns null, this probably is a Quake or Half-Life map.
				if (brushes != null) {
					foreach (Brush brush in brushes) {
						MAPBrush result = ProcessBrush(brush, entity.origin);
						result.isWater |= (entity.className == "func_water");
						if (_master.settings.brushesToWorld) {
							_bsp.entities.GetWithAttribute("classname", "worldspawn").brushes.Add(result);
						} else {
							entity.brushes.Add(result);
						}
						++_itemsProcessed;
						ReportProgress();
					}
				}
				entity.Remove("model");
			}
			// If this is model 0 (worldspawn) there are other things that need to be taken into account.
			if (modelNumber == 0) {
				if (_bsp.faces != null) {
					foreach (Face face in _bsp.faces) {
						if (face.displacement >= 0) {
							MAPDisplacement displacement = ProcessDisplacement(_bsp.dispInfos[face.displacement]);
							MAPBrush newBrush = face.CreateBrush(_bsp, 32);
							newBrush.sides[0].displacement = displacement;
							// If we are not decompiling to VMF, vis will need to skip this brush.
							newBrush.isDetail = true;
							entity.brushes.Add(newBrush);
						} else if (face.flags == 2) {
							MAPPatch patch = ProcessPatch(face);
							MAPBrush newBrush = new MAPBrush();
							newBrush.patch = patch;
							entity.brushes.Add(newBrush);
						} else if ((_bsp.version == MapType.STEF2 || _bsp.version == MapType.STEF2Demo) && face.flags == 5) {
							MAPTerrain terrain = ProcessTerrain(face);
							MAPBrush newBrush = new MAPBrush();
							newBrush.terrain = terrain;
							entity.brushes.Add(newBrush);
						}
					}
				}
			}
		}

		/// <summary>
		/// Processes a <see cref="Brush"/> into a state where it can be output into a file that map editors can read.
		/// </summary>
		/// <param name="brush">The <see cref="Brush"/> to process.</param>
		/// <param name="worldPosition">The position of the parent <see cref="Entity"/> in the world. This is important for calculating UVs on solids.</param>
		/// <returns>The processed <see cref="MAPBrush"/> object, to be added to an <see cref="Entity"/> object.</returns>
		private MAPBrush ProcessBrush(Brush brush, Vector3d worldPosition) {
			List<BrushSide> sides;
			// CoD BSPs store brush sides sequentially so the brush structure doesn't reference a first side.
			if (brush.firstSide < 0) {
				sides = _bsp.brushSides.GetRange(_currentSideIndex, brush.numSides);
				_currentSideIndex += brush.numSides;
			} else {
				sides = _bsp.GetReferencedObjects<BrushSide>(brush, "brushSides");
			}
			MAPBrush mapBrush = new MAPBrush();
			mapBrush.isDetail = brush.IsDetail(_bsp);
			mapBrush.isWater = brush.IsWater(_bsp);
			int sideNum = 0;
			foreach (BrushSide side in sides) {
				MAPBrushSide mapBrushSide = ProcessBrushSide(side, worldPosition, sideNum);
				if (mapBrushSide != null) {
					mapBrush.sides.Add(mapBrushSide);
				}
				++sideNum;
			}

			return mapBrush;
		}

		/// <summary>
		/// Processes a <see cref="BrushSide"/> into a state where it can be output into a file that map editors can read.
		/// </summary>
		/// <param name="brushSide">The <see cref="BrushSide"/> to process.</param>
		/// <param name="worldPosition">The position of the parent <see cref="Entity"/> in the world. This is important for calculating UVs on solids.</param>
		/// <param name="sideIndex">The index of this side reference in the parent <see cref="Brush"/>. Important for Call of Duty series maps, since
		/// the first six <see cref="BrushSide"/>s in a <see cref="Brush"/> don't contain <see cref="Plane"/> references.</param>
		/// <returns>The processed <see cref="MAPBrushSode"/> object, to be added to a <see cref="Brush"/> object.</returns>
		private MAPBrushSide ProcessBrushSide(BrushSide brushSide, Vector3d worldPosition, int sideIndex) {
			if (brushSide.bevel) { return null; }
			MAPBrushSide mapBrushSide;
			// The things we'll need to define a .MAP brush side
			string texture;
			string material = "wld_lightmap";
			TextureInfo texInfo;
			Vector3d[] threePoints;
			Plane plane;
			int flags = 0;

			// If we have a face reference here, let's use it!
			if (brushSide.face >= 0) {
				Face face = _bsp.faces[brushSide.face];
				// In Nightfire, faces with "256" flag set should be ignored
				if ((face.flags & (1 << 8)) != 0) { return null; }
				texture = (_master.settings.replace512WithNull && (face.flags & (1 << 9)) != 0) ? "**nulltexture**" : _bsp.textures[face.texture].name;
				texInfo = _bsp.texInfo[face.textureInfo];
				threePoints = GetPointsForFace(face, brushSide);
				if (face.plane >= 0 && face.plane < _bsp.planes.Count) {
					plane = _bsp.planes[face.plane];
				} else if (brushSide.plane >= 0 && brushSide.plane < _bsp.planes.Count) {
					plane = _bsp.planes[brushSide.plane];
				} else {
					plane = new Plane(0, 0, 0, 0);
				}
				flags = _master.settings.noFaceFlags ? 0 : face.flags;
				if (face.material >= 0) {
					material = _bsp.materials[face.material].name;
				}
			} else {
				// TODO: This is awful. Let's rework the enum to have internal ways to check engine forks.
				if (_bsp.version == MapType.CoD || _bsp.version == MapType.CoD2 || _bsp.version == MapType.CoD4) {
					switch (sideIndex) {
						case 0: { // XMin
							plane = new Plane(-1, 0, 0, -brushSide.dist);
							break;
						}
						case 1: { // XMax
							plane = new Plane(1, 0, 0, brushSide.dist);
							break;
						}
						case 2: { // YMin
							plane = new Plane(0, -1, 0, -brushSide.dist);
							break;
						}
						case 3: { // YMax
							plane = new Plane(0, 1, 0, brushSide.dist);
							break;
						}
						case 4: { // ZMin
							plane = new Plane(0, 0, -1, -brushSide.dist);
							break;
						}
						case 5: { // ZMax
							plane = new Plane(0, 0, 1, brushSide.dist);
							break;
						}
						default: {
							plane = _bsp.planes[brushSide.plane];
							break;
						}
					}
				} else {
					plane = _bsp.planes[brushSide.plane];
				}
				threePoints = plane.GenerateThreePoints();
				if (brushSide.texture >= 0) {
					// TODO: This is awful. Let's rework the enum to have internal ways to check engine forks.
					if (_bsp.version == MapType.Source17 ||
					    _bsp.version == MapType.Source18 ||
					    _bsp.version == MapType.Source19 ||
					    _bsp.version == MapType.Source20 ||
					    _bsp.version == MapType.Source21 ||
					    _bsp.version == MapType.Source22 ||
					    _bsp.version == MapType.Source23 ||
						 _bsp.version == MapType.Source27 ||
						 _bsp.version == MapType.Vindictus ||
						 _bsp.version == MapType.DMoMaM ||
						 _bsp.version == MapType.L4D2 ||
					    _bsp.version == MapType.TacticalInterventionEncrypted) {
						texInfo = _bsp.texInfo[brushSide.texture];
						TextureData currentTexData;
						// I've only found one case where this is bad: c2a3a in HL Source. Don't know why.
						if (texInfo.texture >= 0) {
							currentTexData = _bsp.texDatas[texInfo.texture];
							texture = _bsp.textures.GetTextureAtOffset((uint)_bsp.texTable[currentTexData.stringTableIndex]);
						} else {
							texture = "**skiptexture**";
						}
					} else {
						Texture textureDef = _bsp.textures[brushSide.texture];
						if ((textureDef.flags & (1 << 2)) != 0) {
							texture = "**skytexture**";
						} else if ((textureDef.flags & (1 << 9)) != 0) {
							texture = "**skiptexture**";
						} else if ((textureDef.flags & (1 << 8)) != 0) {
							texture = "**hinttexture**";
						} else {
							texture = textureDef.name;
						}
						texInfo = textureDef.texAxes;
					}
				} else {
					Vector3d[] newAxes = TextureInfo.TextureAxisFromPlane(plane);
					texInfo = new TextureInfo(newAxes[0], 0, 1, newAxes[1], 0, 1, 0, -1);
					texture = "**cliptexture**";
				}
			}

			TextureInfo outputTexInfo;
			if (texInfo != null && !texture.Substring(0, "tools/".Length).Equals("tools/", StringComparison.InvariantCultureIgnoreCase)) {
				outputTexInfo = texInfo.BSP2MAPTexInfo(worldPosition);
			} else {
				Vector3d[] newAxes = TextureInfo.TextureAxisFromPlane(plane);
				outputTexInfo = new TextureInfo(newAxes[0], 0, 1, newAxes[1], 0, 1, 0, -1);
			}

			mapBrushSide = new MAPBrushSide() {
				vertices = threePoints,
				plane = plane,
				texture = texture,
				textureS = outputTexInfo.axes[0],
				textureShiftS = outputTexInfo.shifts[0],
				textureT = outputTexInfo.axes[1],
				textureShiftT = outputTexInfo.shifts[1],
				texRot = 0,
				texScaleX = outputTexInfo.scales[0],
				texScaleY = outputTexInfo.scales[1],
				flags = flags,
				material = material,
				lgtScale = 16,
				lgtRot = 0
			};

			return mapBrushSide;
		}

		/// <summary>
		/// Processes a <see cref="Face"/> as a biquadratic Bezier patch. The vertices of the <see cref="Face"/> are interpreted
		/// as control points for multiple spline curves, which are then interpolated and rendered at an arbitrary quality value.
		/// For Quake 3 engine forks only.
		/// </summary>
		/// <param name="face">The <see cref="Face"/> object to process.</param>
		/// <returns>A <see cref="MAPPatch"/> object to be added to a <see cref="MAPBrush"/> object.</returns>
		private MAPPatch ProcessPatch(Face face) {
			List<UIVertex> vertices = _bsp.GetReferencedObjects<UIVertex>(face, "vertices");
			return new MAPPatch() {
				dims = new Vector2d(face.patchSize.x, face.patchSize.y),
				texture = _bsp.textures[face.texture].name,
				points = vertices.ToArray<UIVertex>()
			};
		}

		/// <summary>
		/// Processes a <see cref="Face"/> as a terrain. The vertices of the <see cref="Face"/> are processed into a heightmap
		/// defining the heights of the terrain at that point.
		/// For Quake 3 engine forks only.
		/// </summary>
		/// <param name="face">The <see cref="Face"/> object to process.</param>
		/// <returns>A <see cref="MAPPatch"/> object to be added to a <see cref="MAPBrush"/> object.</returns>
		private MAPTerrain ProcessTerrain(Face face) {
			string texture = _bsp.textures[face.texture].name;
			int flags = _bsp.textures[face.texture].flags;
			List<UIVertex> vertices = _bsp.GetReferencedObjects<UIVertex>(face, "vertices");
			int side = (int)Math.Sqrt(vertices.Count);
			Vector2d mins = new Vector2d(Double.PositiveInfinity, Double.PositiveInfinity);
			Vector2d maxs = new Vector2d(Double.NegativeInfinity, Double.NegativeInfinity);
			Vector3d start = Vector3d.undefined;
			foreach (UIVertex v in vertices) {
				if (v.position.x < mins.x) {
					mins.x = v.position.x;
				}
				if (v.position.x > maxs.x) {
					maxs.x = v.position.x;
				}
				if (v.position.y < mins.y) {
					mins.y = v.position.y;
				}
				if (v.position.y > maxs.y) {
					maxs.y = v.position.y;
				}
				if (v.position.x == mins.x && v.position.y == mins.y) {
					start = v.position;
				}
			}
			start.z = 0;
			double sideLength = maxs.x - mins.x;
			double gridUnit = sideLength / (side - 1);
			float[][] heightMap = new float[side][];
			float[][] alphaMap = new float[side][];
			foreach (UIVertex v in vertices) {
				int col = (int)Math.Round((v.position.x - mins.x) / gridUnit);
				int row = (int)Math.Round((v.position.y - mins.y) / gridUnit);
				if (heightMap[row] == null) {
					heightMap[row] = new float[side];
					alphaMap[row] = new float[side];
				}
				heightMap[row][col] = (float)(v.position.z);
			}
			return new MAPTerrain() {
				side = side,
				texture = texture,
				textureShiftS = 0,
				textureShiftT = 0,
				texRot = 0,
				texScaleX = 1,
				texScaleY = 1,
				flags = flags,
				sideLength = sideLength,
				start = start,
				IF = Vector4d.zero,
				LF = Vector4d.zero,
				heightMap = heightMap,
				alphaMap = alphaMap
			};
		}

		/// <summary>
		/// Processes a <see cref="DisplacementInfo"/> object into a state where it can be output into a file that Hammer can read.
		/// For Source engine forks only.
		/// </summary>
		/// <param name="displacement">The <see cref="DisplacementInfo"/> object to process.</param>
		/// <returns>A <see cref="MAPDisplacement"/> object to be added to a <see cref="MAPBrushSide"/>.</returns>
		private MAPDisplacement ProcessDisplacement(DisplacementInfo displacement) {
			int power = displacement.power;
			int first = displacement.dispVertStart;
			Vector3d start = displacement.startPosition;
			int numVertsInRow = (int)Math.Pow(2, power) + 1;
			Vector3d[][] normals = new Vector3d[numVertsInRow][];
			float[][] distances = new float[numVertsInRow][];
			float[][] alphas = new float[numVertsInRow][];
			for (int i = 0; i < numVertsInRow; ++i) {
				normals[i] = new Vector3d[numVertsInRow];
				distances[i] = new float[numVertsInRow];
				alphas[i] = new float[numVertsInRow];
				for (int j = 0; j < numVertsInRow; ++j) {
					normals[i][j] = _bsp.dispVerts[first + (i * numVertsInRow) + j].normal;
					distances[i][j] = _bsp.dispVerts[first + (i * numVertsInRow) + j].dist;
					alphas[i][j] = _bsp.dispVerts[first + (i * numVertsInRow) + j].alpha;
				}
			}

			return new MAPDisplacement() {
				power = power,
				start = start,
				normals = normals,
				distances = distances,
				alphas = alphas
			};
		}

		/// <summary>
		/// Changes the progress value of the master object.
		/// </summary>
		private void ReportProgress() {
			int onePercent = _itemsToProcess / 100;
			if (onePercent == 0) {
				onePercent = 1;
			}
			if (_itemsProcessed % onePercent == 0) {
				_master.progress = _itemsProcessed / (double)_itemsToProcess;
			}
		}

		/// <summary>
		/// Looks at the information in the passed <paramref name="face"/> and tries to find the triangle defined
		/// by <paramref name="face"/> with the greatest area. If <paramref name="face"/> does not reference any
		/// vertices then we generate a triangle through the referenced <see cref="Plane"/> instead.
		/// </summary>
		/// <param name="face">The <see cref="Face"/> to find a triangle for.</param>
		/// <returns>Three points defining a triangle which define the plane which <paramref name="face"/> lies on.</returns>
		private Vector3d[] GetPointsForFace(Face face, BrushSide brushSide) {
			Vector3d[] ret;
			if (face.numVertices > 2) {
				ret = new Vector3d[3];
				double bestArea = 0;
				for (int i = 0; i < face.numIndices / 3; ++i) {
					Vector3d[] temp = new Vector3d[] {
						_bsp.vertices[(int)(face.firstVertex + _bsp.indices[face.firstIndex + (i * 3)])].position,
						_bsp.vertices[(int)(face.firstVertex + _bsp.indices[face.firstIndex + 1 + (i * 3)])].position,
						_bsp.vertices[(int)(face.firstVertex + _bsp.indices[face.firstIndex + 2 + (i * 3)])].position
					};
					double area = Vector3d.SqrTriangleArea(temp[0], temp[1], temp[2]);
					if (area > bestArea) {
						bestArea = area;
						ret = temp;
					}
				}
				if (bestArea > 0.001) {
					return ret;
				}
			}
			if (face.numEdges > 0) {
				// TODO: Edges = triangles
			}
			if (face.plane >= 0 && face.plane < _bsp.planes.Count) {
				ret = _bsp.planes[face.plane].GenerateThreePoints();
			} else if (brushSide.plane >= 0 && brushSide.plane < _bsp.planes.Count) {
				ret = _bsp.planes[brushSide.plane].GenerateThreePoints();
			} else {
				_master.Print("WARNING: Brush side with no points!");
				return new Vector3d[3];
			}
			return ret;
		}

	}
}
