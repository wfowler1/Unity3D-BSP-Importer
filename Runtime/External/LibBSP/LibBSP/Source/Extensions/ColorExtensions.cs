#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace LibBSP
{
#if UNITY
    using Color = UnityEngine.Color32;
#elif GODOT
    using Color = Godot.Color;
#elif NEOAXIS
    using Color = NeoAxis.ColorByte;
#else
    using Color = System.Drawing.Color;
#endif

    /// <summary>
    /// Static class containing helper methods for <c>Color</c> objects.
    /// </summary>
    public static partial class ColorExtensions
    {

        /// <summary>
        /// Constructs a new <c>Color</c> from the passed values.
        /// </summary>
        /// <param name="a">Alpha component of the color.</param>
        /// <param name="r">Red component of the color.</param>
        /// <param name="g">Green component of the color.</param>
        /// <param name="b">Blue component of the color.</param>
        /// <returns>The resulting <c>Color</c> object.</returns>
        public static Color FromArgb(int a, int r, int g, int b)
        {
#if UNITY
            return new Color((byte)r, (byte)g, (byte)b, (byte)a);
#elif GODOT
            return new Color((byte)r << 24 | (byte)g << 16 | (byte)b << 8 | (byte)a);
#elif NEOAXIS
            return new Color(r, g, b, a);
#else
            return Color.FromArgb(a, r, g, b);
#endif
        }

        /// <summary>
        /// Gets this <c>Color</c> as R8G8B8A8 (RGBA32).
        /// </summary>
        /// <param name="color">This <c>Color</c>.</param>
        /// <returns>A <c>byte</c> array with four members, RGBA.</returns>
        public static byte[] GetBytes(this Color color)
        {
            byte[] bytes = new byte[4];
#if UNITY
            bytes[0] = color.r;
            bytes[1] = color.g;
            bytes[2] = color.b;
            bytes[3] = color.a;
#elif GODOT
            bytes[0] = (byte)color.r8;
            bytes[1] = (byte)color.g8;
            bytes[2] = (byte)color.b8;
            bytes[3] = (byte)color.a8;
#elif NEOAXIS
            bytes[0] = color.Red;
            bytes[1] = color.Green;
            bytes[2] = color.Blue;
            bytes[3] = color.Alpha;
#else
            bytes[0] = color.R;
            bytes[1] = color.G;
            bytes[2] = color.B;
            bytes[3] = color.A;
#endif
            return bytes;
        }

        /// <summary>
        /// Gets the alpha component of this <see cref="Color"/>.
        /// </summary>
        /// <param name="color">This <see cref="Color"/>.</param>
        /// <returns>The alpha component of this <see cref="Color"/>.</returns>
        public static byte A(this Color color)
        {
#if UNITY
            return color.a;
#elif GODOT
            return (byte)color.a8;
#elif NEOAXIS
            return color.Alpha;
#else
            return color.A;
#endif
        }

        /// <summary>
        /// Gets the red component of this <see cref="Color"/>.
        /// </summary>
        /// <param name="color">This <see cref="Color"/>.</param>
        /// <returns>The red component of this <see cref="Color"/>.</returns>
        public static byte R(this Color color)
        {
#if UNITY
            return color.r;
#elif GODOT
            return (byte)color.r8;
#elif NEOAXIS
            return color.Red;
#else
            return color.R;
#endif
        }

        /// <summary>
        /// Gets the green component of this <see cref="Color"/>.
        /// </summary>
        /// <param name="color">This <see cref="Color"/>.</param>
        /// <returns>The green component of this <see cref="Color"/>.</returns>
        public static byte G(this Color color)
        {
#if UNITY
            return color.g;
#elif GODOT
            return (byte)color.g8;
#elif NEOAXIS
            return color.Green;
#else
            return color.G;
#endif
        }

        /// <summary>
        /// Gets the blue component of this <see cref="Color"/>.
        /// </summary>
        /// <param name="color">This <see cref="Color"/>.</param>
        /// <returns>The blue component of this <see cref="Color"/>.</returns>
        public static byte B(this Color color)
        {
#if UNITY
            return color.b;
#elif GODOT
            return (byte)color.b8;
#elif NEOAXIS
            return color.Blue;
#else
            return color.B;
#endif
        }

    }
}
