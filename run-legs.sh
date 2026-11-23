#!/usr/bin/env bash
# Runs each leg on its own and checks the exit code AND the marker line, so a leg that did not run cannot pass.
set -u
export RESULTS_DIR="$PWD/results"
rm -rf results
mkdir -p results
fail=0
{ echo "| Leg | Expected | Observed | Result |"; echo "|---|---|---|---|"; } > results/summary.md

run_leg() {
  local name="$1" filter="$2" marker="$3" expected="$4"
  dotnet test tests --no-build --filter "FullyQualifiedName~$filter" --logger "console;verbosity=detailed" > "results/$name.log" 2>&1
  local rc=$?
  local observed result
  if [ $rc -eq 0 ] && grep -qF "$marker" results/markers.txt; then
    observed="test passed, marker '$marker' present"; result="OK"
  else
    observed="exit code $rc, marker present: $(grep -cF "$marker" results/markers.txt)"; result="FAIL"; fail=1
  fi
  echo "| $name | $expected | $observed | $result |" >> results/summary.md
}

run_leg bug     Bug_RateZero     "MARKER BUG rate=0 threw System.DivideByZeroException" "rate 0 accepted, then DivideByZeroException from Tweener.Step on the first ticker frame"
run_leg control Control_RateOne  "MARKER CONTROL rate=1 clean"                           "rate 1 runs a frame without any exception"
run_leg fix     Fix_RateZero     "MARKER FIX clamped rate clean"                         "rate 0 clamped to 1 before Animate runs a frame cleanly"

cat results/summary.md
exit $fail
