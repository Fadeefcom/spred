import json
import re
import time
import os
from pathlib import Path
from playwright.sync_api import sync_playwright

# Путь к директории с JSON-файлами
DATA_DIR = Path("D:/Fork/Spred/spred.api.aggregator/source/AggregatorService/saved_data")

# Собираем все json-файлы в папке
json_files = list(DATA_DIR.glob("*.json"))

def extract_number(text):
    if not text:
        return None
    match = re.search(r"([\d\s,.\u00a0]+)", text)
    return int(match.group(1).replace("\u00a0", "").replace(",", "").replace(" ", "")) if match else None

with sync_playwright() as p:
    browser = p.chromium.launch(headless=False)
    context = browser.new_context(
        locale="en-US",
        user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/113.0.0.0 Safari/537.36"
    )
    page = context.new_page()

    for file in json_files:
        print(f"\n🔄 Processing file: {file.name}")
        try:
            with open(file, encoding="utf-8") as f:
                data = json.load(f)
        except Exception as e:
            print(f"  ❌ Failed to load JSON: {e}")
            continue

        if "metdata" not in data:
            print("  ⚠️ No 'metdata' key found.")
            continue

        for i, item in enumerate(data["metdata"]):
            url = item.get("listenUrls", {}).get("spotify")
            if not url:
                print(f"not url {i}")
                continue

            if item.get("status") == 3:
                continue;

            print(f"  [{i+1}/{len(data['metdata'])}] Opening: {url}")
            try:
                page.goto(url, timeout=4000)
                page.wait_for_selector('span:has-text("saves")', timeout=2000)

                like_text = page.locator("span", has_text="saves").first.text_content()
                followers = extract_number(like_text)
                print(f"     -> Followers: {followers}")
                item["followers"] = followers

                time.sleep(1.5)
            except Exception as e:
                print(f"     !! Failed to get followers: {e}")
                item["followers"] = None

        # Перезаписываем тот же файл
        with open(file, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
            print("  ✅ File updated.")

    browser.close()

print("\n🎉 All done.")
