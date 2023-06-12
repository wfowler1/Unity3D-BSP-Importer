using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibBSP
{

    /// <summary>
    /// Static class containing helper methods for <c>string</c> objects.
    /// </summary>
    public static partial class StringExtensions
    {

        /// <summary>
        /// Splits a <c>string</c> using a Unicode character, unless that character is between two instances of a container.
        /// </summary>
        /// <param name="st">The <c>string</c> to split.</param>
        /// <param name="separator">Unicode <c>char</c> that delimits the substrings in this instance.</param>
        /// <param name="container">Container <c>char</c>. Any <paramref name="separator"/> characters that occur between two instances of this character will be ignored.</param>
        /// <returns>Array of <c>string</c> objects that are the resulting substrings.</returns>
        public static string[] SplitUnlessInContainer(this string st, char separator, char container, StringSplitOptions options = StringSplitOptions.None)
        {
            if (st.IndexOf(separator) < 0)
            { return new string[] { st }; }
            if (st.IndexOf(container) < 0)
            { return st.Split(new char[] { separator }, options); }

            List<string> results = new List<string>();
            bool inContainer = false;
            StringBuilder current = new StringBuilder();
            foreach (char c in st)
            {
                if (c == container)
                {
                    inContainer = !inContainer;
                    current.Append(c);
                    continue;
                }

                if (!inContainer)
                {
                    if (c == separator)
                    {
                        switch (options)
                        {
                            case StringSplitOptions.RemoveEmptyEntries:
                            {
                                if (current.Length > 0)
                                {
                                    results.Add(current.ToString());
                                }
                                current.Length = 0;
                                break;
                            }
                            case StringSplitOptions.None:
                            {
                                results.Add(current.ToString());
                                current.Length = 0;
                                break;
                            }
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                results.Add(current.ToString());
            }

            return results.ToArray<string>();
        }

        /// <summary>
        /// Splits a <c>string</c> using a Unicode character, unless that character is contained between matching <paramref name="start"/> and <paramref name="end"/> Unicode characters.
        /// </summary>
        /// <param name="st">The <c>string</c> to split.</param>
        /// <param name="separator">Unicode <c>char</c> that delimits the substrings in this instance.</param>
        /// <param name="start">The starting (left) container character. EX: '('.</param>
        /// <param name="end">The ending (right) container character. EX: ')'.</param>
        /// <returns>Array of <c>string</c> objects that are the resulting substrings.</returns>
        public static string[] SplitUnlessBetweenDelimiters(this string st, char separator, char start, char end, StringSplitOptions options = StringSplitOptions.None)
        {
            List<string> results = new List<string>();
            int containerLevel = 0;
            StringBuilder current = new StringBuilder();
            foreach (char c in st)
            {
                if (c == start)
                {
                    ++containerLevel;
                    if (containerLevel == 1)
                    {
                        continue;
                    }
                }

                if (c == end)
                {
                    --containerLevel;
                    if (containerLevel == 0)
                    {
                        continue;
                    }
                }

                if (containerLevel == 0)
                {
                    if (c == separator)
                    {
                        switch (options)
                        {
                            case StringSplitOptions.RemoveEmptyEntries:
                            {
                                if (current.Length > 0)
                                {
                                    results.Add(current.ToString());
                                }
                                current.Length = 0;
                                break;
                            }
                            case StringSplitOptions.None:
                            {
                                results.Add(current.ToString());
                                current.Length = 0;
                                break;
                            }
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                results.Add(current.ToString());
            }

            return results.ToArray<string>();
        }

        /// <summary>
        /// Parses the <c>byte</c>s in a <c>byte</c> array into an ASCII <c>string</c> up until the first null byte (0x00).
        /// </summary>
        /// <param name="bytes"><c>byte</c>s to parse.</param>
        /// <param name="offset">Position in the array to start copying from.</param>
        /// <param name="length">Number of bytes to read before stopping. Negative values will read to the end of the array.</param>
        /// <returns>The resulting <c>string</c>.</returns>
        public static string ToNullTerminatedString(this byte[] bytes, int offset = 0, int length = -1)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (i > length && length >= 0)
                {
                    break;
                }
                if (bytes[i + offset] == 0)
                {
                    break;
                }
                sb.Append((char)bytes[i + offset]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses the <c>byte</c>s in a <c>byte</c> array into an ASCII <c>string</c>.
        /// </summary>
        /// <param name="bytes"><c>byte</c>s to parse.</param>
        /// <returns>The resulting <c>string</c>.</returns>
        public static string ToRawString(this byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; ++i)
            {
                sb.Append((char)bytes[i]);
            }
            return sb.ToString();
        }

    }
}
