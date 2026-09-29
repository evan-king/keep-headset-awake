

# keep-headset-awake

Keeps a wireless headset awake by periodically playing sounds inaudible to the human ear.  

![keep-headset-awake in action](docs/screenshot.png)

## About this Fork

After contentedly using [sanraith's program](https://github.com/sanraith/keep-headset-awake) for
several years, one day my computer decided to blackhole all uses of Console.Beep (even directly
through CLI commands) for no reason, and keep-headset-awake stopped working.  After exhausting all
reasonable troubleshooting attempts, I just edited the program to use waveOut instead.

Between their version and mine, there's no reason to choose one over the other, unless only one
version works for you.  There's absolutely no reason that should be the case, but 🤷.

## Usage

- Ensure that .NET Core 3.1 Runtime is installed.
- Download and unzip the binaries from [the latest release](https://github.com/evan-king/keep-headset-awake/releases/latest).
- Edit `appsettings.json` to customize the beep length, frequency and interval.
- Run `keep-headset-awake.exe`.  
If you want to hide the a console, run `keep-headset-awake-NOCONSOLE.exe` instead.

## Build from source

- Ensure that .NET Core 3.1 SDK is installed.
- Clone the repo with `git clone https://github.com/evan-king/keep-headset-awake.git`
- Run `publish.bat` from the project directory.  
The binaries are placed into `bin\keep-headset-awake\`
