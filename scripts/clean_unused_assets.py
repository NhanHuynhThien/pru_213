import os
import re
import sys
import shutil

# Target packages to analyze and clean
TARGET_PACKAGES = [
    "Assets/Idyllic Fantasy Nature",
    "Assets/Naganeupseong",
    "Assets/Namhansanseong"
]

# Paths to ignore when scanning root files for references
IGNORE_DIRS = [
    "Library",
    "Temp",
    "Logs",
    "UserSettings",
    ".git",
    ".vs",
    ".gemini",
    "brain"
]

# Normalise paths to use forward slashes
def normalize_path(path):
    return os.path.normpath(path).replace('\\', '/')

def find_package_assets():
    package_assets = {} # guid -> {path, meta_path}
    path_to_guid = {} # path -> guid
    
    print("Scanning packages for assets...")
    for pkg in TARGET_PACKAGES:
        pkg_norm = normalize_path(pkg)
        if not os.path.exists(pkg_norm):
            print(f"Warning: Package directory {pkg_norm} does not exist.")
            continue
            
        for root, dirs, files in os.walk(pkg_norm):
            for file in files:
                if file.endswith('.meta'):
                    continue
                
                full_path = normalize_path(os.path.join(root, file))
                meta_path = full_path + '.meta'
                
                if os.path.exists(meta_path):
                    # Read GUID from meta file
                    guid = None
                    try:
                        with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
                            for line in f:
                                if 'guid:' in line:
                                    guid = line.split('guid:')[1].strip()
                                    break
                    except Exception as e:
                        print(f"Error reading meta file {meta_path}: {e}")
                    
                    if guid:
                        package_assets[guid] = {
                            'path': full_path,
                            'meta_path': meta_path
                        }
                        path_to_guid[full_path] = guid
                else:
                    # File has no meta. We will associate it with a dummy GUID so we can track it by path
                    dummy_guid = f"dummy_{full_path}"
                    package_assets[dummy_guid] = {
                        'path': full_path,
                        'meta_path': None
                    }
                    path_to_guid[full_path] = dummy_guid

    print(f"Found {len(package_assets)} assets in target packages.")
    return package_assets, path_to_guid

def find_root_files(project_dir):
    root_files = []
    print("Finding root files to search for references...")
    
    # We want to scan files in Assets/ that are NOT in target packages,
    # plus scripts/, Server/, UnityMCP/, and files in the root folder.
    for item in os.listdir(project_dir):
        item_path = normalize_path(os.path.join(project_dir, item))
        
        # Skip ignore directories
        if item in IGNORE_DIRS or item.startswith('.'):
            continue
            
        if os.path.isdir(item_path):
            # If it's Assets/, we only want to scan folders outside TARGET_PACKAGES
            if item == "Assets":
                for root_asset_item in os.listdir(item_path):
                    sub_path = normalize_path(os.path.join(item_path, root_asset_item))
                    is_pkg = False
                    for pkg in TARGET_PACKAGES:
                        if sub_path == normalize_path(os.path.join(project_dir, pkg)):
                            is_pkg = True
                            break
                    if not is_pkg:
                        # Scan this non-package folder/file recursively
                        if os.path.isdir(sub_path):
                            for r, d, fs in os.walk(sub_path):
                                d[:] = [dirname for dirname in d if dirname not in IGNORE_DIRS and dirname != 'node_modules' and not dirname.startswith('.')]
                                for f in fs:
                                    root_files.append(normalize_path(os.path.join(r, f)))
                        else:
                            root_files.append(sub_path)
            else:
                # Scan any other directory in the project recursively
                for r, d, fs in os.walk(item_path):
                    d[:] = [dirname for dirname in d if dirname not in IGNORE_DIRS and dirname != 'node_modules' and not dirname.startswith('.')]
                    for f in fs:
                        root_files.append(normalize_path(os.path.join(r, f)))
        else:
            # File in the project root
            root_files.append(item_path)
            
    print(f"Found {len(root_files)} root files to scan for references.")
    return root_files

def scan_references(root_files, package_assets):
    used_guids = set()
    used_paths = set()
    
    # We want to search for both GUIDs and relative paths of package assets in the root files
    guids_to_search = set(package_assets.keys())
    paths_to_search = {info['path']: guid for guid, info in package_assets.items()}
    
    print("Scanning root files for references to package assets...")
    count = 0
    for file_path in root_files:
        # Skip binary files if they are huge and not assets
        if file_path.endswith(('.dll', '.exe', '.png', '.jpg', '.tga', '.wav', '.mp3', '.fbx', '.obj', '.zip', '.pdf')):
            continue
            
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                
                # Check for GUIDs
                for guid in list(guids_to_search):
                    if guid in content:
                        used_guids.add(guid)
                        guids_to_search.remove(guid) # Found, no need to search again
                        
                # Check for paths
                for path in list(paths_to_search.keys()):
                    if path in content or os.path.basename(path) in content:
                        # To be safe, if the path or the specific filename with extension is in the file
                        # (e.g. Bamboo_Rafter_House_01.prefab), we mark it as used.
                        guid = paths_to_search[path]
                        used_guids.add(guid)
                        if guid in guids_to_search:
                            guids_to_search.remove(guid)
                        del paths_to_search[path]
        except Exception as e:
            print(f"Error reading file {file_path}: {e}")
            
        count += 1
        if count % 100 == 0:
            print(f"Scanned {count}/{len(root_files)} root files...")
            
    print(f"Directly referenced assets found: {len(used_guids)}")
    return used_guids

def trace_dependencies(used_guids, package_assets):
    print("Tracing transitive dependencies (closure)...")
    queue = list(used_guids)
    visited = set(used_guids)
    
    # Map of all asset files to their content (loaded lazily)
    # To speed up, we look for GUIDs in the text-based package assets (prefabs, materials, controllers, etc.)
    TEXT_EXTENSIONS = ('.prefab', '.mat', '.asset', '.unity', '.controller', '.anim', '.overrideController', '.physicsMaterial2D', '.physicMaterial', '.guiskin', '.fontsettings', '.spriteatlas')
    
    # Pre-build a map of all target package GUIDs for fast lookup
    all_pkg_guids = set(package_assets.keys())
    
    steps = 0
    while queue:
        current_guid = queue.pop(0)
        info = package_assets.get(current_guid)
        if not info:
            continue
            
        file_path = info['path']
        if not file_path.endswith(TEXT_EXTENSIONS):
            continue
            
        # Read the file and look for referenced GUIDs
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                # Find all 32-char hex strings which are GUIDs
                # Unity GUIDs are 32 hex chars
                found_guids = re.findall(r'guid:\s*([a-fA-F0-9]{32})', content)
                for g in found_guids:
                    if g in all_pkg_guids and g not in visited:
                        visited.add(g)
                        queue.append(g)
        except Exception as e:
            print(f"Error reading asset file {file_path}: {e}")
            
        steps += 1
        if steps % 100 == 0:
            print(f"Processed {steps} files in transitive closure...")
            
    print(f"Total used assets after closure: {len(visited)}")
    return visited

def clean_unused(project_dir, package_assets, used_guids):
    unused_files = []
    unused_meta = []
    
    for guid, info in package_assets.items():
        if guid not in used_guids:
            unused_files.append(info['path'])
            if info['meta_path']:
                unused_meta.append(info['meta_path'])
                
    total_size = sum(os.path.getsize(f) for f in unused_files if os.path.exists(f))
    size_mb = total_size / (1024 * 1024)
    
    print("\n--- Cleanup Summary ---")
    print(f"Total unused files: {len(unused_files)}")
    print(f"Total size to reclaim: {size_mb:.2f} MB")
    
    # Perform deletion if confirmed
    force_delete = len(sys.argv) > 1 and sys.argv[1] in ('--force', '-y', '/y')
    confirm = 'yes' if force_delete else input("Do you want to delete these unused files? (yes/no): ").strip().lower()
    if confirm == 'yes':
        deleted_count = 0
        for f in unused_files:
            try:
                if os.path.exists(f):
                    os.remove(f)
                    deleted_count += 1
            except Exception as e:
                print(f"Error deleting {f}: {e}")
                
        for m in unused_meta:
            try:
                if os.path.exists(m):
                    os.remove(m)
            except Exception as e:
                print(f"Error deleting meta file {m}: {e}")
                
        print(f"Successfully deleted {deleted_count} files.")
        
        # Clean up empty folders recursively
        clean_empty_folders(project_dir)
    else:
        print("Deletion cancelled.")

def clean_empty_folders(project_dir):
    print("Cleaning up empty folders...")
    for pkg in TARGET_PACKAGES:
        pkg_norm = normalize_path(os.path.join(project_dir, pkg))
        if not os.path.exists(pkg_norm):
            continue
            
        # Walk bottom-up
        for root, dirs, files in os.walk(pkg_norm, topdown=False):
            for d in dirs:
                dir_path = normalize_path(os.path.join(root, d))
                # Check if directory is empty or only contains other empty directories
                try:
                    if not os.listdir(dir_path):
                        print(f"Deleting empty folder: {dir_path}")
                        os.rmdir(dir_path)
                        meta_file = dir_path + '.meta'
                        if os.path.exists(meta_file):
                            os.remove(meta_file)
                except Exception as e:
                    print(f"Error deleting folder {dir_path}: {e}")

def main():
    project_dir = normalize_path(os.getcwd())
    print(f"Project directory: {project_dir}")
    
    package_assets, path_to_guid = find_package_assets()
    if not package_assets:
        print("No package assets found.")
        return
        
    root_files = find_root_files(project_dir)
    directly_used = scan_references(root_files, package_assets)
    all_used = trace_dependencies(directly_used, package_assets)
    
    clean_unused(project_dir, package_assets, all_used)

if __name__ == '__main__':
    main()
