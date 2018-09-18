namespace CCM.Core.Helpers
{
    public class MetadataHelper
    {
        /// <summary>
        /// Returnerar v�rdet p� str�ngen som pekas ut av propertyPath p� objektet obj
        /// </summary>
        public static string GetPropertyValue(object obj, string propertyPath)
        {
            // TODO: Reflection. Kolla prestanda p� denna
            if (obj == null) { return string.Empty; }
            propertyPath = propertyPath ?? string.Empty;

            var o = obj;
            foreach (var s in propertyPath.Split('.'))
            {
                var prop = o.GetType().GetProperty(s);
                if (prop == null)
                {
                    return string.Empty;
                }
                o = prop.GetValue(o, null);
                if (o == null)
                {
                    return string.Empty;
                }
            }
            return o as string ?? string.Empty;
        }
    }
}