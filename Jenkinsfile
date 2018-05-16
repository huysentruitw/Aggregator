/*
Required Jenkins plugins:
  * Pipeline Utility Steps (for findFiles)
  * File Operations (for folderDeleteOperation, folderCreateOperation)
*/

pipeline {
  agent {
    label 'dotnet'
  }

  stages {
    stage('Build') {
      steps {
        bat 'dotnet build --no-incremental -c Release /p:DebugType=Full'
      }
    }

    stage('UnitTest') {
      steps {
        script {
          // Requires
          testAssemblies = findFiles(glob: 'tests/**/bin/Release/**/*.Tests.dll').collect({ x -> return x.path }).join(' ')
          println "Found test assemblies: ${testAssemblies}"
        }

        folderDeleteOperation('_testing')
        folderCreateOperation('_testing')

        bat 'nuget install OpenCover -Version 4.6.519 -OutputDirectory .\\_testing'
        bat ".\\_testing\\OpenCover.4.6.519\\tools\\OpenCover.Console.exe -register:user -target:dotnet.exe -targetargs:\"vstest --logger:nunit;LogFilePath=.\\_testing\\NUnit.Result.xml ${testAssemblies}\" -output:.\\_testing\\OpenCover.Result.xml -filter:\"+[*]* -[*.Tests*]*\" -excludebyattribute:*.ExcludeFromCodeCoverage*^ -hideskipped:Filter,Attribute^ -oldStyle"
      }
    }

    stage('TransformUnitTestResults') {
      steps {
        bat 'nuget install OpenCoverToCoberturaConverter -Version 0.3.2 -OutputDirectory .\\_testing'
        bat '.\\_testing\\OpenCoverToCoberturaConverter.0.3.2\\tools\\OpenCoverToCoberturaConverter.exe "-input:.\\_testing\\OpenCover.Result.xml" "-output:.\\_testing\\Cobertura.Result.xml" "-sources:%cd%"'
      }
    }
  }

  post {
    success {
      nunit testResultsPattern: '_testing\\NUnit.Result.xml'
      cobertura autoUpdateHealth: false, autoUpdateStability: false, coberturaReportFile: '_testing\\Cobertura.Result.xml', conditionalCoverageTargets: '70, 0, 0', failUnhealthy: false, failUnstable: false, lineCoverageTargets: '80, 0, 0', maxNumberOfBuilds: 0, methodCoverageTargets: '80, 0, 0', onlyStable: false, sourceEncoding: 'ASCII', zoomCoverageChart: false
    }
  }
}
