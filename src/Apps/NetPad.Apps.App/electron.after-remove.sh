#!/bin/bash

# Removes the "netpad" command (/usr/bin/netpad) when the rpm is uninstalled.
# Used instead of electron-builder's own script, which gives update-alternatives
# the wrong path and makes the uninstall report an error.

# rpm also runs this script during an upgrade, after the new version is installed.
# NetPad is still there in that case, so the command must stay.
if [ ! -e '/opt/${sanitizedProductName}/${executable}' ]; then
    if type update-alternatives >/dev/null 2>&1; then
        update-alternatives --remove '${executable}' '/opt/${sanitizedProductName}/${executable}' || true
    fi

    # Delete the command if it is still there and now points to nothing.
    if [ -L '/usr/bin/${executable}' ] && [ ! -e '/usr/bin/${executable}' ]; then
        rm -f '/usr/bin/${executable}'
    fi
fi
