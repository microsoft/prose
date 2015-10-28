
function Get-MSBuildProject {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string[]]$ProjectName
    )
    Process {
        (Resolve-ProjectName $ProjectName) | % {
            $path = $_.FullName
            @([Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($path))[0]
        }
    }
}

function Resolve-ProjectName {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string[]]$ProjectName
    )
    
    if($ProjectName) {
        $projects = Get-Project $ProjectName
    }
    else {
        # All projects by default
        $projects = Get-Project
    }
    
    $projects
}




$project = Get-Project
$buildProject = Get-MSBuildProject

#remove typescript targets
$projectRoot = $buildProject.Xml;
Foreach ($target in $projectRoot.Targets)
{
    If ($target.Name -eq "TypeScript")
    {
        $projectRoot.RemoveChild($target);
    }
}


$project.Save() #persists the changes