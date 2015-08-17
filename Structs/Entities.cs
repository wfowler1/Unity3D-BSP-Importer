using System;
using System.Collections.Generic;
// Entities class

// This class extends the Lump class, providing methods which are only useful
// for a group of Entity objects (and not really anything else).

namespace BSPImporter {
	/// <summary>
	/// Class representing a group of <c>Entity</c> objects.
	/// </summary>

	public class Entities : List<Entity> {

		/// <summary>
		/// Initializes a new instance of an <c>Entities</c> object copying a passed <c>IEnumerable</c> of <c>Entity</c> objects.
		/// </summary>
		/// <param name="data">Collection of <c>Entity</c> objects to copy</param>
		public Entities(IEnumerable<Entity> data) : base(data) { }

		/// <summary>
		/// Initializes a new instance of an <c>Entities</c> object with a specified initial capacity.
		/// </summary>
		/// <param name="initialCapacity">Initial capacity of the <c>List</c> of <c>Entity</c> objects</param>
		public Entities(int initialCapacity) : base(initialCapacity) { }

		/// <summary>
		/// Initializes a new empty <c>Entities</c> object.
		/// </summary>
		public Entities() : base() { }

		/// <summary>
		/// Initializes a new <c>Entities</c> object, and parses the passed <c>byte</c> array into the <c>List</c>.
		/// </summary>
		/// <param name="data"></param>
		public Entities(byte[] data)
			: base() {
			// Keep track of whether or not we're currently in a set of quotation marks.
			// I came across a map where the idiot map maker used { and } within a value. This broke the code before.
			bool inQuotes = false;
			int braceCount = 0;

			// The current character being read in the file. This is necessary because
			// we need to know exactly when the { and } characters occur and capture
			// all text between them.
			char currentChar;
			// This will be the resulting entity, fed into Entity.FromString
			System.Text.StringBuilder current = new System.Text.StringBuilder();

			for (int offset = 0; offset < data.Length; ++offset) {
				currentChar = (char)data[offset];

				// Allow for escape-sequenced quotes to not affect the state machine.
				if (currentChar == '\"' && (offset == 0 || (char)data[offset - 1] != '\\')) {
					inQuotes = !inQuotes;
				}

				if (!inQuotes) {
					if (currentChar == '{') {
						// Occasionally, texture paths have been known to contain { or }. Since these aren't always contained
						// in quotes, we must be a little more precise about how we want to select our delimiters.
						// As a general rule, though, making sure we're not in quotes is still very effective at error prevention.
						if (offset == 0 || (char)data[offset - 1] == '\n' || (char)data[offset - 1] == '\t' || (char)data[offset - 1] == ' ' || (char)data[offset - 1] == '\r') {
							++braceCount;
						}
					}
				}

				if (braceCount > 0) {
					current.Append(currentChar);
				}

				if (!inQuotes) {
					if (currentChar == '}') {
						if (offset == 0 || (char)data[offset - 1] == '\n' || (char)data[offset - 1] == '\t' || (char)data[offset - 1] == ' ' || (char)data[offset - 1] == '\r') {
							--braceCount;
							if (braceCount == 0) {
								this.Add(Entity.FromString(current.ToString()));
								// Reset StringBuilder
								current.Length = 0;
							}
						}
					}
				}
			}

			if (braceCount != 0) {
				UnityEngine.Debug.LogErrorFormat("Brace mismatch when parsing entities! Brace level: {0}", braceCount);
			}
		}

		/// <summary>
		/// Deletes all <c>Entity</c> with "<paramref name="key" />" set to "<paramref name="value" />".
		/// </summary>
		/// <param name="key">Attribute to match</param>
		/// <param name="value">Desired value of attribute</param>
		public void RemoveAllWithAttribute(string key, string value) {
			//DeleteEnts(FindAllWithAttribute(key, value));
			this.RemoveAll(entity => { return entity[key] == value; });
		}

		/// <summary>
		/// Gets a <c>List</c> of all <c>Entity</c>s with "<paramref name="key" />" set to "<paramref name="value" />".
		/// </summary>
		/// <param name="key">Name of the attribute to search for</param>
		/// <param name="value">Value of the attribute to search for</param>
		/// <returns><c>List</c>(<c>Entity</c>) that have the specified key/value pair</returns>
		public List<Entity> GetAllWithAttribute(string key, string value) {
			return FindAll(entity => { return entity[key] == value; });
		}

		/// <summary>
		/// Gets a <c>List</c> of <c>Entity</c>s objects with the specified targetname
		/// </summary>
		/// <param name="targetname">Targetname attribute to find</param>
		/// <returns><c>List</c>(<c>Entity</c>) with the specified targetname</returns>
		public List<Entity> GetAllWithName(string targetname) {
			return GetAllWithAttribute("targetname", targetname);
		}

		/// <summary>
		/// Gets the first <c>Entity</c> with "<paramref name="key" />" set to "<paramref name="value" />".
		/// </summary>
		/// <param name="key">Name of the attribute to search for</param>
		/// <param name="value">Value of the attribute to search for</param>
		/// <returns><c>Entity</c> with the specified key/value pair, or null if none exists</returns>
		public Entity GetWithAttribute(string key, string value) {
			return Find(entity => { return entity[key] == value; });
		}

		/// <summary>
		/// Gets the first <c>Entity</c> with the specified targetname.
		/// </summary>
		/// <param name="targetname">Targetname attribute to find</param>
		/// <returns>Entity object with the specified targetname</returns>
		public Entity GetWithName(string targetname) {
			return GetWithAttribute("targetname", targetname);
		}
	}
}
