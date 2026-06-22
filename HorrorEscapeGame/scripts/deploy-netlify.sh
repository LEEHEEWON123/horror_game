#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PUBLISH_DIR="$ROOT/Builds/WebGL"

if command -v netlify >/dev/null 2>&1; then
  NETLIFY=(netlify)
elif command -v npx >/dev/null 2>&1; then
  NETLIFY=(npx netlify-cli)
else
  echo "Install Netlify CLI once:"
  echo "  npm install -g netlify-cli"
  exit 1
fi

if [[ ! -f "$PUBLISH_DIR/index.html" ]]; then
  echo "Missing WebGL build. Run in Unity first:"
  echo "  Tools → Build WebGL for Netlify"
  exit 1
fi

cd "$PUBLISH_DIR"

if [[ ! -d ".netlify" ]]; then
  echo "Link this folder to your Netlify site (one time):"
  echo "  netlify login"
  echo "  netlify link"
  exit 1
fi

echo "Deploying $PUBLISH_DIR ..."
"${NETLIFY[@]}" deploy --prod --dir="."
