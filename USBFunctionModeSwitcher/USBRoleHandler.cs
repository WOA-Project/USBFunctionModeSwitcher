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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace USBFunctionModeSwitcher
{
    public class USBRoleHandler
    {
        public USBRoleHandler()
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

            USBRoles = GetListOfUSBRoles();
        }

        public class USBFunctionRole
        {
            public string Name { get; set; }
            public int idProduct { get; set; }
            public int idVendor { get; set; }
            public bool TransportType { get; set; }
            public bool UseDefaultConfig { get; set; }
        }

        public class USBHostRole
        {
            public bool EnableVbus { get; set; }
        }

        public class USBRole
        {
            public string Description { get; set; }
            public string DisplayName { get; set; }
            public bool IsHost { get; set; }
            public USBFunctionRole FunctionRole { get; set; }
            public USBHostRole HostRole { get; set; }
        }

        public USBRole[] USBRoles;

        public USBRole CurrentUSBRole { get => GetUSBRole(); set => SetUSBRole(value); }

        private static bool IsUSBC()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc");

            if (key != null)
            {
                key.Close();
                return true;
            }

            return false;
        }

        private static void SetUSBRole(USBRole role)
        {
            if (IsUSBC())
            {
                if (role.IsHost)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USB", true))
                    {
                        key.SetValue("OSDefaultRoleSwitchMode", 6, RegistryValueKind.DWord);
                    }

                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                    {
                        if (role.HostRole.EnableVbus)
                        {
                            key.SetValue("VBusEnable", 1, RegistryValueKind.DWord);
                        }
                        else
                        {
                            key.SetValue("VBusEnable", 0, RegistryValueKind.DWord);
                        }
                    }
                }
                else
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USB", true))
                    {
                        key.SetValue("OSDefaultRoleSwitchMode", 2, RegistryValueKind.DWord);
                    }
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                    {
                        key.SetValue("VBusEnable", 0, RegistryValueKind.DWord);
                    }
                }
            }

            if (!role.IsHost)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN", true))
                {
                    if (role.FunctionRole.UseDefaultConfig)
                    {
                        key.SetValue("IncludeDefaultCfg", 1, RegistryValueKind.DWord);
                    }
                    else
                    {
                        key.SetValue("IncludeDefaultCfg", 0, RegistryValueKind.DWord);
                    }

                    key.SetValue("idProduct", role.FunctionRole.idProduct, RegistryValueKind.DWord);
                    key.SetValue("idVendor", role.FunctionRole.idVendor, RegistryValueKind.DWord);
                    key.SetValue("CurrentConfiguration", role.FunctionRole.Name, RegistryValueKind.String);
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\QCDIAGROUTER", true))
                {
                    if (role.FunctionRole.TransportType)
                    {
                        key.SetValue("TransportType", 1, RegistryValueKind.DWord);
                    }
                    else
                    {
                        key.SetValue("TransportType", 0, RegistryValueKind.DWord);
                    }
                }
            }
        }

        private USBRole GetUSBRole()
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

                    if (USBRoles.Any(x => !x.IsHost && x.FunctionRole.Name == CurrentConfiguration))
                    {
                        USBRole currole = USBRoles.First(x => !x.IsHost && x.FunctionRole.Name == CurrentConfiguration);
                        if (currole.FunctionRole.idProduct == idProduct && currole.FunctionRole.idVendor == idVendor && currole.FunctionRole.UseDefaultConfig == IncludeDefaultCfg && currole.FunctionRole.TransportType == TransportType)
                        {
                            return currole;
                        }
                    }
                }
            }
            else
            {
                bool vbus = false;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc"))
                {
                    if (key != null && key.GetValueNames().Any(x => x.ToLower() == "vbusenable"))
                        vbus = (int)key.GetValue("VBusEnable") == 1;
                }

                if (vbus)
                    return USBRoles.First(x => x.IsHost && x.HostRole.EnableVbus);
                return USBRoles.First(x => x.IsHost && !x.HostRole.EnableVbus);
            }

            return null;
        }

        private static USBRole[] GetListOfUSBRoles()
        {
            List<USBRole> usbroles = new List<USBRole>();

            if (IsUSBC())
            {
                USBRole hostunpowered = new USBRole() {
                    DisplayName = "Host mode (Power output disabled)",
                    Description = "Default mode of the device. Enables connecting USB devices to the phone using the Continuum dock or any powered USB docking station or hub.",
                    IsHost = true,
                    HostRole = new USBHostRole() { EnableVbus = false }
                };

                USBRole hostpowered = new USBRole()
                {
                    DisplayName = "Host mode (Power output enabled) (Unsafe, read before enabling)",
                    Description = "Enables connecting USB devices to the phone using a standard USB cable, non powered USB docking station, or any non powered hub.\n" +
                    "\nImportant: Do not plug a cable transmiting power into the device when running in this mode. This includes a charging cable, PC USB port, wall charger or Continuum dock. Doing so will harm your device!",
                    IsHost = true,
                    HostRole = new USBHostRole() { EnableVbus = true }
                };

                usbroles.Add(hostunpowered);
                usbroles.Add(hostpowered);
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\USBFN\Configurations"))
            {
                var subkeys = key.GetSubKeyNames();
                foreach (var subkey in subkeys)
                {
                    using (RegistryKey rkey = key.OpenSubKey(subkey))
                    {
                        string[] interfaceList = (string[])rkey.GetValue("InterfaceList");

                        string interfaceListString = string.Join(", ", interfaceList);
                        if (interfaceList.Length > 1)
                        {
                            interfaceListString = string.Join(", ", interfaceList.Reverse().Skip(1).Reverse()) + " and " + interfaceList[interfaceList.Length - 1];
                        }

                        string description = "Enables " + interfaceListString + " connections from another computer.";
                        switch (subkey.ToLower())
                        {
                            case "default":
                                {
                                    description += " Windows Phone Default USB Mode.";
                                    break;
                                }
                            case "retailconfig":
                                {
                                    description += " Windows Phone Normal USB Mode.";
                                    break;
                                }
                            case "dplcompositeconfig":
                                {
                                    description += " Qualcomm Data Protocol Logging Mode.";
                                    break;
                                }
                            case "rmnetcompositeconfig":
                                {
                                    description += " Qualcomm Wireless Diagnostics Mode.";
                                    break;
                                }
                            case "serialcompositeconfig":
                                {
                                    description += " Qualcomm Serial Diagnostics Mode.";
                                    break;
                                }
                            case "vidstream":
                                {
                                    description += " Windows Phone Video Stream USB Mode.";
                                    break;
                                }
                            default:
                                {
                                    description += " Custom.";
                                    break;
                                }
                        }

                        USBFunctionRole functionRole = new USBFunctionRole() { Name = subkey };

                        switch (subkey.ToLower())
                        {
                            case "retailconfig":
                                {
                                    functionRole.UseDefaultConfig = true;
                                    break;
                                }
                            default:
                                {
                                    functionRole.UseDefaultConfig = false;
                                    break;
                                }
                        }

                        switch (subkey.ToLower())
                        {
                            case "default":
                            case "retailconfig":
                            case "vidstream":
                                {
                                    functionRole.TransportType = false;
                                    break;
                                }
                            default:
                                {
                                    functionRole.TransportType = true;
                                    break;
                                }
                        }

                        switch (subkey.ToLower())
                        {
                            case "default":
                                {
                                    functionRole.idProduct = 0xf0ca;
                                    functionRole.idVendor = 0x45e;
                                    break;
                                }
                            case "retailconfig":
                                {
                                    functionRole.idProduct = 0xa00;
                                    functionRole.idVendor = 0x45e;
                                    break;
                                }
                            case "dplcompositeconfig":
                                {
                                    functionRole.idProduct = 0x90b7;
                                    functionRole.idVendor = 0x5c6;
                                    break;
                                }
                            case "rmnetcompositeconfig":
                                {
                                    functionRole.idProduct = 0x9001;
                                    functionRole.idVendor = 0x5c6;
                                    break;
                                }
                            case "serialcompositeconfig":
                                {
                                    functionRole.idProduct = 0x319b;
                                    functionRole.idVendor = 0x5c6;
                                    break;
                                }
                            case "vidstream":
                                {
                                    functionRole.idProduct = 0xf0ca;
                                    functionRole.idVendor = 0x45e;
                                    break;
                                }
                            default:
                                {
                                    functionRole.idProduct = 0xf0ca;
                                    functionRole.idVendor = 0x45e;
                                    break;
                                }
                        }

                        USBRole usbrole = new USBRole() { DisplayName = "Function mode (" + subkey.Replace("Config", "").Replace("Composite", "") + ")", Description = description, IsHost = false, FunctionRole = functionRole };
                        usbroles.Add(usbrole);
                    }
                }
            }
            return usbroles.ToArray();
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
