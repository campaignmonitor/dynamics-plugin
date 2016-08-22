properties {
    $projectDir = Resolve-Path .
    $slnFile = "$projectDir\Campmon.Dynamics.sln"
    $webResourceProject = 'Campmon.Dynamics.WebResources'
    $pluginProject = 'Campmon.Dynamics.Plugins'
    $workflowProject = 'Campmon.Dynamics.WorkflowActivities'
    $connectionStrings = @{
        "dev" = "Url=https://campmondev0.crm.dynamics.com/;UserId=admin@campmondev0.onmicrosoft.com;Password=pass@word1;AuthType=Office365";
        "test" = "Url=https://campmontest1.crm.dynamics.com/;UserId=admin@campmontest1.onmicrosoft.com;Password=pass@word1;AuthType=Office365"
    }
    $solutionName = 'CampaignMonitor'
    $solutionVersionNumber = $versionNumber
    $outputDir = $outputPath
}

task RestoreNuGet {
    &"$projectDir\.nuget\nuget.exe" restore "$slnFile"
}

task Compile -depends RestoreNuGet {
    $params = '/target:Rebuild /property:Configuration=Release /verbosity:minimal'
    
    $buildResult = Invoke-MsBuild $slnFile -MsBuildParameters $params -ShowBuildOutputInCurrentWindow
    
    "Compile succeeded: $($buildResult.buildSucceeded)"
}

task UpdateVersion -precondition { $solutionVersionNumber } {
    $org = Get-Crmconnection -ConnectionString $connectionStrings.dev
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
    $org = Get-Crmconnection -ConnectionString $connectionStrings.dev
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

task DeployToTest {
    $org = Get-CrmConnection -ConnectionString $connectionStrings.test

    $managedFileName = "$solutionName-Managed-$solutionVersionNumber.zip"
    $filePath = Join-Path $outputDir $managedFileName
    Import-CrmSolution -conn $org -SolutionFilePath $filePath
}

task Default -depends Compile
task Create -depends ExportSolutions, DeployToTest