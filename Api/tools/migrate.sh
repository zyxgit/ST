#!/usr/bin/env bash
# ST 微服务迁移管理工具 — 一键检查/新增/移除迁移
set -euo pipefail

# ── 初始化 ──────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CONFIG="$SCRIPT_DIR/migrations.json"

if [ ! -f "$CONFIG" ]; then
  echo "✗ 配置文件不存在: $CONFIG" >&2; exit 1
fi

# ── 颜色 ────────────────────────────────────────────────────────────────
G='\033[0;32m'; Y='\033[0;33m'; R='\033[0;31m'; C='\033[0;36m'; D='\033[0;2m'; M='\033[0m'

info()  { echo -e "${C}${*}${M}"; }
ok()    { echo -e "${G}${*}${M}"; }
warn()  { echo -e "${Y}${*}${M}"; }
err()   { echo -e "${R}${*}${M}" >&2; }
dim()   { echo -e "${D}${*}${M}"; }

# ── 解析配置 ────────────────────────────────────────────────────────────
JSON_REL="Api/tools/migrations.json"

list_services() {
  if command -v node &>/dev/null; then
    (cd "$REPO_ROOT" && node -e "const d=JSON.parse(require('fs').readFileSync('$JSON_REL','utf8')); Object.keys(d.services).forEach(k=>console.log(k))" 2>/dev/null)
  elif command -v jq &>/dev/null; then
    jq -r '.services | keys | .[]' "$CONFIG" 2>/dev/null
  elif command -v python3 &>/dev/null; then
    python3 -c "import json; d=json.load(open('$CONFIG')); [print(k) for k in d['services'].keys()]" 2>/dev/null
  elif command -v python &>/dev/null; then
    python -c "import json; d=json.load(open('$CONFIG')); [print(k) for k in d['services'].keys()]" 2>/dev/null
  fi
}

resolve_service_name() {
  local id="$1"
  case "$id" in
    ''|*[!0-9]*) echo "$id" ;;  # 非数字，作为服务名返回
    *)
      local i=0
      for s in $(list_services); do
        i=$((i+1))
        [ "$i" -eq "$id" ] && echo "$s" && return
      done
      echo "" ;;  # 未找到
  esac
}

get_svc_field() {
  local svc="$1" field="$2"
  if command -v node &>/dev/null; then
    (cd "$REPO_ROOT" && node -e "const d=JSON.parse(require('fs').readFileSync('$JSON_REL','utf8')); console.log(d.services['$svc']['$field'])" 2>/dev/null)
  elif command -v jq &>/dev/null; then
    jq -r ".services[\"$svc\"][\"$field\"]" "$CONFIG" 2>/dev/null
  elif command -v python3 &>/dev/null; then
    python3 -c "import json,sys; d=json.load(open('$CONFIG')); print(d['services']['$svc']['$field'])" 2>/dev/null
  elif command -v python &>/dev/null; then
    python -c "import json,sys; d=json.load(open('$CONFIG')); print(d['services']['$svc']['$field'])" 2>/dev/null
  else
    err "✗ 需要 node / jq / python 来解析 JSON"; exit 1
  fi
}

# ── 辅助 ────────────────────────────────────────────────────────────────
check_dotnet_ef() {
  if ! dotnet_ef --version &>/dev/null; then
    err "✗ 未安装 dotnet-ef 工具。请执行:"
    warn "    dotnet tool install --global dotnet-ef"
    exit 1
  fi
}

dotnet_ef() {
  (cd "$REPO_ROOT/Api/src" && dotnet ef "$@")
}

ef_args() {
  local svc="$1" no_build="${2:-}"
  printf '%s' "--project $REPO_ROOT/$(get_svc_field "$svc" infra) --startup-project $REPO_ROOT/$(get_svc_field "$svc" api) --context $(get_svc_field "$svc" dbContext)"
  [ -n "$no_build" ] && printf '%s' ' --no-build'
  echo
}

# ── 命令: help ──────────────────────────────────────────────────────────
show_help() {
  info  "ST 微服务迁移管理工具"
  echo
  echo "用法:"
  ok    "  $(basename "$0") check  [service]"
  ok    "  $(basename "$0") list   [service]"
  ok    "  $(basename "$0") add    <service> [name]"
  ok    "  $(basename "$0") remove <service>"
  ok    "  $(basename "$0") update <service> [migration]"
  ok    "  $(basename "$0") script <service> [from] [to]"
  ok    "  $(basename "$0") help"
  echo
  info  "选项:"
  dim   "  --no-build    跳过构建步骤（除 check 外均支持）"
  echo
  info  "服务列表:"
  local i=0
  for s in $(list_services); do
    i=$((i+1))
    db="$(get_svc_field "$s" database)"
    ctx="$(get_svc_field "$s" dbContext)"
    printf "  [%d] %-14s → %s  (%s)\n" "$i" "$s" "$db" "$ctx"
  done
}

# ── 命令: check ─────────────────────────────────────────────────────────
cmd_check() {
  local svc="${1:-}"
  local any_pending=false

  for s in $( [ -n "$svc" ] && echo "$svc" || list_services ); do
    if [ -z "$(get_svc_field "$s" dbContext 2>/dev/null)" ]; then
      err "  ✗ 未知服务: $s"; continue
    fi

    label="[$s]"

    local output ret=0
    output="$(dotnet_ef migrations has-pending-model-changes $(ef_args "$s") 2>&1)" || ret=$?

    if [ "$ret" -eq 0 ]; then
      ok "  $label ✓ 无需迁移"
    elif [ "$ret" -eq 1 ]; then
      warn "  $label ⚠ 有模型变更未迁移"
      dim "           → 执行: dotnet ef migrations add <名称> $(ef_args "$s")"
      any_pending=true
    else
      err "  $label ✗ 检查失败"
      err "    $(echo "$output" | head -3)"
    fi
  done

  [ "$any_pending" = true ] && exit 1 || exit 0
}

# ── 命令: list ──────────────────────────────────────────────────────────
cmd_list() {
  local svc="${1:-}"
  for s in $( [ -n "$svc" ] && echo "$svc" || list_services ); do
    [ -z "$(get_svc_field "$s" dbContext 2>/dev/null)" ] && { err "  ✗ 未知服务: $s"; continue; }

    label="[$s]"
    info "  $label 迁移列表:"

    local output
    output="$(dotnet_ef migrations list $(ef_args "$s") 2>&1 || true)"

    while IFS= read -r line; do
      if echo "$line" | grep -qE '\b[0-9]{14}\b'; then
        local status="✓"
        echo "$line" | grep -qi 'pending' && status="⏳"
        local name
        name="$(echo "$line" | awk '{print $NF}')"
        echo "    $status $name"
      fi
    done <<< "$output"
    echo
  done
}

# ── 命令: add ───────────────────────────────────────────────────────────
cmd_add() {
  local svc="$1" name="$2" no_build="${3:-}"
  if [ -z "$svc" ]; then
    err "✗ 用法: $(basename "$0") add <service> [name]"; exit 1
  fi
  [ -z "$(get_svc_field "$svc" dbContext 2>/dev/null)" ] && { err "✗ 未知服务: $svc"; exit 1; }

  # name 省略时自动编号 000n
  if [ -z "$name" ]; then
    local infra_dir="$REPO_ROOT/$(get_svc_field "$svc" infra)/Migrations"
    local next_number
    next_number="$(ls "$infra_dir"/*.cs 2>/dev/null | grep -oP '_\K\d{4}(?=\.cs$)' | sort -rn | head -1)"
    if [ -z "$next_number" ]; then
      name="0001"
    else
      name="$(printf "%04d" $((10#$next_number + 1)))"
    fi
    dim "  → 未指定名称，自动编号: $name"
  fi

  dim "  → 生成迁移 $name ..."
  local output
  output="$(dotnet_ef migrations add "$name" $(ef_args "$svc" "$no_build") 2>&1)"
  local ret=$?

  if [ $ret -eq 0 ]; then
    ok "  ✓ 已生成迁移: $name"
    while IFS= read -r line; do
      if echo "$line" | grep -q '\.cs\b' && ! echo "$line" | grep -qE 'Designer|Snapshot'; then
        dim "    $line"
      fi
    done <<< "$output"
  else
    err "  ✗ 迁移生成失败:"
    echo "$output" >&2
    exit $ret
  fi
}

# ── 命令: remove ────────────────────────────────────────────────────────
cmd_remove() {
  local svc="$1" no_build="${2:-}"
  if [ -z "$svc" ]; then
    err "✗ 用法: $(basename "$0") remove <service>"; exit 1
  fi
  [ -z "$(get_svc_field "$svc" dbContext 2>/dev/null)" ] && { err "✗ 未知服务: $svc"; exit 1; }

  warn "  即将移除 $svc 的最后一条迁移，是否继续? [y/N] "
  read -r confirm
  if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    dim '  已取消'; exit 0
  fi

  local output
  output="$(dotnet_ef migrations remove $(ef_args "$svc" "$no_build") 2>&1)"
  local ret=$?

  if [ $ret -eq 0 ]; then
    ok '  ✓ 已移除最后一条迁移'
  else
    err '  ✗ 移除失败:'
    echo "$output" >&2
    exit $ret
  fi
}

# ── 命令: update ────────────────────────────────────────────────────────
cmd_update() {
  local svc="$1" target="$2" no_build="${3:-}"
  if [ -z "$svc" ]; then
    err "✗ 用法: $(basename "$0") update <service> [migration]"; exit 1
  fi
  [ -z "$(get_svc_field "$svc" dbContext 2>/dev/null)" ] && { err "✗ 未知服务: $svc"; exit 1; }

  local db
  db="$(get_svc_field "$svc" database)"
  dim "  → 更新数据库 $db ..."

  local output
  local args="database update"
  [ -n "$target" ] && args="$args $target"
  output="$(dotnet_ef $args $(ef_args "$svc" "$no_build") 2>&1)"
  local ret=$?

  if [ $ret -eq 0 ]; then
    ok '  ✓ 数据库已更新'
  else
    err '  ✗ 数据库更新失败:'
    echo "$output" >&2
    exit $ret
  fi
}

# ── 命令: script ────────────────────────────────────────────────────────
cmd_script() {
  local svc="$1" from="$2" to="$3"
  if [ -z "$svc" ]; then
    err "✗ 用法: $(basename "$0") script <service> [from] [to]"; exit 1
  fi
  [ -z "$(get_svc_field "$svc" dbContext 2>/dev/null)" ] && { err "✗ 未知服务: $svc"; exit 1; }

  local script_dir="$REPO_ROOT/scripts"
  mkdir -p "$script_dir" 2>/dev/null || true
  local output_file="$script_dir/${svc}_$(date +%Y%m%d%H%M%S).sql"

  dim "  → 生成 SQL 脚本 ..."

  # 构建参数数组
  set -- migrations script --output "$output_file"
  [ -n "$from" ] && set -- "$@" --from "$from"
  [ -n "$to" ]   && set -- "$@" --to "$to"

  local output
  output="$(dotnet_ef "$@" $(ef_args "$svc" true) 2>&1)"
  local ret=$?

  if [ $ret -eq 0 ]; then
    ok "  ✓ SQL 脚本已生成: $output_file"
  else
    err '  ✗ SQL 脚本生成失败:'
    echo "$output" >&2
    exit $ret
  fi
}

# ── 入口 ────────────────────────────────────────────────────────────────
check_dotnet_ef

CMD="${1:-help}"
SERVICE=""
NAME=""
SCRIPT_TO=""
NO_BUILD=""

for arg in "${@:2}"; do
  case "$arg" in
    --help)     show_help; exit 0 ;;
    --no-build) NO_BUILD="true" ;;
    *)
      if [ -z "$SERVICE" ]; then
        SERVICE="$arg"
      elif [ -z "$NAME" ]; then
        NAME="$arg"
      elif [ -z "$SCRIPT_TO" ]; then
        SCRIPT_TO="$arg"
      fi
      ;;
  esac
done

# 解析服务编号为名称（支持 migrate.sh check 1 / add 1 等）
if [ -n "$SERVICE" ]; then
  resolved="$(resolve_service_name "$SERVICE")"
  if [ -z "$resolved" ]; then
    err "✗ 未知服务: $SERVICE（使用 list 查看可用服务）"; exit 1
  fi
  SERVICE="$resolved"
fi

case "$CMD" in
  check)  cmd_check   "$SERVICE" ;;
  list)   cmd_list    "$SERVICE" ;;
  add)    cmd_add     "$SERVICE" "$NAME" "$NO_BUILD" ;;
  remove) cmd_remove  "$SERVICE" "$NO_BUILD" ;;
  update) cmd_update  "$SERVICE" "$NAME" "$NO_BUILD" ;;
  script) cmd_script  "$SERVICE" "$NAME" "$SCRIPT_TO" ;;
  help|*) show_help ;;
esac
