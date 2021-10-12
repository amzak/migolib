# MigoLib

This is an alternative software for Migo 3D printer.

Since Migo project is pretty much abandoned, stock software is unmaintained and very unreliable, so this is an attempt to fix that. At least, to make printer usable.

MigoLib contains gui and cli tools, these functions have been implemented:
- Gcode-file upload, start/stop print
- Simple chart for temperature visualization
- Preheat bed before printing
- Manual head movement, homing, retract e.t.c
- Manual head/bed heating
- Setting Z offset value
- Manual calibration with paper sheet (experimental)

This is all pretty much alpha! No guaranties whatsoever.
This software can't fix firmware issues and there are some.

No slicer is included. Use your favorite slicer, save .gcode and upload it.

## Important
Printer's wifi **can not** be configured with this software! 

## Known issues

If file upload failed, like due to network issues, it can't be cancelled or resumed. Restart the app. If that won't help - reboot the printer.

## GUI tool

![screenshot](/images/screenshot.png "Screenshot")

## CLI tool

#### Usage:

```text
./MigoToolCli --endpoint {Ip:Port} {command} {subcommand} [options]
```

| | Command | Sub command | Options | Description |
| --- | --- | --- | --- | --- |
| MigoToolCli | | | -help / -h / -? | Shows available commands and parameters |
| | get | info | | Returns some current state data
| | | state | | Returns current head coordinates, nozzle and bed temperatures
| | | zoffset | | Returns current z offset value
| | set | zoffset | {value} | Sets z offset. On next run Migo will perform z calibration
| | | temperature | --bed {value} | Sets current bed temperature in celsius
| | | | --nozzle {value} | Sets current nozzle temperature in celsius
| | exec | gcode | {gcode commands} | Sends gcode to the printer to be executed. May contain multiple lines, separated with semicolon. Be aware, Migo does not respond back, only "ok" upon completion
| | | upload | {path to .gcode file} | Uploads file to Migo. Without validation.
| | | print | {name of uploaded file} | Starts print. File name without path.
| | | stop | | Stops current print
| | | bedcalibration | {FivePoints/NinePoints} | Experimental. Starts manual bed level calibration, moves the nozzle through a set of points, lowering it to zoffset for "paper-sheet" measurements
