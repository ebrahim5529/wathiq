# نشر وثيق (Wathiq) — Docker + VPS + GitHub Actions

## الملفات المضافة

| ملف | الغرض |
|-----|--------|
| `Dockerfile` | بناء صورة التطبيق |
| `docker-compose.yml` | تشغيل Web + MySQL |
| `.env.example` | نموذج متغيرات البيئة |
| `.github/workflows/deploy.yml` | CI/CD: بناء → GHCR → نشر VPS |

---

## 1) إعداد المستودع على GitHub

```bash
cd /Users/user/Desktop/copmany/asp/HR
git init
git add .
git commit -m "Initial Wathiq archive system with Docker CI/CD"
# أنشئ مستودعاً فارغاً على GitHub ثم:
git branch -M main
git remote add origin https://github.com/USER/REPO.git
git push -u origin main
```

---

## 2) إعداد VPS (مرة واحدة)

المتطلبات: Docker + Docker Compose Plugin.

```bash
# مثال Ubuntu
sudo apt update
sudo apt install -y docker.io docker-compose-plugin
sudo usermod -aG docker $USER
```

انسخ ملفات النشر إلى السيرفر:

```bash
mkdir -p /opt/wathiq
# انسخ docker-compose.yml و .env إلى /opt/wathiq
```

أنشئ `.env` من المثال:

```bash
cp .env.example .env
nano .env
```

عدّل على الأقل:
- `MYSQL_ROOT_PASSWORD`
- `MYSQL_PASSWORD`
- `WEB_IMAGE=ghcr.io/USER/REPO:latest`

سجّل دخول GHCR على السيرفر (لحساب خاص أو PAT):

```bash
echo YOUR_GITHUB_PAT | docker login ghcr.io -u YOUR_GITHUB_USER --password-stdin
```

الصلاحية المطلوبة للـ PAT: `read:packages` (وللكتابة من Actions تُستخدم `GITHUB_TOKEN` تلقائياً).

---

## 3) أسرار GitHub Actions (Settings → Secrets and variables → Actions)

| Secret | القيمة |
|--------|--------|
| `VPS_HOST` | IP أو دومين السيرفر |
| `VPS_USER` | مستخدم SSH (مثل `root` أو `ubuntu`) |
| `VPS_SSH_KEY` | المفتاح الخاص SSH كاملاً |
| `VPS_PORT` | اختياري، افتراضي 22 |
| `VPS_APP_DIR` | مسار المشروع على السيرفر مثل `/opt/wathiq` |
| `GHCR_USER` | اسم مستخدم GitHub |
| `GHCR_TOKEN` | PAT بصلاحية `read:packages` |

> ملاحظة: حزمة الصورة تُرفع إلى `ghcr.io/<owner>/<repo>`. إن كان المستودع خاصاً، اجعل الحزمة قابلة للقراءة من حساب النشر أو استخدم نفس الـ PAT.

---

## 4) تدفق CI/CD

1. دفع إلى فرع `main`
2. بناء صورة Docker ودفعها إلى GitHub Container Registry
3. الاتصال بالـ VPS عبر SSH
4. `docker compose pull web && docker compose up -d`

يمكن أيضاً تشغيل الـ workflow يدوياً من تبويب Actions → **Build and Deploy** → Run workflow.

---

## 5) تشغيل محلي بـ Docker

```bash
cp .env.example .env
# عدّل كلمات المرور في .env
docker compose up -d --build
```

التطبيق: http://localhost:8080

---

## 6) Nginx (اختياري على الـ VPS)

```nginx
server {
    listen 80;
    server_name archive.example.com;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

ثم: `certbot --nginx -d archive.example.com`

---

## 7) أمان مهم

- لا ترفع ملف `.env` إلى GitHub
- غيّر كلمة مرور MySQL في `appsettings.json` المحلية ولا تعتمد عليها في الإنتاج
- الإنتاج يعتمد على `ConnectionStrings__DefaultConnection` من Docker Compose
