#!/bin/bash -e

while [ "$#" -gt 0 ]; do
    case "$1" in
        --dpsConnString)                  dpsConnString="$2" ;;
        --idScope)                        idScope="$2" ;;
    esac
    shift
done

echo "Prepare machine..."
# install powershell and call the setup command
sudo apt-get update
sudo apt-get install -y --no-install-recommends powershell

# wait until config.yaml is available
configFile=/etc/iotedge/config.yaml
until [ -f $configFile ]
do
    echo "Installing iotedge runtime..."
    sleep 5
done
echo "Provisioning iotedge..."
sudo pwsh -File ./vm-setup.ps1 -dpsConnString $dpsConnString -idScope $idScope

echo "Restarting iotedge runtime!"
sudo systemctl restart iotedge
