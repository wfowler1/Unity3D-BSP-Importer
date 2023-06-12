using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LibBSP
{

    /// <summary>
    /// Class representing a group of <see cref="Entity"/> objects. Contains helpful methods to handle Entities in the <c>List</c>.
    /// </summary>
    [Serializable]
    public class Entities : Lump<Entity>
    {

        /// <summary>
        /// Gets the length of this lump in bytes.
        /// </summary>
        public override int Length
        {
            get
            {
                int length = 1;

                foreach (Entity ent in this)
                {
                    length += ent.ToString().Length;
                }

                return length;
            }
        }

        /// <summary>
        /// Initializes a new empty <see cref="Entities"/> object.
        /// </summary>
        public Entities() : base(null, default(LumpInfo)) { }

        /// <summary>
        /// Initializes a new instance of an <see cref="Entities"/> object copying a passed <c>IEnumerable</c> of <see cref="Entity"/> objects.
        /// </summary>
        /// <param name="entities">Collection of <see cref="Entity"/> objects to copy.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        public Entities(IEnumerable<Entity> entities, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(entities, bsp, lumpInfo) { }

        /// <summary>
        /// Initializes a new instance of an <see cref="Entities"/> object with a specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the <c>List</c> of <see cref="Entity"/> objects.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        public Entities(int initialCapacity, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(initialCapacity, bsp, lumpInfo) { }

        /// <summary>
        /// Initializes a new <see cref="Entities"/> object.
        /// </summary>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        public Entities(BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(bsp, lumpInfo) { }

        /// <summary>
        /// Initializes a new <see cref="Entities"/> object, parsing all the bytes in the passed <paramref name="file"/>.
        /// </summary>
        /// <remarks>
        /// This will not populate Bsp or LumpInfo since a file is being read directly. This should not be used to read a
        /// lump file, they should be handled through <see cref="BSPReader"/>. Use this to read raw MAP formats instead.
        /// </remarks>
        /// <param name="file">The file to read.</param>
        public Entities(FileInfo file) : this(File.ReadAllBytes(file.FullName)) { }

        /// <summary>
        /// Initializes a new <see cref="Entities"/> object, and parses the passed <c>byte</c> array as a <c>string</c>.
        /// </summary>
        /// <param name="data"><c>Byte</c>s read from a file.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        public Entities(byte[] data, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(bsp, lumpInfo)
        {

            // Keep track of whether or not we're currently in a set of quotation marks.
            // I came across a map where the map maker used { and } within a value.
            bool inQuotes = false;
            int braceCount = 0;

            // The current character being read in the file. This is necessary because
            // we need to know exactly when the { and } characters occur and capture
            // all text between them.
            char currentChar;
            // This will be the resulting entity, fed into Entity.FromString
            StringBuilder current = new StringBuilder();

            for (int offset = 0; offset < data.Length; ++offset)
            {
                currentChar = (char)data[offset];

                if (currentChar == '\"')
                {
                    if (offset == 0)
                    {
                        inQuotes = !inQuotes;
                    }
                    else if ((char)data[offset - 1] != '\\')
                    {
                        // Allow for escape-sequenced quotes to not affect the state machine, but only if the quote isn't at the end of a line.
                        // Some Source engine entities use escape sequence quotes in values, but MoHAA has a map with an obvious erroneous backslash before a quote at the end of a line.
                        if (inQuotes && (offset + 1 >= data.Length || (char)data[offset + 1] == '\n' || (char)data[offset + 1] == '\r'))
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }

                if (!inQuotes)
                {
                    if (currentChar == '{')
                    {
                        // Occasionally, texture paths have been known to contain { or }. Since these aren't always contained
                        // in quotes, we must be a little more precise about how we want to select our delimiters.
                        // As a general rule, though, making sure we're not in quotes is still very effective at error prevention.
                        if (offset == 0 || (char)data[offset - 1] == '\n' || (char)data[offset - 1] == '\t' || (char)data[offset - 1] == ' ' || (char)data[offset - 1] == '\r')
                        {
                            ++braceCount;
                        }
                    }
                }

                if (braceCount > 0)
                {
                    current.Append(currentChar);
                }

                if (!inQuotes)
                {
                    if (currentChar == '}')
                    {
                        if (offset == 0 || (char)data[offset - 1] == '\n' || (char)data[offset - 1] == '\t' || (char)data[offset - 1] == ' ' || (char)data[offset - 1] == '\r')
                        {
                            --braceCount;
                            if (braceCount == 0)
                            {
                                Entity entity = new Entity(this);
                                entity.ParseString(current.ToString());
                                Add(entity);
                                // Reset StringBuilder
                                current.Length = 0;
                            }
                        }
                    }
                }
            }

            if (braceCount != 0)
            {
                throw new ArgumentException(string.Format("Brace mismatch when parsing entities! Entity: {0} Brace level: {1}", Count, braceCount));
            }
        }

        /// <summary>
        /// Deletes all <see cref="Entity"/> objects with "<paramref name="key"/>" set to "<paramref name="value"/>".
        /// </summary>
        /// <param name="key">Attribute to match.</param>
        /// <param name="value">Desired value of attribute.</param>
        /// <returns>The number of entities removed from the lump.</returns>
        public int RemoveAllWithAttribute(string key, string value)
        {
            return RemoveAll(entity => { return entity[key].Equals(value, StringComparison.InvariantCultureIgnoreCase); });
        }

        /// <summary>
        /// Deletes all <see cref="Entity"/> objects with the specified classname.
        /// </summary>
        /// <param name="classname">Classname attribute to find.</param>
        /// <returns>The number of entities removed from the lump.</returns>
        public int RemoveAllOfType(string classname)
        {
            return RemoveAll(entity => { return entity.ClassName.Equals(classname, StringComparison.InvariantCultureIgnoreCase); });
        }

        /// <summary>
        /// Gets a <c>List</c> of all <see cref="Entity"/> objects with "<paramref name="key"/>" set to "<paramref name="value"/>".
        /// </summary>
        /// <param name="key">Name of the attribute to search for.</param>
        /// <param name="value">Value of the attribute to search for.</param>
        /// <returns><c>List</c>&lt;<see cref="Entity"/>&gt; that have the specified key/value pair.</returns>
        public List<Entity> GetAllWithAttribute(string key, string value)
        {
            return FindAll(entity => { return entity[key].Equals(value, StringComparison.InvariantCultureIgnoreCase); });
        }

        /// <summary>
        /// Gets a <c>List</c> of <see cref="Entity"/>s objects with the specified classname.
        /// </summary>
        /// <param name="classname">Classname attribute to find.</param>
        /// <returns><c>List</c>&lt;<see cref="Entity"/>&gt; with the specified classname.</returns>
        public List<Entity> GetAllOfType(string classname)
        {
            return FindAll(entity => { return entity.ClassName.Equals(classname, StringComparison.InvariantCultureIgnoreCase); });
        }

        /// <summary>
        /// Gets a <c>List</c> of <see cref="Entity"/>s objects with the specified targetname.
        /// </summary>
        /// <param name="targetname">Targetname attribute to find.</param>
        /// <returns><c>List</c>&lt;<see cref="Entity"/>&gt; with the specified targetname.</returns>
        public List<Entity> GetAllWithName(string targetname)
        {
            return FindAll(entity => { return entity.Name.Equals(targetname, StringComparison.InvariantCultureIgnoreCase); });
        }

        /// <summary>
        /// Gets the first <see cref="Entity"/> with "<paramref name="key"/>" set to "<paramref name="value"/>".
        /// </summary>
        /// <param name="key">Name of the attribute to search for.</param>
        /// <param name="value">Value of the attribute to search for.</param>
        /// <returns><see cref="Entity"/> with the specified key/value pair, or null if none exists.</returns>
        public Entity GetWithAttribute(string key, string value)
        {
            return Find(entity => { return entity[key].Equals(value, StringComparison.InvariantCultureIgnoreCase); });
        }

        /// <summary>
        /// Gets the first <see cref="Entity"/> with the specified classname.
        /// </summary>
        /// <param name="classname">Classname attribute to find.</param>
        /// <returns><see cref="Entity"/> object with the specified classname.</returns>
        public Entity GetOfType(string classname)
        {
            return Find(entity => { return entity.ClassName.Equals(classname, StringComparison.InvariantCultureIgnoreCase); });
        }

        /// <summary>
        /// Gets the first <see cref="Entity"/> with the specified targetname.
        /// </summary>
        /// <param name="targetname">Targetname attribute to find.</param>
        /// <returns><see cref="Entity"/> object with the specified targetname.</returns>
        public Entity GetWithName(string targetname)
        {
            return Find(entity => { return entity.Name.Equals(targetname, StringComparison.InvariantCultureIgnoreCase); });
        }

        /// <summary>
        /// Gets all the data in this lump as a byte array.
        /// </summary>
        /// <param name="lumpOffset">The offset of the beginning of this lump.</param>
        /// <returns>The data.</returns>
        public override byte[] GetBytes(int lumpOffset = 0)
        {
            if (Count == 0)
            {
                return new byte[] { 0 };
            }

            StringBuilder sb = new StringBuilder();
            foreach (Entity ent in this)
            {
                sb.Append(ent.ToString());
            }

            sb.Append((char)0x00);

            return Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}
