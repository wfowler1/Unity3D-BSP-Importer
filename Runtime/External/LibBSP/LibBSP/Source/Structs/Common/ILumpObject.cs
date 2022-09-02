using System;
using System.Reflection;

namespace LibBSP {

	/// <summary>
	/// Interface for an object intended to be stored in an <see cref="ILump"/> object.
	/// </summary>
	public interface ILumpObject {

		/// <summary>
		/// The <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
		/// </summary>
		ILump Parent { get; }

		/// <summary>
		/// Array of <c>byte</c>s used as the data source for this <see cref="ILumpObject"/>.
		/// </summary>
		byte[] Data { get; }

		/// <summary>
		/// The <see cref="LibBSP.MapType"/> to use to interpret <see cref="Data"/>.
		/// </summary>
		MapType MapType { get; }

		/// <summary>
		/// The version number of the <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
		/// </summary>
		int LumpVersion { get; }

	}
}
