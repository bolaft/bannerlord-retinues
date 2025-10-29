import sys
import xml.dom.minidom

from pathlib import Path
from jinja2 import Environment, FileSystemLoader, TemplateError, ChoiceLoader

# Directory containing this script (expected: .../tpl)
SCRIPT_DIR = Path(__file__).resolve().parent

# Output base is ../gui relative to tpl directory -> .../gui
OUTPUT_BASE = SCRIPT_DIR.parent / "gui"


def pretty_xml(raw: str, indent: str = "  ") -> str:
    """
    Return pretty-printed XML text with consistent indentation.
    Falls back to the raw string if parsing fails (e.g. incomplete XML snippet).
    """
    try:
        parsed = xml.dom.minidom.parseString(raw)
        pretty = parsed.toprettyxml(indent=indent)
        # remove blank lines added by toprettyxml()
        lines = [line for line in pretty.splitlines() if line.strip()]
        return "\n".join(lines)
    except Exception:
        return raw


def register_global_macros(env: Environment) -> None:
    """
    Load all .j2 files under tpl/_macros and register each as env.globals[name].
    """
    macros_dir = SCRIPT_DIR / "_macros"
    if not macros_dir.exists():
        return

    for path in macros_dir.glob("*.j2"):
        if path.name.startswith(("_", ".")):
            continue

        name = path.stem  # e.g. "button"
        try:
            module = env.get_template(path.name).module
            env.globals[name] = module
        except Exception as e:
            print(f"[WARN] Failed to register macro '{name}': {e}", file=sys.stderr)


def build_env(loader_path: Path) -> Environment:
    """
    Create a Jinja2 Environment with access to both:
      - the module-specific directory (loader_path)
      - the global macros directory (SCRIPT_DIR / "_macros")
    """
    loaders = [
        FileSystemLoader(str(loader_path)),
        FileSystemLoader(str(SCRIPT_DIR / "_macros")),
    ]
    env = Environment(
        loader=ChoiceLoader(loaders),
        autoescape=False,
        trim_blocks=True,
        lstrip_blocks=True,
    )

    register_global_macros(env)
    return env


def render_template(module_dir: Path, template_rel_path: Path, context: dict | None = None) -> str:
    """
    Render a single template which is located under module_dir.
    template_rel_path is the path relative to module_dir (can include subfolders).
    """
    env = build_env(module_dir)
    tpl = env.get_template(template_rel_path.as_posix())
    return tpl.render(context or {})


def process_module(module_dir: Path) -> None:
    """
    Find all .j2 files under module_dir, render them and write to gui output.
    Output root: ../gui/{ModuleName}/PrefabExtensions/ClanScreen/<same relative path>.xml
    """
    module_name = module_dir.name
    out_base = OUTPUT_BASE / module_name / "PrefabExtensions" / "ClanScreen"

    for j2_file in module_dir.rglob("*.j2"):
        # skip caches or hidden
        if any(part.startswith(".") for part in j2_file.parts):
            continue

        rel = j2_file.relative_to(module_dir)
        # skip templates that are macros / subtemplates starting with '_' either
        # by filename or by any folder in their relative path
        if any(part.startswith((".", "_")) for part in rel.parts):
            continue
        out_rel = rel.with_suffix(".xml")
        out_path = out_base / out_rel
        out_path.parent.mkdir(parents=True, exist_ok=True)

        try:
            rendered = render_template(module_dir, rel, {})
            rendered = pretty_xml(rendered)   # <-- add this line
        except TemplateError as e:
            print(f"[ERROR] Template error rendering {j2_file}: {e}", file=sys.stderr)
            continue

        out_path.write_text(rendered, encoding="utf-8")
        print(f"Wrote: {out_path}")


def main() -> int:
    if not SCRIPT_DIR.exists():
        print("Script directory not found", file=sys.stderr)
        return 2

    for entry in sorted(SCRIPT_DIR.iterdir()):
        if not entry.is_dir():
            continue
        # skip hidden, underscore-prefixed module folders, and caches
        if entry.name.startswith((".", "_")) or entry.name == "__pycache__":
            continue
        print(f"Processing module: {entry.name}")
        process_module(entry)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
