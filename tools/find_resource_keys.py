#!/usr/bin/env python3
"""Lists localization keys referenced in code/views and reports keys missing from the .resx files.

Besides literal lookups (L["Key"]), it derives keys that are built at runtime:
  * Enum.{Type}.{Value}   for every enum in Domain/Enums (rendered through L.Enum / L.EnumOptions)
  * Field.{Property}      for every property of *Input / *Model form classes (display-name metadata)
  * Role.{Role}[.Description] for every role in Roles.All

Usage: python3 tools/find_resource_keys.py [--missing]
"""
import os, re, sys, xml.etree.ElementTree as ET

ROOT = os.path.join(os.path.dirname(__file__), "..", "src")
RESX_DIR = os.path.join(ROOT, "NationalAirspace.Web", "Resources", "Localization")
KEY = r'[A-Z][A-Za-z]+\.[A-Za-z0-9.]+'
PATTERNS = [
    re.compile(r'L\["([A-Za-z0-9_.]+)"'),
    re.compile(r'localizer\["([A-Za-z0-9_.]+)"'),
    re.compile(r'(?:Error|Success|AfterCommand)\([^)]*?"(' + KEY + r')"'),
    re.compile(r'new Error\("(' + KEY + r')"'),
    re.compile(r'ErrorMessage = "(' + KEY + r')"'),
    re.compile(r'(?:hint|label|empty|placeholder|text|confirm|title)="(' + KEY + r')"'),
]
# String literals that look like keys are counted when their first segment is a known key namespace.
LITERAL = re.compile(r'"(' + KEY + r')"')
# View-model properties whose labels are never rendered (ids, lookups, hidden/bot-trap fields).
HIDDEN_FIELDS = {"Id", "ConcurrencyStamp", "PageId", "SystemKey", "Input", "IsNew", "Options", "SecondaryOptions",
                 "FormToken", "Website", "Sent", "Site"}
# "Input.X" literals are ModelState keys, not resource keys.
NOT_KEY_NAMESPACES = {"Input"}


def source_files():
    for d, _, files in os.walk(ROOT):
        if "/obj" in d or "/bin" in d or "wwwroot" in d:
            continue
        for f in files:
            if f.endswith((".cs", ".cshtml")):
                path = os.path.join(d, f)
                yield path, open(path, encoding="utf-8").read()


def enum_keys():
    path = os.path.join(ROOT, "NationalAirspace.Domain", "Enums", "Enums.cs")
    text = open(path, encoding="utf-8").read()
    keys = set()
    for name, body in re.findall(r'public enum (\w+)\s*\{([^}]*)\}', text):
        body = re.sub(r'///.*', '', body)
        for member in re.findall(r'^\s*(\w+)\s*(?:=\s*\d+)?\s*,?', body, re.M):
            keys.add(f"Enum.{name}.{member}")
    return keys


def field_keys(files):
    keys = set()
    for path, text in files:
        if not path.endswith(".cs"):
            continue
        for cls, body in re.findall(r'public (?:sealed )?class (\w+(?:Input|Model))\b[^{]*\{(.*?)\n\}', text, re.S):
            for prop in re.findall(r'public [\w<>?\[\], ]+ (\w+) \{ get; set; \}', body):
                if prop not in HIDDEN_FIELDS:
                    keys.add(f"Field.{prop}")
    return keys


def role_keys(files):
    for path, text in files:
        if path.endswith("Permissions.cs"):
            roles = re.findall(r'public const string \w+ = "(\w+)";', text.split("class Permissions")[0])
            return {k for r in roles for k in (f"Role.{r}", f"Role.{r}.Description")}
    return set()


def resx_keys(name):
    path = os.path.join(RESX_DIR, name)
    if not os.path.exists(path):
        return set()
    return {e.get("name") for e in ET.parse(path).getroot().findall("data")}


def used_keys():
    files = list(source_files())
    keys = set()
    for _, text in files:
        for p in PATTERNS:
            keys.update(p.findall(text))
    namespaces = {k.split(".")[0] for k in keys | resx_keys("SharedResource.resx")}
    for path, text in files:
        if "/Migrations/" in path:
            continue
        keys.update(k for k in LITERAL.findall(text) if k.split(".")[0] in namespaces)
    keys |= enum_keys() | field_keys(files) | role_keys(files)
    return {k for k in keys if not k.endswith(".") and k.split(".")[0] not in NOT_KEY_NAMESPACES}


if __name__ == "__main__":
    used = used_keys()
    if "--missing" in sys.argv:
        en, ar = resx_keys("SharedResource.resx"), resx_keys("SharedResource.ar.resx")
        for k in sorted(used - en): print("missing en:", k)
        for k in sorted(used - ar): print("missing ar:", k)
    else:
        print("\n".join(sorted(used)))
