#!/usr/bin/env python3
"""Adds or updates UI strings in both resource files.

The .resx files remain the source of truth; this tool only saves typing when adding many keys.
Usage: python3 tools/upsert_resx.py strings.json   where strings.json = {"Key": ["English", "العربية"], ...}
"""
import json, os, sys, xml.etree.ElementTree as ET
from xml.sax.saxutils import escape

DIR = os.path.join(os.path.dirname(__file__), "..", "src", "NationalAirspace.Web", "Resources", "Localization")
FILES = {0: "SharedResource.resx", 1: "SharedResource.ar.resx"}
HEADER = """<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
"""

def load(path):
    if not os.path.exists(path):
        return {}
    return {e.get("name"): (e.find("value").text or "") for e in ET.parse(path).getroot().findall("data")}

def save(path, data):
    with open(path, "w", encoding="utf-8") as f:
        f.write(HEADER)
        for k in sorted(data, key=str.lower):
            f.write(f'  <data name="{escape(k, {chr(34): "&quot;"})}" xml:space="preserve"><value>{escape(data[k])}</value></data>\n')
        f.write("</root>\n")

if __name__ == "__main__":
    os.makedirs(DIR, exist_ok=True)
    updates = json.load(open(sys.argv[1], encoding="utf-8"))
    for idx, name in FILES.items():
        path = os.path.join(DIR, name)
        data = load(path)
        for key, values in updates.items():
            data[key] = values[idx]
        save(path, data)
    print(f"upserted {len(updates)} keys")
