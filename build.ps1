param($task = 'default')

Get-Module psake | Remove-Module
Get-Module Invoke-MsBuild | Remove-Module
Get-Module Microsoft.Xrm.Data.Powershell | Remove-Module

Import-Module @(
    '.\Build\psake\psake.psm1'
    '.\Build\Invoke-MsBuild.psm1'
    '.\Build\Microsoft.Xrm.Data.Powershell\Microsoft.Xrm.Data.Powershell.psd1'
)

$psake.use_exit_on_error = $true
Invoke-Psake .\default.ps1 $task