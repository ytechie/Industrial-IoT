<#
 .SYNOPSIS
    Installs Industrial IoT edge on windows

 .DESCRIPTION
    Installs Industrial IoT edge on windows and finishes installation.

 .PARAMETER dpsConnString
    The Dps connection string

 .PARAMETER idScope
    The Dps id scope

 .PARAMETER Install
    Whether to install

 .PARAMETER Linux
    Running on linux
#>
param(
    [string] $dpsConnString,
    [string] $idScope,
    [switch] $Install,
    [switch] $Linux
)

return $null

$path = $script:MyInvocation.MyCommand.Path

if ([string]::IsNullOrEmpty($dpsConnString)) {
    Write-Host "Nothing to do."
}
else {
    if ($Install.IsPresent) {
        Write-Host "Create new IoT Edge enrollment."
        $enrollment = & (join-path $path vm-enroll.ps1) -dpsConnString $dpsConnString
        Write-Host "Configure and initialize IoT Edge."
        if ($Linux.IsPresent) {
            # configure config.yaml
            $file = "/etc/iotedge/config.yaml"
            $configyml = Get-Content $file -Raw

            # comment out existing 
            $configyml.Replace('`nprovisioning:', '`n#provisioning:')
            $configyml.Replace('`n  source:', '`n#  source:')
            $configyml.Replace('`n  device_connection_string:', '`n#  device_connection_string:')
            $configyml.Replace('`n  dynamic_reprovisioning:', '`n#  dynamic_reprovisioning:')

            # add dps setting
            $configyml += '`n'
            $configyml += '`n # DPS symmetric key provisioning configuration - added'
            $configyml += '`nprovisioning:'
            $configyml += '`n   source: "dps"'
            $configyml += '`n   global_endpoint: "https://global.azure-devices-provisioning.net"'
            $configyml += '`n   scope_id: "$($idScope)"'
            $configyml += '`n   attestation:'
            $configyml += '`n      method: "symmetric_key"'
            $configyml += '`n      registration_id: "$($enrollment.registrationId)"'
            $configyml += '`n      symmetric_key: "$($enrollment.primaryKey)"'
            $configyml += '`n'

            $configyml | Out-File $file -Force
            Write-Host "Restart edge with new configuration."
            & systemctl @("restart", "iotedge")

            # todo: Test edge
        }
        else {
            . { Invoke-WebRequest -useb https://aka.ms/iotedge-win } | Invoke-Expression; `
                Initialize-IoTEdge -Dps -ScopeId $idScope -RegistrationId `
                    $enrollment.registrationId -SymmetricKey $enrollment.primaryKey
        
            # todo: Test edge
        }
    }
    else {
        Write-Host "Deploying IoT Edge to machine."

        . { Invoke-WebRequest -useb https://aka.ms/iotedge-win } | Invoke-Expression; `
            Deploy-IoTEdge -RestartIfNeeded

        # Register ourselves to initialize edge        
        $trigger = New-JobTrigger -AtStartup -RandomDelay 00:00:30
        $out = Register-ScheduledJob -Trigger $trigger –Name "IotEdge" -FilePath $path -ArgumentList `
            @("-dpsConnString", $script:dpsConnString, "-idScope", $script:idScope, "-Install")
        Write-Host $out.Command

        Write-Host "Restart to finish installation."
        Restart-Computer
    }
}