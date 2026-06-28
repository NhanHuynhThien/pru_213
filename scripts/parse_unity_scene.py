import sys
import re


def parse_scene(path):
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()

    name_by_go = {}
    father_by_go = {}
    children_by_go = {}

    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        if line.startswith('--- !u!1 &'):
            m = re.search(r'--- !u!1 &(\d+)', line)
            goid = int(m.group(1)) if m else None
            j = i + 1
            name = None
            while j < n and not lines[j].startswith('--- !u!'):
                if 'm_Name:' in lines[j]:
                    name = lines[j].split('m_Name:')[1].strip()
                    break
                j += 1
            if goid is not None and name is not None:
                name_by_go[goid] = name
            i = j
            continue

        if line.startswith('--- !u!4 &'):
            j = i + 1
            goid = None
            father = 0
            child_ids = []
            block = []
            while j < n and not lines[j].startswith('--- !u!'):
                block.append(lines[j])
                j += 1
            block_text = ''.join(block)
            m_go = re.search(r'm_GameObject:\s*\{fileID:\s*(\d+)\}', block_text)
            if m_go:
                goid = int(m_go.group(1))
            m_f = re.search(r'm_Father:\s*\{fileID:\s*(\d+)\}', block_text)
            if m_f:
                father = int(m_f.group(1))
            child_ids = [int(x) for x in re.findall(r'fileID:\s*(\d+)', re.search(r'm_Children:\s*\[(.*?)\]', block_text, re.DOTALL).group(1))] if re.search(r'm_Children:\s*\[', block_text) else []

            if goid is not None:
                father_by_go[goid] = father
                children_by_go.setdefault(goid, child_ids)
            i = j
            continue

        i += 1

    for gid in name_by_go.keys():
        father_by_go.setdefault(gid, 0)
        children_by_go.setdefault(gid, [])

    for gid, childs in list(children_by_go.items()):
        for c in childs:
            father_by_go[c] = gid

    roots = [gid for gid, f in father_by_go.items() if f == 0]

    def print_tree(gid, indent=0, visited=None):
        if visited is None:
            visited = set()
        if gid in visited:
            print('  ' * indent + f"{name_by_go.get(gid, '?')} (loop)")
            return
        visited.add(gid)
        print('  ' * indent + f"{name_by_go.get(gid, str(gid))}")
        for c in children_by_go.get(gid, []):
            print_tree(c, indent+1, visited)

    if not roots:
        print('No root GameObjects detected; listing all objects:')
        for gid in sorted(name_by_go.keys()):
            print(f"{gid}: {name_by_go[gid]}")
        return

    for r in sorted(roots):
        print_tree(r)


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print('Usage: parse_unity_scene.py <path/to/scene.unity>')
        sys.exit(2)
    print('PARSER START')
    try:
        parse_scene(sys.argv[1])
    except Exception as e:
        import traceback
        print('ERROR:', e)
        traceback.print_exc()
