// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.ArchivalSettings
import jobs.generation.Utilities

def project = GithubProject
def branchName = GithubBranchName

// Generate jobs to build the latest head, and PRs
[true, false].each { isPr ->
    // Build both debug and release
    ['Debug', 'Release'].each { configuration ->
        def newJobName = Utilities.getFullJobName(project, "windows_${configuration.toLowerCase()}", isPr)
        def newJob = job(newJobName) {
            steps {
                batchFile("""
SET VS150COMNTOOLS=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\Common7\\Tools\\
SET VSSDK150Install=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\
SET VSSDKInstall=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\

build.cmd /no-deploy-extension /${configuration.toLowerCase()} /no-node-reuse /trx-test-results
""")
            }
        }

        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles("bin/**/*")
        archiveSettings.excludeFiles("bin/obj/*")
        archiveSettings.setFailIfNothingArchived()
        archiveSettings.setArchiveOnFailure()
        Utilities.addArchival(newJob, archiveSettings)
        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto-dev15-0')
        Utilities.addMSTestResults(newJob, "TestResults/*.trx")
        Utilities.standardJobSetup(newJob, project, isPr, "*/$branchName")
        if (isPr) {
            Utilities.addGithubPRTriggerForBranch(newJob, branchName, "Windows ${configuration}")
        } else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}
