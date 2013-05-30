﻿#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Cloo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace Forge.Framework.Resources{
    /// <summary>
    /// Used to wrap OpenCL script loading, compiling, and saving.
    /// </summary>
    internal class OpenCLScriptLoader : ResourceLoader{
        readonly List<ComputeDevice> _devices;
        readonly ComputePlatform _platform;
        readonly ComputeContextPropertyList _properties;
        readonly List<OpenCLScript> _scripts;

        public OpenCLScriptLoader(){
            //first initlze opencl
            DebugConsole.WriteLine("Initializing OpenCL context");
            var timer = new Stopwatch();
            timer.Start();
            _platform = ComputePlatform.Platforms[0];
            /*
#if CPU_DEBUG
            var platform = ComputePlatform.Platforms[1];
#else
            var platform = ComputePlatform.Platforms[0];
#endif
             */
            _devices = new List<ComputeDevice>();
            _devices.Add(_platform.Devices[0]);
            _properties = new ComputeContextPropertyList(_platform);
            ComputeContext = new ComputeContext(_devices, _properties, null, IntPtr.Zero);
            CommandQueue = new ComputeCommandQueue(ComputeContext, _devices[0], ComputeCommandQueueFlags.None);
            DebugConsole.WriteLine("OpenCL context initialized in " + timer.ElapsedMilliseconds + " ms");

            //now we check to make sure none of the scripts have changed since the last time they were compiled.
            var scriptFiles = GetAllFilesInDirectory("Scripts");

            var sr = new StreamReader((Directory.GetCurrentDirectory() + "\\Data\\Hashes.json"));
            var jobj = JObject.Parse(sr.ReadToEnd());
            sr.Close();
            var oldMD5 = jobj["Scripts"].ToObject<string>();
            var currentMD5 = GenerateCumulativeMD5(scriptFiles);

            bool compileScripts = !oldMD5.Equals(currentMD5);

            List<OpenCLScript> compiledScripts;

            if (compileScripts){
                DebugConsole.WriteLine("The md5 of an OpenCL script has changed since last execution, recompiling OpenCL scripts...");
                timer.Restart();
                compiledScripts = CompileScripts(scriptFiles);
                DebugConsole.WriteLine("OpenCL script recompilation completed in " + timer.ElapsedMilliseconds + " ms");
                SaveBinaries(compiledScripts);
                SaveMD5(currentMD5);
            }
            else{
                compiledScripts = LoadScripts(scriptFiles);
                DebugConsole.WriteLine("Loaded OpenCL script binaries.");
            }
            _scripts = compiledScripts;
        }

        /// <summary>
        /// Saves md5 to the //Data//Hashes.json file under the Scripts category.
        /// </summary>
        /// <param name="md5"></param>
        public static void SaveMD5(string md5){
            var sr = new StreamReader((Directory.GetCurrentDirectory() + "\\Data\\Hashes.json"));
            var jobj = JObject.Parse(sr.ReadToEnd());
            sr.Close();

            jobj["Scripts"] = md5;
            var sw = new StreamWriter((Directory.GetCurrentDirectory() + "\\Data\\Hashes.json"));
            string ss = JsonConvert.SerializeObject(jobj, Formatting.Indented);
            sw.Write(ss);
            sw.Close();
        }

        /// <summary>
        ///   The context in which openCL scripts will be run.
        /// </summary>
        public ComputeContext ComputeContext { get; private set; }

        /// <summary>
        ///   The queue of commands for the context to execute.
        /// </summary>
        public ComputeCommandQueue CommandQueue { get; private set; }

        /// <summary>
        /// Returns an OpenCL script that was compiled/loaded at the start of the program.
        /// </summary>
        /// <param name="fileName">The relative file location of the script.</param>
        /// <returns></returns>
        public ComputeProgram LoadOpenclScript(string fileName){
            var scriptEnumerable = from s in _scripts where s.SrcFileInfo.RelativeFileLocation.Equals(fileName) select s;
            var script = scriptEnumerable.Single();//defensive precaution
            return script.Program;
        }

        /// <summary>
        ///   Generates a md5 that represents the contents of all of the files in the files parameter. The md5 for each individual file is appended together into one string and returned.
        /// </summary>
        /// <param name="files"> </param>
        /// <returns> </returns>
        static string GenerateCumulativeMD5(List<FileAttributes> files){
            var md5Gen = MD5.Create();
            string hash = "";

            foreach (var file in files){
                var fr = new FileStream(file.FullFileLocation, FileMode.Open, FileAccess.Read);
                var fileHash = md5Gen.ComputeHash(fr);
                fr.Close();

                for (int i = 0; i < 16; i++){
                    string temp = "";
                    foreach (var b in fileHash){
                        temp += b.ToString();
                    }
                    hash += temp;
                }
            }
            return hash;
        }

        /// <summary>
        ///   Loads openCL binaries from the "Compiled" folder. Only loads scripts specified by the RelativeFileLocation field of the files parameter.
        /// </summary>
        /// <param name="files"> </param>
        /// <returns> </returns>
        List<OpenCLScript> LoadScripts(ICollection<FileAttributes> files){
            var ret = new List<OpenCLScript>(files.Count);

            foreach (var file in files){
                string address = "Compiled\\" + file.RelativeFileLocation;
                address += "c"; //the file extension for a compiled binary is .clc for .cl files

                var binaryFormatter = new BinaryFormatter();
                var fileStrm = new FileStream(address, FileMode.Open, FileAccess.Read, FileShare.None);
                var binary = (ReadOnlyCollection<byte[]>) binaryFormatter.Deserialize(fileStrm);
                fileStrm.Close();
                ret.Add
                    (new OpenCLScript
                        (
                        new ComputeProgram
                            (
                            ComputeContext,
                            binary,
                            _devices
                            ),
                        file
                        ));
                ret.Last().Program.Build(null, "", null, IntPtr.Zero);
            }

            return ret;
        }

        /// <summary>
        ///   Saves openCL binaries to the "Compiled" folder.
        /// </summary>
        /// <param name="scripts"> </param>
        static void SaveBinaries(IEnumerable<OpenCLScript> scripts){
            foreach (var clScript in scripts){
                var directory = Directory.GetCurrentDirectory() + "\\Compiled\\" + clScript.SrcFileInfo.RelativeFileLocation;
                directory += "c"; //the file extension for a compiled binary is .clc for .cl files
                var binaryFormatter = new BinaryFormatter();
                var fileStrm = new FileStream(directory, FileMode.Create, FileAccess.Write, FileShare.None);
                binaryFormatter.Serialize(fileStrm, clScript.Program.Binaries);
                fileStrm.Close();
            }
        }

        /// <summary>
        ///   Compile openCL scripts by loading the files theyre located in and compiling them using platform compiler.
        /// </summary>
        /// <param name="scripts"> File attributes for the scripts that are to be loaded. One script per file for OpenCL. </param>
        /// <returns> </returns>
        List<OpenCLScript> CompileScripts(ICollection<FileAttributes> scripts){
            var ret = new List<OpenCLScript>(scripts.Count);
            foreach (var file in scripts){
                string scriptContents;
                using (var sr = new StreamReader(file.FullFileLocation)){
                    scriptContents = sr.ReadToEnd();
                }
                var program = new ComputeProgram(ComputeContext, scriptContents);
                program.Build(null, "", null, IntPtr.Zero);

                ret.Add(new OpenCLScript(program, file));
            }

            return ret;
            //old build method that might be useful later
            /*
#if CPU_DEBUG
                _generationPrgm.Build(null, @"-g -s D:\Projects\Forge\Scripts\GenTerrain.cl", null, IntPtr.Zero); //use option -I + scriptDir for header search
#else
                 */
        }

        #region Nested type: OpenCLScript

        struct OpenCLScript{
            public readonly ComputeProgram Program;
            public readonly FileAttributes SrcFileInfo;

            public OpenCLScript(ComputeProgram program, FileAttributes srcFileInfo){
                Program = program;
                SrcFileInfo = srcFileInfo;
            }
        }

        #endregion
    }
}