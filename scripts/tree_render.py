import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

IGNORE = {'bin', 'obj', '.git', '.vs', 'Migrations'}

SOLUTION_ICON = "🟣"
FOLDER_ICON = "📁"
PROJECT_ICON = "📦"
FILE_ICON = "📄"


def get_project_files(proj_path, base_dir):

    proj_dir = (base_dir / proj_path).parent

    if not proj_dir.exists():
        return {}

    tree = {}

    for root, dirs, files in os.walk(proj_dir):

        dirs[:] = [d for d in dirs if d not in IGNORE]

        for file in files:

            if file.endswith('.csproj'):
                continue

            rel = Path(root).relative_to(proj_dir) / file

            current = tree

            for part in rel.parts[:-1]:
                current = current.setdefault(part, {})

            current[rel.parts[-1]] = None

    return tree


def build_slnx_tree(node, tree, base_dir):

    if node.tag == "Folder":

        name = node.attrib['Name'].strip('/')
        parts = [p for p in name.replace('\\', '/').split('/') if p]

        # Walk (or create) each path segment so that names like
        # "src/Aspire" are nested properly instead of used as a flat key.
        current = tree
        for part in parts:
            if part not in current or not isinstance(current[part], dict):
                current[part] = {"__icon__": FOLDER_ICON}
            current = current[part]

        for child in node:
            build_slnx_tree(child, current, base_dir)

    elif node.tag == "Project":

        path_str = node.attrib['Path']

        name = Path(path_str).stem

        # Show docker-compose without recursion
        if path_str.endswith('.dcproj'):
            tree[name] = {
                "__icon__": PROJECT_ICON
            }
            return

        files = get_project_files(path_str, base_dir)
        files["__icon__"] = PROJECT_ICON

        tree[name] = files


def _sort_key(item):
    """Folders first, then projects, then files — each group alphabetical."""
    key, value = item
    if isinstance(value, dict):
        icon = value.get("__icon__", FOLDER_ICON)
        group = 0 if icon == FOLDER_ICON else 1   # folders < projects
    else:
        group = 2                                   # files last
    return (group, key.lower())


def print_tree(data, prefix=""):

    items = sorted(
        [(k, v) for k, v in data.items() if k != "__icon__"],
        key=_sort_key,
    )

    for i, (key, value) in enumerate(items):

        is_last = i == len(items) - 1

        connector = "└── " if is_last else "├── "

        if isinstance(value, dict):

            icon = value.get("__icon__", FOLDER_ICON)

            print(f"{prefix}{connector}{icon} {key}")

            next_prefix = prefix + (
                "    " if is_last else "│   "
            )

            print_tree(value, next_prefix)

        else:
            print(f"{prefix}{connector}{FILE_ICON} {key}")


def main():

    target_dir = Path(
        sys.argv[1] if len(sys.argv) > 1 else "."
    ).resolve()

    slnx_files = list(target_dir.glob('*.slnx'))

    if not slnx_files:
        print("No .slnx found")
        return

    slnx_file = slnx_files[0]

    print(f"{SOLUTION_ICON} {slnx_file.stem}")

    tree = {}

    root = ET.parse(slnx_file).getroot()

    for child in root:
        build_slnx_tree(child, tree, target_dir)

    print_tree(tree)


if __name__ == "__main__":
    main()