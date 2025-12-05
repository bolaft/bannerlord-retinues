#!/usr/bin/env python3

import argparse
import pathlib
import re
import shutil
import subprocess
import xml.etree.ElementTree as ET

import yaml  # type: ignore


def find_7z_executable() -> str | None:
    """
    Try to locate a 7z executable.

    Order:
      - whatever is in PATH (7z / 7z.exe)
      - common Windows install locations
    """
    exe = shutil.which("7z") or shutil.which("7z.exe")
    if exe:
        return exe

    # Try common Windows install locations
    candidates = [
        pathlib.Path(r"C:\Program Files\7-Zip\7z.exe"),
        pathlib.Path(r"C:\Program Files (x86)\7-Zip\7z.exe"),
    ]
    for c in candidates:
        if c.is_file():
            return str(c)

    return None


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


def resolve_releases_root(cfg: dict, game_dir: pathlib.Path) -> pathlib.Path:
    paths_cfg = cfg.get("paths", {}) or {}
    rel_root = paths_cfg.get("releasesRoot", "Releases")

    p = pathlib.Path(rel_root)
    if p.is_absolute():
        return p

    # default: "<GameDir>\Releases"
    return game_dir / rel_root


def generate_workshop_files(
    cfg: dict,
    ver_key: str,
    effective_version: str,
    module_src_dir: pathlib.Path,
) -> None:
    workshop_cfg = cfg.get("workshop", {}) or {}
    ver_cfg = workshop_cfg.get(ver_key, {}) or {}
    common_cfg = workshop_cfg.get("common", {}) or {}

    item_id = str(ver_cfg.get("itemId", "")).strip()
    if not item_id:
        print(f"[workshop] No workshop.itemId configured for BL{ver_key}, skipping Workshop files.")
        return

    # Determine game dir from module_src_dir: <GameDir>\Modules\<ModuleName>
    game_dir = module_src_dir.parent.parent

    # Where we keep release copies and the zip
    releases_root = resolve_releases_root(cfg, game_dir)
    version_root = releases_root / ver_key

    module_name = module_src_dir.name
    release_module_dir = version_root / module_name

    # 1) Ensure dirs
    version_root.mkdir(parents=True, exist_ok=True)

    # 2) Copy Retinues/ module into Releases/<BL>/Retinues
    if release_module_dir.exists():
        shutil.rmtree(release_module_dir)
    shutil.copytree(module_src_dir, release_module_dir)
    print(f"[workshop] Copied module to {release_module_dir}")

    # 3) Resolve bin\Win64_Shipping_Client target for Workshop XMLs
    bin_client_dir = game_dir / "bin" / "Win64_Shipping_Client"
    bin_client_dir.mkdir(parents=True, exist_ok=True)

    # Filenames suffixed with the BL version key, e.g. WorkshopUpdate12.xml
    update_filename = f"WorkshopUpdate{ver_key}.xml"
    create_filename = f"WorkshopCreate{ver_key}.xml"

    # 4) WorkshopUpdate<ver>.xml
    update_version_tag = ver_cfg.get("updateVersionTag")
    tags_common = list(common_cfg.get("tags", []))
    tags_update = tags_common.copy()
    if update_version_tag:
        tags_update.append(update_version_tag)

    update_root = ET.Element("Tasks")
    get_item = ET.SubElement(update_root, "GetItem")
    ET.SubElement(get_item, "ItemId", Value=item_id)

    upd = ET.SubElement(update_root, "UpdateItem")
    ET.SubElement(upd, "ModuleFolder", Value=str(release_module_dir))
    ET.SubElement(upd, "ChangeNotes", Value=f"Update {effective_version}")
    tags_node = ET.SubElement(upd, "Tags")
    for t in tags_update:
        ET.SubElement(tags_node, "Tag", Value=t)

    indent(update_root)
    update_tree = ET.ElementTree(update_root)
    update_path = bin_client_dir / update_filename
    update_tree.write(update_path, encoding="utf-8", xml_declaration=False)
    print(f"[workshop] Wrote {update_path}")

    # 5) WorkshopCreate<ver>.xml
    desc = common_cfg.get("description", "")
    image_file = common_cfg.get("imageFile", "")
    visibility = common_cfg.get("visibility", "Private")
    create_version_tag = ver_cfg.get("createVersionTag") or update_version_tag

    tags_create = tags_common.copy()
    if create_version_tag and create_version_tag not in tags_create:
        tags_create.append(create_version_tag)

    create_root = ET.Element("Tasks")
    ET.SubElement(create_root, "CreateItem")
    upd2 = ET.SubElement(create_root, "UpdateItem")
    ET.SubElement(upd2, "ModuleFolder", Value=str(release_module_dir))
    if desc:
        ET.SubElement(upd2, "ItemDescription", Value=desc)

    tags_node2 = ET.SubElement(upd2, "Tags")
    for t in tags_create:
        ET.SubElement(tags_node2, "Tag", Value=t)

    # Resolve image as <GameDir>\<imageFile> if imageFile is relative
    if image_file:
        img_path = pathlib.Path(image_file)
        if not img_path.is_absolute():
            img_path = game_dir / image_file
        ET.SubElement(upd2, "Image", Value=str(img_path))

    ET.SubElement(upd2, "Visibility", Value=visibility)

    indent(create_root)
    create_tree = ET.ElementTree(create_root)
    create_path = bin_client_dir / create_filename
    create_tree.write(create_path, encoding="utf-8", xml_declaration=False)
    print(f"[workshop] Wrote {create_path}")

    # 6) Retinues_v<fullVersion>.zip via 7zip (still in Releases/<BL>)
    ver = effective_version
    if ver.startswith("v"):
        zip_name = f"Retinues_{ver}.zip"
    else:
        zip_name = f"Retinues_v{ver}.zip"
    zip_path = version_root / zip_name

    exe = find_7z_executable()
    if not exe:
        print("[workshop] 7z executable not found (PATH or common locations); skipping archive creation.")
        return

    try:
        # Run 7z in the version_root so that archive contains a "Retinues" root folder
        subprocess.run(
            [exe, "a", str(zip_path), module_name],
            cwd=version_root,
            check=True,
        )
        print(f"[workshop] Created archive {zip_path}")
    except subprocess.CalledProcessError as ex:
        print(f"[workshop] 7z failed with exit code {ex.returncode}; archive not created.")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate SubModule.xml and/or release packaging from build.yaml")
    parser.add_argument(
        "--config",
        default="build.yaml",
        help="Path to build.yaml (default: build.yaml in repo root)",
    )
    parser.add_argument(
        "--only",
        choices=["12", "13"],
        help="If set, use only this BL version key.",
    )
    parser.add_argument(
        "--out",
        help="Target module directory (SubModule.xml will be written here).",
    )
    parser.add_argument(
        "--release-patch",
        help="Optional release patch number to override the last version segment.",
    )
    parser.add_argument(
        "--package-release",
        action="store_true",
        help="Instead of writing SubModule.xml, create a Releases/<BL> package with Workshop XMLs and a 7z archive.",
    )
    parser.add_argument(
        "--module-dir",
        help="Deployed module directory (e.g. '<GameDir>\\Modules\\Retinues'). Required with --package-release.",
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

    # Find the version to use
    target_ver_key = args.only
    if target_ver_key is None:
        if len(versions) == 1:
            target_ver_key = next(iter(versions.keys()))
        else:
            print("ERROR: multiple versions in config; please specify --only 12 or --only 13")
            return 1

    if target_ver_key not in versions:
        print(f"ERROR: version '{target_ver_key}' not found in config. Available: {', '.join(versions.keys())}")
        return 1

    vcfg = versions[target_ver_key]
    base_version = vcfg["version"]
    eff_version = apply_release_patch(base_version, args.release_patch)

    if args.package_release:
        if not args.module_dir:
            print("ERROR: --package-release requires --module-dir")
            return 1
        module_src_dir = pathlib.Path(args.module_dir).resolve()
        if not module_src_dir.is_dir():
            print(f"ERROR: module-dir is not a directory: {module_src_dir}")
            return 1

        print(f"[release] BL{target_ver_key} effective version: {eff_version}")
        generate_workshop_files(cfg, target_ver_key, eff_version, module_src_dir)
        return 0

    # SubModule.xml generation mode
    if not args.out:
        print("ERROR: --out is required when not using --package-release")
        return 1

    out_dir = pathlib.Path(args.out).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "SubModule.xml"

    root = build_module_tree(cfg, target_ver_key, vcfg, eff_version)
    indent(root)
    tree = ET.ElementTree(root)
    tree.write(out_path, encoding="utf-8", xml_declaration=False)

    print(f"BL{target_ver_key}: wrote {out_path} (Version={eff_version})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
