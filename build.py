#!/usr/bin/env python3
import argparse
import pathlib
import re
import xml.etree.ElementTree as ET
import yaml # type: ignore


def bool_str(value: bool) -> str:
    return "true" if value else "false"


def indent(elem, level=0):
    """
    Pretty-print ElementTree in-place.
    """
    i = "\n" + level * "  "
    if len(elem):
        if not elem.text or not elem.text.strip():
            elem.text = i + "  "
        for child in elem:
            indent(child, level + 1)
        if not child.tail or not child.tail.strip():  # type: ignore[name-defined]
            child.tail = i
    if level and (not elem.tail or not elem.tail.strip()):
        elem.tail = i


def apply_release_patch(base_version: str, release_patch: str | None) -> str:
    """
    Override the last numeric segment of base_version with release_patch, if provided.

    Example:
      base_version = 'v1.3.14.0', release_patch = '16' -> 'v1.3.14.16'
      base_version = 'v1.3.14',   release_patch = '2'  -> 'v1.3.14.2'
      base_version = 'v1.3',      release_patch = '5'  -> 'v1.3.5'
      base_version = 'v1',        release_patch = '3'  -> 'v1.3'
    """
    if not release_patch:
        return base_version

    m = re.match(r"^(.*\.)(\d+)$", base_version)
    if m:
        prefix, _ = m.groups()
        return f"{prefix}{release_patch}"
    # Fallback: append .<patch>
    return f"{base_version}.{release_patch}"


def build_module_tree(cfg: dict, ver_key: str, vcfg: dict, effective_version: str) -> ET.Element:
    module_cfg = cfg["module"]

    root = ET.Element("Module")

    ET.SubElement(root, "Name", value=module_cfg["name"])
    ET.SubElement(root, "Id", value=module_cfg.get("id", module_cfg["name"]))
    ET.SubElement(root, "Version", value=effective_version)
    ET.SubElement(root, "DefaultModule", value=bool_str(module_cfg.get("defaultModule", False)))
    ET.SubElement(root, "SingleplayerModule", value=bool_str(module_cfg.get("singleplayer", True)))
    ET.SubElement(root, "MultiplayerModule", value=bool_str(module_cfg.get("multiplayer", False)))
    ET.SubElement(root, "Official", value=bool_str(module_cfg.get("official", False)))

    # Version-specific dependencies
    deps_node = ET.SubElement(root, "DependedModules")
    for dep in vcfg.get("dependencies", []):
        ET.SubElement(
            deps_node,
            "DependedModule",
            Id=dep["id"],
            DependentVersion=dep["version"],
        )

    # Common SubModules
    submodules_node = ET.SubElement(root, "SubModules")
    for sm in cfg.get("submodules", []):
        sm_node = ET.SubElement(submodules_node, "SubModule")
        ET.SubElement(sm_node, "Name", value=sm["name"])
        ET.SubElement(sm_node, "DLLName", value=sm["dllName"])
        ET.SubElement(sm_node, "SubModuleClassType", value=sm["classType"])

        tags_node = ET.SubElement(sm_node, "Tags")
        for tag in sm.get("tags", []):
            ET.SubElement(tags_node, "Tag", key=tag)

        assemblies_node = ET.SubElement(sm_node, "Assemblies")
        for asm_path in sm.get("assemblies", []):
            ET.SubElement(assemblies_node, "Assembly", Path=asm_path)

    # Common Xmls
    xmls_node = ET.SubElement(root, "Xmls")
    for xml_cfg in cfg.get("xmls", []):
        xml_node = ET.SubElement(xmls_node, "XmlNode")
        ET.SubElement(
            xml_node,
            "XmlName",
            id=xml_cfg["id"],
            path=xml_cfg["path"],
        )
        game_types = xml_cfg.get("gameTypes", [])
        if game_types:
            inc_node = ET.SubElement(xml_node, "IncludedGameTypes")
            for gt in game_types:
                ET.SubElement(inc_node, "GameType", value=gt)

    return root


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate SubModule.xml from build.yaml")
    parser.add_argument(
        "--config",
        default="build.yaml",
        help="Path to build.yaml (default: build.yaml in repo root)",
    )
    parser.add_argument(
        "--only",
        choices=["12", "13"],
        help="If set, generate XML only for this BL version key.",
    )
    parser.add_argument(
        "--out",
        required=True,
        help="Target module directory (SubModule.xml will be written here).",
    )
    parser.add_argument(
        "--release-patch",
        help="Optional release patch number to override the last version segment.",
    )
    args = parser.parse_args()

    config_path = pathlib.Path(args.config).resolve()
    if not config_path.is_file():
        print(f"ERROR: config file not found: {config_path}")
        return 1

    with config_path.open("r", encoding="utf-8") as f:
        cfg = yaml.safe_load(f)

    versions = cfg.get("versions", {})
    if not versions:
        print("ERROR: no 'versions' section found in config")
        return 1

    out_dir = pathlib.Path(args.out).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "SubModule.xml"

    # We assume you call with --only "$BL", so there will be exactly one
    # version to emit. But we still support the generic loop.
    for ver_key, vcfg in versions.items():
        if args.only and ver_key != args.only:
            continue

        base_version = vcfg["version"]
        eff_version = apply_release_patch(base_version, args.release_patch)

        root = build_module_tree(cfg, ver_key, vcfg, eff_version)
        indent(root)
        tree = ET.ElementTree(root)
        tree.write(out_path, encoding="utf-8", xml_declaration=False)

        print(f"BL{ver_key}: wrote {out_path} (Version={eff_version})")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
