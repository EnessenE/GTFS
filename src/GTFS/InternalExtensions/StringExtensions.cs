namespace GTFS.InternalExtensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Nullable version of string.intern for lazy coding
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string? Intern(this string? data)
        {
            if (data == null)
            {
                return null;
            }
            return string.Intern(data);
        }
    }
}
