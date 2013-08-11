#region

using System.Linq;
using Forge.Core.Airship.Data;
using Forge.Core.ObjectEditor;
using Forge.Framework.Resources;
using Microsoft.Xna.Framework.Graphics;
using MonoGameUtility;
using Newtonsoft.Json.Linq;

#endregion

namespace Forge.Core.GameObjects.Statistics{
    /// <summary>
    /// Used to load statistics about gameobjects. Essentially, GameObject class only contains identifying
    /// information for what the object is and any data -specific- to that instantiation of the object such as position, rotation, etc.
    /// This provider is used to provide additional data about gameobjects so that we can extend gameobject functionality
    /// without changing the gameobject data struct or adding new ones. This class will provide information such as dimensions, access points, and any
    /// other information that will be identical across objects of the same uid/family.
    /// 
    /// Another bonus is that this class essentially defines every single possible kv field that can be assigned to an object
    /// in its configuration file. Using this class as an interface to those unique kv fields makes it easy to change them later
    /// without changing the interface.
    /// </summary>
    public static class ObjectStatisticProvider{
        static readonly GenericObjectDef[] _gameObjects;

        static ObjectStatisticProvider(){
            _gameObjects = Resource.GameObjectLoader.LoadAllGameObjects();
        }

        public static void Initialize(){
        }

        static JObject GetObject(GameObjectFamily family, long uid){
            return _gameObjects.Single(o => o.Family == (int) family && o.Uid == uid).JObject;
        }

        public static XZPoint GetObjectDims(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            return obj["Dimensions"].ToObject<XZPoint>();
        }

        public static Model GetModel(GameObjectFamily family, long uid){
            var modelStr = GetModelString(family, uid);
            var models = Resource.LoadContent<Model>(modelStr);
            return models;
        }

        public static string GetModelString(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var modelStrs = obj["Model"].ToObject<string>();
            return modelStrs;
        }

        public static GameObjectEnvironment.SideEffect GetSideEffects(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var sideEffect = obj["SideEffect"].ToObject<GameObjectEnvironment.SideEffect>();
            return sideEffect;
        }

        public static Texture2D GetIcon(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var iconTex = obj["Icon"].ToObject<string>();
            return Resource.LoadContent<Texture2D>(iconTex);
        }

        public static string GetName(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var name = obj["Name"].ToObject<string>();
            return name;
        }

        public static Vector3 GetProjectileEmitterOffset(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var offset = obj["ProjectileEmitterOffset"].ToObject<Vector3>();
            return offset;
        }

        public static float GetFiringForce(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var firingForce = obj["FiringForce"].ToObject<float>();
            return firingForce;
        }

        public static float GetMass(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var mass = obj["Mass"].ToObject<float>();
            return mass;
        }

        public static float GetRadius(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var radius = obj["Radius"].ToObject<float>();
            return radius;
        }

        public static XZRectangle GetAccessArea(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var accessArea = obj["InteractionArea"].ToObject<XZRectangle>();
            return accessArea;
        }

        public static Quadrant.Direction GetAccessAreaOrientation(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var orientation = obj["InteractionOrientation"].ToObject<Quadrant.Direction>();
            return orientation;
        }

        public static XZRectangle GetCeilingCutArea(GameObjectFamily family, long uid){
            var obj = GetObject(family, uid);
            var cutArea = obj["CeilingCutArea"].ToObject<XZRectangle>();
            return cutArea;
        }
    }
}