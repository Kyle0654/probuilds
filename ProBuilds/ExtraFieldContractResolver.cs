using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace ProBuilds
{
    /// <summary>
    /// Custom attribute to exclude any extra fields
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class JsonExtraFieldAttribute : Attribute
    {
    }

    /// <summary>
    /// Custom contract resolver to exclude any extra fields
    /// </summary>
    public class ExtraFieldContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            //Check if this property declared the attribute
            var extraPropertyAttr = member.GetCustomAttribute<JsonExtraFieldAttribute>();
            if (extraPropertyAttr != null)
            {
                property.ShouldSerialize = x => false;
            }

            return property;
        }
    }
}
