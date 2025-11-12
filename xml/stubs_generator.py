#!/usr/bin/env python3

# Generate minimal Bannerlord NPCCharacter stubs for Retinues.
# Usage:
#   python generate_stubs.py -n 100 -o stubs.xml --culture Culture.empire --start 0

import argparse
import math
import sys
import xml.etree.ElementTree as ET

DEFAULT_FACE_BY_CULTURE = {
    "Culture.empire": "BodyProperty.fighter_empire",
    "Culture.vlandia": "BodyProperty.fighter_vlandia",
    "Culture.sturgia": "BodyProperty.fighter_sturgia",
    "Culture.battania": "BodyProperty.fighter_battania",
    "Culture.khuzait": "BodyProperty.fighter_khuzait",
    "Culture.aserai": "BodyProperty.fighter_aserai",
    # Fallback used if culture not in map:
    "_default": "BodyProperty.fighter_empire",
}

def build_npc_character(
    npc_id: str,
    culture: str,
    default_group: str,
    level: int,
    face_key_template: str | None,
) -> ET.Element:
    npc = ET.Element("NPCCharacter", {
        "id": npc_id,
        "name": npc_id,
        "is_hidden_encyclopedia": "true",
        "default_group": default_group,
        "level": str(level),
        "is_basic_troop": "true",
        "occupation": "Soldier",
        "culture": culture,
    })

    if face_key_template:
        face = ET.SubElement(npc, "face")
        ET.SubElement(face, "face_key_template", {"value": face_key_template})

    return npc

def main(argv=None):
    p = argparse.ArgumentParser(description="Generate minimal NPCCharacter stubs (Retinues).")
    p.add_argument("-n", "--count", type=int, required=True, help="Number of stubs to generate.")
    p.add_argument("--start", type=int, default=0, help="Starting index for numbering (default: 0).")
    p.add_argument("-o", "--output", default="stubs.xml", help="Output XML file path (default: stubs.xml).")
    p.add_argument("--culture", default="Culture.empire", help="Culture string (default: Culture.empire).")
    p.add_argument("--group", default="Infantry", help="Default battle group (default: Infantry).")
    p.add_argument("--level", type=int, default=1, help="Level (default: 1).")
    p.add_argument("--no-face", action="store_true", help="Do not include a face template.")
    p.add_argument("--face-template", default=None, help="Explicit face_key_template value (overrides defaults).")

    args = p.parse_args(argv)

    # Determine zero-padding width from the largest index
    max_index = args.start + args.count - 1
    pad = max(4, int(math.log10(max(1, max_index))) + 1)  # at least 4 digits for nice sorting

    # Pick face template (unless --no-face)
    if args.no_face:
        face_template = None
    elif args.face_template:
        face_template = args.face_template
    else:
        face_template = DEFAULT_FACE_BY_CULTURE.get(args.culture, DEFAULT_FACE_BY_CULTURE["_default"])

    root = ET.Element("NPCCharacters")

    for i in range(args.start, args.start + args.count):
        num = str(i).zfill(pad)
        npc_id = f"retinues_custom_{num}"
        npc = build_npc_character(
            npc_id=npc_id,
            culture=args.culture,
            default_group=args.group,
            level=args.level,
            face_key_template=face_template,
        )
        root.append(npc)

    tree = ET.ElementTree(root)

    # Pretty print (ElementTree doesn't indent by default in 3.8/3.9)
    try:
        ET.indent(tree, space="  ", level=0)  # Python 3.9+
    except AttributeError:
        # Fallback: manual pretty printer
        def _indent(elem, level=0):
            i = "\n" + level * "  "
            if len(elem):
                if not elem.text or not elem.text.strip():
                    elem.text = i + "  "
                for e in elem:
                    _indent(e, level + 1)
                if not e.tail or not e.tail.strip():
                    e.tail = i
            if level and (not elem.tail or not elem.tail.strip()):
                elem.tail = i
        _indent(root)

    tree.write(args.output, encoding="utf-8", xml_declaration=True)
    print(f"Wrote {args.count} stubs to {args.output}")


if __name__ == "__main__":
    sys.exit(main())
