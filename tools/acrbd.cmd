@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

if not "%1" == "" goto :build
echo Must specify name of deployment.

:build
pushd %build_root%\tools\scripts
powershell ./acr-matrix.ps1 -Subscription IOT_GERMANY -Registry industrialiotdev -Fast -Build
popd
if !ERRORLEVEL! == 0 goto :deploy
echo Build failed.

:deploy
pushd %build_root%\deploy\scripts
powershell ./deploy.ps1 -type app -acrSubscriptionName IOT_GERMANY -acrRegistryName industrialiotdev -subscriptionName IOT-OPC-WALLS -aadApplicationName iiot -resourceGroupName %1 -applicationName %1 -resourceGroupLocation westeurope
popd
if !ERRORLEVEL! == 0 goto :eof
echo Deploy failed.
goto :eof
