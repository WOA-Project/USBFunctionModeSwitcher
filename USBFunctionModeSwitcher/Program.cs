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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USBFunctionModeSwitcher
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            (bool supported, string reason) = IsSupported();

            if (supported)
            {
                ShowTaskDialog();
            }
            else
            {
                var dialogPage = new TaskDialogPage()
                {
                    Heading = "USB Function Mode Switcher",
                    Caption = "Unsupported device",
                    Text = reason,

                    Footnote =
                    {
                        Text = "Please verify that you run a supported device with the latest drivers available for it.",
                        Icon = TaskDialogIcon.Information,
                    },

                    AllowCancel = true,
                    SizeToContent = true,

                    Icon = TaskDialogIcon.Error
                };

                var closemainbutton = new TaskDialogCommandLinkButton("Close");
                var aboutbutton = new TaskDialogCommandLinkButton("About", "About USB Function Mode Switcher", allowCloseDialog: false);
                dialogPage.Buttons.Add(closemainbutton);
                dialogPage.Buttons.Add(aboutbutton);

                aboutbutton.Click += (object sender, EventArgs args) =>
                {
                    ShowAboutDialog(dialogPage);
                };

                TaskDialogButton result = TaskDialog.ShowDialog(dialogPage);
            }
        }

        private static void ShowTaskDialog()
        {
            var dialogPage = new TaskDialogPage()
            {
                Heading = "USB Function Mode Switcher",
                Caption = "Select a USB function mode to switch to",
                Text = "Below are the available modes your phone supports switching to.",

                Footnote =
                {
                    Text = "Switching modes will require a reboot of the device.",
                    Icon = TaskDialogIcon.Warning,
                },

                AllowCancel = true,
                SizeToContent = true
            };

            try
            {
                var handler = new USBRoleHandler();

                foreach (var role in handler.USBRoles)
                {
                    var rolebutton = new TaskDialogCommandLinkButton(role.DisplayName, role.Description);
                    if (role.IsHost && role.HostRole.EnableVbus)
                    {
                        rolebutton.AllowCloseDialog = false;
                    }
                    dialogPage.Buttons.Add(rolebutton);

                    if (role == handler.CurrentUSBRole)
                    {
                        rolebutton.Enabled = false;
                    }

                    rolebutton.Click += (object sender, EventArgs args) =>
                    {
                        if (role.IsHost && role.HostRole.EnableVbus)
                        {
                            ShowDisclaimerDialog(dialogPage, () =>
                            {
                                handler.CurrentUSBRole = role;
                                RebootDevice();
                            });
                        }
                        else
                        {
                            handler.CurrentUSBRole = role;
                            RebootDevice();
                        }
                    };
                }

                if (USBRoleHandler.IsUSBCv2())
                {
                    var polaritybutton = new TaskDialogCommandLinkButton("Polarity", "Change the polarity of the USB C port");
                    polaritybutton.AllowCloseDialog = false;
                    dialogPage.Buttons.Add(polaritybutton);
                    polaritybutton.Click += (object sender, EventArgs args) =>
                    {
                        ShowPolarityDialog(dialogPage);
                    };
                }

                var aboutbutton = new TaskDialogCommandLinkButton("About", "About USB Function Mode Switcher", allowCloseDialog: false);
                dialogPage.Buttons.Add(aboutbutton);
                aboutbutton.Click += (object sender, EventArgs args) =>
                {
                    ShowAboutDialog(dialogPage);
                };

                TaskDialogButton result = TaskDialog.ShowDialog(dialogPage);
            }
            catch (Exception ex)
            {
                dialogPage = new TaskDialogPage()
                {
                    Heading = "USB Function Mode Switcher",
                    Caption = "Something happened",
                    Text = ex.ToString(),
                    AllowCancel = true,
                    SizeToContent = true,
                    Icon = TaskDialogIcon.Error
                };

                var closemainbutton = new TaskDialogCommandLinkButton("Close");
                var aboutbutton = new TaskDialogCommandLinkButton("About", "About USB Function Mode Switcher", allowCloseDialog: false);
                dialogPage.Buttons.Add(closemainbutton);
                dialogPage.Buttons.Add(aboutbutton);

                aboutbutton.Click += (object sender, EventArgs args) =>
                {
                    ShowAboutDialog(dialogPage);
                };

                TaskDialogButton result = TaskDialog.ShowDialog(dialogPage);
            }
        }

        private static (bool, string) IsSupported()
        {
            RegistryKey ufnkey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ufnserialclass");
            if (ufnkey == null)
                return (false, "Your device is missing the Qualcomm USB Composite device or driver.");
            ufnkey.Close();

            RegistryKey trkey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\QCDIAGROUTER");
            if (trkey == null)
                return (false, "Your device is missing the Qualcomm Diagnostic Router device or driver.");
            trkey.Close();

            return (true, "");
        }

        private static void RebootDevice()
        {
            if (!Debugger.IsAttached)
                Process.Start("shutdown", "/r /t 10 /f");
        }

        private static void ShowDisclaimerDialog(TaskDialogPage origpage, Action action)
        {
            var newPage = new TaskDialogPage()
            {
                Heading = "USB Function Mode Switcher",
                Text = "Switching to this mode will enable power output from the USB Type C port. This may harm your device if you plug in a charging cable or a continuum dock. In this mode NEVER plug in any charging cable, wall charger, PC USB Cable (connected to a PC) or any externally powered USB hub! We cannot be taken responsible for any damage caused by this, you have been warned!",
                Caption = "Do you really want to do this?",
                Icon = TaskDialogIcon.Warning,
                AllowCancel = true,
                SizeToContent = true
            };

            var nobutton = new TaskDialogCommandLinkButton("No", allowCloseDialog: false);
            var yesbutton = new TaskDialogCommandLinkButton("Yes I understand all the risks");
            newPage.Buttons.Add(nobutton);
            newPage.Buttons.Add(yesbutton);

            yesbutton.Click += (object sender2, EventArgs args2) =>
            {
                action();
            };

            nobutton.Click += (object sender2, EventArgs args2) =>
            {
                newPage.Navigate(origpage);
            };

            origpage.Navigate(newPage);
        }

        private static void ShowPolarityDialog(TaskDialogPage origpage)
        {
            var newPage = new TaskDialogPage()
            {
                Heading = "USB Function Mode Switcher",
                Text = "You can change the polarity of the USB C port. This effectively allows you to use the cable in another direction.",
                Caption = "Polarity",
                AllowCancel = true,
                SizeToContent = true
            };

            var PolarityFirst = new TaskDialogCommandLinkButton("Polarity 1");
            var PolaritySecond = new TaskDialogCommandLinkButton("Polarity 2");
            var closebutton = new TaskDialogCommandLinkButton("Close", allowCloseDialog: false);
            newPage.Buttons.Add(PolarityFirst);
            newPage.Buttons.Add(PolaritySecond);
            newPage.Buttons.Add(closebutton);

            int pol = 0;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
            {
                if (key != null && key.GetValueNames().Any(x => x.ToLower() == "polarity"))
                    pol = (int)key.GetValue("Polarity");
            }

            if (pol == 0)
                PolarityFirst.Enabled = false;
            else
                PolaritySecond.Enabled = false;

            PolarityFirst.Click += (object sender2, EventArgs args2) =>
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                {
                    key.SetValue("Polarity", 0, RegistryValueKind.DWord);
                }
                RebootDevice();
            };

            PolaritySecond.Click += (object sender2, EventArgs args2) =>
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                {
                    key.SetValue("Polarity", 1, RegistryValueKind.DWord);
                }
                RebootDevice();
            };

            closebutton.Click += (object sender2, EventArgs args2) =>
            {
                newPage.Navigate(origpage);
            };

            origpage.Navigate(newPage);
        }

        private static void ShowAboutDialog(TaskDialogPage origpage)
        {
            var newPage = new TaskDialogPage()
            {
                Heading = "USB Function Mode Switcher",
                Text = "Version " + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version +
                        "\n" + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright +
                        "\nReleased under the MIT License",
                Caption = "About",
                Icon = TaskDialogIcon.Information,
                AllowCancel = true,
                SizeToContent = true
            };
            
            var srcbutton = new TaskDialogCommandLinkButton("Source Code", allowCloseDialog: false);
            var closebutton = new TaskDialogCommandLinkButton("Close", allowCloseDialog: false);
            newPage.Buttons.Add(srcbutton);
            newPage.Buttons.Add(closebutton);

            srcbutton.Click += (object sender2, EventArgs args2) =>
            {
                Process.Start("https://github.com/WOA-Project/USBFunctionModeSwitcher");
            };

            closebutton.Click += (object sender2, EventArgs args2) =>
            {
                newPage.Navigate(origpage);
            };

            origpage.Navigate(newPage);
        }
    }
}
