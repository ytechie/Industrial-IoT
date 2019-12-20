#!/bin/bash -e

while [ "$#" -gt 0 ]; do
    case "$1" in
        --dpsConnString)                  dpsConnString="$2" ;;
        --idScope)                        idScope="$2" ;;
    esac
    shift
done

curdir="$( cd "$(dirname "$0")" ; pwd -P )"

function install() {
    echo "In $curdir..."
    echo "Prepare machine..."
    # install powershell and call the setup command
    sudo apt-get update
    sudo apt-get install -y --no-install-recommends powershell

    # wait until config.yaml is available
    configFile=/etc/iotedge/config.yaml
    until [ -f $configFile ]
    do
        echo "Waiting until iotedge runtime is installed..."
        sleep 5
    done
    echo "Iotedge installed."
    
    echo "Provisioning iotedge..."
    sudo pwsh -File $curdir/vm-setup.ps1 -dpsConnString $dpsConnString -idScope $idScope
    echo "Iotedge provisioned."

    echo "Restarting iotedge runtime..."
    sudo systemctl restart iotedge
    echo "Iotedge running."
}

install | tee /etc/iotedge/install.log
