#!/bin/bash

PLUGIN_NAME="xgmobile-manager"
REPO="tanayabhay-creator/xg-mobile-manager-internal"
ARCHIVE_URL="https://github.com/${REPO}/archive/refs/heads/main.tar.gz"
PLUGIN_DIR="$HOME/homebrew/plugins"
TMP_DIR=$(mktemp -d)

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

echo "Installing XG Mobile Manager..."

echo "Stopping Decky Plugin Loader... (You may be prompted for your sudo password)"
sudo systemctl stop plugin_loader < /dev/tty

echo "Downloading $ARCHIVE_URL"
curl -fsSL "$ARCHIVE_URL" -o "$TMP_DIR/plugin.tar.gz"
tar -xzf "$TMP_DIR/plugin.tar.gz" -C "$TMP_DIR"
SRC=$(find "$TMP_DIR" -mindepth 1 -maxdepth 1 -type d | head -n 1)

if [ ! -f "$SRC/main.py" ] || [ ! -f "$SRC/dist/index.js" ]; then
  echo "ERROR: Download did not contain the plugin files."
  exit 1
fi

sudo mkdir -p "$PLUGIN_DIR"
sudo rm -rf "$PLUGIN_DIR/$PLUGIN_NAME"
sudo mkdir -p "$PLUGIN_DIR/$PLUGIN_NAME"
sudo cp -a "$SRC/dist" "$SRC/bin" "$SRC/assets" "$SRC/main.py" "$SRC/plugin.json" "$SRC/package.json" "$SRC/decky.pyi" "$PLUGIN_DIR/$PLUGIN_NAME/"
sudo chmod +x "$PLUGIN_DIR/$PLUGIN_NAME/bin/"*

echo "Restarting Decky Plugin Loader..."
sudo systemctl start plugin_loader < /dev/tty

echo "Installation complete. Return to Game Mode, turn on Play on Internal Screen, then press Enable XG Mobile."
