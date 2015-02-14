using System;
using System.Collections.Generic;
using UnityEngine;

public static class UIVertexExtensions {

	public static UIVertex Scale(this UIVertex v1, float scalar) {
		v1.position *= scalar;
		return v1;
	}

	public static UIVertex Add(this UIVertex v1, UIVertex v2) {
		v1.position += v2.position;
		return v1;
	}

	public static UIVertex Translate(this UIVertex v1, Vector3 v2) {
		v1.position += v2;
		return v1;
	}

}
