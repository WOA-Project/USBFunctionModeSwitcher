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
using System.Runtime.InteropServices;
using KPreisser.UI;

namespace USBFunctionModeSwitcher
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            ShowTaskDialog();
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

            var dialog = new KPreisser.UI.TaskDialog(dialogPage);

            ModeHandler handler = new ModeHandler();

            // Create custom buttons that are shown as command links.
            if (ModeHandler.IsUSBC())
            {
                TaskDialogCustomButton hostunpoweredbutton = dialogPage.CustomButtons.Add("Host mode (Power output disabled)", "Default mode of the device. Enables connecting USB devices to the phone using the Continuum dock or any powered USB docking station or hub.");
                TaskDialogCustomButton hostpoweredbutton = dialogPage.CustomButtons.Add("Host mode (Power output enabled) (Unsafe, read before enabling)", "Enables connecting USB devices to the phone using a standard USB cable, non powered USB docking station, or any non powered hub.\n\nImportant: Do not plug a cable transmiting power into the device when running in this mode. This includes a charging cable, PC USB port, wall charger or Continuum dock. Doing so will harm your device!");

                switch (handler.CheckCurrentMode())
                {
                    case ModeHandler.USBModes.HostNonPowered:
                        hostunpoweredbutton.Enabled = false;
                        break;

                    case ModeHandler.USBModes.HostPowered:
                        hostpoweredbutton.Enabled = false;
                        break;
                }

                hostunpoweredbutton.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
                {
                    handler.SetCurrentMode(ModeHandler.USBModes.HostNonPowered);
                    System.Diagnostics.Process.Start("shutdown", "/r /t 10 /f");
                };

                hostpoweredbutton.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
                {
                    args.CancelClose = true;

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
                        handler.SetCurrentMode(ModeHandler.USBModes.HostPowered);
                        System.Diagnostics.Process.Start("shutdown", "/r /t 10 /f");
                        dialog.Close();
                    };

                    var innerDialog = new TaskDialog(newPage);
                    TaskDialogButton innerResult = innerDialog.Show();
                };
            }

            TaskDialogCustomButton functionretail      = dialogPage.CustomButtons.Add("Function mode (Retail)", "Enables MTP, NCSd, IPOverUSB and VidStream connections from another computer. Windows Phone Normal USB Mode.");
            TaskDialogCustomButton functionserial      = dialogPage.CustomButtons.Add("Function mode (Serial)", "Enables DIAG, MODEM, NMEA and TRACE connections from another computer. Qualcomm Serial Diagnostics Mode.");
            TaskDialogCustomButton functionrmnet       = dialogPage.CustomButtons.Add("Function mode (RmNet)", "Enables DIAG, NMEA, MODEM and RMNET connections from another computer. Qualcomm Wireless Diagnostics Mode.");
            TaskDialogCustomButton functiondpl         = dialogPage.CustomButtons.Add("Function mode (DPL)", "Enables DIAG, MODEM, RMNET and DPL connections from another computer. Qualcomm Data Protocol Logging Mode.");

            switch (handler.CheckCurrentMode())
            {
                case ModeHandler.USBModes.FunctionDPL:
                    functiondpl.Enabled = false;
                    break;

                case ModeHandler.USBModes.FunctionRetail:
                    functionretail.Enabled = false;
                    break;

                case ModeHandler.USBModes.FunctionRmNet:
                    functionrmnet.Enabled = false;
                    break;

                case ModeHandler.USBModes.FunctionSerial:
                    functionserial.Enabled = false;
                    break;
            }

            functiondpl.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
            {
                handler.SetCurrentMode(ModeHandler.USBModes.FunctionDPL);
                System.Diagnostics.Process.Start("shutdown", "/r /t 10 /f");
            };

            functionretail.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
            {
                handler.SetCurrentMode(ModeHandler.USBModes.FunctionRetail);
                System.Diagnostics.Process.Start("shutdown", "/r /t 10 /f");
            };

            functionrmnet.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
            {
                handler.SetCurrentMode(ModeHandler.USBModes.FunctionRmNet);
                System.Diagnostics.Process.Start("shutdown", "/r /t 10 /f");
            };

            functionserial.Click += (object sender, TaskDialogButtonClickedEventArgs args) =>
            {
                handler.SetCurrentMode(ModeHandler.USBModes.FunctionSerial);
                System.Diagnostics.Process.Start("shutdown", "/r /t 10 /f");
            };

            TaskDialogButton result = dialog.Show();
        }
    }
}

