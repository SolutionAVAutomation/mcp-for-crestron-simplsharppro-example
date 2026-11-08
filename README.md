# MCP for Crestron: SIMPL# Pro example

A minimal SIMPL# Pro program that lets an AI assistant (Claude, or any client that speaks MCP)
query and control a Crestron 4-Series processor in plain English, using the
[MCPForCrestron](https://www.nuget.org/packages/MCPForCrestron) NuGet package.

It simulates a small "Study" (a display, three lighting and blind levels, a source and a
temperature sensor), so it runs on any 4-Series processor with nothing connected. In a real
program the write handlers drive your devices and the feedback comes from their state.

`MCPForCrestronExample/ControlSystem.cs` shows the three ways in:

- `MCPServerConfig`: the server's security mode (here mode 2, a secure key over TLS)
- `MCPDigitals`, `MCPAnalogs`, `MCPSerials`: banks of channels, the same shape the SIMPL modules use
- `MCPManager.Registry`: a device registered directly with read and write callbacks

## Build

Open `MCPForCrestronExample/MCPForCrestronExample.csproj` in Visual Studio 2019 or later and build
Release. NuGet restores `MCPForCrestron` and Crestron's `Crestron.SimplSharp.SDK.Program`. The build
writes `bin/Release/MCPForCrestronExample.cpz`.

## Load

Copy the `.cpz` to a program folder on the processor (for example `/program02`) over SFTP, then at
the processor console run `progload -p:2`. Toolbox works too.

## Connect an assistant

The example runs in mode 2. An administrator console command prints the key the client needs.
Program console commands answer for slot 1 unless you name the slot, so in slot 2 type
`mcpkey:2`.

Then point a client at the processor. For Claude Code:

```
claude mcp add crestron --env CRESTRON_HOST=<processor-ip> --env CRESTRON_KEY=<secure-key> -- npx -y mcp-for-crestron
```

The Claude Desktop extension and other MCP apps are on the
[product page](https://solutionav.com.au/mcp-for-crestron/). If you write SIMPL# Pro with an AI
coding assistant, connecting it to your test processor this way lets it press buttons, set levels
and read feedback on the program it is writing.

The assistant needs a per-processor licence to control anything. Free one-week trials start from
the chat; the Crestron program runs normally without one.

## Things to know

- One server per processor. It listens on port 50794, so only one program on a processor can use
  MCP for Crestron. Don't load the SIMPL modules in another slot alongside it.
- The licence, the secure key, presets and schedules belong to the processor, not the program slot.
  A licensed processor stays licensed when you move from a SIMPL program to a SIMPL# Pro one, and
  saved presets and schedules come with it. They refer to device ids, so keep the same room and
  channel numbers if you want them to keep working.
- Digital channels are momentary presses. The write handler gets 1 and then 0 about 300 ms later;
  act on the 1 and report the resulting state as feedback.
- Analog fades run in the library and arrive as a series of writes, so the write handler only has
  to set the level and report it back.
- Device ids follow the SIMPL modules: `<Room>_d1`, `<Room>_a1`, `<Room>_s1`, with the category
  added when it isn't "Controls".

More: the [SIMPL# Pro guide](https://solutionav.com.au/mcp-for-crestron/simplsharp-pro.html) and
the modules help inside the free modules download.

## Licence

The example code is MIT licensed (see `LICENSE`). The MCPForCrestron library is proprietary and
licensed separately, per processor.

Crestron is a registered trademark of Crestron Electronics, Inc. MCP for Crestron is a product of
Solution AV Automation, not affiliated with or endorsed by Crestron.
