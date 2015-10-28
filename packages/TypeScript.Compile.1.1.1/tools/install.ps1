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

$target = $buildProject.Xml.AddTarget("TypeScript")
$target.AfterTargets = "Afterbuild"

$task = $target.AddTask("Exec")
$task.SetParameter("Command", "`"`$(PROGRAMFILES)\Microsoft SDKs\TypeScript\tsc`" --sourcemap  --target ES5  @(TypeScriptCompile ->'`"%(fullpath)`"' , ' ')")
$task.Condition = "'@(TypeScriptCompile)'!=''";

$project.Save()