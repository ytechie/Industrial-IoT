#!/bin/bash -e

while [ "$#" -gt 0 ]; do
    case "$1" in
        --dpsConnString)                  dpsConnString="$2" ;;
        --idScope)                        idScope="$2" ;;
    esac
    shift
done

# install powershell and call the setup command
sudo apt-get install -y powershell
sudo powershell ./vm-setup.ps1 -dpsConnString $dpsConnString -idScope $idScope -Install -Linux
sudo systemctl restart iotedge
