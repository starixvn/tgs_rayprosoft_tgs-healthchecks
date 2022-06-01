@Library('global-shared-library') _
def message = "";

pipeline {
    agent any
    environment {
        BuildConfiguration='Debug'
        DOTNET_USE_POLLING_FILE_WATCHER="true"
    }
    stages {
        stage('Prepare') {
            steps {
                sh "echo 'Branch: $GIT_BRANCH'";
                script {
                    switch(env.GIT_BRANCH)
                    {
                        case ['origin/main','main']:
                            env.kube_ns = "prod";
                            env.k8s_config = 'stag-k8s-config';
                            break;
                        case ['origin/release','release']:
                            env.kube_ns = "staging";
                            env.k8s_config = 'stag-k8s-config';
                            break;
                        default:
                            env.kube_ns = "dev";
                            env.k8s_config = 'k8s-kube-config';
                            break;
                    }
                }
                configFileProvider([
                    configFile(fileId: 'tgs.' + env.kube_ns , variable: 'tgsconfig')]) {
                        script {
                            def envItems = readProperties file: "$tgsconfig";
                            envItems.each { key, value -> env."tgs_${key}" = value;
                        }
                        env.TAG = sh(script: "echo `date +%Y%m%d%H%M`", returnStdout: true).trim() + '-' + env.kube_ns;
                        message += "\nBranch: ${env.GIT_BRANCH}"; 
                        message += "\nTag: ${env.TAG}";
                        message += "\nEnv: ${env.kube_ns}";
                        env.commit_url = "${env.GIT_URL}"
                            .replace("git@gitlab.com:", "https://gitlab.com/")
                            .replace(".git","/-/commit/") + "${env.GIT_COMMIT}";
                        message += "\n Commit: ${env.commit_url}";
                    }
                }
                sh "echo tag: $TAG"
            }
        }
        stage('Build') {
            steps {
                sh 'dotnet build -c $BuildConfiguration'
            }
        }
		
		stage('Push packages') {
			steps {
				sh 'dotnet pack -c $BuildConfiguration "Tgs.HealthChecks/Tgs.HealthChecks.csproj" --no-build'
				sh 'dotnet nuget push "Tgs.HealthChecks/bin/$BuildConfiguration/*.nupkg" -s "raypro-baget" -k admin --skip-duplicate'
			}
		}
    }

    post {
        success {
             sendTelegram("Pipeline: ${currentBuild.fullDisplayName} OK! ${message} \n${currentBuild.getBuildCauses()[0].shortDescription}\n")
        }
        failure {
            sendTelegram("<b>Failed Pipeline:</b> ${currentBuild.fullDisplayName} Something is wrong with ${env.BUILD_URL}/console\n${currentBuild.getBuildCauses()[0].shortDescription}\n")
        }
    }
}
