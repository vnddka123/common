namespace LeagueSharp.Data.Utility.Resources
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Text;

    /// <summary>
    ///     Obtrieves resources.
    /// </summary>
    public static class ResourceFactory
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Gets a byte resource.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static byte[] ByteResource(string file, Assembly assembly = null)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (assembly == null)
            {
                assembly = Assembly.GetExecutingAssembly();
            }

            var resourceFile = assembly.GetManifestResourceNames().FirstOrDefault(f => f.EndsWith(file));
            if (resourceFile == null)
            {
                throw new Exception($"{(file)} Embedded Resource not found");
            }

            using (var ms = new MemoryStream())
            {
                assembly.GetManifestResourceStream(resourceFile)?.CopyTo(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        ///     Gets a string resource.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string StringResource(string file, Assembly assembly = null)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return Encoding.Default.GetString(ByteResource(file, assembly));
        }

        #endregion
    }
}