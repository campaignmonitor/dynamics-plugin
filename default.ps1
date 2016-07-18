properties {
    $projectDir = Resolve-Path .
    $slnFile = "$projectDir\Campmon.Dynamics.sln"
    $webResourceProject = 'Campmon.Dynamics.WebResources'
    $pluginProject = 'Campmon.Dynamics.Plugins'
    $workflowProject = 'Campmon.Dynamics.WorkflowActivities'
    $connectionString = "Url=https://msp472630.crm.dynamics.com/;UserId=admin@MSP472630.onmicrosoft.com;Password=pass@word1;AuthType=Office365"
    $solutionName = 'Campaign Monitor'
}

task default -depends Compile

task RestoreNuGet {
    &"$projectDir\.nuget\nuget.exe" restore "$slnFile"
}

task Compile -depends RestoreNuGet {
    $params = '/target:Rebuild /property:Configuration=Release /verbosity:minimal'
    Invoke-MsBuild $slnFile -MsBuildParameters $params -ShowBuildOutputInCurrentWindow > $null
}