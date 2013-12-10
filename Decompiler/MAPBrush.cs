using System;
// MAPBrush class
// Maintains a list of MAPBrushSides, to be written to a .MAP format file.
[Serializable]
public class MAPBrush {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private MAPBrushSide[] sides;
	private bool isDetailBrush = false;
	private bool isWaterBrush = false; // Both Source and Quake 2 compile water brushes into the world. Hammer doesn't use func_water.
	// So I need to keep track of whether a brush is water or not, so I can deal with it on a per-mapformat basis.
	
	// For debugging purposes only, if there's a problem these will be in the error message
	private int entnum;
	private int brushnum;
	
	// CONSTRUCTORS
	
	public MAPBrush(int num, int entnum, bool isDetailBrush) {
		sides = new MAPBrushSide[0];
		brushnum = num;
		this.entnum = entnum;
		this.isDetailBrush = isDetailBrush;
	}
	
	public MAPBrush(MAPBrushSide[] sides, int num, int entnum, bool isDetail) {
		this.sides = new MAPBrushSide[0];
		for (int i = 0; i < sides.Length; i++) {
			add(sides[i]);
		}
		brushnum = num;
		this.entnum = entnum;
		this.isDetailBrush = isDetail;
	}
	
	public MAPBrush(MAPBrush sides) {
		brushnum = sides.Brushnum;
		entnum = sides.Entnum;
		this.isDetailBrush = sides.Detail;
		this.isWaterBrush = sides.Water;
		this.sides = new MAPBrushSide[sides.NumSides];
		for (int i = 0; i < sides.NumSides; i++) {
			this.sides[i] = new MAPBrushSide(sides[i]);
		}
	}
	
	// METHODS
	
	// Adds the new brush side to the brush, but only if it's not an already defined
	// side. Otherwise you might get "brush with coplanar planes" error.
	public virtual void add(MAPBrushSide newSide) {
		if (newSide != null) {
			bool duplicate = false;
			int i = 0;
			for (i = 0; i < sides.Length; i++) {
				if (newSide.Plane.Equals(sides[i].Plane)) {
					duplicate = true;
					break;
				}
			}
			if (!duplicate) {
				MAPBrushSide[] newList = new MAPBrushSide[sides.Length + 1];
				for (int j = 0; j < sides.Length; j++) {
					newList[j] = sides[j];
				}
				newList[sides.Length] = newSide;
				sides = newList;
			} else {
				// If it is a duplicate, one of them probably came form a Doom node subdivision which will always be textured "special/nodraw"
				// but the other one (from a segment) will have a texture which needs to be visible. Select for that one.
				if (sides[i].Texture.ToUpper().Equals("special/nodraw".ToUpper()) && !newSide.Texture.ToUpper().Equals("special/nodraw".ToUpper())) {
					sides[i].Texture = newSide.Texture;
				}
			}
		}
	}
	
	public virtual void delete(int side) {
		MAPBrushSide[] newList = new MAPBrushSide[sides.Length - 1];
		for (int i = 0; i < side; i++) {
			newList[i] = sides[i];
		}
		for (int i = side + 1; i < sides.Length; i++) {
			newList[i - 1] = sides[i];
		}
		sides = newList;
	}
	
	// hasGoodSide()
	// Returns true if any side in the brush did have a triangle defined in its constructor,
	// or had one defined for it elsewhere.
	public virtual bool hasGoodSide() {
		for (int i = 0; i < sides.Length; i++) {
			if (sides[i].DefinedByTriangle) {
				return true;
			}
		}
		return false;
	}
	
	// hasBadSide()
	// Returns true if any side in the brush didn't have a triangle defined.
	// Even if it didn't, that's not necessarily a bad thing. MAPBrushSides
	// will generate a triangle from the plane passed to them instead, but
	// the triangle might be very esoteric, and certainly won't be brush corners.
	public virtual bool hasBadSide() {
		for (int i = 0; i < sides.Length; i++) {
			if (!sides[i].DefinedByTriangle) {
				return true;
			}
		}
		return false;
	}
	
	// Moves the brush by the Vector.
	public void translate(Vector3D shiftVector) {
		for (int i = 0; i < sides.Length; i++) {
			sides[i].translate(shiftVector);
		}
	}
	
	// toString()
	// DEPRECATED for use in generating mapfiles.
	[Obsolete]
	public override string ToString() {
		string out_Renamed = "{\n";
		if (isDetailBrush) {
			out_Renamed += "\"BRUSHFLAGS\" \"DETAIL\"\n";
		}
		for (int i = 0; i < sides.Length; i++) {
			out_Renamed += (sides[i].ToString() + "\n");
		}
		return out_Renamed+"}";
	}
	
	// Calculates 3 face corners, to be used to define the plane in ASCII format.
	/// Author:		UltimateSniper
	/// Returns:	List of normalised plane vertex triplets.
	public static MAPBrush CalcBrushVertices(MAPBrush mapBrush) {
		//DecompilerThread.OnMessage(this, "Recalculating vertices");
		Plane[] planes = mapBrush.Planes;
		Vector3D[][] out_Renamed = new Vector3D[planes.Length][];
		// For each triplet of planes, find intersect point.
		for (int iP1 = 0; iP1 < planes.Length; iP1++) {
			for (int iP2 = iP1 + 1; iP2 < planes.Length; iP2++) {
				for (int iP3 = iP2 + 1; iP3 < planes.Length; iP3++) {
					Vector3D testV = planes[iP1].trisect(planes[iP2], planes[iP3]);
					if (testV!=Vector3D.UNDEFINED) {
						bool isCorner = true;
						// If point is not null, test if point is behind/on all planes (if so, it is a corner).
						for (int iTest = 0; iTest < planes.Length; iTest++) {
							if (planes[iTest].Normal!=planes[iP1].Normal && planes[iTest].Normal!=planes[iP2].Normal && planes[iTest].Normal!=planes[iP3].Normal) {
								if (planes[iTest].distance(testV) > Settings.precision) {
									isCorner = false;
									break;
								}
							}
						}
						// If so, check which planes it is on.
						if (isCorner) {
							for (int iChk = 0; iChk < planes.Length; iChk++) {
								// If on this plane, and plane's vertex triplet missing min 1 point (and does not already have this point), add it.
								double dist = planes[iChk].distance(testV);
								if (System.Math.Abs(dist) <= Settings.precision) {
									// If first point on this plane, must create array.
									if (out_Renamed[iChk] == null) {
										out_Renamed[iChk] = new Vector3D[]{new Vector3D(testV), null, null};
									} else {
										// Check each value in the array for open spot OR identical point.
										for (int iChk2 = 0; iChk2 < 3; iChk2++) {
											// Open spot, fill it.
											if (out_Renamed[iChk][iChk2] == null) {
												out_Renamed[iChk][iChk2] = new Vector3D(testV);
												// If this is now a complete plane.
												if (iChk2 == 2) {
													// Order complete triplet to make a plane facing same way as given plane.
													Plane testP = new Plane(out_Renamed[iChk][0], out_Renamed[iChk][1], out_Renamed[iChk][2]);
													// If normals are not pointing in same direction, re-order points.
													if (testP.Normal*planes[iChk].Normal < 0) {
														Vector3D temp = new Vector3D(out_Renamed[iChk][1]);
														out_Renamed[iChk][1] = new Vector3D(out_Renamed[iChk][2]);
														out_Renamed[iChk][2] = temp;
													}
												}
												break;
												// Else, if this list already has this point, skip out (to avoid doubling it).
											} else if (out_Renamed[iChk][iChk2]==testV) {
												break;
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		for (int i = 0; i < mapBrush.NumSides; i++) {
			mapBrush[i].setSide(mapBrush[i].Plane, out_Renamed[i]);
		}
		return mapBrush;
	}
	
	// SimpleCorrectPlanes(MAPBrush, float)
	// Uses all sides' defined points to ensure all planes are flipped correctly.
	public static MAPBrush SimpleCorrectPlanes(MAPBrush brush)
	{
		//DecompilerThread.OnMessage(this, "Plane flip. Method: simple");
		// Find midpoint of triangle, and use that to normalise all other planes.
		int triIndex = - 1; // So we know which plane the triangle belongs to.
		Vector3D[] triangle = new Vector3D[0]; // This'll cause an exception if the loop fails
		for (int i = 0; i < brush.NumSides; i++)
		{
			if (brush[i].DefinedByTriangle)
			{
				triangle = brush[i].Triangle;
				triIndex = i;
				break;
			}
		}
		double[] normPoint = new double[]{(triangle[0].X + triangle[1].X + triangle[2].X) / 3.0, (triangle[0].Y + triangle[1].Y + triangle[2].Y) / 3.0, (triangle[0].Z + triangle[1].Z + triangle[2].Z) / 3.0};
		Plane[] allplanes = brush.Planes;
		//bool foundTriPlane = false;
		for (int iPlane = 0; iPlane < allplanes.Length; iPlane++)
		{
			// For each plane
			double dist = allplanes[iPlane].distance(normPoint); // calculate distance from point
			if (iPlane == triIndex)
			{
				// if triangle's plane & normals point in opposite direction
				Vector3D tmp = new Plane(triangle[2], triangle[0], triangle[1]).Normal;
				if (allplanes[iPlane].Normal*(new Plane(triangle[2], triangle[0], triangle[1]).Normal) < 0)
				{
					brush[iPlane].flipSide(); // flip plane
				}
			}
			else
			{
				// or if not the triangle's plane
				if (dist > Settings.precision)
				{
					// if point is on positive (outside) side of plane
					brush[iPlane].flipSide(); // flip plane
				}
			}
		}
		
		return brush;
	}
	
	// Use if brush has no triangles.
	/// Author:		UltimateSniper
	/// Returns:	Ordered list of normalised vertex triplets (ready to feed in to map).
	public static MAPBrush AdvancedCorrectPlanes(MAPBrush mapBrush)
	{
		//DecompilerThread.OnMessage(this, "Plane flip. Method: advanced");
		Plane[] allplanes = mapBrush.Planes;
		// Method:
		//1. Collect all vertices created by plane intercepts.
		//2. Create arrays of these vertices and inputted planes, to access planes via points they intersect and vice versa.
		//  MORE IMPORTANTLY, create an array indicating sides of each plane each vertex is on (-1 for -, 0 for on, 1 for +).
		//3. Run through each possible cavity (each combination of sides of each plane), collecting satisfying vertices.
		//	Correct cavity is found when there are at least 3 vertices on each plane.
		//	If fail, returns Vector3D[0][].
		//4. Generate central point of brush, and use it to produce normalised vertex triplets to return.
		
		// 1. Collect all plane intersects (all possible corners).
		//Find MaxVerts: max = N!/3!(N-3)! = (1/3!) * (N/(N-3)) * ((N-1)/(N-4)) * ((N-2)/(N-5)) * ... * (5/2) * 4 * 3!
		double dmaxVerts = 4.0;
		for (int iP = allplanes.Length; iP > 4; iP--)
		{
			dmaxVerts *= iP / (iP - 3.00);
		}
		//UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextSettingsIndex'&keyword='jlca1042'"
		Vector3D[] allverts = new Vector3D[(int) dmaxVerts]; // Max possible number of unique plane trisects: nC3.
		int iallverts = 0; // Pointer, so we know next vacant index.
		for (int iP1 = 0; iP1 < allplanes.Length; iP1++)
		{
			for (int iP2 = iP1 + 1; iP2 < allplanes.Length; iP2++)
			{
				for (int iP3 = iP2 + 1; iP3 < allplanes.Length; iP3++)
				{
					Vector3D testV = allplanes[iP1].trisect(allplanes[iP2], allplanes[iP3]);
					if (testV!=Vector3D.UNDEFINED)
					{
						// Arbitrary precision: Just checking if UNDEFINED or real.
						bool hasVtx = false;
						for (int iVtx = 0; iVtx < iallverts; iVtx++)
						{
							if (allverts[iVtx]==testV)
							{
								hasVtx = true;
								break;
							}
						}
						if (!hasVtx)
						{
							allverts[iallverts] = testV;
							iallverts++;
						}
					}
				}
			}
		}
		Vector3D[] tmp = new Vector3D[iallverts];
		Array.Copy(allverts, 0, tmp, 0, iallverts);
		allverts = tmp;
		
		
		// 2. Make array to access verts' sides of planes (can also be used to check if vert is on plane).
		sbyte[][] VertPlaneSides = new sbyte[allverts.Length][];
		for (int iV = 0; iV < allverts.Length; iV++)
		{
			sbyte[] PlaneSides = new sbyte[allplanes.Length];
			for (int iVP = 0; iVP < allplanes.Length; iVP++)
			{
				double dist = allplanes[iVP].distance(allverts[iV]);
				if (System.Math.Abs(dist) < Settings.precision)
				{
					PlaneSides[iVP] = 0;
				}
				else
				{
					PlaneSides[iVP] = ((dist >= Settings.precision)?(sbyte) 1:(sbyte) (- 1));
				}
			}
			VertPlaneSides[iV] = PlaneSides;
		}
		
		// THEORY: Collect vertices that are all either on, or on the same side of all planes.
		//		 If there are at least 3 vertices on each plane, then this is the correct shape.
		//  NOTES: -Some vertices may be included in multiple collections.
		//		 -Must have 3 vertices sharing 1 plane, and a fourth which is not on that plane to start 'cavity'.
		//		 -4 defining vertices cannot make more than 1 'cavity'.
		
		
		// Java is retarded, as it doesn't allow uints. This causes me a serious problem with this, because I am not sure
		// whether or not bitwise operations will return positive values, or switch the signs, or switch the bit order...
		
		// Cannot handle more than the max positive val for a long, and need a bit to represent each plane.
		if (allplanes.Length > 62)
		{
			throw new System.IndexOutOfRangeException("More than 62 planes in brush!");
		}
		
		
		// 3. Find all vertices which satisfy each possible cavity, and break when true brush is found.
		// Let the madness of this one great fucking for-loop commence...
		int[] TrueCorns = new int[0];
		
		for (long lCav = 0; lCav < (1 << allplanes.Length); lCav++)
		{
			int[] Corns = new int[allverts.Length];
			int iCorns = 0;
			for (int iCorn = 0; iCorn < allverts.Length; iCorn++)
			{
				bool addable = true;
				for (int iPlane = 0; iPlane < allplanes.Length; iPlane++)
				{
					// Get bit value of lCav which represents this plane (true = +, false = -), check if vert is addable.
					if (((lCav >> iPlane) & 1) == 1)
					{
						if (VertPlaneSides[iCorn][iPlane] == - 1)
						{
							addable = false;
						}
					}
					else
					{
						if (VertPlaneSides[iCorn][iPlane] == 1)
						{
							addable = false;
						}
					}
				}
				if (addable)
				{
					Corns[iCorns] = iCorn;
					iCorns++;
				}
			}
			// Check if we already have the brush...
			if (iCorns >= allplanes.Length)
			{
				bool isBrush = true;
				for (int iChkP = 0; iChkP < allplanes.Length; iChkP++)
				{
					// If all planes have at least 3 verts in this solid, IT IS THE BRUSH.
					int numOnPlane = 0;
					for (int iChkC = 0; iChkC < iCorns; iChkC++)
					{
						if (VertPlaneSides[Corns[iChkC]][iChkP] == 0)
						{
							numOnPlane++;
							if (numOnPlane >= 3)
							{
								break;
							}
						}
					}
					if (numOnPlane < 3)
					{
						isBrush = false;
						break;
					}
				}
				if (isBrush)
				{
					// Copy to TrueCorns.
					TrueCorns = new int[iCorns];
					Array.Copy(Corns, 0, TrueCorns, 0, iCorns);
					break;
				}
			}
		}
		
		
		
		
		
		// Idea: Loop all verts on 1 plane.
		//   Collect 1 cavity.
		//	Collect others, but make sure that points are not on same sides of planes as others.
		//	(Start plane side array (make upper-level one, too), and make sure that it doesn't become identical to any previous arrays.)
		
		
		
		
		
		
		/*Vector3D[] corners = new Vector3D[allverts.length];
		
		
		
		// SCREW IT. NEW PLAN...
		// Iterate through all possibilities of all sides of all planes.
		// NEED: byte[vert][plane] = -1, 0, +1, to tell whether or not vertex can be included in decompile.
		// 2^n iterations, sequence of booleans determines sides of planes.
		
		
		
		
		
		Vector3D[][] solidCollection = new Vector3D[allverts.length];
		int isolidCollection = 0;
		
		// METHOD 1: Take 1 starting vert, and find all verts on same side of all planes, and on same side of startplanes as first verts found not on startplanes.
		for (int iStart = 0; iStart < PlaneVerts[0].length; iStart++) {
		
		// Set up list of sides of planes which define this solid. 0=ON 1=+Norm -1=-Norm
		byte[] planeSidesStart = new byte[allplanes.length];
		// Collect info from first point.
		for (int i1Side = 1; i1Side < allplanes.length; i1Side++) {
		if (indexOf(PlaneVerts[i1Side], PlaneVerts[0][iStart]) == -1) {
		planeSidesStart[i1Side] = ((allplanes[i1Side].distance(PlaneVerts[0][iStart]) > 0) ? 1 : -1);
		} else {
		planeSidesStart[i1Side] = 0;
		}
		}
		
		// Collect a list of points which satisfy the conditions in planeSideStart.
		int[] potCorns = new int[allverts.length];
		int ipotCorns = 0;
		for (int iTCorn = 0; iTCorn < allverts.length; iTCorn++) {
		if (iTCorn != indexOf(allverts, PlaneVerts[0][iStart])) {
		// Check if on same side of planes as solid-defining list...
		boolean addable = true;
		for (int iPlanes = 0; iPlanes < allplanes.length; iPlanes++) {
		if (indexOf(PlaneVerts[iPlanes], allpoints[iCorn]) == -1 && planeSidesStart[iPlanes] != 0) {
		double dist = allplanes[iPlanes].distance(allpoints[iCorn]);
		if ((dist > 0 && planeSidesStart[iPlanes] < 0) || (dist < 0 && planeSidesStart[iPlanes] > 0)) {
		addable = false;
		}
		}
		}
		if (addable) {
		potCorns[ipotCorns] = iTCorn;
		ipotCorns++;
		}
		}
		}
		
		int[] basePlanes = new int[VertPlanes[indexOf(allverts, PlaneVerts[0][iStart])].length];
		// A possibility of 2^NumUnknownPlaneSides number of valid solids. Possibly on + or - side of each unknown plane.
		// Need to code something to run over each possibility once.
		for (int iSideCheck = 0; iSideCheck < basePlanes.length; iSideCheck++) {
		
		
		// For all possible solids starting from this point...
		for (int iChkNum = 0; iChkNum < allverts.length; iChkNum++) {
		// Look through all verts & find all on same side of planes.
		int[] blockcorners = new int[allpoints.length];
		blockcorners[0] = indexOf(allverts, PlaneVerts[0][iStart]);
		int iblockcorners = 1;
		
		// Set up list of sides of planes which define this solid. 0=ON 1=+Norm -1=-Norm
		// DEEP COPY IT PLEASE?
		byte[] planeSides = planeSidesStart;
		
		// Screw this complicated shit, doesn't work anyway.
		// INSTEAD, must find valid solid, and verts on opposite side of a plane that startVert is on than other solids.
		
		//
		
		
		int FirstID = 0;
		for (int iCorn = 0; iCorn < allverts.length; iCorn++) {
		boolean checkable = true;
		boolean isFirst = 
		// If is startvert or is contained in any solid also containing startvert, IS NOT CHECKABLE FIRST TIME AROUND.
		// Do for this vert, then reset to find all verts in solid.
		for (int iCheck = 0; iCheck < iSolidCollection; iCheck++) {
		
		if (!allpoints[iCorn].equals(PlaneVerts[0][iStart]) && iCorn != FirstID)
		if (!allpoints[iCorn].equals(PlaneVerts[0][iStart])) {
		// Check if on same side of planes as solid-defining list...
		int[] thisPlaneSides = new int[allplanes.length]
		int ithisPlaneSides = 0;
		boolean addable = true;
		for (int iPlanes = 0; iPlanes < allplanes.length; iPlanes++) {
		if (indexOf(PlaneVerts[iPlanes], allpoints[iCorn]) == -1) {
		double dist = allplanes[iPlanes].distance(allpoints[iCorn]);
		if (planeSides[iPlanes] == 0) {
		// If point is valid at end, temp set vert's planesides to +- plane index.
		thisPlaneSides[ithisPlaneSides] = ((dist > 0) ? iPlanes + 1 : -iPlanes - 1);
		} else if ((dist > 0 && planeSides[iPlanes] < 0) || (dist < 0 && planeSides[iPlanes] > 0)) {
		addable = false;
		}
		}
		}
		if (addable) {
		for (int iNewSides = 0; iNewSides < ithisPlaneSides; iNewSides++) {
		if (thisPlaneSides[iNewSides] > 0) {
		planeSides[thisPlaneSides[iNewSides] - 1] = 1;
		} else {
		planeSides[-thisPlaneSides[iNewSides] - 1] = -1;
		}
		}
		blockcorners[iblockcorners] = iCorn;
		iblockcorners++;
		}
		}
		}
		solidCollection[isolidCollection] = new Vector3D[iblockcorners];
		for (int iKnownCorn = 0; iKnownCorn < iblockcorners; iKnownCorn++) {
		solidCollection[isolidCollection][iKnownCorn] = allverts[blockcorners[iKnownCorn]];
		}
		isolidCollection++;
		}
		
		
		
		
		// For each vert on base plane, find 2 adjacent vertices.
		for (int iV1 = 0; iV1 < PlaneVerts[0].length; iV1++) {
		// For each other vert on base plane, check if 1st vert and this one share 2 planes & are on same sides of all planes.
		for (int iV2 = iV1 + 1; iV2 < PlaneVerts[0].length; iV2++) {
		boolean isOKVert = false;
		// Check if these 2 share 2 planes.
		// For each plane on first vert...
		for (int iChkP = 0; iChkP < VertPlanes[indexOf(allverts, PlaneVerts[0][iV1])].length; iChkP++)
		// If plane is NOT base-plane...
		if (!allplanes[0].getNormal().equals(VertPlanes[indexOf(allverts, PlaneVerts[0][iV1])][iChkP]) || allplanes[0].getDist() != VertPlanes[indexOf(allverts, PlaneVerts[0][iV1])][iChkP].getDist())
		// If this plane is common, VERT IS OK...
		if (indexOf(VertPlanes[indexOf(allverts, PlaneVerts[0][iV2])], VertPlanes[indexOf(allverts, PlaneVerts[0][iV1])][iChkP]) > -1)
		isOKVert = true;
		// Check if these 2 are on same side of all planes they are not on.
		if (isOKVert) {
		isOKVert = false;
		// For each plane, not base-plane...
		for (int iChkP2 = 1; iChkP2 < allplanes.length; iChkP2++)
		// If V1 and V2 are NOT on plane...
		if (indexOf(PlaneVerts[iChkP2], PlaneVerts[0][iV1]) == -1 && indexOf(PlaneVerts[iChkP2], PlaneVerts[0][iV2]) == -1) {
		// If V1 and V2 are on same side of plane...
		double dv1 = allplanes[iChkP2].distance(PlaneVerts[0][iV1]);
		double dv2 = allplanes[iChkP2].distance(PlaneVerts[0][iV2]);
		if ((dv1 > 0 && dv2 > 0) || (dv1 < 0 && dv2 < 0))
		isOKVert = true;
		}
		}
		if (isOKVert) {
		for (int iV3 = iV2 + 1; iV3 < PlaneVerts[0].length; iV3++) {
		
		*/
		
		
		
		// Return null value if method failed.
		if (TrueCorns.Length == 0)
		{
			throw new System.ArithmeticException("No corners found for brush!");
		}
		
		
		// 4. Create brush central point, and use it to create normalised plane triplets.
		// Create central point of brush for normalising vert-planes.
		Vector3D centrePoint = new Vector3D(0.0, 0.0, 0.0);
		for (int iCorn = 0; iCorn < TrueCorns.Length; iCorn++)
		{
			centrePoint = centrePoint+(allverts[TrueCorns[iCorn]]);
		}
		centrePoint = centrePoint/((double) TrueCorns.Length);
		// Use corners to generate brush plane triplets.
		Vector3D[][] output = new Vector3D[allplanes.Length][];
		for (int iPlane = 0; iPlane < allplanes.Length; iPlane++)
		{
			int[] vertPlane = new int[3];
			int ivertPlane = 0;
			for (int iCorn = 0; iCorn < TrueCorns.Length; iCorn++)
			{
				if (VertPlaneSides[TrueCorns[iCorn]][iPlane] == 0)
				{
					vertPlane[ivertPlane] = TrueCorns[iCorn];
					ivertPlane++;
					if (ivertPlane == 3)
					{
						break;
					}
				}
			}
			// Order triplet correctly & save to output array.
			if (new Plane(allverts[vertPlane[0]], allverts[vertPlane[1]], allverts[vertPlane[2]]).distance(centrePoint) > 0)
			{
				output[iPlane] = new Vector3D[]{allverts[vertPlane[0]], allverts[vertPlane[2]], allverts[vertPlane[1]]};
			}
			else
			{
				output[iPlane] = new Vector3D[]{allverts[vertPlane[0]], allverts[vertPlane[1]], allverts[vertPlane[2]]};
			}
		}
		for (int i = 0; i < output.Length; i++)
		{
			// This isn't setSide because the plane definition is no longer reliable; it might be flipped the wrong way
			mapBrush[i].Triangle = output[i];
		}
		return mapBrush;
	}
	
	// Some algorithms might produce planes which are correctly normalized, but
	// some don't actually contribute to the solid (such as those solids created
	// by iterating through a binary tree, and keeping track of all node subdivisions).
	// This finds them, returns a list of their indices as an array. I could return
	// a MAPBrush with those planes culled, but oftentimes there are two or three
	// brushes with the same unnecessary planes.
	public static int[] findUnusedPlanes(MAPBrush in_Renamed)
	{
		Plane[] thePlanes = in_Renamed.Planes;
		//Console.Write("Finding unnecessary planes. Before: " + thePlanes.Length);
		
		// Step 1: Get all points of intersection
		double numVerts = 4;
		// Iterative nCr algorithm; thanks to Alex's code
		for (int i = thePlanes.Length; i > 4; i--)
		{
			numVerts *= (double) i / (double) (i - 3);
		}
		//UPGRADE_TODO: Method 'java.lang.Math.round' was converted to 'System.Math.Round' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextSettingsIndex'&keyword='jlca1073_javalangMathround_double'"
		Vector3D[] theVerts = new Vector3D[(int) System.Math.Round(numVerts)];
		int index = 0;
		for (int i = 0; i < thePlanes.Length - 2; i++)
		{
			for (int j = i + 1; j < thePlanes.Length - 1; j++)
			{
				for (int k = j + 1; k < thePlanes.Length; k++)
				{
					theVerts[index++] = thePlanes[i].trisect(thePlanes[j], thePlanes[k]);
				}
			}
		}
		
		// Step 2: Throw out all vertices on the wrong side of any plane, since they're
		// all facing the "right" way.
		for (int i = 0; i < theVerts.Length; i++)
		{
			for (int j = 0; j < thePlanes.Length; j++)
			{
				if (thePlanes[j].distance(theVerts[i]) > Settings.precision)
				{
					theVerts[i] = Vector3D.UNDEFINED;
					break; //break the inner loop, let the outer loop iterate
				}
			}
		}
		
		// Step 3: Only keep sides which have three or more vertices defined
		int[] badSides = new int[0];
		for (int i = 0; i < thePlanes.Length; i++)
		{
			int numMatches = 0;
			Vector3D[] matches = new Vector3D[3];
			for (int j = 0; j < theVerts.Length; j++)
			{
				if (System.Math.Abs(thePlanes[i].distance(theVerts[j])) < Settings.precision)
				{
					bool duplicate = false;
					for (int k = 0; k < numMatches; k++)
					{
						if (theVerts[j]==matches[k])
						{
							duplicate = true;
						}
					}
					if (!duplicate)
					{
						matches[numMatches] = theVerts[j];
						numMatches++;
					}
				}
				if (numMatches >= 3)
				{
					// We have enough points.
					break;
				}
			}
			if (numMatches < 3)
			{
				int[] newList = new int[badSides.Length + 1];
				for (int j = 0; j < badSides.Length; j++)
				{
					newList[j] = badSides[j];
				}
				newList[newList.Length - 1] = i;
				badSides = newList;
			}
		}
		//DecompilerThread.OnMessage(this, " After: " + (thePlanes.Length - badSides.Length));
		return badSides;
	}
	
	// createFaceBrush(String, String, Vector3D, Vector3D)
	// This creates a rectangular brush. The String is assumed to be a texture for a face, and
	// the two vectors are a bounding box to create a plane with (mins-maxs).
	// The second String is the texture to apply to all other sides.
	public static MAPBrush createFaceBrush(string texture, string backTexture, Vector3D mins, Vector3D maxs, double xoff, double yoff, bool lowerUnpegged, int shiftYCieling, int shiftYFloor)
	{
		//DecompilerThread.OnMessage(this, "Creating brush for face with " + texture);
		MAPBrush newBrush = new MAPBrush(0, 0, false);
		Vector3D[][] planes = new Vector3D[6][];
		for (int i = 0; i < 6; i++)
		{
			planes[i] = new Vector3D[3];
		} // Six planes for a cube brush, three vertices for each plane
		double[][] texS = new double[6][];
		for (int i2 = 0; i2 < 6; i2++)
		{
			texS[i2] = new double[3];
		}
		double[][] texT = new double[6][];
		for (int i3 = 0; i3 < 6; i3++)
		{
			texT[i3] = new double[3];
		}
		
		double sideLengthXY = System.Math.Sqrt(System.Math.Pow(mins.X - maxs.X, 2) + System.Math.Pow(mins.Y - maxs.Y, 2));
		Vector3D diffVec1 = new Vector3D(mins.X, mins.Y, maxs.Z)-mins;
		Vector3D diffVec2 = new Vector3D(maxs.X, maxs.Y, mins.Z)-mins;
		Vector3D cross = diffVec2^diffVec1;
		cross.normalize();
		
		// Face
		planes[0][0] = new Vector3D(mins.X, mins.Y, maxs.Z);
		planes[0][1] = new Vector3D(maxs.X, maxs.Y, mins.Z);
		planes[0][2] = mins;
		texS[0][0] = (- (mins.X - maxs.X)) / sideLengthXY;
		texS[0][1] = (- (mins.Y - maxs.Y)) / sideLengthXY;
		texT[0][2] = - 1;
		// To be fair, to find these properly you ought to take the dot product of these over the length.
		// However, length is always one, and there's only two components (the third sum would turn out to be 0)
		double SShift = xoff - (texS[0][0] * mins.X) - (texS[0][1] * mins.Y);
		double TShift = yoff;
		// One sided linedefs (which this usually makes walls for) are only affected by lower unpegged. Upper is
		// always assumed unless lower is true.
		if (lowerUnpegged)
		{
			TShift += shiftYFloor;
		}
		else
		{
			TShift += shiftYCieling;
		}
		// Far
		planes[1][0] = new Vector3D(mins.X, mins.Y, maxs.Z)-(cross);
		planes[1][1] = mins-(cross);
		planes[1][2] = new Vector3D(maxs.X, maxs.Y, mins.Z)-(cross);
		texS[1][0] = texS[0][0];
		texS[1][1] = texS[0][1];
		texT[1][2] = - 1;
		// Top
		planes[2][0] = new Vector3D(mins.X, mins.Y, maxs.Z);
		planes[2][1] = new Vector3D(mins.X, mins.Y, maxs.Z)-(cross);
		planes[2][2] = maxs;
		texS[2][0] = 1;
		texT[2][1] = 1;
		// Bottom
		planes[3][0] = mins;
		planes[3][1] = new Vector3D(maxs.X, maxs.Y, mins.Z);
		planes[3][2] = new Vector3D(maxs.X, maxs.Y, mins.Z)-(cross);
		texS[3][0] = 1;
		texT[3][1] = 1;
		// Left
		planes[4][0] = mins;
		planes[4][1] = mins-cross;
		planes[4][2] = new Vector3D(mins.X, mins.Y, maxs.Z);
		texS[4][0] = texS[0][1];
		texS[4][1] = texS[0][0];
		texT[4][2] = 1;
		// Right
		planes[5][0] = maxs;
		planes[5][1] = maxs-cross;
		planes[5][2] = new Vector3D(maxs.X, maxs.Y, mins.Z);
		texS[5][0] = texS[0][1];
		texS[5][1] = texS[0][0];
		texT[5][2] = 1;
		
		MAPBrushSide front = new MAPBrushSide(planes[0], texture, texS[0], SShift, texT[0], TShift, 0, 1, 1, 0, "wld_lightmap", 16, 0);
		newBrush.add(front);
		for (int i = 1; i < 6; i++)
		{
			newBrush.add(new MAPBrushSide(planes[i], backTexture, texS[i], 0, texT[i], 0, 0, 1, 1, 32, "wld_lightmap", 16, 0));
		}
		
		return newBrush;
	}
	
	// Create a rectangular brush from mins to maxs, with specified texture
	public static MAPBrush createBrush(Vector3D mins, Vector3D maxs, string texture)
	{
		MAPBrush newBrush = new MAPBrush(- 1, 0, false);
		Vector3D[][] planes = new Vector3D[6][];
		for (int i = 0; i < 6; i++)
		{
			planes[i] = new Vector3D[3];
		} // Six planes for a cube brush, three vertices for each plane
		double[][] textureS = new double[6][];
		for (int i2 = 0; i2 < 6; i2++)
		{
			textureS[i2] = new double[3];
		}
		double[][] textureT = new double[6][];
		for (int i3 = 0; i3 < 6; i3++)
		{
			textureT[i3] = new double[3];
		}
		// The planes and their texture scales
		// I got these from an origin brush created by Gearcraft. Don't worry where these numbers came from, they work.
		// Top
		planes[0][0] = new Vector3D(mins.X, maxs.Y, maxs.Z);
		planes[0][1] = new Vector3D(maxs.X, maxs.Y, maxs.Z);
		planes[0][2] = new Vector3D(maxs.X, mins.Y, maxs.Z);
		textureS[0][0] = 1;
		textureT[0][1] = - 1;
		// Bottom
		planes[1][0] = new Vector3D(mins.X, mins.Y, mins.Z);
		planes[1][1] = new Vector3D(maxs.X, mins.Y, mins.Z);
		planes[1][2] = new Vector3D(maxs.X, maxs.Y, mins.Z);
		textureS[1][0] = 1;
		textureT[1][1] = - 1;
		// Left
		planes[2][0] = new Vector3D(mins.X, maxs.Y, maxs.Z);
		planes[2][1] = new Vector3D(mins.X, mins.Y, maxs.Z);
		planes[2][2] = new Vector3D(mins.X, mins.Y, mins.Z);
		textureS[2][1] = 1;
		textureT[2][2] = - 1;
		// Right
		planes[3][0] = new Vector3D(maxs.X, maxs.Y, mins.Z);
		planes[3][1] = new Vector3D(maxs.X, mins.Y, mins.Z);
		planes[3][2] = new Vector3D(maxs.X, mins.Y, maxs.Z);
		textureS[3][1] = 1;
		textureT[3][2] = - 1;
		// Near
		planes[4][0] = new Vector3D(maxs.X, maxs.Y, maxs.Z);
		planes[4][1] = new Vector3D(mins.X, maxs.Y, maxs.Z);
		planes[4][2] = new Vector3D(mins.X, maxs.Y, mins.Z);
		textureS[4][0] = 1;
		textureT[4][2] = - 1;
		// Far
		planes[5][0] = new Vector3D(maxs.X, mins.Y, mins.Z);
		planes[5][1] = new Vector3D(mins.X, mins.Y, mins.Z);
		planes[5][2] = new Vector3D(mins.X, mins.Y, maxs.Z);
		textureS[5][0] = 1;
		textureT[5][2] = - 1;
		
		for (int j = 0; j < 6; j++)
		{
			MAPBrushSide currentEdge = new MAPBrushSide(planes[j], texture, textureS[j], 0, textureT[j], 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			newBrush.add(currentEdge);
		}
		return newBrush;
	}
	
	// The inspiration for this is the BSPSource source code. It's not a direct copy but does essentially
	// the same thing as the algorithm to "create a prism back", and isn't as neatly written.
	public static MAPBrush createBrushFromWind(Vector3D[] froms, Vector3D[] tos, string texture, string backtex, TexInfo scaling)
	{
		Vector3D[] planepts = new Vector3D[3];
		MAPBrushSide[] sides = new MAPBrushSide[froms.Length + 2]; // Each edge, plus a front and back side
		planepts[0] = froms[0];
		planepts[1] = tos[0];
		planepts[2] = tos[1];
		Plane plane = new Plane(planepts);
		Vector3D reverseNormal = plane.Normal;
		sides[0] = new MAPBrushSide(planepts, texture, scaling.SAxis.Point, scaling.SShift, scaling.TAxis.Point, scaling.TShift, 0, 1, 1, 0, "wld_lightmap", 16, 0);
		
		Vector3D[] backplanepts = new Vector3D[3];
		backplanepts[0] = froms[0]-(reverseNormal);
		backplanepts[1] = tos[1]-(reverseNormal);
		backplanepts[2] = tos[0]-(reverseNormal);
		Plane backplane = new Plane(backplanepts);
		Vector3D[] backaxes = TexInfo.textureAxisFromPlane(backplane);
		sides[1] = new MAPBrushSide(backplane, backtex, backaxes[0].Point, 0, backaxes[1].Point, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
		
		for (int i = 0; i < froms.Length; i++)
		{
			// each edge
			Vector3D[] sideplanepts = new Vector3D[3];
			sideplanepts[0] = froms[i];
			sideplanepts[1] = tos[i];
			sideplanepts[2] = froms[i]+(reverseNormal);
			Plane sideplane = new Plane(sideplanepts);
			Vector3D[] sideaxes = TexInfo.textureAxisFromPlane(sideplane);
			sides[i + 2] = new MAPBrushSide(sideplane, backtex, sideaxes[0].Point, 0, sideaxes[1].Point, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
		}
		
		return new MAPBrush(sides, 0, 0, false);
	}
	
	// ACCESSORS/MUTATORS

	public MAPBrushSide this[int index] {
		get {
			return sides[index];
		}
		set {
			sides[index] = value;
		}
	}

	virtual public Plane[] Planes {
		get {
			Plane[] planes = new Plane[sides.Length];
			for (int i = 0; i < sides.Length; i++) {
				planes[i] = sides[i].Plane;
			}
			return planes;
		}
	}

	virtual public int NumSides {
		get {
			return sides.Length;
		}
	}

	virtual public bool Detail {
		get {
			return isDetailBrush;
		}
		set {
			isDetailBrush = value;
		}
	}

	virtual public bool Water {
		get {
			return isWaterBrush;
		}
		set {
			isWaterBrush = value;
		}
	}

	virtual public int Brushnum {
		get {
			return brushnum;
		}
	}

	virtual public int Entnum {
		get {
			return entnum;
		}
	}
}