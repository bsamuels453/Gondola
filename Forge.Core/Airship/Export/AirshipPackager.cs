﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Forge.Core.Airship.Data;
using Forge.Core.Airship.Generation;
using Forge.Core.Physics;
using Forge.Core.Util;
using Forge.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;

#endregion

namespace Forge.Core.Airship.Export{
    internal static class AirshipPackager{
        static readonly Dictionary<string, AirshipSerializationStruct> _airshipCache;

        static AirshipPackager(){
            _airshipCache = new Dictionary<string, AirshipSerializationStruct>();
        }

        public static Airship LoadAirship(string fileName, bool usePlayerController, ProjectilePhysics physicsEngine){
            AirshipSerializationStruct airship;
            if (_airshipCache.ContainsKey(fileName)) {
                airship = _airshipCache[fileName];
                DebugConsole.WriteLine("Airship serialization structure loaded from cache");
            }
            else{
                DebugConsole.WriteLine("Airship serialization structure not in cache, importing protocol...");
                _airshipCache.Add(
                    fileName,
                    ImportFromProtocol(fileName)
                    );
                airship = _airshipCache[fileName];
            }

            var hullSections = new HullSectionContainer(airship.HullSections);
            var deckSections = new DeckSectionContainer(airship.DeckSections);
            var modelAttribs = airship.ModelAttributes;
            var ret = new Airship(modelAttribs, deckSections, hullSections, usePlayerController, physicsEngine);
            return ret;
        }

        public static void ExportAirshipDefinition(string fileName, BezierInfo[] backCurveInfo, BezierInfo[] sideCurveInfo, BezierInfo[] topCurveInfo){
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            JObject jObj = new JObject();
            jObj["FrontBezierSurf"] = JToken.FromObject(backCurveInfo);
            jObj["SideBezierSurf"] = JToken.FromObject(sideCurveInfo);
            jObj["TopBezierSurf"] = JToken.FromObject(topCurveInfo);

            var sw = new StreamWriter(Directory.GetCurrentDirectory() + "\\Data\\" + fileName);
            sw.Write(JsonConvert.SerializeObject(jObj, Formatting.Indented));
            sw.Close();
            stopwatch.Stop();
            DebugConsole.WriteLine("Airship serialized to definition in " + stopwatch.ElapsedMilliseconds + " ms");
        }

        static Airship ImportFromDefinition(string fileName){
            var sw = new Stopwatch();
            sw.Start();
            var sr = new StreamReader(Directory.GetCurrentDirectory() + "\\Data\\" + fileName);
            var jObj = JObject.Parse(sr.ReadToEnd());
            sr.Close();

            var backInfo = jObj["FrontBezierSurf"].ToObject<List<BezierInfo>>();
            var sideInfo = jObj["SideBezierSurf"].ToObject<List<BezierInfo>>();
            var topInfo = jObj["TopBezierSurf"].ToObject<List<BezierInfo>>();

            var hullData = HullGeometryGenerator.GenerateShip
                (
                    backInfo,
                    sideInfo,
                    topInfo
                );

            var modelAttribs = new ModelAttributes();
            //in the future these attributes will be defined based off analyzing the hull
            modelAttribs.Length = 50;
            modelAttribs.MaxAscentSpeed = 10;
            modelAttribs.MaxForwardSpeed = 30;
            modelAttribs.MaxReverseSpeed = 10;
            modelAttribs.MaxTurnSpeed = 4f;
            modelAttribs.Berth = 13.95f;
            modelAttribs.NumDecks = hullData.NumDecks;
            modelAttribs.Centroid = new Vector3(modelAttribs.Length/3, 0, 0);
            sw.Stop();

            DebugConsole.WriteLine("Airship deserialized from definition in " + sw.ElapsedMilliseconds + " ms");
            throw new NotImplementedException();
            var ret = new Airship(modelAttribs, hullData.DeckSectionContainer, hullData.HullSections, true, null);
            return ret;
        }

        public static void ExportToProtocol(string fileName, HullSectionContainer hullSectionContainer, DeckSectionContainer deckSectionContainer,
            ModelAttributes attributes){
            var sw = new Stopwatch();
            sw.Start();
            var fs = new FileStream(Directory.GetCurrentDirectory() + "\\Data\\" + fileName, FileMode.Create);
            var sections = hullSectionContainer.ExtractSerializationStruct();
            var decks = deckSectionContainer.ExtractSerializationStruct();
            var aship = new AirshipSerializationStruct();
            aship.DeckSections = decks;
            aship.HullSections = sections;
            aship.ModelAttributes = attributes;
            Serializer.Serialize(fs, aship);
            fs.Close();
            sw.Stop();
            DebugConsole.WriteLine("Airship serialized to protocol in " + sw.ElapsedMilliseconds + " ms");
        }

        static AirshipSerializationStruct ImportFromProtocol(string fileName) {
            var sw = new Stopwatch();
            sw.Start();
            var fs = new FileStream(Directory.GetCurrentDirectory() + "\\Data\\" + fileName, FileMode.Open);
            var serializedStruct = Serializer.Deserialize<AirshipSerializationStruct>(fs);
            fs.Close();

            sw.Stop();

            DebugConsole.WriteLine("Airship deserialized from protocol in " + sw.ElapsedMilliseconds + " ms");

            return serializedStruct;
        }

        static Vector3 CalculateCenter(VertexPositionNormalTexture[][] airshipVertexes){
            var ret = new Vector3(0, 0, 0);
            int numVerts = 0;
            foreach (var layer in airshipVertexes){
                numVerts += layer.Length;
                foreach (var vert in layer){
                    ret += (Vector3)vert.Position;
                }
            }
            ret /= numVerts;
            return ret;
        }

        #region Nested type: AirshipSerializationStruct

        [ProtoContract]
        struct AirshipSerializationStruct{
            [ProtoMember(2)] public DeckSectionContainer.Serialized DeckSections;
            [ProtoMember(1)] public HullSectionContainer.Serialized HullSections;
            [ProtoMember(3)] public ModelAttributes ModelAttributes;
        }

        #endregion
    }
}