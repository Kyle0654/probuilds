using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The attribute of type T that exists on the enum value</returns>
        /// <example>string desc = myEnumVariable.GetAttributeOfType<DescriptionAttribute>().Description;</example>
        public static T GetAttribute<T>(this Enum enumVal) where T : Attribute
        {
            var type = enumVal.GetType();
            var memberInfo = type.GetMember(enumVal.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);

            // check if no attributes have been specified.
            if (((Array)attributes).Length > 0)
            {
                return (T)attributes[0];
            }
            else
            {
                return null;
            }
        }
    }
}
