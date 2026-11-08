// MCP for Crestron SIMPL# Pro example - Copyright (c) 2026 Solution AV Automation Pty Ltd.
// Example code under the MIT licence (see LICENSE). The MCP for Crestron library it uses, the
// MCPForCrestron NuGet package, is licensed separately, per processor.
//
// MCP for Crestron in a SIMPL# Pro program.
//
// The SIMPL+ modules are thin wrappers around MCPForCrestron.dll, and everything they call is
// public, so a SIMPL# Pro program uses the same classes directly:
//
//   MCPServerConfig  what the Server Config module does (mode, password, key, owner edits)
//   MCPDigitals      a bank of digital channels (what the Digitals module does)
//   MCPAnalogs       a bank of analog channels
//   MCPSerials       a bank of serial channels
//   MCPManager.Registry  register a device directly with read and write callbacks
//
// Licensing, TLS, presets and schedules all live in the library and behave exactly as they do
// under a SIMPL program. Only one program per processor can run the server (it listens on
// port 50794), so don't load the modules in another slot on the same processor.
//
// This example has no hardware. A small "Study" is simulated in fields below; in a real
// program the write handlers drive your devices and the feedback comes from their state.

using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using MCPForCrestron;

namespace MCPForCrestronExample
{
    public class ControlSystem : CrestronControlSystem
    {
        private const string Room = "Study";

        private MCPServerConfig _config;
        private MCPDigitals _digitals;
        private MCPAnalogs _analogs;
        private MCPSerials _serials;

        // The simulated room. A real program reads these from its devices.
        private bool _displayOn;
        private bool _doNotDisturb;
        private string _source = "Apple TV";
        private readonly ushort[] _levels = new ushort[4];   // 1 ceiling, 2 desk lamp, 3 blinds
        private ushort _temperature = 215;                    // tenths of a degree C
        private CTimer _sensor;

        public ControlSystem() : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;
            }
            catch (Exception e)
            {
                ErrorLog.Error("MCP example constructor: {0}", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                // 1. Server config first, so the server opens in the right mode. Mode 2 is the
                //    secure key over TLS; the key is minted on first start and kept on the processor.
                _config = new MCPServerConfig();
                _config.Apply(2, "");
                _config.SetUserEdits(1);

                // The key is what the client needs. A SIMPL program shows it on the Server Config
                // module's Key$ output; here an admin-only console command prints it.
                CrestronConsole.AddNewConsoleCommand(KeyCommand, "mcpkey",
                    "Show the MCP for Crestron secure key", ConsoleAccessLevelEnum.AccessAdministrator);

                // 2. Banks. Channel ids follow the SIMPL modules: Study_d1, Study_a1, Study_s1.
                //    RegisterAt(index, location, category, label, hasFeedback, hasControl)
                _digitals = new MCPDigitals();
                _digitals.WriteRequest = OnDigitalWrite;
                _digitals.RegisterAt(1, Room, "", "Display on", 1, 1);
                _digitals.RegisterAt(2, Room, "", "Display off", 1, 1);
                _digitals.RegisterAt(3, Room, "", "Do not disturb toggle", 1, 1);

                _analogs = new MCPAnalogs();
                _analogs.WriteRequest = OnAnalogWrite;
                _analogs.RegisterAt(1, Room, "", "Ceiling lights, use 3sec ramp", 1, 1);
                _analogs.RegisterAt(2, Room, "", "Desk lamp", 1, 1);
                _analogs.RegisterAt(3, Room, "", "Blinds, 0 closed, 65535 open", 1, 1);

                _serials = new MCPSerials();
                _serials.WriteRequest = OnSerialWrite;
                _serials.RegisterAt(1, Room, "", "Display source, accepts Apple TV or Laptop", 1, 1);

                // 3. Or register a device directly with callbacks, with no bank. A read-only
                //    temperature sensor here; the callback is read whenever the assistant asks.
                MCPManager.EnsureStarted();
                MCPManager.Registry.RegisterDevice(new AnalogDevice(
                    "Study_temperature", "Room temperature", Room, "Climate",
                    () => _temperature, null, "tenths of a degree C",
                    "Room temperature in tenths of a degree Celsius, read only"));

                PushAllFeedback();

                // The sensor drifts a little so there is something live to read.
                _sensor = new CTimer(_ => { _temperature = (ushort)(205 + (DateTime.Now.Minute % 20)); }, null, 60000, 60000);

                CrestronConsole.PrintLine("MCP example: Study registered (3 digital, 3 analog, 1 serial, 1 sensor)");
            }
            catch (Exception e)
            {
                ErrorLog.Error("MCP example InitializeSystem: {0}", e.Message);
            }
        }

        private void KeyCommand(string args)
        {
            CrestronConsole.ConsoleCommandResponse("MCP secure key: " + _config.GetKey());
        }

        // Digital channels are momentary presses: the library sends 1, then 0 about 300 ms
        // later. Act on the press and report the resulting state as feedback.
        private void OnDigitalWrite(ushort index, ushort value)
        {
            if (value == 0)
                return;
            switch (index)
            {
                case 1: _displayOn = true; break;
                case 2: _displayOn = false; break;
                case 3: _doNotDisturb = !_doNotDisturb; break;
            }
            PushDigitalFeedback();
        }

        // Analog writes arrive as levels, including each step of a fade the library runs.
        private void OnAnalogWrite(ushort index, ushort value)
        {
            if (index < 1 || index > 3)
                return;
            _levels[index] = value;
            _analogs.UpdateFeedbackAt(index, value);
        }

        private void OnSerialWrite(ushort index, SimplSharpString value)
        {
            if (index != 1)
                return;
            string v = value.ToString().Trim();
            if (string.Equals(v, "Apple TV", StringComparison.OrdinalIgnoreCase)) _source = "Apple TV";
            else if (string.Equals(v, "Laptop", StringComparison.OrdinalIgnoreCase)) _source = "Laptop";
            _serials.UpdateFeedbackAt(1, _source);   // an unknown source leaves it unchanged
        }

        private void PushDigitalFeedback()
        {
            _digitals.UpdateFeedbackAt(1, (ushort)(_displayOn ? 1 : 0));
            _digitals.UpdateFeedbackAt(2, (ushort)(_displayOn ? 0 : 1));
            _digitals.UpdateFeedbackAt(3, (ushort)(_doNotDisturb ? 1 : 0));
        }

        private void PushAllFeedback()
        {
            PushDigitalFeedback();
            for (ushort i = 1; i <= 3; i++)
                _analogs.UpdateFeedbackAt(i, _levels[i]);
            _serials.UpdateFeedbackAt(1, _source);
        }
    }
}
