#!/usr/bin/env python3
"""Creates deploy/monsterasp.secrets.json (git-ignored) with generated values for the first admin password
and the IP hashing key. Fill in Ftp.Password and the SQL Server connection string afterwards.
Never overwrites an existing file."""
import json, os, secrets, string, sys

HERE = os.path.dirname(os.path.abspath(__file__))
target = os.path.join(HERE, "monsterasp.secrets.json")
if os.path.exists(target):
    sys.exit(f"{target} already exists; not overwriting.")

def password(n=20):
    # Satisfies the Identity policy: upper, lower, digit and symbol, length >= 10.
    alphabet = string.ascii_letters + string.digits + "!#%*+-=?@^_"
    while True:
        p = "".join(secrets.choice(alphabet) for _ in range(n))
        if (any(c.islower() for c in p) and any(c.isupper() for c in p)
                and any(c.isdigit() for c in p) and any(not c.isalnum() for c in p)):
            return p

data = json.load(open(os.path.join(HERE, "monsterasp.secrets.example.json"), encoding="utf-8"))
data["Ftp"]["Password"] = ""
data["Production"]["ConnectionStrings"]["DefaultConnection"] = ""
data["Production"]["Seed"]["AdminPassword"] = password()
data["Production"]["Security"]["IpHashKey"] = secrets.token_urlsafe(48)
fd = os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(fd, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2, ensure_ascii=False)
print(f"created {target} (mode 600). Fill in Ftp.Password and Production.ConnectionStrings.DefaultConnection.")
