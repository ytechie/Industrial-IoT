<#
 .SYNOPSIS
    Installs Industrial IoT edge on windows

 .DESCRIPTION
    Installs Industrial IoT edge on windows and finishes installation.

 .PARAMETER dpsConnString
    The Dps connection string
#>
param(
    [string] $dpsConnString,
    [string] $idScope,
    [switch] $Install,
    [switch] $Linux
)

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
            # configure config.yml
$configyml = @"
provisioning:
   source: "dps"
   global_endpoint: "https://global.azure-devices-provisioning.net"
   scope_id: "$($idScope)"
   attestation:
      method: "symmetric_key"
      registration_id: "$($enrollment.registrationId)"
      symmetric_key: "$($enrollment.primaryKey)"
"@
            $configyml | Out-File /etc/iotedge/config.yml
        }
        else {
            . { Invoke-WebRequest -useb https://aka.ms/iotedge-win } | Invoke-Expression; `
                Initialize-IoTEdge -Dps -ScopeId $idScope -RegistrationId `
                    $enrollment.registrationId -SymmetricKey $enrollment.primaryKey
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