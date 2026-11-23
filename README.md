# maui-animation-rate-zero-repro

`Animation.Commit` / `AnimationExtensions.Animate` accept `rate: 0` without complaint. The first platform ticker frame then divides by it in `Tweener.Step` (`_lastMilliseconds / Rate`) and throws `DivideByZeroException` on the ticker callback, which terminates the app.

## Run locally

```
./run-legs.sh      # needs the .NET SDK from global.json (11.0.100)
```

## Observed vs expected

- Observed: `Animate(..., rate: 0)` returns normally, then the first frame throws `System.DivideByZeroException` at `Microsoft.Maui.Controls.Tweener.Step`.
- Expected: `rate: 0` is rejected at the call site (`ArgumentOutOfRangeException`) or treated as 1.

The exception cannot be caught around `Commit`: it is thrown later from the platform ticker (CADisplayLink, ValueAnimator, CompositionTarget.Rendering), so the process dies. The crash is hidden when system animations are off (iOS Reduce Motion, Android animator scale 0, Windows animations off), because `Ticker.SystemEnabled` is false then.

## Versions

- Microsoft.Maui.Controls 11.0.10, .NET SDK 11.0.100, net11.0 (managed legs) and net11.0-android (emulator job), ubuntu-24.04 runners, API 36 x86_64 emulator.
- Actions pinned to the same SHAs as dotnet/maui's device-tests.yml: checkout v7.0.1, setup-dotnet v6.1.0, upload-artifact v7.

## Legs

| Leg | What | Expected |
|---|---|---|
| bug | `Animate(rate: 0)`, fire one ticker frame | `DivideByZeroException` from `Tweener.Step` |
| control | same with `rate: 1` | no exception |
| fix | rate 0 clamped to 1 before calling `Animate` | no exception |

- The tests drive `AnimationManager` with a manual `ITicker`, the same code path the platform tickers use, so no device is needed.
- The `android` job runs the real crash: one APK (`app/C04Repro`) commits `new Animation(...).Commit(box, "Rate", rate: N, length: 1000)` inside `try/catch` on launch and gets N from `am start --ei rate N`. Rate 0: logcat shows `FATAL EXCEPTION`, `DivideByZeroException` and `Tweener.Step`, never `CAUGHT` or `SURVIVED`, and the process is gone. Rate 1: `SURVIVED` is logged and the process is alive. A cell fails when `STARTED` is missing. Logcat is uploaded as the `results-android` artifact.
- The managed `bug` leg writes the exception to `results/bug-exception.txt` and the job summary shows it.
- Each leg fails unless its test passed and its marker line is in `results/markers.txt`. The workflow is green only when all three legs are OK.
- The recommended fix (`Rate = Math.Max(rate, 1u)` in the `Tweener` constructor) is internal, so it cannot be applied from user code. The fix leg shows the equivalent clamp at the call site.

CI: (run link added after the first run)

Upstream issue: (to be filed)

## Root cause and recommended fix

- `src/Controls/src/Core/Tweener.cs:128`: `long wantedFrames = (_lastMilliseconds / Rate) + 1;` with `Rate` a `uint` that is never validated (constructor, `AnimationExtensions.AnimateInternal`).
- Introduced by xamarin/Xamarin.Forms#9856 (2020); before it rate was ignored. Reported and closed as caller error in xamarin/Xamarin.Forms#12806, but `try/catch` around the call cannot help and the docs state no minimum.
- Fix: `Rate = Math.Max(rate, 1u)` in the `Tweener` constructor (non-breaking, both mean "update every tick"), or `ArgumentOutOfRangeException.ThrowIfZero(rate)` in `Animate<T>` to fail fast.
