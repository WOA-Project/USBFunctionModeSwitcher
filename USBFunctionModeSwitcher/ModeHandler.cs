/*
MIT License

Copyright (c) 2019 LumiaWOA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace USBFunctionModeSwitcher
{
    public class ModeHandler
    {
        public enum USBModes
        {
            HostNonPowered,
            HostPowered,
            FunctionRetail,
            FunctionSerial,
            FunctionRmNet,
            FunctionDPL,
            Unknown
        }

        public static bool IsUSBC()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc");

            if (key != null)
            {
                key.Close();
                return true;
            }

            return false;
        }

        public void SetCurrentMode(USBModes mode)
        {
            switch (mode)
            {
                case USBModes.HostNonPowered:
                case USBModes.HostPowered:
                case USBModes.FunctionRetail:
                    {
                        using (RegistryKey trkey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\QCDIAGROUTER", true))
                        {
                            trkey.SetValue("TransportType", 0, RegistryValueKind.DWord);
                        }
                        break;
                    }
                default:
                    {
                        using (RegistryKey trkey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\QCDIAGROUTER", true))
                        {
                            trkey.SetValue("TransportType", 1, RegistryValueKind.DWord);
                        }
                        break;
                    }
            }

            bool isUSBC = IsUSBC();

            if (isUSBC)
            {
                switch (mode)
                {
                    case USBModes.HostNonPowered:
                    case USBModes.HostPowered:
                        {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USB", true))
                            {
                                key.SetValue("OSDefaultRoleSwitchMode", 6, RegistryValueKind.DWord);
                            }
                            break;
                        }
                    default:
                        {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USB", true))
                            {
                                key.SetValue("OSDefaultRoleSwitchMode", 2, RegistryValueKind.DWord);
                            }
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                            {
                                key.SetValue("VBusEnable", 0, RegistryValueKind.DWord);
                            }
                            break;
                        }
                }
            }

            switch (mode)
            {
                case USBModes.HostNonPowered:
                    {
                        if (isUSBC)
                        {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                            {
                                if (key != null)
                                    key.SetValue("VBusEnable", 0, RegistryValueKind.DWord);
                            }
                        }
                        break;
                    }
                case USBModes.HostPowered:
                    {
                        if (isUSBC)
                        {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                            {
                                if (key != null)
                                    key.SetValue("VBusEnable", 1, RegistryValueKind.DWord);
                            }
                        }
                        break;
                    }
                case USBModes.FunctionDPL:
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN", true))
                        {
                            key.SetValue("IncludeDefaultCfg", 0, RegistryValueKind.DWord);
                            key.SetValue("idProduct", 0x90b7, RegistryValueKind.DWord);
                            key.SetValue("idVendor", 0x5c6, RegistryValueKind.DWord);
                            key.SetValue("CurrentConfiguration", "DplCompositeConfig", RegistryValueKind.String);
                        }
                        break;
                    }
                case USBModes.FunctionRetail:
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN", true))
                        {
                            key.SetValue("IncludeDefaultCfg", 1, RegistryValueKind.DWord);
                            key.SetValue("idProduct", 0xa00, RegistryValueKind.DWord);
                            key.SetValue("idVendor", 0x45e, RegistryValueKind.DWord);
                            key.SetValue("CurrentConfiguration", "RetailConfig", RegistryValueKind.String);
                        }
                        break;
                    }
                case USBModes.FunctionRmNet:
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN", true))
                        {
                            key.SetValue("IncludeDefaultCfg", 0, RegistryValueKind.DWord);
                            key.SetValue("idProduct", 0x9001, RegistryValueKind.DWord);
                            key.SetValue("idVendor", 0x5c6, RegistryValueKind.DWord);
                            key.SetValue("CurrentConfiguration", "RmNetCompositeConfig", RegistryValueKind.String);
                        }
                        break;
                    }
                case USBModes.FunctionSerial:
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN", true))
                        {
                            key.SetValue("IncludeDefaultCfg", 0, RegistryValueKind.DWord);
                            key.SetValue("idProduct", 0x319b, RegistryValueKind.DWord);
                            key.SetValue("idVendor", 0x5c6, RegistryValueKind.DWord);
                            key.SetValue("CurrentConfiguration", "SerialCompositeConfig", RegistryValueKind.String);
                        }
                        break;
                    }
            }
        }

        public USBModes CheckCurrentMode()
        {
            bool isUSBC = IsUSBC();

            bool TransportType = false;
            using (RegistryKey trkey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\QCDIAGROUTER"))
            {
                TransportType = (int)trkey.GetValue("TransportType") == 1;
            }

            bool IsFunction = false;
            if (isUSBC)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USB"))
                {
                    IsFunction = (int)key.GetValue("OSDefaultRoleSwitchMode") == 2;
                }
            }
            else
            {
                IsFunction = true;
            }

            if (IsFunction)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN"))
                {
                    bool IncludeDefaultCfg = (int)key.GetValue("IncludeDefaultCfg") == 1;
                    int idProduct = (int)key.GetValue("idProduct");
                    int idVendor = (int)key.GetValue("idVendor");
                    string CurrentConfiguration = (string)key.GetValue("CurrentConfiguration");

                    switch (CurrentConfiguration)
                    {
                        case "RetailConfig":
                            {
                                if (IncludeDefaultCfg && idProduct == 0xa00 && idVendor == 0x45e && !TransportType)
                                {
                                    return USBModes.FunctionRetail;
                                }
                                break;
                            }
                        case "DplCompositeConfig":
                            {
                                if (!IncludeDefaultCfg && idProduct == 0x90b7 && idVendor == 0x5c6 && TransportType)
                                {
                                    return USBModes.FunctionDPL;
                                }
                                break;
                            }
                        case "RmNetCompositeConfig":
                            {
                                if (!IncludeDefaultCfg && idProduct == 0x9001 && idVendor == 0x5c6 && TransportType)
                                {
                                    return USBModes.FunctionRmNet;
                                }
                                break;
                            }
                        case "SerialCompositeConfig":
                            {
                                if (!IncludeDefaultCfg && idProduct == 0x319b && idVendor == 0x5c6 && TransportType)
                                {
                                    return USBModes.FunctionSerial;
                                }
                                break;
                            }
                    }
                }
            }
            else
            {
                if (!TransportType)
                {
                    bool vbus = false;
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc"))
                    {
                        if (key != null && key.GetValueNames().Any(x => x.ToLower() == "vbusenable"))
                            vbus = (int)key.GetValue("VBusEnable") == 1;
                    }

                    if (vbus)
                        return USBModes.HostPowered;
                    return USBModes.HostNonPowered;
                }
            }

            return USBModes.Unknown;
        }

        public ModeHandler()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN\Configurations\RetailConfig");
            if (key == null)
            {
                ImportRegistry();
            }
            else
            {
                key.Close();
            }
        }

        private static void WriteResourceToFile(string resourceName, string fileName)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    resource.CopyTo(file);
                }
            }
        }

        private static void ImportRegistry()
        {
            var path = Path.GetTempFileName();
            WriteResourceToFile("USBFunctionModeSwitcher.USBFN.reg", path);
            Process proc = new Process();

            try
            {
                proc.StartInfo.FileName = "reg.exe";
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;

                string command = "import " + path;
                proc.StartInfo.Arguments = command;
                proc.Start();

                proc.WaitForExit();
            }
            catch (Exception)
            {
                proc.Dispose();
            }

            File.Delete(path);

            string name = "Lumia XXX (RM-XXX)";

            string targetproductfile = Environment.ExpandEnvironmentVariables("%SystemDrive%\\DPP\\MMO\\product.dat");

            if (File.Exists(targetproductfile))
            {
                string type = File.ReadAllLines(targetproductfile).First(x => x.StartsWith("TYPE:")).Split(':').Last();

                switch (type)
                {
                    case "RM-1085":
                        {
                            name = "Lumia 950 XL (" + type + ")";
                            break;
                        }
                    case "RM-1104":
                    case "RM-1105":
                        {
                            name = "Lumia 950 (" + type + ")";
                            break;
                        }
                    case "RM-1116":
                        {
                            name = "Lumia 950 XL Dual SIM (" + type + ")";
                            break;
                        }
                    case "RM-1118":
                        {
                            name = "Lumia 950 Dual SIM (" + type + ")";
                            break;
                        }
                    case "RX-130":
                    case "RX-127":
                        {
                            name = "id330-1 (" + type + ")";
                            break;
                        }
                }
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN", true))
            {
                key.SetValue("ProductString", name, RegistryValueKind.String);
            }
        }
    }
}
