# -*- coding: utf-8 -*-
"""Zepp/Huami step submitter — credentials from env only."""

import json
import os
import random
import re
import sys
import time
from datetime import datetime
from pathlib import Path
from urllib.parse import quote

import requests

BASE_DIR = Path(__file__).resolve().parent
DATA_FILE = BASE_DIR / "data.txt"
STATE_FILE = BASE_DIR / "bushu.json"

UA = "MiFit/6.12.0 (MCE16; Android 16; Density/1.5)"
HEADERS_UA = {"User-Agent": UA, "app_name": "com.xiaomi.hm.health"}
PHONE_RE = re.compile(r"^(1)\d{10}$")


def mask_account(user: str) -> str:
    u = str(user)
    if len(u) <= 8:
        return u[:2] + "****" + u[-2:] if len(u) >= 4 else "****"
    return f"{u[:4]}****{u[-4:]}"


def get_time() -> str:
    try:
        r = requests.get(
            "https://acs.m.taobao.com/gw/mtop.common.getTimestamp/",
            headers=HEADERS_UA,
            timeout=10,
        )
        t = r.json().get("data", {}).get("t")
        if t:
            return str(t)
    except Exception:
        pass
    return str(int(datetime.now().timestamp() * 1000))


def login(user: str, password: str):
    """Modern Zepp login (api-user.zepp.com JSON). Returns (app_token, userid) or (0, 0)."""
    login.last_error = ""
    if PHONE_RE.match(user):
        login_account = "+86" + user
        third_name = "huami_phone"
    else:
        login_account = user
        third_name = "huami"

    url1 = "https://api-user.zepp.com/registrations/" + quote(login_account, safe="") + "/tokens"
    headers = {
        "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
        "User-Agent": UA,
        "app_name": "com.xiaomi.hm.health",
    }
    data1 = {
        "client_id": "HuaMi",
        "country_code": "CN",
        "json_response": "true",
        "name": login_account,
        "password": password,
        "redirect_uri": "https://s3-us-west-2.amazonaws.com/hm-registration/successsignin.html",
        "state": "REDIRECTION",
        "token": "access",
    }
    r1 = requests.post(url1, data=data1, headers=headers, timeout=30)
    if r1.status_code == 429:
        login.last_error = "HTTP 429 too many requests"
        return 0, 0
    try:
        token_json = r1.json()
    except Exception:
        login.last_error = f"HTTP {r1.status_code} non-json token response"
        return 0, 0
    if not r1.ok:
        login.last_error = f"HTTP {r1.status_code} {token_json.get('message', token_json)}"
        return 0, 0
    access_code = token_json.get("access")
    if not access_code:
        # legacy Location fallback
        location = r1.headers.get("Location") or ""
        m = re.search(r"(?<=access=).*?(?=&)", location)
        access_code = m.group(0) if m else None
    if not access_code:
        login.last_error = "no access code"
        return 0, 0

    url2 = "https://account.zepp.com/v2/client/login"
    data2 = {
        "app_name": "com.xiaomi.hm.health",
        "country_code": "CN",
        "code": access_code,
        "device_id": "fuck1069-2002-7869-0129-757geoi6sam1",
        "device_model": "android_phone",
        "app_version": "6.12.0",
        "grant_type": "access_token",
        "allow_registration": "false",
        "dn": (
            "account.zepp.com,api-user.zepp.com,api-mifit.zepp.com,"
            "api-watch.zepp.com,app-analytics.zepp.com,api-analytics.huami.com,auth.zepp.com"
        ),
        "source": "com.xiaomi.hm.health",
        "third_name": third_name,
    }
    r2 = requests.post(url2, data=data2, headers=headers, timeout=30)
    try:
        login_json = r2.json()
    except Exception:
        login.last_error = f"HTTP {r2.status_code} non-json login response"
        return 0, 0
    if not r2.ok:
        login.last_error = f"login HTTP {r2.status_code} {login_json.get('message', login_json)}"
        return 0, 0
    token_info = login_json.get("token_info") or {}
    app_token = token_info.get("app_token")
    userid = token_info.get("user_id")
    if not app_token or not userid:
        login.last_error = "missing app_token/user_id"
        return 0, 0
    return app_token, userid


def submit_steps(user: str, passwd: str, step: int) -> tuple:
    """Return (ok: bool, message: str). Never includes password."""
    user = str(user)
    password = str(passwd)
    step_s = str(int(step))
    now = time.strftime("%Y-%m-%d %H:%M:%S", time.localtime())
    masked = mask_account(user)

    if not user or not password:
        return False, f"{masked}: 用户名或密码未配置"

    app_token, userid = login(user, password)
    if app_token == 0:
        extra = getattr(login, "last_error", "") or "登陆失败"
        return False, f"{masked}: [{now}] 登陆失败 ({extra})"

    t = get_time()
    today = time.strftime("%F")

    if not DATA_FILE.is_file():
        return False, f"{masked}: data.txt 不存在 ({DATA_FILE})"

    data_json = DATA_FILE.read_text(encoding="utf-8", errors="replace")
    finddate = re.compile(r".*?date%22%3A%22(.*?)%22%2C%22data.*?")
    findstep = re.compile(r".*?ttl%5C%22%3A(.*?)%2C%5C%22dis.*?")
    dates = finddate.findall(data_json)
    steps = findstep.findall(data_json)
    if not dates or not steps:
        return False, f"{masked}: data.txt 格式无法解析"
    data_json = re.sub(dates[0], today, str(data_json), count=1)
    data_json = re.sub(steps[0], step_s, str(data_json), count=1)

    url = f"https://api-mifit-cn.huami.com/v1/data/band_data.json?&t={t}"
    head = {
        "apptoken": app_token,
        "Content-Type": "application/x-www-form-urlencoded",
        "User-Agent": UA,
    }
    data = (
        f"userid={userid}&last_sync_data_time=1597306380&device_type=0"
        f"&last_deviceid=DA932FFFFE8816E7&data_json={data_json}"
    )
    response = requests.post(url, data=data, headers=head, timeout=30).json()
    msg = response.get("message", str(response))
    line = f"{masked}: [{now}] 修改步数（{step_s}）{msg}"
    ok = str(msg).lower() == "success"
    return ok, line


def compute_step() -> int:
    step_min = int(os.environ.get("ZEPP_STEP_MIN", "100"))
    step_max = int(os.environ.get("ZEPP_STEP_MAX", "400"))
    if step_min > step_max:
        step_min, step_max = step_max, step_min
    step = random.randint(step_min, step_max)
    try:
        if STATE_FILE.is_file():
            data = json.loads(STATE_FILE.read_text(encoding="utf-8"))
            if data.get("time"):
                last_time = datetime.fromisoformat(data["time"])
                if last_time.date() == datetime.today().date():
                    step += int(data.get("step") or 0)
    except Exception:
        pass
    step = max(step_min, min(step_max, step))
    STATE_FILE.write_text(
        json.dumps({"step": step, "time": datetime.now().isoformat()}),
        encoding="utf-8",
    )
    return step


def maybe_telegram(title: str, content: str) -> None:
    tg_bot_token = os.environ.get("TG_BOT_TOKEN", "").strip()
    tg_user_id = os.environ.get("TG_USER_ID", "").strip()
    if not tg_bot_token or not tg_user_id:
        return
    try:
        requests.post(
            f"https://api.telegram.org/bot{tg_bot_token}/sendMessage",
            data={
                "chat_id": tg_user_id,
                "text": title + "\n\n" + content,
                "disable_web_page_preview": "true",
            },
            timeout=15,
        )
    except Exception:
        pass


def main() -> int:
    user = os.environ.get("ZEPP_USER", "").strip()
    passwd = os.environ.get("ZEPP_PASS", "").strip()
    if not user or not passwd:
        print("缺少环境变量 ZEPP_USER / ZEPP_PASS", file=sys.stderr)
        return 2
    try:
        step = compute_step()
        ok, message = submit_steps(user, passwd, step)
        print(message)
        maybe_telegram("Zepp 刷步", message)
        return 0 if ok else 1
    except Exception as e:
        err = f"{mask_account(user)}: 异常 {type(e).__name__}: {e}"
        print(err, file=sys.stderr)
        print(err)
        return 1


if __name__ == "__main__":
    sys.exit(main())
