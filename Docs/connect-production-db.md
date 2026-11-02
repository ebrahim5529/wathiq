# الاتصال بقاعدة بيانات الإنتاج

## بيانات الاتصال (من `.env` على السيرفر)

| الحقل | القيمة |
|------|--------|
| Host | `127.0.0.1` عبر SSH Tunnel |
| Port | `3307` |
| Database | `HR_Archive` |
| User | `wathiq` |
| Password | من ملف `/opt/wathiq-src/.env` → `MYSQL_PASSWORD` |

## 1) على السيرفر: طبّق منفذ localhost

```bash
cd /opt/wathiq-src
git pull origin main
docker compose up -d db
docker compose ps
ss -tlnp | grep 3307
```

## 2) من جهازك: نفق SSH

```bash
ssh -L 3307:127.0.0.1:3307 root@31.97.37.248
```

اترك هذه النافذة مفتوحة.

## 3) من MySQL Workbench / DBeaver / Rider

- Host: `127.0.0.1`
- Port: `3307`
- User: `wathiq`
- Password: قيمة `MYSQL_PASSWORD`
- Database: `HR_Archive`

## اتصال سريع من داخل السيرفر

```bash
docker exec -it wathiq-db mysql -uwathiq -p HR_Archive
```
