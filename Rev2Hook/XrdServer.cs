﻿using System;
using System.Collections.Generic;
using Binarysharp.MemoryManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Rev2Hook
{
    public class XrdServer : MarshalByRefObject
        {
            int PID;
            string mods_path;
            List<(IntPtr, int)> loadedScripts = new List<(IntPtr, int)> { };
            MemorySharp ms;
            StreamWriter log;

            public void SetModsPath(string path)
            {
                mods_path = path;
                log = File.CreateText($"{path}/../log.txt");
            }

            public void IsInjected(int clientPID)
            {
                PID = clientPID;
                ms = new MemorySharp(Process.GetProcessById(PID));
                ReportMessage($"Injected into process {clientPID}!");
            }

            public void ReportMessage(string message)
            {
                log.WriteLine(message);
                Console.WriteLine(message);
            }

            public void ReportException(Exception e)
            {
                
                Console.WriteLine("Reported exception: {0}", e);
            }

            public void Ping()
            {

            }

            public string CheckForCharacter(IntPtr scriptLocation)
            {
                int functionCount = ms.Read<int>(scriptLocation, false);
                IntPtr charLocation = IntPtr.Add(scriptLocation, 0x8 + ((functionCount + 1) * 0x24));

                string shortName = ms.ReadString(charLocation, false).ToUpper();
                return shortName;
            }

            public byte[] ExtractScript(int index)
            {
                try
                {
                    if (index >= loadedScripts.Count)
                    {
                        throw new Exception("Can't detect loaded script!");
                    }

                    IntPtr location = loadedScripts[index].Item1;
                    int size = loadedScripts[index].Item2;

                    var extracted = ms.Read<byte>(location, size, false);
                    return extracted;
                }
                catch (Exception e)
                {
                    throw (e);
                }
            }

            public void AddScriptEntry(IntPtr location, int size)
            {
                loadedScripts.Add((location, size));
            }

            public void ClearScriptEntries()
            {
                loadedScripts.Clear();
            }

            public (IntPtr, int) TryLoadScript(ScriptType script, string character)
            {
                var result = (IntPtr.Zero, -1);
                string path = String.Empty;


                ReportMessage($"Got character: {character}\nGot ScriptType: {script}");
                try
                {
                    if (script == ScriptType.Player1 || script == ScriptType.Player2)
                    {
                        path = Path.Combine(mods_path, $"{character}.bbscript");
                    }
                    else if (script == ScriptType.Player1EF || script == ScriptType.Player2EF)
                    {
                        path = Path.Combine(mods_path, $"{character}_ETC.bbscript");
                    }
                    else if (script == ScriptType.Cmn)
                    {
                        path = Path.Combine(mods_path, "CMN.bbscript");
                    }
                    else if (script == ScriptType.CmnEF)
                    {
                        path = Path.Combine(mods_path, "CMNEF.bbscript");
                    }
                }
                catch
                {
                    return result;
                }
                ReportMessage($"SCRIPT PATH: {path}");
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    var allocated = ms.Memory.Allocate(bytes.Length);
                    allocated.Write<byte>(bytes);

                    result = (allocated.BaseAddress, bytes.Length);
                }

                return result;
            }
        }
}
