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
using System.Linq;
using System.Reflection;
using KPreisser.UI;
using Microsoft.Win32;

namespace USBFunctionModeSwitcher
{
    class Program
    {
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
                    Title = "USB Function Mode Switcher",
                    Instruction = "Unsupported device",
                    Text = reason,

                    Footer =
                    {
                        Text = "Please verify that you run a supported device with the latest drivers available for it.",
                        Icon = TaskDialogStandardIcon.Information,
                    },

                    CustomButtonStyle = TaskDialogCustomButtonStyle.CommandLinks,
                    AllowCancel = true,
                    SizeToContent = true,

                    Icon = TaskDialogStandardIcon.Error
                };

                TaskDialogCustomButton closemainbutton = dialogPage.CustomButtons.Add("Close");
                TaskDialogCustomButton aboutbutton = dialogPage.CustomButtons.Add("About", "About USB Function Mode Switcher");

                var dialog = new TaskDialog(dialogPage);

                aboutbutton.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
                {
                    args.CancelClose = true;
                    ShowAboutDialog(dialog);
                };

                TaskDialogButton result = dialog.Show();
            }
        }

        private static void ShowTaskDialog()
        {
            var dialogPage = new TaskDialogPage()
            {
                Title = "USB Function Mode Switcher",
                Instruction = "Select a USB function mode to switch to",
                Text = "Below are the available modes your phone supports switching to.",

                Footer =
                {
                    Text = "Switching modes will require a reboot of the device.",
                    Icon = TaskDialogStandardIcon.Warning,
                },

                CustomButtonStyle = TaskDialogCustomButtonStyle.CommandLinks,
                AllowCancel = true,
                SizeToContent = true
            };

            try
            {
                var dialog = new TaskDialog(dialogPage);

                var handler = new USBRoleHandler();

                foreach (var role in handler.USBRoles)
                {
                    TaskDialogCustomButton rolebutton = dialogPage.CustomButtons.Add(role.DisplayName, role.Description);
                    if (role == handler.CurrentUSBRole)
                    {
                        rolebutton.Enabled = false;
                    }

                    rolebutton.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
                    {
                        if (role.IsHost && role.HostRole.EnableVbus)
                        {
                            args.CancelClose = true;
                            ShowDisclaimerDialog(dialog, () =>
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
                    TaskDialogCustomButton polaritybutton = dialogPage.CustomButtons.Add("Polarity", "Change the polarity of the USB C port");
                    polaritybutton.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
                    {
                        args.CancelClose = true;
                        ShowPolarityDialog(dialog);
                    };
                }

                TaskDialogCustomButton aboutbutton = dialogPage.CustomButtons.Add("About", "About USB Function Mode Switcher");
                aboutbutton.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
                {
                    args.CancelClose = true;
                    ShowAboutDialog(dialog);
                };

                TaskDialogButton result = dialog.Show();
            }
            catch (Exception ex)
            {
                dialogPage = new TaskDialogPage()
                {
                    Title = "USB Function Mode Switcher",
                    Instruction = "Something happened",
                    Text = ex.ToString(),
                    CustomButtonStyle = TaskDialogCustomButtonStyle.CommandLinks,
                    AllowCancel = true,
                    SizeToContent = true,
                    Icon = TaskDialogStandardIcon.Error
                };

                TaskDialogCustomButton closemainbutton = dialogPage.CustomButtons.Add("Close");
                TaskDialogCustomButton aboutbutton = dialogPage.CustomButtons.Add("About", "About USB Function Mode Switcher");

                var dialog = new TaskDialog(dialogPage);

                aboutbutton.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
                {
                    args.CancelClose = true;
                    ShowAboutDialog(dialog);
                };

                TaskDialogButton result = dialog.Show();
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

        private static void ShowDisclaimerDialog(TaskDialog dialog, Action action)
        {
            var newPage = new TaskDialogPage()
            {
                Title = "USB Function Mode Switcher",
                Text = "Switching to this mode will enable power output from the USB Type C port. This may harm your device if you plug in a charging cable or a continuum dock. In this mode NEVER plug in any charging cable, wall charger, PC USB Cable (connected to a PC) or any externally powered USB hub! We cannot be taken responsible for any damage caused by this, you have been warned!",
                Instruction = "Do you really want to do this?",
                Icon = TaskDialogStandardIcon.Warning,
                CustomButtonStyle = TaskDialogCustomButtonStyle.CommandLinks,
                AllowCancel = true,
                SizeToContent = true
            };

            TaskDialogCustomButton nobutton = newPage.CustomButtons.Add("No");
            TaskDialogCustomButton yesbutton = newPage.CustomButtons.Add("Yes I understand all the risks");

            yesbutton.Click += (object sender2, TaskDialogButtonClickedEventArgs args2) =>
            {
                action();
            };

            var origpage = dialog.Page;

            nobutton.Click += (object sender2, TaskDialogButtonClickedEventArgs args2) =>
            {
                args2.CancelClose = true;
                dialog.Page = origpage;
            };

            dialog.Page = newPage;
        }

        private static void ShowPolarityDialog(TaskDialog dialog)
        {
            var newPage = new TaskDialogPage()
            {
                Title = "USB Function Mode Switcher",
                Text = "You can change the polarity of the USB C port. This effectively allows you to use the cable in another direction.",
                Instruction = "Polarity",
                CustomButtonStyle = TaskDialogCustomButtonStyle.CommandLinks,
                AllowCancel = true,
                SizeToContent = true
            };

            TaskDialogCustomButton PolarityFirst = newPage.CustomButtons.Add("Polarity 1");
            TaskDialogCustomButton PolaritySecond = newPage.CustomButtons.Add("Polarity 2");
            TaskDialogCustomButton closebutton = newPage.CustomButtons.Add("Close");

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

            PolarityFirst.Click += (object sender2, TaskDialogButtonClickedEventArgs args2) =>
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                {
                    key.SetValue("Polarity", 0, RegistryValueKind.DWord);
                }
                RebootDevice();
            };

            PolaritySecond.Click += (object sender2, TaskDialogButtonClickedEventArgs args2) =>
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\usbc", true))
                {
                    key.SetValue("Polarity", 1, RegistryValueKind.DWord);
                }
                            RebootDevice();
            };

            var origpage = dialog.Page;

            closebutton.Click += (object sender2, TaskDialogButtonClickedEventArgs args2) =>
            {
                args2.CancelClose = true;
                dialog.Page = origpage;
            };

            dialog.Page = newPage;
        }

        private static void ShowAboutDialog(TaskDialog dialog)
        {
            var newPage = new TaskDialogPage()
            {
                Title = "USB Function Mode Switcher",
                Text = "Version " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion +
                        "\n" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).LegalCopyright +
                        "\nReleased under the MIT License" +
                        "\n\nLibraries used for this application:" +
                        "\n\n<A HREF=\"https://github.com/kpreisser/TaskDialog\">TaskDialog</A>" +
                        "\nCopyright (c) 2018 Konstantin Preißer, www.preisser-it.de (MIT)",
                Instruction = "About",
                Icon = TaskDialogStandardIcon.Information,
                CustomButtonStyle = TaskDialogCustomButtonStyle.CommandLinks,
                AllowCancel = true,
                EnableHyperlinks = true,
                SizeToContent = true
            };

            TaskDialogCustomButton srcbutton = newPage.CustomButtons.Add("Source Code");
            TaskDialogCustomButton closebutton = newPage.CustomButtons.Add("Close");

            srcbutton.Click += (object sender2, TaskDialogButtonClickedEventArgs args2) =>
            {
                args2.CancelClose = true;
                Process.Start("https://github.com/WOA-Project/USBFunctionModeSwitcher");
            };

            var origpage = dialog.Page;

            closebutton.Click += (object sender2, TaskDialogButtonClickedEventArgs args2) =>
            {
                args2.CancelClose = true;
                dialog.Page = origpage;
            };

            dialog.Page = newPage;
        }
    }
}

