#!/usr/bin/env python3
"""
push 직전 CHANGELOG 갱신 (월별 파일에 누적).

사용:
  update_changelog.py --apply              # 월별 md + CHANGELOG.md 갱신, 변경 시 커밋
  update_changelog.py --preview -          # stdout 미리보기 (파일 없음)
  update_changelog.py --simulate         # origin/master..HEAD + 미커밋(untracked) 시뮬레이션

종료 코드:
  0 = 성공 (apply: 커밋했거나 변경 없음)
  2 = push 범위에 새 커밋 없음
  1 = 오류
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from datetime import datetime
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
CHANGELOG_INDEX = REPO_ROOT / "CHANGELOG.md"
CHANGELOG_DIR = REPO_ROOT / "docs" / "changelog"
MONTHS_MARKER_START = "<!-- changelog:auto:months -->"
MONTHS_MARKER_END = "<!-- /changelog:auto:months -->"

CHANGELOG_AUTO_PREFIX = "docs: changelog 갱신"


def is_changelog_auto_commit(subject: str) -> bool:
    return subject.startswith(CHANGELOG_AUTO_PREFIX)


PREFIX_MAP = {
    "feat": "added",
    "feature": "added",
    "fix": "fixed",
    "bugfix": "fixed",
    "refactor": "changed",
    "perf": "changed",
    "change": "changed",
    "style": "changed",
    "docs": "other",
    "chore": "other",
    "test": "other",
    "ci": "other",
    "build": "other",
}

SECTION_LABELS = {
    "added": "추가",
    "changed": "변경",
    "fixed": "수정",
    "other": "기타",
}

# 커밋 본문 불릿: "- English — Korean" (em/en dash or " - ")
BULLET_KO_RE = re.compile(r"^[-*]\s+.+\s+(?:—|–|-)\s+(.+)$")


def run_git(*args: str, check: bool = True) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if check and result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or f"git {' '.join(args)} failed")
    return (result.stdout or "").strip()


def now_kst() -> datetime:
    try:
        from zoneinfo import ZoneInfo

        return datetime.now(ZoneInfo("Asia/Seoul"))
    except Exception:
        return datetime.now()


def get_push_range(simulate: bool = False) -> tuple[str, str]:
    if simulate:
        branch = run_git("symbolic-ref", "--short", "HEAD", check=False) or "master"
        upstream = f"origin/{branch}"
        if run_git("rev-parse", "--verify", upstream, check=False):
            return f"{upstream}..HEAD", f"{upstream}..HEAD (+ simulate)"
        base = run_git("merge-base", "HEAD", upstream, check=False)
        if not base:
            base = run_git("rev-list", "--max-parents=0", "HEAD").splitlines()[-1]
        return f"{base}..HEAD", f"{base}..HEAD (+ simulate)"

    if run_git("rev-parse", "--verify", "@{u}", check=False):
        label = run_git("rev-parse", "--abbrev-ref", "@{u}") + "..HEAD"
        return "@{u}..HEAD", label

    branch = run_git("symbolic-ref", "--short", "HEAD", check=False) or "HEAD"
    upstream = f"origin/{branch}"
    base = run_git("merge-base", "HEAD", upstream, check=False)
    if not base:
        base = run_git("rev-list", "--max-parents=0", "HEAD").splitlines()[-1]
    return f"{base}..HEAD", f"{base}..HEAD"


def classify_commit(subject: str) -> tuple[str, str]:
    match = re.match(r"^(\w+)(?:\([^)]+\))?!?:\s*(.*)$", subject, re.IGNORECASE)
    if not match:
        return "other", subject
    prefix, rest = match.group(1).lower(), match.group(2).strip()
    bucket = PREFIX_MAP.get(prefix, "other")
    return bucket, rest or subject


def collect_commits(rev_range: str) -> tuple[list[str], dict[str, list[str]]]:
    raw = run_git("log", rev_range, "--pretty=format:%h|%s", check=False)
    commit_lines: list[str] = []
    buckets: dict[str, list[str]] = {k: [] for k in SECTION_LABELS}

    if not raw:
        return commit_lines, buckets

    for line in raw.splitlines():
        if "|" not in line:
            continue
        short_hash, subject = line.split("|", 1)
        if is_changelog_auto_commit(subject):
            continue
        commit_lines.append(f"- {subject} (`{short_hash}`)")
        bucket, desc = classify_commit(subject)
        buckets[bucket].append(f"- {desc} (`{short_hash}`)")

    return commit_lines, buckets


def extract_korean_from_body(body: str) -> list[str]:
    items: list[str] = []
    for line in body.splitlines():
        match = BULLET_KO_RE.match(line.strip())
        if not match:
            continue
        text = match.group(1).strip()
        if text:
            items.append(text)
    return items


def collect_korean_summaries(rev_range: str) -> list[str]:
    """커밋 본문 불릿에서 ` — ` 뒤 한국어를 추출 (commit-message rule)."""
    hashes = run_git("log", rev_range, "--pretty=format:%h", check=False)
    if not hashes:
        return []

    seen: set[str] = set()
    lines: list[str] = []
    for short_hash in hashes.splitlines():
        subject = run_git("log", "-1", f"--pretty=format:%s", short_hash, check=False)
        if is_changelog_auto_commit(subject):
            continue
        body = run_git("log", "-1", "--pretty=format:%b", short_hash, check=False)
        for text in extract_korean_from_body(body):
            if text in seen:
                continue
            seen.add(text)
            lines.append(f"- {text}")
    return lines


def collect_files(rev_range: str, simulate: bool) -> list[str]:
    files = run_git("diff", "--name-only", rev_range, check=False)
    names = [f for f in files.splitlines() if f]

    if simulate:
        untracked = run_git("ls-files", "--others", "--exclude-standard", check=False)
        names.extend(f for f in untracked.splitlines() if f)
        names = sorted(set(names))

    if not names:
        return ["- (없음)"]
    return [f"- {name}" for name in names]


def collect_stat(rev_range: str, simulate: bool) -> str:
    stat = run_git("diff", "--stat", rev_range, check=False)
    line = stat.splitlines()[-1] if stat else "(diff stat 없음)"
    if simulate:
        untracked_n = len(
            [f for f in run_git("ls-files", "--others", "--exclude-standard", check=False).splitlines() if f]
        )
        if untracked_n:
            line = f"{line} (+ untracked {untracked_n} files, stat 미포함)"
    return line


def build_entry(rev_range: str, range_label: str, simulate: bool = False) -> dict | None:
    commit_lines, buckets = collect_commits(rev_range)

    if simulate and not commit_lines:
        untracked = run_git("ls-files", "--others", "--exclude-standard", check=False)
        modified = run_git("diff", "--name-only", rev_range, check=False)
        if not untracked and not modified:
            return None
        commit_lines = ["- (아직 커밋 안 됨 — push 전 실제 커밋 메시지·해시로 대체됨)"]
        buckets = {k: [] for k in SECTION_LABELS}

    if not commit_lines:
        return None

    moment = now_kst()
    timestamp = moment.strftime("%Y-%m-%d %H:%M:%S KST")
    month_key = moment.strftime("%Y-%m")
    month_title = moment.strftime("%Y년 %m월")

    branch = run_git("symbolic-ref", "--short", "HEAD", check=False) or "HEAD"
    base_ref = rev_range.split("..")[0]
    sha_from = run_git("rev-parse", "--short", base_ref, check=False) if base_ref else "?"
    sha_to = run_git("rev-parse", "--short", "HEAD", check=False)

    file_lines = collect_files(rev_range, simulate)
    stat_line = collect_stat(rev_range, simulate)
    summary_lines = [] if simulate else collect_korean_summaries(rev_range)

    parts = [
        f"## {timestamp}",
        "",
        f"**브랜치:** `{branch}` → `origin`  ",
        f"**범위:** `{range_label}` (`{sha_from}`..`{sha_to}`)  ",
        f"**커밋 수:** {len(commit_lines)}",
        "",
    ]

    if summary_lines:
        parts.append("### 요약")
        parts.extend(summary_lines)
        parts.append("")

    for key in ("added", "changed", "fixed", "other"):
        items = buckets[key]
        if items:
            parts.append(f"### {SECTION_LABELS[key]}")
            parts.extend(items)
            parts.append("")

    parts.extend(
        [
            "### 커밋",
            *commit_lines,
            "",
            "### 변경 파일",
            *file_lines,
            "",
            "### diff 요약",
            "```",
            stat_line,
            "```",
            "",
            "---",
            "",
        ]
    )

    return {
        "entry": "\n".join(parts),
        "month_key": month_key,
        "month_title": month_title,
        "month_file": CHANGELOG_DIR / f"{month_key}.md",
        "timestamp": timestamp,
    }


def ensure_changelog_index(month_key: str, month_title: str) -> None:
    link_line = f"- [{month_key}](docs/changelog/{month_key}.md) — {month_title}"

    if not CHANGELOG_INDEX.exists():
        CHANGELOG_INDEX.write_text(
            "\n".join(
                [
                    "# Changelog",
                    "",
                    "push할 때마다 **월별 파일 맨 위**에 기록이 누적됩니다.",
                    "",
                    "## 월별",
                    "",
                    MONTHS_MARKER_START,
                    link_line,
                    MONTHS_MARKER_END,
                    "",
                ]
            ),
            encoding="utf-8",
            newline="\n",
        )
        return

    text = CHANGELOG_INDEX.read_text(encoding="utf-8")
    if MONTHS_MARKER_START not in text or MONTHS_MARKER_END not in text:
        if "## 월별" not in text:
            text = text.rstrip() + "\n\n## 월별\n\n"
        text += (
            f"{MONTHS_MARKER_START}\n{link_line}\n{MONTHS_MARKER_END}\n"
        )
        CHANGELOG_INDEX.write_text(text, encoding="utf-8", newline="\n")
        return

    pre, rest = text.split(MONTHS_MARKER_START, 1)
    block, post = rest.split(MONTHS_MARKER_END, 1)
    lines = [ln for ln in block.strip().splitlines() if ln.strip()]
    if not any(month_key in ln for ln in lines):
        lines.insert(0, link_line)
    new_block = "\n".join(lines)
    CHANGELOG_INDEX.write_text(
        pre + MONTHS_MARKER_START + "\n" + new_block + "\n" + MONTHS_MARKER_END + post,
        encoding="utf-8",
        newline="\n",
    )


def prepend_month_entry(month_file: Path, month_title: str, entry: str) -> None:
    CHANGELOG_DIR.mkdir(parents=True, exist_ok=True)
    header = f"# {month_title}\n\n"

    if month_file.exists():
        body = month_file.read_text(encoding="utf-8")
        if not body.startswith("#"):
            body = header + body
        lines = body.splitlines(keepends=True)
        insert_at = 0
        if lines and lines[0].startswith("#"):
            insert_at = 1
            if len(lines) > 1 and lines[1].strip() == "":
                insert_at = 2
        month_file.write_text("".join(lines[:insert_at]) + entry + "".join(lines[insert_at:]), encoding="utf-8", newline="\n")
    else:
        month_file.write_text(header + entry, encoding="utf-8", newline="\n")


def git_commit_changelog(timestamp: str) -> None:
    paths = [CHANGELOG_INDEX, CHANGELOG_DIR]
    for path in paths:
        if path.exists():
            run_git("add", "--", str(path.relative_to(REPO_ROOT)).replace("\\", "/"))

    if run_git("diff", "--cached", "--quiet", check=False):
        return

    run_git("commit", "-m", f"docs: changelog 갱신 ({timestamp})", "--no-verify")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true", help="CHANGELOG 갱신 및 커밋")
    parser.add_argument("--preview", nargs="?", const="CHANGELOG-preview.txt", metavar="FILE")
    parser.add_argument("--simulate", action="store_true", help="커밋 전 작업 트리 포함 미리보기")
    args = parser.parse_args()

    if not args.apply and args.preview is None:
        parser.print_help()
        return 1

    rev_range, range_label = get_push_range(simulate=args.simulate)
    data = build_entry(rev_range, range_label, simulate=args.simulate)
    if data is None:
        return 2

    if args.preview is not None:
        mode = "simulate-pending" if args.simulate else "push-range (@{u}..HEAD)"
        header = "\n".join(
            [
                f"대상: docs/changelog/{data['month_key']}.md",
                f"모드: {mode}",
                "",
            ]
        )
        body = header + data["entry"]
        if args.preview == "-":
            sys.stdout.buffer.write(body.encode("utf-8"))
            sys.stdout.buffer.write(b"\n")
        else:
            preview_path = REPO_ROOT / args.preview
            preview_path.write_text(
                "\n".join(
                    [
                        "=== CHANGELOG 미리보기 (hook이 월별 파일 맨 위에 붙일 블록) ===",
                        f"생성: {data['timestamp']}",
                        body,
                    ]
                ),
                encoding="utf-8",
                newline="\n",
            )
            print(f"미리보기 저장: {preview_path}")
        if not args.apply:
            return 0

    if args.apply:
        prepend_month_entry(data["month_file"], data["month_title"], data["entry"])
        ensure_changelog_index(data["month_key"], data["month_title"])

        before = run_git("status", "--porcelain", check=False)
        git_commit_changelog(data["timestamp"])
        after = run_git("status", "--porcelain", check=False)
        if before != after:
            print(f"pre-push: changelog 갱신 커밋 생성 ({data['month_file'].name})")
        else:
            print("pre-push: changelog 변경 없음")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except RuntimeError as exc:
        print(f"changelog 오류: {exc}", file=sys.stderr)
        raise SystemExit(1) from exc
