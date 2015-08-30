using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;
using System.Linq;

namespace ProBuilds.IO
{
    /// <summary>
    /// Custom attribute to exclude any metadata fields
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MetadataFieldAttribute : Attribute
    {
    }

    /// <summary>
    /// Custom contract resolver to exclude any metadata fields
    /// </summary>
    public class ExcludeMetadataFieldsContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            //Check if this property declared the attribute
            var attributes = member.GetCustomAttributes<MetadataFieldAttribute>();
            if (attributes.Count() > 0)
            {
                property.ShouldSerialize = x => false;
            }

            return property;
        }
    }
}
