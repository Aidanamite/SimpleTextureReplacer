using System.Collections.Generic;

namespace SimpleResourceReplacer
{
    public class Keys
    {
        public List<ResouceKey> SingleAssets = new List<ResouceKey>();
        public List<(ResouceKey resource, string texture)> GameObjects = new List<(ResouceKey, string)>();
    }

    public struct ResouceKey
    {
        public string bundle;
        public string resource;
        public string FullResourceString => bundle + '/' + resource;
        public ResouceKey(string bundle, string resource)
        {
            this.bundle = bundle;
            this.resource = resource;
        }
        public ResouceKey(string FullResourceString)
        {
            resource = FullResourceString.After('/');
            if (resource == FullResourceString)
            {
                UnityEngine.Debug.LogWarning($"Resource path \"{FullResourceString}\" does not appear to be a valid resource path!");
                bundle = FullResourceString;
                resource = null;
            }
            else
                bundle = FullResourceString.Remove(FullResourceString.Length - 1 - resource.Length);
        }
        public static implicit operator ResouceKey((string bundle, string resource) tuple) => new ResouceKey(tuple.bundle, tuple.resource);
    }
}