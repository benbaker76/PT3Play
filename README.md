# PT3Play

This is a Pro Tracker 3.x format chip tune music player ported to C#. It plays [AY-3-8910](https://en.wikipedia.org/wiki/General_Instrument_AY-3-8910) chip tune music found on the ZX Spectrum.

## Demo

This demo runs on Windows only as it uses the SlimDX (DirectX 9) library for the spectrum analyzer rendering.

## Installation

You need the following libraries installed for SlimDX to work:

- [Microsoft Visual C++ 2010 Service Pack 1 Redistributable Package MFC Security Update](https://www.microsoft.com/en-us/download/details.aspx?id=26999)
- [DirectX End-User Runtimes (June 2010)](https://www.microsoft.com/en-ie/download/details.aspx?id=8109)

## Controls

- **Up/Down**: Change song
- **Left/Right**: Change sound effect
- **Space**: Play currently selected sound effect
- **ESC or Window Close Button**: Exit

## Screenshot

![](/images/pt3play.png)

## License

The source code is released under the MIT license.

## Credits

Special thanks to the following individuals and projects for their contributions:

- The original PT3Play and AY Emulator code was written by [Sergey Bulba](mailto:svbulba@gmail.com) ([link](https://bulba.untergrund.net/vortex_e.htm)) and contains modified code from [ayfly](https://github.com/l29ah/ayfly).
- The AY FX player code is based on [AYFX Editor v0.6](https://shiru.untergrund.net/software.shtml) by [Shiru](mailto:shiru@mail.ru).
- The spectrum analyzer code is from the [ESPboy_PT3Play](https://github.com/ESPboy-edu/ESPboy_PT3Play) project by Shiru
- The demo music is by Shiru ([link](https://shiru.untergrund.net/software.shtml)).
- Also thanks to authors of the sound effects that are included in the library.
