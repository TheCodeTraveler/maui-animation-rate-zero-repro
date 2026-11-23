#!/usr/bin/env bash
# Runs each cell on the booted emulator with the same APK. A crash in one cell must not stop the other, so there is no `set -e`.
set -u -o pipefail

package=com.example.c04repro
artifacts="artifacts/android"
mkdir -p "$artifacts"

adb logcat -G 16M

apk=$(ls app/C04Repro/bin/Release/*/android-x64/*-Signed.apk | head -1)
timeout 300 adb install -r "$apk" > "$artifacts/install.txt" 2>&1 || { sleep 10; timeout 300 adb install -r "$apk" > "$artifacts/install.txt" 2>&1; } || { echo "Install failed"; cat "$artifacts/install.txt"; exit 1; }

for cell in "bug 0" "control 1"; do
    set -- $cell
    name="$1"; rate="$2"
    folder="$artifacts/$name"
    mkdir -p "$folder"
    adb logcat -c
    adb logcat -b crash -c
    timeout 60 adb shell am start -W -n "$package/com.example.c04repro.MainActivity" --ei rate "$rate" > "$folder/am-start.txt" 2>&1

    # Wait until the app logged STARTED, then give the animation 10 s: the bug cell must crash within its first frames
    waited=0
    while [ "$waited" -lt 60 ]; do
        adb logcat -d -s C04:I | grep -q 'C04 STARTED' && break
        sleep 2
        waited=$((waited + 2))
    done
    sleep 10

    adb logcat -d > "$folder/logcat.txt"
    adb logcat -d -b crash > "$folder/crash.txt"
    adb shell pidof "$package" | tr -d '\r' > "$folder/pidof.txt"
    adb shell am force-stop "$package"
done
