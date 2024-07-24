------------------------------

DryWetMIDI 7.1.0 by Melanchall

------------------------------

DryWetMIDI is the .NET library to work with MIDI data and MIDI devices. It allows:

- Read, write and create Standard MIDI Files (SMF). It is also possible to read RMID files where SMF wrapped to RIFF chunk. You can easily catch specific error when reading or writing MIDI file since all possible errors in a MIDI file are presented as separate exception classes.
- Send MIDI events to/receive them from MIDI devices, play MIDI data and record it. This APIs support Windows and macOS.
- Finely adjust process of reading and writing. It allows, for example, to read corrupted files and repair them, or build MIDI file validators.
- Implement custom meta events and custom chunks that can be written to and read from MIDI files.
- Manage content of a MIDI file either with low-level objects, like event, or high-level ones, like note (read the High-level data managing section of the library docs).
- Build musical compositions (see Pattern page of the library docs) and use music theory API (see Music Theory - Overview article).
- Perform complex tasks like quantizing, notes splitting or converting MIDI file to CSV representation (see Tools page of the library docs).

------------------------------

Project GitHub: https://github.com/melanchall/drywetmidi
Full changelog: https://github.com/melanchall/drywetmidi/releases/tag/v7.1.0
Project documentation: https://melanchall.github.io/drywetmidi

------------------------------

Please take a look at Demo folder of the package where demo scene placed along with sample script using the package.