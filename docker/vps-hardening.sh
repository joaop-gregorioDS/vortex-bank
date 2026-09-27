#!/bin/sh
set -eu

if [ "$(id -u)" -ne 0 ]; then
  echo "Execute na VPS como root." >&2
  exit 1
fi

ufw default deny incoming
ufw default allow outgoing
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 22/tcp
ufw --force enable

apt-get update
apt-get install -y fail2ban
cat >/etc/fail2ban/jail.d/bankcore.local <<'EOF'
[sshd]
enabled = true
port = ssh
maxretry = 5
bantime = 1h

[nginx-limit-req]
enabled = true
port = http,https
filter = nginx-limit-req
logpath = /var/log/nginx/error.log
maxretry = 10
findtime = 10m
bantime = 1h
EOF
systemctl enable --now fail2ban
fail2ban-client reload
echo "Firewall e Fail2ban ativos. O certificado precisa existir em /etc/letsencrypt/live/vortexbank/."
