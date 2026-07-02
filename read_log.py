import os
import sys

if hasattr(sys.stdout, 'reconfigure'):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except:
        pass

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    matches = []
    for idx, line in enumerate(lines):
        if "ApplySkinByTier gọi với tier" in line or "ApplyTierBonuses" in line or "U Sử dụng mô hình mặc định" in line or "Sử dụng mô hình mặc định" in line:
            matches.append(idx)
            
    print(f"Found {len(matches)} matches.")
    for idx in matches[-15:]:
        print("\n--- MATCH ---")
        for j in range(12):
            if idx + j < len(lines):
                text = lines[idx + j].strip()
                try:
                    print(text)
                except UnicodeEncodeError:
                    print(text.encode('ascii', 'replace').decode('ascii'))
else:
    print("Log not found")
