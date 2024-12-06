Steam Linux Runtime 1.0 (scout)
===============================

This container-based release of the Steam Runtime is enabled on a
per-title basis by forcing its use in the title's Properties dialog,
and is used by default for native Linux games on Steam Deck.

For general information please see
<https://gitlab.steamos.cloud/steamrt/steam-runtime-tools/-/blob/main/docs/container-runtime.md>

Release notes
-------------

Please see
<https://gitlab.steamos.cloud/steamrt/steamrt/-/wikis/Steam-Linux-Runtime-1.0-(scout)-release-notes>

Known issues
------------

Please see
<https://github.com/ValveSoftware/steam-runtime/blob/master/doc/steamlinuxruntime-known-issues.md>

Reporting bugs
--------------

Please see
<https://github.com/ValveSoftware/steam-runtime/blob/master/doc/reporting-steamlinuxruntime-bugs.md>

Development and debugging
-------------------------

See `SteamLinuxRuntime_soldier/README.md` for details of the container
runtime.

This additional layer uses a `LD_LIBRARY_PATH`-based Steam Runtime to
provide the required libraries for the Steam Runtime version 1 ABI.

By default, it will use the version in the Steam installation directory,
`~/.steam/root/ubuntu12_32/steam-runtime` (normally this is the same as
`~/.local/share/Steam/ubuntu12_32/steam-runtime`). You can use a different
version of Steam Runtime 1 'scout' by unpacking a `steam-runtime.tar.xz`
into the `SteamLinuxRuntime/steam-runtime/` directory, so that you have
files like `SteamLinuxRuntime/steam-runtime/run.sh`.

If you have `SteamLinuxRuntime` and `SteamLinuxRuntime_soldier` installed
in the same Steam library, you can use `run-in-scout-on-soldier` to test
commands in the scout-on-soldier environment, for example:

    .../steamapps/common/SteamLinuxRuntime/run-in-scout-on-soldier -- xterm

Please see
<https://gitlab.steamos.cloud/steamrt/steam-runtime-tools/-/blob/main/docs/distro-assumptions.md>
for details of assumptions made about the host operating system, and some
advice on debugging the container runtime on new Linux distributions.

Game developers who are interested in targeting this environment should
check the SDK documentation <https://gitlab.steamos.cloud/steamrt/scout/sdk>
and general information for game developers
<https://gitlab.steamos.cloud/steamrt/steam-runtime-tools/-/blob/main/docs/slr-for-game-developers.md>.

Licensing and copyright
-----------------------

The Steam Runtime contains many third-party software packages under
various open-source licenses.

For full source code, please see the version-numbered subdirectories of
<https://repo.steampowered.com/steamrt-images-scout/snapshots/>
corresponding to the version numbers listed in VERSIONS.txt.
