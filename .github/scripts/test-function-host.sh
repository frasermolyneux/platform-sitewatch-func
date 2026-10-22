#!/usr/bin/env bash
set -euo pipefail

script="${1:?A Function host verification script is required}"
test_dir=$(mktemp -d)
export TEST_LOG="${test_dir}/calls"
output="${test_dir}/output"
trap 'rm -f "$TEST_LOG" "$output"; rmdir "$test_dir"' EXIT
export FUNCTION_APP_NAME=test-function
export FUNCTION_APP_RESOURCE_GROUP=test-resource-group
export FUNCTION_APP_HOSTNAME=test-function.azurewebsites.net

az() {
  case "$*" in
    *"appsettings delete"*)
      echo delete >> "$TEST_LOG"
      return "${TEST_DELETE_EXIT:-0}"
      ;;
    *accountName*) echo "${TEST_ACCOUNT:-teststorage}" ;;
    *credential*) echo "${TEST_CREDENTIAL:-managedidentity}" ;;
    *"length("*) echo "${TEST_LEGACY:-1}" ;;
    *"functionapp show"*) echo "$FUNCTION_APP_HOSTNAME" ;;
    *) echo "Unexpected Azure operation in test." >&2; return 99 ;;
  esac
}
curl() {
  echo probe >> "$TEST_LOG"
  echo "${TEST_HTTP_STATUS:-200}"
}
sleep() {
  SECONDS=$((SECONDS + 600))
}
export -f az curl sleep

run_case() {
  local name="$1" expected="$2" actual=0
  shift 2
  : > "$TEST_LOG"
  env "$@" bash "$script" > "$output" 2>&1 || actual=$?
  if [[ "$actual" != "$expected" ]]; then
    cat "$output"
    echo "Failed ${name}: expected exit ${expected}, received ${actual}." >&2
    exit 1
  fi
}
require_call() {
  if ! grep -qx "$1" "$TEST_LOG"; then
    echo "Expected ${1} call was missing." >&2
    exit 1
  fi
}
forbid_call() {
  if grep -qx "$1" "$TEST_LOG"; then
    echo "Unexpected ${1} call." >&2
    exit 1
  fi
}

run_case legacy-setting 0
require_call delete
require_call probe
run_case already-reconciled 0 TEST_LEGACY=0
forbid_call delete
require_call probe
run_case wrong-credential 1 TEST_CREDENTIAL=sharedkey
forbid_call delete
forbid_call probe
run_case invalid-account 1 TEST_ACCOUNT=invalid-account
forbid_call delete
forbid_call probe
run_case failed-reconciliation 42 TEST_DELETE_EXIT=42
require_call delete
forbid_call probe
run_case unhealthy-host 1 TEST_HTTP_STATUS=503
require_call probe
echo "Function host reconciliation and health tests passed."
