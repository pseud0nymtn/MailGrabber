#!/bin/sh
set -eu

APP_ID="io.github.thomasn.MailGrabber"

CONFIG_BASE="${XDG_CONFIG_HOME:-$HOME/.var/app/$APP_ID/config}"
DATA_BASE="${XDG_DATA_HOME:-$HOME/.var/app/$APP_ID/data}"
CONFIG_DIR="$CONFIG_BASE/mailgrabber"
DATA_DIR="$DATA_BASE/mailgrabber"

mkdir -p "$CONFIG_DIR" "$DATA_DIR/output" "$DATA_DIR/gmail-token"

if [ ! -f "$CONFIG_DIR/appsettings.json" ] && [ -f /app/lib/mailgrabber/appsettings.example.json ]; then
    cp /app/lib/mailgrabber/appsettings.example.json "$CONFIG_DIR/appsettings.json"
fi

if [ ! -f "$CONFIG_DIR/google-client-secret.json" ] && [ -f /app/lib/mailgrabber/google-client-secret.json ]; then
    cp /app/lib/mailgrabber/google-client-secret.json "$CONFIG_DIR/google-client-secret.json"
fi

export MAILGRABBER_GMAIL_CLIENT_SECRETS_PATH="$CONFIG_DIR/google-client-secret.json"
export MAILGRABBER_GMAIL_TOKEN_DIRECTORY="$DATA_DIR/gmail-token"
export MAILGRABBER_OUTPUT_PATH="$DATA_DIR/output/sender-clusters.csv"
export MAILGRABBER_JSON_OUTPUT_PATH="$DATA_DIR/output/sender-clusters.json"
export MAILGRABBER_HTML_OUTPUT_PATH="$DATA_DIR/output/cluster-viewer.html"

if [ -z "${WAYLAND_DISPLAY:-}" ] && [ -z "${DISPLAY:-}" ]; then
    echo "No display server detected (WAYLAND_DISPLAY/DISPLAY are not set)." >&2
    echo "Start MailGrabber from a graphical session." >&2
    exit 1
fi

if [ -n "${WAYLAND_DISPLAY:-}" ] && [ -z "${DISPLAY:-}" ]; then
    # KDE/Wayland with XWayland often uses :0 but does not always propagate DISPLAY into Flatpak.
    export DISPLAY=":0"

    if [ -z "${DISPLAY:-}" ]; then
        echo "Wayland session detected, but DISPLAY is not set (XWayland unavailable)." >&2
        echo "Current Avalonia Linux backend requires X11/XWayland." >&2
        echo "Try: flatpak run --env=DISPLAY=:0 io.github.thomasn.MailGrabber" >&2
        exit 1
    fi
fi

cd "$CONFIG_DIR"
exec /app/lib/mailgrabber/MailGrabber --config "$CONFIG_DIR/appsettings.json" "$@"
