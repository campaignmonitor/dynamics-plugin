properties {
    $projectDir = Resolve-Path .
    $slnFile = "$projectDir\Campmon.Dynamics.sln"
    $webResourceProject = 'Campmon.Dynamics.WebResources'
    $pluginProject = 'Campmon.Dynamics.Plugins'
    $workflowProject = 'Campmon.Dynamics.WorkflowActivities'
    $connectionString = "Url=https://msp674563.crm.dynamics.com/;UserId=admin@MSP674563.onmicrosoft.com;Password=pass@word1;AuthType=Office365"
    $solutionName = 'CampaignMonitor'
    $solutionVersionNumber = $versionNumber
    $outputDir = $outputPath
}

task Default -depends Compile

task RestoreNuGet {
    &"$projectDir\.nuget\nuget.exe" restore "$slnFile"
}

task Compile -depends RestoreNuGet {
    $params = '/target:Rebuild /property:Configuration=Release /verbosity:minimal'
    Invoke-MsBuild $slnFile -MsBuildParameters $params -ShowBuildOutputInCurrentWindow > $null
}

task UpdateVersion -precondition { $solutionVersionNumber } {
    $org = Get-Crmconnection -ConnectionString $connectionString
    if ($org.IsReady) { "Connected successfully" } else { throw "Unable to connect" }

    $results = Get-CrmRecords -conn $org -EntityLogicalName solution -FilterAttribute uniquename -FilterOperator like -FilterValue $solutionName -Fields version

    if ($results.CrmRecords.Count -ne 1) {
        throw "Could not find solution with name $solutionName."
    }

    $updatedSolution = $results.CrmRecords[0]
    $updatedSolution.version = $solutionVersionNumber
    Set-CrmRecord -conn $org -CrmRecord $updatedSolution
    "Updated solution to version $solutionVersionNumber"
}

task ExportSolutions -depends Compile,UpdateVersion {
    $org = Get-Crmconnection -ConnectionString $connectionString
    if ($org.IsReady) { "Connected successfully" } else { throw "Unable to connect" }

    if (-not (Test-Path $outputDir)) {
        New-Item $outputDir -type Directory > $null
    }

    Publish-CrmAllCustomization -conn $org

    $unmanagedFileName = "$solutionName-Unmanaged-$solutionVersionNumber.zip"
    Export-CrmSolution -conn $org -SolutionName $solutionName -SolutionFilePath $outputDir -SolutionZipFileName $unmanagedFileName -TargetVersion 8.0

    $managedFileName = "$solutionName-Managed-$solutionVersionNumber.zip"
    Export-CrmSolution -conn $org -SolutionName $solutionName -SolutionFilePath $outputDir -SolutionZipFileName $managedFileName -TargetVersion 8.0 -Managed

}

task Create -depends ExportSolutions