#!/usr/bin/env python3
"""Publishes the web app and uploads it to MonsterASP.NET over FTPS (explicit TLS).

WebDeploy (msdeploy) only runs on Windows; FTPS works from macOS/Linux. Steps:
  1. dotnet publish (Release, framework-dependent)
  2. write appsettings.Production.json from deploy/monsterasp.secrets.json into the publish output only
  3. upload (curl, FTPS) app_offline.htm (stops the app so its DLLs can be replaced), upload all files, remove app_offline.htm

Files are only added/overwritten - never deleted - so uploads (wwwroot/uploads), App_Data (data-protection
keys, documents) and other server-side state survive redeploys.

Usage: python3 deploy/deploy_monsterasp.py [--skip-publish] [--dry-run]
"""
import json, os, subprocess, sys, tempfile, time
from urllib.parse import quote

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SECRETS = os.path.join(ROOT, "deploy", "monsterasp.secrets.json")
OUT = os.path.join(ROOT, "artifacts", "publish", "web")
REMOTE_ROOT = "/wwwroot"
DOTNET = os.environ.get("DOTNET", os.path.expanduser("~/.dotnet/dotnet"))
OFFLINE_HTML = (b'<!doctype html><html lang="ar" dir="rtl"><meta charset="utf-8"><title>Maintenance</title>'
                b'<body style="font-family:sans-serif;text-align:center;padding:4rem">'
                b'<h1>\xd8\xac\xd8\xa7\xd8\xb1\xd9\x8d \xd8\xa7\xd9\x84\xd8\xaa\xd8\xad\xd8\xaf\xd9\x8a\xd8\xab</h1>'
                b'<p>The website is being updated. Please try again in a few minutes.</p></body></html>')


def load_secrets():
    if not os.path.exists(SECRETS):
        sys.exit("deploy/monsterasp.secrets.json is missing - run: python3 deploy/init_secrets.py")
    s = json.load(open(SECRETS, encoding="utf-8"))
    missing = [name for name, value in [
        ("Ftp.Password", s["Ftp"].get("Password")),
        ("Production.ConnectionStrings.DefaultConnection", s["Production"]["ConnectionStrings"].get("DefaultConnection")),
    ] if not value]
    if missing:
        sys.exit("Fill in " + ", ".join(missing) + " in deploy/monsterasp.secrets.json")
    return s


def publish():
    project = os.path.join(ROOT, "src", "NationalAirspace.Web", "NationalAirspace.Web.csproj")
    subprocess.run([DOTNET, "publish", project, "-c", "Release", "-o", OUT, "--nologo"], check=True)
    # Not needed (or wanted) on the server.
    for name in ("appsettings.Development.json",):
        path = os.path.join(OUT, name)
        if os.path.exists(path):
            os.remove(path)


def write_production_settings(secrets_):
    with open(os.path.join(OUT, "appsettings.Production.json"), "w", encoding="utf-8") as f:
        json.dump(secrets_["Production"], f, indent=2, ensure_ascii=False)


def _curl(secrets_, config_lines, extra=()):
    """Runs curl over explicit FTPS with credentials and commands in temporary 0600 config files
    (keeps the password out of the process list). curl reuses the TLS session for data connections,
    which SFTPGo requires (Python's ftplib does not)."""
    ftp = secrets_["Ftp"]
    with tempfile.TemporaryDirectory() as tmp:
        cred = os.path.join(tmp, "cred.cfg")
        cmds = os.path.join(tmp, "cmds.cfg")
        for path, text in ((cred, f'user = {_q(ftp["User"] + ":" + ftp["Password"])}\n'), (cmds, "\n".join(config_lines) + "\n")):
            fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                f.write(text)
        return subprocess.run(["curl", "--silent", "--show-error", "--fail", "--ssl-reqd", "--ftp-create-dirs",
                                "--retry", "3", "-K", cred, "-K", cmds, *extra], check=False).returncode


def _q(value):
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def upload(secrets_, dry_run):
    files = []
    for base, _, names in os.walk(OUT):
        for n in names:
            local = os.path.join(base, n)
            files.append((local, REMOTE_ROOT + "/" + os.path.relpath(local, OUT).replace(os.sep, "/")))
    # Libraries first, web.config last, so the app restarts against a complete file set.
    files.sort(key=lambda f: (f[1].endswith("/web.config"), f[1]))
    print(f"{len(files)} files to upload")
    if dry_run:
        return
    base_url = f"ftp://{secrets_['Ftp']['Host']}"
    with tempfile.TemporaryDirectory() as tmp:
        offline = os.path.join(tmp, "app_offline.htm")
        with open(offline, "wb") as f:
            f.write(OFFLINE_HTML)
        if _curl(secrets_, [f"upload-file = {_q(offline)}", f"url = {_q(base_url + REMOTE_ROOT + '/app_offline.htm')}"]) != 0:
            sys.exit("could not upload app_offline.htm")
    time.sleep(5)  # let IIS shut the app down and release file locks
    failed = False
    try:
        batch = 100
        for i in range(0, len(files), batch):
            lines = []
            for local, remote in files[i:i + batch]:
                lines += [f"upload-file = {_q(local)}", f"url = {_q(base_url + quote(remote))}"]
            if _curl(secrets_, lines) != 0:
                failed = True
                print("upload batch failed", file=sys.stderr)
                break
            print(f"  uploaded {min(i + batch, len(files))}/{len(files)}")
    finally:
        if not failed:
            _curl(secrets_, [f"url = {_q(base_url + REMOTE_ROOT + '/')}", f"quote = {_q('DELE ' + REMOTE_ROOT + '/app_offline.htm')}"], ["--list-only", "-o", os.devnull])
    if failed:
        sys.exit("deploy incomplete: app_offline.htm left in place so visitors see the maintenance page; rerun the deploy")


if __name__ == "__main__":
    s = load_secrets()
    if "--skip-publish" not in sys.argv:
        publish()
    write_production_settings(s)
    upload(s, "--dry-run" in sys.argv)
    print("done")
