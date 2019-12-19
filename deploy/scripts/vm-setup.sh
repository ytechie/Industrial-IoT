#!/bin/bash -e

while [ "$#" -gt 0 ]; do
    case "$1" in
        --dpsConnString)                  dpsConnString="$2" ;;
        --idScope)                        idScope="$2" ;;
    esac
    shift
done

echo "Installing powershell"
# install powershell and call the setup command
apt-get update
apt-get install -y --no-install-recommends powershell

echo "Installing iotedge"
pwsh -File ./vm-setup.ps1 -dpsConnString $dpsConnString -idScope $idScope

echo "Restarting"
systemctl restart iotedge
