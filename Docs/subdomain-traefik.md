# ربط وثيق بـ Subdomain عبر Traefik

## 1) DNS
في لوحة الدومين أنشئ سجل:

| Type | Name | Value |
|------|------|--------|
| A | `archive` | IP السيرفر |

مثال: `archive.example.com` → `1.2.3.4`

انتظر انتشار DNS (دقائق إلى ساعة).

## 2) اعرف شبكة Traefik على السيرفر
```bash
docker network ls
docker inspect traefik --format '{{json .NetworkSettings.Networks}}' 2>/dev/null
# أو
docker ps --format '{{.Names}}' | grep -i traefik
docker inspect $(docker ps -qf name=traefik) --format '{{range $k,$v := .NetworkSettings.Networks}}{{$k}}{{"\n"}}{{end}}'
```

غالباً الاسم: `proxy` أو `traefik` أو `web`.

## 3) عدّل `.env` في مجلد المشروع
```env
WATHIQ_DOMAIN=ar.ebrahim-it.net
TRAEFIK_NETWORK=proxy
TRAEFIK_ENTRYPOINT=websecure
TRAEFIK_CERT_RESOLVER=letsencrypt
WEB_PUBLISH_PORT=8080
WEB_IMAGE=wathiq-web:local
```

إذا كانت أسماء entrypoint/certresolver مختلفة عندك، طابقها مع إعداد Traefik الحالي (انظر labels خدمة nextjs عندك).

## 4) طبّق
```bash
cd /opt/wathiq-src
git pull origin main
docker compose up -d --force-recreate
docker compose ps
```

## 5) افتح
```text
https://ar.ebrahim-it.net
```

## ملاحظة
إذا فشل `network proxy declared as external but could not be found`:
```bash
# غيّر TRAEFIK_NETWORK في .env إلى الاسم الحقيقي من docker network ls
```
