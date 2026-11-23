#!/usr/bin/env bash
# Prints a markdown table and exits 1 unless every cell matches. A cell without STARTED fails: the app never ran.
set -u
dir="${1:?usage: assert-cells.sh <cells-dir>}"
failed=0
echo '| Cell | Expected | Observed | Result |'
echo '|---|---|---|---|'

check_cell() {
    local name="$1" expected="$2" log="$dir/$1/logcat.txt" pid_file="$dir/$1/pidof.txt"
    local observed result=PASS
    if ! grep -q 'C04 STARTED' "$log" 2>/dev/null; then
        observed='STARTED missing: the app did not run'; result=FAIL
    elif [ "$name" = bug ]; then
        grep -q 'FATAL EXCEPTION' "$log" || result=FAIL
        grep -q 'DivideByZeroException' "$log" || result=FAIL
        grep -q 'Tweener.Step' "$log" || result=FAIL
        grep -q 'C04 CAUGHT' "$log" && result=FAIL
        grep -q 'C04 SURVIVED' "$log" && result=FAIL
        test -s "$pid_file" && result=FAIL
        observed="FATAL=$(grep -c 'FATAL EXCEPTION' "$log") DivideByZero=$(grep -c 'DivideByZeroException' "$log") Tweener.Step=$(grep -c 'Tweener.Step' "$log") CAUGHT=$(grep -c 'C04 CAUGHT' "$log") SURVIVED=$(grep -c 'C04 SURVIVED' "$log") pid='$(cat "$pid_file" 2>/dev/null)'"
    else
        grep -q 'C04 SURVIVED' "$log" || result=FAIL
        grep -q 'FATAL EXCEPTION' "$log" && result=FAIL
        test -s "$pid_file" || result=FAIL
        observed="SURVIVED=$(grep -c 'C04 SURVIVED' "$log") FATAL=$(grep -c 'FATAL EXCEPTION' "$log") pid='$(cat "$pid_file" 2>/dev/null)'"
    fi
    [ "$result" = PASS ] || failed=1
    echo "| $name | $expected | $observed | $result |"
}

check_cell bug 'rate 0: FATAL EXCEPTION, DivideByZeroException in Tweener.Step, no CAUGHT, no SURVIVED, process gone'
check_cell control 'rate 1: SURVIVED logged, process alive'
exit $failed
