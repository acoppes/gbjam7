GB_GAME_NAME="SeedcityChasers"
export GB_GAME_BUILD_LOG_PATH="builds/macos.txt"

BUILD_COMMAND="${GB_UNITY_EDITOR_PATH} -projectPath . -buildOSXUniversalPlayer builds/macos/${GB_GAME_NAME}.app -logFile ${GB_GAME_BUILD_LOG_PATH} -buildTarget OSXUniversal -forgetProjectPath -quit -silent-crashes -batchmode -nographics"

echo ${BUILD_COMMAND}
${BUILD_COMMAND}