# MPTimer
Simple MP timer status bar for black mages.  Shows the time until your next MP tick, and optionally shows a marker indicating when it is safe to cast Fire III without losing an MP tick.  The Fire III marker will also take into account Ley Lines, if you happen to be standing in it.

### Usage
1) Change jobs to black mage.  The plugin is disabled while on other jobs.
2) Use an action that consumes MP, like Blizzard II.  This allows the plugin to start tracking your MP tick.
3) Use **/mptimer** to show the configuration window, which allows you to adjust the size, position, and visibility conditions for MPTimer.

**NOTE:** Changing zones or respawning after a wipe can reset your character's MP tick timer, which can cause MPTimer to become desynced.  You can cast Blizzard II (or any other spell that consumes MP) to resync your timer.

### To-do
- Make colors configurable
- Make Fire III fast-cast time configurable
  - Better yet, get real value cast time via the Fire III tooltip


https://user-images.githubusercontent.com/52226546/128637619-ea6d3336-c0a3-4cd2-9f97-c047b80436e3.mp4

