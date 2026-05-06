# 🚀 Quick Fix for Docker DNS Issue

## ✅ What I Fixed
1. Removed obsolete `version: '3.8'` from docker-compose files (warning fixed)
2. Created comprehensive DNS troubleshooting guide

## 🔧 IMMEDIATE ACTION REQUIRED

Your Docker Desktop cannot resolve DNS. Follow these steps:

### Step 1: Configure Docker DNS (2 minutes)

1. **Open Docker Desktop**
2. **Click Settings (⚙️ gear icon)** in top-right corner
3. **Click "Docker Engine"** in left sidebar
4. **You'll see JSON configuration**
5. **Add the `"dns"` line** to the JSON:

```json
{
  "builder": {
    "gc": {
      "defaultKeepStorage": "20GB",
      "enabled": true
    }
  },
  "experimental": false,
  "dns": ["8.8.8.8", "8.8.4.4"]
}
```

6. **Click "Apply & Restart"** button at bottom
7. **Wait 30-60 seconds** for Docker to restart

### Step 2: Verify DNS Works

Open PowerShell and run:

```powershell
docker run --rm alpine nslookup mcr.microsoft.com
```

**Expected output:**
```
Server:    8.8.8.8
Address:   8.8.8.8:53

Name:      mcr.microsoft.com
Address:   150.171.69.10
```

If you see IP addresses, DNS is working! ✅

### Step 3: Build and Run SmartSure

```bash
# Clean up any partial builds
docker compose down -v

# Build all services
docker compose build

# Start all services
docker compose up -d

# Check status
docker compose ps
```

---

## 🎯 Expected Result

After fixing DNS, you should see:

```
[+] Building 120.5s (45/45) FINISHED
 => [identity-service] pulling mcr.microsoft.com/dotnet/aspnet:10.0
 => [policy-service] pulling mcr.microsoft.com/dotnet/aspnet:10.0
 => [frontend] pulling node:20-alpine
 ...

[+] Running 9/9
 ✔ Container smartsure-sqlserver         Started
 ✔ Container smartsure-rabbitmq          Started
 ✔ Container smartsure-identity          Started
 ✔ Container smartsure-policy            Started
 ✔ Container smartsure-claims            Started
 ✔ Container smartsure-admin             Started
 ✔ Container smartsure-gateway           Started
 ✔ Container smartsure-frontend          Started
```

---

## 🌐 Access Your Application

Once all containers are running:

- **Frontend**: http://localhost:4200
- **API Gateway**: http://localhost:5000
- **RabbitMQ Management**: http://localhost:15672 (user: smartsure, pass: smartsure123)
- **SQL Server**: localhost:1433 (user: sa, pass: SmartSure@2025!)

---

## ❌ If DNS Fix Doesn't Work

### Alternative DNS Servers

Try Cloudflare DNS instead:

```json
"dns": ["1.1.1.1", "1.0.0.1"]
```

### Or Your ISP DNS

Find your DNS servers:

```powershell
Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object {$_.InterfaceAlias -notlike "*Loopback*"}
```

Use those IP addresses in Docker settings.

### Nuclear Option: Restart Everything

```powershell
# Quit Docker Desktop (right-click system tray icon)
# Wait 10 seconds
# Start Docker Desktop again

# Then flush DNS
ipconfig /flushdns

# Try building again
docker compose build
```

---

## 📞 Still Having Issues?

Check if you're:
- ❓ Behind a corporate firewall/proxy
- ❓ Using VPN (disconnect and try)
- ❓ Have antivirus blocking Docker (temporarily disable)

See `docker-dns-fix.md` for detailed troubleshooting.

---

## ✅ Checklist

- [ ] Added DNS to Docker Engine settings
- [ ] Clicked "Apply & Restart"
- [ ] Waited for Docker to restart
- [ ] Tested DNS with `docker run --rm alpine nslookup mcr.microsoft.com`
- [ ] Ran `docker compose build`
- [ ] Ran `docker compose up -d`
- [ ] Checked `docker compose ps` - all services running
- [ ] Opened http://localhost:4200 in browser

---

Good luck! The DNS fix should resolve your issue. 🎉
