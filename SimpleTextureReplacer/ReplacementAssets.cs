using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimpleResourceReplacer
{
    public class ReplacementAssets
    {
        Dictionary<Type, AssetReplacer> replacers = new Dictionary<Type, AssetReplacer>();
        public static Type SimplfyType(Type type)
        {
            if (typeof(Texture).IsAssignableFrom(type))
                return typeof(Texture);
            if (typeof(Component).IsAssignableFrom(type))
                return typeof(GameObject);
            return type;
        }
        public bool TryReplace(Type type, out Object replace)
        {
            var t = SimplfyType(type);
            if (replacers.TryGetValue(t, out var replacer) && replacer.TryReplace(out replace))
            {
                if (type == typeof(GameObject) && !(replace is GameObject))
                {
                    if (replace is Component component)
                        replace = component.gameObject;
                    else
                    {
                        Main.logger.LogError($"Cannot replace asset with {replace} because it is not a GameObject or Component");
                        replace = null;
                        return false;
                    }
                }
                else if (typeof(Component).IsAssignableFrom(type) && !type.IsAssignableFrom(replace.GetType()))
                {
                    GameObject gameObject = null;
                    if (replace is Component component)
                        gameObject = component.gameObject;
                    else if (replace is GameObject gameObject2)
                        gameObject = gameObject2;
                    else
                        Main.logger.LogError($"Cannot replace asset with {replace} because it is not a GameObject or Component");
                    if (gameObject)
                    {
                        var c = gameObject.GetComponent(type) ?? gameObject.GetComponentInChildren(type) ?? gameObject.GetComponentInChildren(type, true);
                        if (c)
                            replace = c;
                        else
                        {
                            Main.logger.LogError($"Cannot replace asset with {replace} because no {type.FullName} could not be found on it");
                            replace = null;
                            return false;
                        }
                    }
                }
                return true;
            }
            replace = null;
            return false;
        }
        public bool TryReplace<T>(out T replace) where T : Object
        {
            if (TryReplace(typeof(T), out var r))
            {
                replace = (T)r;
                return true;
            }
            replace = null;
            return false;
        }
        public void AddAsset(Object asset, int quality)
        {
            var t = SimplfyType(asset.GetType());
            if (!replacers.TryGetValue(t,out var replacer))
                replacers[t] = replacer = t == typeof(Texture) ? (AssetReplacer)new TextureData() : new GenericReplacer();
            replacer.AddAsset(asset, quality);
        }
        public void RemoveAsset(Object asset)
        {
            if (Main.Generated.TryGetValue(asset.name, out var children) && !children.Contains(asset))
                foreach (var child in children)
                    RemoveAsset(child);
            var t = SimplfyType(asset.GetType());
            if (replacers.TryGetValue(t,out var replacer))
                replacer.RemoveAsset(asset);
        }
    }
}