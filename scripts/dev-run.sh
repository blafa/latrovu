#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

ROOT=$(pwd)
SESSION=latrovu
LOG_DIR=/tmp/latrovu

command -v tmux > /dev/null || { echo "tmux is missing: sudo apt install tmux"; exit 1; }

mkdir -p "$LOG_DIR"
rm -f "$LOG_DIR"/*.log
tmux kill-session -t "$SESSION" 2> /dev/null || true
dotnet build --nologo -v quiet

host_command() {
  local name=$1
  echo "cd '$ROOT' && dotnet run --no-build --project src/Hosts/Latrovu.Hosts.$name 2>&1 | tee '$LOG_DIR/${name,,}.log'; exec bash"
}

web=$(tmux new-session -d -s "$SESSION" -n hosts -P -F '#{pane_id}' "$(host_command Web)")
intake=$(tmux split-window -t "$web" -h -P -F '#{pane_id}' "$(host_command Intake)")
extraction=$(tmux split-window -t "$intake" -v -P -F '#{pane_id}' "$(host_command Extraction)")
scheduler=$(tmux split-window -t "$extraction" -v -P -F '#{pane_id}' "$(host_command Scheduler)")
shell=$(tmux split-window -t "$web" -v -c "$ROOT" -P -F '#{pane_id}')

tmux select-pane -t "$web" -T "Web :5082"
tmux select-pane -t "$intake" -T "Intake :5083"
tmux select-pane -t "$extraction" -T "Extraction"
tmux select-pane -t "$scheduler" -T "Scheduler"
tmux select-pane -t "$shell" -T "Shell"
tmux set -t "$SESSION" mouse on
tmux set -t "$SESSION" pane-border-status top
tmux set -t "$SESSION" pane-border-format " #{pane_title} "
tmux select-pane -t "$shell"
tmux attach -t "$SESSION"
