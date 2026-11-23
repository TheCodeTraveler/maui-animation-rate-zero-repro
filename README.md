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

- Microsoft.Maui.Controls 11.0.10, .NET SDK 11.0.100, net11.0, ubuntu-latest.

## Legs

| Leg | What | Expected |
|---|---|---|
| bug | `Animate(rate: 0)`, fire one ticker frame | `DivideByZeroException` from `Tweener.Step` |
| control | same with `rate: 1` | no exception |
| fix | rate 0 clamped to 1 before calling `Animate` | no exception |

- The tests drive `AnimationManager` with a manual `ITicker`, the same code path the platform tickers use, so no device is needed. Platform runs (iOS 27 simulator and Android 36 emulator, animations enabled) crashed the same way when the maintainer's verifier ran them; this repo does not repeat them.
- Each leg fails unless its test passed and its marker line is in `results/markers.txt`. The workflow is green only when all three legs are OK.
- The recommended fix (`Rate = Math.Max(rate, 1u)` in the `Tweener` constructor) is internal, so it cannot be applied from user code. The fix leg shows the equivalent clamp at the call site.

CI: (run link added after the first run)

Upstream issue: (to be filed)

## Root cause and recommended fix

- `src/Controls/src/Core/Tweener.cs:128`: `long wantedFrames = (_lastMilliseconds / Rate) + 1;` with `Rate` a `uint` that is never validated (constructor, `AnimationExtensions.AnimateInternal`).
- Introduced by xamarin/Xamarin.Forms#9856 (2020); before it rate was ignored. Reported and closed as caller error in xamarin/Xamarin.Forms#12806, but `try/catch` around the call cannot help and the docs state no minimum.
- Fix: `Rate = Math.Max(rate, 1u)` in the `Tweener` constructor (non-breaking, both mean "update every tick"), or `ArgumentOutOfRangeException.ThrowIfZero(rate)` in `Animate<T>` to fail fast.
