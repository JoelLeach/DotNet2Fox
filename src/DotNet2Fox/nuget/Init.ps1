param($installPath, $toolsPath, $package)

# Check to see if FoxCOM registered
# Some script taken from https://powershelladministrator.com/2019/12/17/register-dll-or-ocx-files-check-result/
# Also see https://www.jonathanmedd.net/2014/02/testing-for-the-presence-of-a-registry-key-and-value.html
# Create a new PSDrive, as powershell doesn't have a default drive for HKEY_CLASSES_ROOT
New-PSDrive -Name HKCR -PSProvider Registry -Root HKEY_CLASSES_ROOT | Out-Null
If(Test-Path "HKCR:\FoxCOM.Application")
{
    Write-Host "DotNet2Fox FoxCOM.exe is already registered."
}
Else 
{
    Write-Host "Registering DotNet2Fox FoxCOM.exe..."
    $foxCOMPath = $installPath + "\content\FoxCOM.exe"
    # Calling FoxCOM /regserver with elevated Start-Process did not work, so put in separate batch file
    Start-Process $toolsPath/RegisterFoxCOM.cmd -ArgumentList $foxCOMPath -Verb RunAs 
}
# Remove the PSDrive that was created
Remove-PSDrive -Name HKCR